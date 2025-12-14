using UnityEngine;
using System.IO.Ports;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController_Arduino : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5.0f;
    public float rotationSpeed = 200.0f;

    //[Header("Jump")]
    //public float jumpForce = 5.0f;

    [Header("Auto Righting")]
    public float tiltThreshold = 40f;
    public float restoreSpeed = 6f;
    public float maxCorrectionPerSecond = 180f;
    public float flipDotThreshold = -0.1f;
    public bool onlyWhenAirborne = false;

    [Header("Physics Tuning")]
    public float centerOfMassYOffset = -0.4f;
    public float angularDragValue = 1.5f;
    public float linearDragValue = 0.5f;
    public float extraGravityForce = 120f;

    [Header("Shift Boost")]
    public float boostMultiplier = 1.1f;
    public float boostDuration = 3f;

    [Header("Brake Settings")]
    public float brakeDeceleration = 1f;

    [Header("Arduino")]
    public string portName = "COM3";
    public int baudRate = 115200;
    public float deadzone = 0.02f;
    public bool useNonLinearCurve = true;
    [Range(0f, 1f)] public float steeringCurveFactor = 0.7f;
    public float portRetryInterval = 2f;

    [Header("Speed Display Scaling")]
    public float unityUnitsToMeters = 0.1f; // real toy car scale
    public float speedSmoothFactor = 5f;
    public float displaySpeedMultiplier = 12f; // scale for full-size car UI

    [Header("Collectibles")]
    public int collectibles = 0;
    public TextMeshProUGUI countText;
    public int totalCollectibles = 32;

    [Header("Speed UI")]
    public TextMeshProUGUI speedText;
    public float speedPopThreshold = 5f;
    public float speedPopScale = 1.5f;
    public float speedPopDuration = 0.2f;
    public Color speedPopColor = Color.cyan;

    [Header("Winner Text")]
    public TextMeshProUGUI winnerText;
    public float winnerPopScale = 2f;
    public float winnerPopDuration = 0.3f;
    public Color winnerColor = Color.green;

    [Header("Game Over Text")]
    public TextMeshProUGUI gameOverText;
    public float gameOverPopScale = 2f;
    public float gameOverPopDuration = 0.3f;
    public Color gameOverColor = Color.red;

    [Header("Fade")]
    public Image fadeImage;
    public float fadeDuration = 1f;

    [Header("Collectibles UI Animation")]
    public float popScale = 1.5f;
    public float popDuration = 0.2f;
    public Color popColor = Color.yellow;

    // -------------------------------------------------------
    // Private fields
    // -------------------------------------------------------
    private Rigidbody rb;
    private SerialPort port;
    private bool portOpen = false;
    private float steeringValue = 0f;
    private float displayedSpeed = 0f;
    private float lastDisplayedSpeed = 0f;
    private float lastPortAttemptTime = 0f;

    private bool btnForward = false;
    private bool btnBackward = false;
    private bool btnBrake = false;

    private int lastCollectibleCount = 0;
    private bool winnerShown = false;
    private bool gameOverShown = false;

    private Vector3 countTextOriginalScale;
    private Vector3 speedTextOriginalScale;
    private Vector3 winnerTextOriginalScale;
    private Vector3 gameOverTextOriginalScale;
    private Vector3 lastFixedPosition;

    private float boostTimer = 0f;
    private float speedMultiplier = 1f;

    private float lastRawSteering = 0f;
    public float steeringSensitivity = 3.0f;
    public float steeringDecay = 2.0f;
    public float edgeThreshold = 0.95f;
    public float edgeSmoothSpeed = 5f;
    private float smoothedSteering = 0f;

    private bool speedPopRunning = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, centerOfMassYOffset, 0f);
        rb.angularDamping = angularDragValue;
        rb.linearDamping = linearDragValue;
        rb.mass = 1500f;

        lastFixedPosition = rb.position;

        displayedSpeed = 0f;
        lastDisplayedSpeed = 0f;

        OpenPort();

        if (countText != null) countText.text = $"Collectibles: {collectibles} / {totalCollectibles}";
        if (winnerText != null) winnerText.gameObject.SetActive(false);
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);

        if (countText != null) countTextOriginalScale = countText.transform.localScale;
        if (speedText != null) speedTextOriginalScale = speedText.transform.localScale;
        if (winnerText != null) winnerTextOriginalScale = winnerText.transform.localScale;
        if (gameOverText != null) gameOverTextOriginalScale = gameOverText.transform.localScale;
    }

    void Update()
    {
        if (!portOpen && Time.time - lastPortAttemptTime > portRetryInterval)
        {
            OpenPort();
            lastPortAttemptTime = Time.time;
        }

        ReadArduino();

        //if (Input.GetButtonDown("Jump"))
        //    rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

        if (countText != null && collectibles > lastCollectibleCount)
        {
            countText.text = $"Collectibles: {collectibles} / {totalCollectibles}";
            lastCollectibleCount = collectibles;

            if (collectibles > 0)
                StartCoroutine(PopText(countText, popScale, popDuration, popColor));
        }

        if (!winnerShown && collectibles >= totalCollectibles)
        {
            winnerShown = true;
            if (winnerText != null)
            {
                winnerText.gameObject.SetActive(true);
                StartCoroutine(PopText(winnerText, winnerPopScale, winnerPopDuration, winnerColor));
            }
            StartCoroutine(FadeAndRestart(4f));
        }
    }

    void FixedUpdate()
    {
        Vector3 gravityVelocity = Vector3.zero;
        if (!IsGrounded())
            gravityVelocity = Vector3.down * extraGravityForce * Time.fixedDeltaTime;

        HandleMovement(gravityVelocity);
        HandleAutoRight();

        // -----------------------
        // SPEED CALCULATION & UI (scaled for display only)
        // -----------------------
        Vector3 flatVel = new Vector3(rb.position.x - lastFixedPosition.x, 0f, rb.position.z - lastFixedPosition.z) / Time.fixedDeltaTime;
        float actualSpeedMps = flatVel.magnitude * unityUnitsToMeters; // toy car speed

        // Scale speed for full-size car display, with smoothing curve
        float targetDisplaySpeed = actualSpeedMps * 2.23694f * displaySpeedMultiplier;

        // Smoothly interpolate displayed speed
        displayedSpeed = Mathf.Lerp(displayedSpeed, targetDisplaySpeed, Time.fixedDeltaTime * speedSmoothFactor);

        // Update UI
        if (speedText != null)
        {
            speedText.text = $"Speed: {displayedSpeed:F0} mph";

            if (!speedPopRunning && Mathf.Abs(displayedSpeed - lastDisplayedSpeed) > speedPopThreshold)
            {
                StartCoroutine(PopText(speedText, speedPopScale, speedPopDuration, speedPopColor, () => speedPopRunning = false));
                speedPopRunning = true;
            }

            lastDisplayedSpeed = displayedSpeed;
        }


        lastFixedPosition = rb.position;

        if (portOpen && port != null && port.IsOpen)
        {
            string msg = displayedSpeed.ToString("F1") + "," + collectibles.ToString();
            port.WriteLine(msg);
        }
    }

    void HandleMovement(Vector3 extraVelocity = default)
    {
        float keyboardVertical = Input.GetAxis("Vertical");
        float moveVertical = keyboardVertical;
        if (btnForward) moveVertical = 1f;
        if (btnBackward) moveVertical = -1f;

        float turnInput = portOpen ? steeringValue : Input.GetAxis("Horizontal");

        if (useNonLinearCurve)
        {
            float t = turnInput;
            float a = steeringCurveFactor;
            turnInput = t * (a + (1f - a) * t * t);
        }

        float appliedSpeed = speed;

        if (Input.GetKey(KeyCode.LeftShift) && boostTimer < boostDuration)
        {
            appliedSpeed *= boostMultiplier;
            boostTimer += Time.fixedDeltaTime;
        }
        else if (!Input.GetKey(KeyCode.LeftShift))
        {
            boostTimer = 0f;
        }

        if (btnBrake || Input.GetKey(KeyCode.C))
        {
            speedMultiplier -= brakeDeceleration * Time.fixedDeltaTime;
            speedMultiplier = Mathf.Clamp(speedMultiplier, 0f, 1f);
        }
        else
        {
            speedMultiplier = 1f;
        }

        appliedSpeed *= speedMultiplier;

        Vector3 movement = transform.forward * moveVertical * appliedSpeed * Time.fixedDeltaTime + extraVelocity;

        rb.MovePosition(rb.position + movement);

        float turnAmount = turnInput * rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnAmount, 0f));
    }

    private void HandleAutoRight()
    {
        Vector3 up = transform.up;
        float tiltAngle = Vector3.Angle(up, Vector3.up);
        float upDot = Vector3.Dot(up, Vector3.up);
        bool isFlipped = upDot < flipDotThreshold;
        bool isTooTilted = tiltAngle > tiltThreshold;
        if (!isFlipped && !isTooTilted) return;
        if (onlyWhenAirborne && IsGrounded()) return;

        float yaw = rb.rotation.eulerAngles.y;
        Quaternion target = Quaternion.Euler(0f, yaw, 0f);
        float step = restoreSpeed * Time.fixedDeltaTime;
        Quaternion slerped = Quaternion.Slerp(rb.rotation, target, step);
        float maxStep = maxCorrectionPerSecond * Time.fixedDeltaTime;
        Quaternion finalRot = Quaternion.RotateTowards(rb.rotation, slerped, maxStep);
        rb.MoveRotation(finalRot);

        Vector3 localAngVel = transform.InverseTransformDirection(rb.angularVelocity);
        localAngVel.x *= 0.2f;
        localAngVel.z *= 0.2f;
        rb.angularVelocity = transform.TransformDirection(localAngVel);
    }

    private bool IsGrounded()
    {
        float rayLength = 1.2f;
        Vector3[] offsets = {
            Vector3.zero,
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(-0.5f, 0, 0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(-0.5f, 0, -0.5f)
        };
        foreach (var offset in offsets)
        {
            if (Physics.Raycast(transform.position + offset, Vector3.down, rayLength))
                return true;
        }
        return false;
    }

    private Vector3 GetOriginalScale(TextMeshProUGUI text)
    {
        if (text == countText) return countTextOriginalScale;
        if (text == speedText) return speedTextOriginalScale;
        if (text == winnerText) return winnerTextOriginalScale;
        if (text == gameOverText) return gameOverTextOriginalScale;
        return Vector3.one;
    }

    private IEnumerator PopText(TextMeshProUGUI textElement, float scale, float duration, Color color, System.Action onComplete = null)
    {
        if (textElement == null) yield break;
        Vector3 originalScale = GetOriginalScale(textElement);
        Vector3 poppedScale = originalScale * scale;
        Color originalColor = textElement.color;

        textElement.transform.localScale = poppedScale;
        textElement.color = color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            textElement.transform.localScale = Vector3.Lerp(poppedScale, originalScale, t);
            textElement.color = Color.Lerp(color, originalColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        textElement.transform.localScale = originalScale;
        textElement.color = originalColor;

        onComplete?.Invoke();
    }

    private IEnumerator FadeAndRestart(float delay)
    {
        if (fadeImage != null)
        {
            float elapsed = 0f;
            Color startColor = fadeImage.color;
            Color targetColor = new Color(0f, 0f, 0f, 1f);
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeImage.color = Color.Lerp(startColor, targetColor, elapsed / fadeDuration);
                yield return null;
            }
            fadeImage.color = targetColor;
        }
        yield return new WaitForSeconds(Mathf.Max(0f, delay - fadeDuration));
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OpenPort()
    {
        if (portOpen) return;
        try
        {
            port = new SerialPort(portName, baudRate);
            port.ReadTimeout = 50;
            port.Open();
            portOpen = true;
        }
        catch
        {
            portOpen = false;
            Debug.LogWarning("Could not open COM port. Using keyboard fallback.");
        }
    }

    void ReadArduino()
    {
        if (!portOpen) return;

        try
        {
            string line = port.ReadLine().Trim();
            string[] parts = line.Split(',');
            if (parts.Length != 2) return;

            int raw = int.Parse(parts[0]);
            float normalized = Mathf.Clamp01(raw / 1023f);
            float absoluteSteer = Mathf.Lerp(-1f, 1f, normalized);

            float speedThreshold = 0.1f;
            if (rb.linearVelocity.magnitude * unityUnitsToMeters > speedThreshold)
            {
                float target = 0f;
                if (absoluteSteer >= edgeThreshold) target = 1f;
                else if (absoluteSteer <= -edgeThreshold) target = -1f;
                else target = absoluteSteer;

                float smoothSpeed = (Mathf.Abs(target) == 1f) ? edgeSmoothSpeed : steeringDecay;
                smoothedSteering = Mathf.MoveTowards(smoothedSteering, target, smoothSpeed * Time.deltaTime);

                float delta = absoluteSteer - lastRawSteering;
                lastRawSteering = absoluteSteer;

                if (Mathf.Abs(delta) > deadzone)
                    smoothedSteering += delta * steeringSensitivity * Time.deltaTime;

                smoothedSteering = Mathf.Clamp(smoothedSteering, -1f, 1f);
                steeringValue = smoothedSteering;
            }

            int mask = int.Parse(parts[1]);
            btnForward = (mask & 0b00000001) != 0;
            btnBackward = (mask & 0b00000100) != 0;
            btnBrake = (mask & 0b00010000) != 0;
        }
        catch { }
    }

    public void ShowGameOver()
    {
        if (gameOverShown) return;
        gameOverShown = true;
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            StartCoroutine(PopText(gameOverText, gameOverPopScale, gameOverPopDuration, gameOverColor));
        }
        StartCoroutine(FadeAndRestart(4f));
    }

    private void OnApplicationQuit()
    {
        if (port != null && port.IsOpen)
            port.Close();
    }
}
