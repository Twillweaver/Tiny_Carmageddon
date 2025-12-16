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
    public float boostDuration = 3f;
    public float boostCooldown = 10f;
    public float boostAcceleration = 8f;

    [Header("Steering")]
    public float steeringSensitivity = 3.0f;
    public float steeringDecay = 2.0f;
    public float edgeThreshold = 0.95f;
    public float edgeSmoothSpeed = 5f;

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
    public float unityUnitsToMeters = 0.1f;
    public float speedSmoothFactor = 5f;
    public float displaySpeedMultiplier = 12f;

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

    [Header("Boost UI")]
    public TextMeshProUGUI boostStatusText;
    public Color boostReadyColor = Color.green;
    public Color boostActiveColor = Color.cyan;
    public Color boostCooldownColor = Color.red;

    [Header("Boost Camera")]
    public Vector3 normalCameraOffset = new Vector3(0f, 2f, -5f);
    public Vector3 boostCameraOffset = new Vector3(0f, 2f, -7f);
    public float cameraLerpSpeed = 5f;

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

    [Header("Sound Effects")]
    public AudioSource collectibleSFX;
    public AudioSource winSFX;
    public AudioSource boostSFX;
    public AudioSource gameOverSFX;
    public AudioSource engineAudioSource;  // assign in inspector or auto-add
    public AudioClip engineClip;           // looping engine sound
    [Range(0f, 1f)]
    public float engineVolume = 0.7f;


    // Private fields
    //private Rigidbody rb;
    private UnityEngine.Rigidbody rb;
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

    private float speedMultiplier = 1f;
    private float boostTimer = 0f;
    private float boostCooldownTimer = 0f;
    private bool isBoosting = false;

    private Vector3 currentCameraOffset;
    private Transform cameraTransform;
    private Vector3 originalCameraLocalPos;

    private float lastRawSteering = 0f;
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

        if (engineAudioSource == null)
            engineAudioSource = gameObject.AddComponent<AudioSource>();

        engineAudioSource.clip = engineClip;
        engineAudioSource.loop = true;
        engineAudioSource.playOnAwake = false;
        engineAudioSource.spatialBlend = 0f; // 2D
        engineAudioSource.volume = engineVolume;

        if (engineClip != null)
        {
            engineAudioSource.pitch = 0.9f;   // idle pitch
            engineAudioSource.volume = engineVolume * 0.6f; // idle volume
            engineAudioSource.Play();
        }



        // Camera setup
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            originalCameraLocalPos = cameraTransform.localPosition;
        }
        currentCameraOffset = Vector3.zero;

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
        // Retry opening COM port
        if (!portOpen && Time.time - lastPortAttemptTime > portRetryInterval)
        {
            OpenPort();
            lastPortAttemptTime = Time.time;
        }

        // Read Arduino input
        ReadArduino();

        bool throttleInput =
        btnForward ||
        btnBackward ||
        Input.GetAxis("Vertical") != 0f ||
        isBoosting;

        if (engineAudioSource != null && engineClip != null)
        {
            float targetPitch;
            float targetVolume;

            if (isBoosting)
            {
                targetPitch = 1.35f;
                targetVolume = engineVolume * 0.9f;
            }
            else if (throttleInput)
            {
                targetPitch = 1.15f;
                targetVolume = engineVolume * 0.7f;
            }
            else
            {
                // Idle
                targetPitch = 0.85f;
                targetVolume = engineVolume * 0.4f;
            }

            engineAudioSource.pitch =
                Mathf.Lerp(engineAudioSource.pitch, targetPitch, Time.deltaTime * 4f);

            engineAudioSource.volume =
                Mathf.Lerp(engineAudioSource.volume, targetVolume, Time.deltaTime * 4f);
        }



        // Collectibles UI update and sound
        if (countText != null && collectibles > lastCollectibleCount)
        {
            countText.text = $"Collectibles: {collectibles} / {totalCollectibles}";
            lastCollectibleCount = collectibles;

            // Pop animation
            if (collectibles > 0)
                StartCoroutine(PopText(countText, popScale, popDuration, popColor));

            // Play collectible sound
            if (collectibleSFX != null)
                collectibleSFX.Play();
        }


        // Winner logic
        if (!winnerShown && collectibles >= totalCollectibles)
        {
            winnerShown = true;

            if (winnerText != null)
            {
                winnerText.gameObject.SetActive(true);
                StartCoroutine(PopText(winnerText, winnerPopScale, winnerPopDuration, winnerColor));
            }

            // Play win sound
            if (winSFX != null)
                winSFX.Play();

            MusicManager.Instance.FadeOut(2f);  // or whatever duration you want


            StartCoroutine(FadeAndRestart(4f));
        }


        // Game Over logic
        if (!gameOverShown)
        {
            float upDot = Vector3.Dot(transform.up, Vector3.up);
            if (upDot < flipDotThreshold)
            {
                ShowGameOver();
            }
        }
    }

    void FixedUpdate()
    {
        Vector3 gravityVelocity = Vector3.zero;
        if (!IsGrounded())
            gravityVelocity = Vector3.down * extraGravityForce * Time.fixedDeltaTime;

        HandleMovement(gravityVelocity);
        HandleAutoRight();

        // Speed calculation
        Vector3 flatVel = new Vector3(rb.position.x - lastFixedPosition.x, 0f, rb.position.z - lastFixedPosition.z) / Time.fixedDeltaTime;
        float actualSpeedMps = flatVel.magnitude * unityUnitsToMeters;

        float targetDisplaySpeed = actualSpeedMps * 2.23694f * displaySpeedMultiplier;
        displayedSpeed = Mathf.Lerp(displayedSpeed, targetDisplaySpeed, Time.fixedDeltaTime * speedSmoothFactor);

        // Speed UI
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

        // Send speed & collectibles to Arduino
        if (portOpen && port != null && port.IsOpen)
        {
            string msg = displayedSpeed.ToString("F1") + "," + collectibles.ToString();
            port.WriteLine(msg);
        }

        // Boost UI
        UpdateBoostUI();

        // Boost camera
        UpdateCameraPosition();
    }

    void HandleMovement(Vector3 extraVelocity = default)
    {
        float moveVertical = Input.GetAxis("Vertical");
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

        // Boost logic
        float boostOffset = 0f;
        if (!isBoosting && boostCooldownTimer <= 0f && Input.GetKey(KeyCode.LeftShift))
        {
            isBoosting = true;
            boostTimer = 0f;

            // Play boost sound
            if (boostSFX != null)
                boostSFX.Play();
        }


        if (isBoosting)
        {
            boostTimer += Time.fixedDeltaTime;
            boostOffset = boostAcceleration;

            if (boostTimer >= boostDuration)
            {
                isBoosting = false;
                boostTimer = 0f;
                boostCooldownTimer = boostCooldown;
            }
        }
        else if (boostCooldownTimer > 0f)
        {
            boostCooldownTimer -= Time.fixedDeltaTime;
            boostCooldownTimer = Mathf.Max(0f, boostCooldownTimer);
        }

        // Brake
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

        float throttle = Mathf.Clamp01(moveVertical);
        float scaledBoost = boostOffset * throttle;

        Vector3 movement;
        if (moveVertical > 0f)
            movement = transform.forward * (moveVertical * appliedSpeed + scaledBoost) * Time.fixedDeltaTime + extraVelocity;
        else
            movement = transform.forward * moveVertical * appliedSpeed * Time.fixedDeltaTime + extraVelocity;

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
        Vector3[] offsets = { Vector3.zero, new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f),
                              new Vector3(0.5f, 0, -0.5f), new Vector3(-0.5f, 0, -0.5f) };
        foreach (var offset in offsets)
            if (Physics.Raycast(transform.position + offset, Vector3.down, rayLength))
                return true;
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

    private void UpdateBoostUI()
    {
        if (boostStatusText == null) return;

        if (isBoosting)
        {
            boostStatusText.text = "PEDAL TO THE METAL!";
            boostStatusText.color = boostActiveColor;
        }
        else if (boostCooldownTimer > 0f)
        {
            boostStatusText.text = $"BOOST READY IN: {boostCooldownTimer:F0}s";
            boostStatusText.color = boostCooldownColor;
        }
        else
        {
            boostStatusText.text = "BOOST READY!";
            boostStatusText.color = boostReadyColor;
        }
    }

    private void UpdateCameraPosition()
    {
        if (cameraTransform == null) return;

        Vector3 boostOffsetLocal = isBoosting ? boostCameraOffset * 0.1f : Vector3.zero;
        currentCameraOffset = Vector3.Lerp(currentCameraOffset, boostOffsetLocal, Time.deltaTime * cameraLerpSpeed);
        cameraTransform.localPosition = originalCameraLocalPos + currentCameraOffset;
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



    public void ShowGameOver()
    {
        if (gameOverShown) return;
        gameOverShown = true;

        // Activate and animate Game Over UI
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            StartCoroutine(PopText(gameOverText, gameOverPopScale, gameOverPopDuration, gameOverColor));
        }

        // Fade out music
        if (MusicManager.Instance != null)
            MusicManager.Instance.FadeOut(2f);

        // Play Game Over sound using the existing AudioSource
        float clipLength = 0f;
        if (gameOverSFX != null && gameOverSFX.clip != null)
        {
            gameOverSFX.volume = 1f; // ensure volume is audible
            gameOverSFX.spatialBlend = 0f; // 2D sound
            gameOverSFX.Play();
            clipLength = gameOverSFX.clip.length;
        }

        // Restart scene after the sound finishes, or at least 4 seconds
        StartCoroutine(FadeAndRestart(Mathf.Max(4f, clipLength)));
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

    private void OnApplicationQuit()
    {
        if (port != null && port.IsOpen)
            port.Close();
    }
}
