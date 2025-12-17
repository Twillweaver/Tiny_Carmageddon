using UnityEngine;
using System.IO.Ports; // Arduino serial communication
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// PlayerController_Arduino
// ------------------------
// Controls a physics-based car using Arduino steering input + keyboard fallback.
// Handles movement, boosting, collectibles, UI, audio, win/lose states, camera effects,
// particle VFX, and scene reset

[RequireComponent(typeof(Rigidbody))]
public class PlayerController_Arduino : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5.0f; // Base forward speed
    public float rotationSpeed = 200.0f; // Turning speed (degrees/sec)

    [Header("Auto Righting")]
    public float tiltThreshold = 40f; // Max allowed tilt before correction
    public float restoreSpeed = 6f; // How fast we rotate upright
    public float maxCorrectionPerSecond = 180f;
    public float flipDotThreshold = -0.1f; // Dot product threshold for upside-down
    public bool onlyWhenAirborne = false; // Only auto-right if not grounded

    [Header("Physics Tuning")]
    public float centerOfMassYOffset = -0.4f; // Lowers COM to reduce flipping
    public float angularDragValue = 1.5f; // Reduces rotational spin
    public float linearDragValue = 0.5f; // Dampens linear movement
    public float extraGravityForce = 120f; // Extra gravity when airborne

    [Header("Shift Boost")]
    public float boostDuration = 3f;
    public float boostCooldown = 10f;
    public float boostAcceleration = 8f;

    [Header("Steering")]
    public float steeringSensitivity = 3.0f;
    public float steeringDecay = 2.0f; // How fast steering recenters
    public float edgeThreshold = 0.95f; // Snap-to-edge threshold
    public float edgeSmoothSpeed = 5f; // Smooth snap speed

    [Header("Braking")]
    public float brakeDeceleration = 1f; // How quickly speed multiplier drops when braking

    [Header("Arduino")]
    public string portName = "COM3";
    public int baudRate = 115200;
    public float deadzone = 0.02f; // Ignore tiny steering noise
    public bool useNonLinearCurve = true; // Apply steering curve
    [Range(0f, 1f)] public float steeringCurveFactor = 0.7f; // Curve strength
    public float portRetryInterval = 2f; // Time between reconnection attempts

    [Header("Speed Display Scaling")]
    public float unityUnitsToMeters = 0.1f;
    public float speedSmoothFactor = 5f; // UI smoothing
    public float displaySpeedMultiplier = 12f;

    [Header("Collectibles")]
    public int collectibles = 0;
    public TextMeshProUGUI countText;
    public int totalCollectibles = 32;

    [Header("Speed UI")]
    public TextMeshProUGUI speedText;
    public float speedPopThreshold = 5f; // Change required to trigger pop
    public float speedPopScale = 1.5f;
    public float speedPopDuration = 0.2f;
    public Color speedPopColor = Color.cyan;

    [Header("Boost UI")]
    public TextMeshProUGUI boostStatusText;
    public Color boostReadyColor = Color.green;
    public Color boostActiveColor = Color.cyan;
    public Color boostCooldownColor = Color.red;

    [Header("Camera Boost Effect")]
    public Vector3 normalCameraOffset = new Vector3(0f, 2f, -5f);
    public Vector3 boostCameraOffset = new Vector3(0f, 2f, -7f);
    public float cameraLerpSpeed = 5f;

    [Header("Win Confetti VFX")]
    public GameObject winConfettiVFX;
    public float confettiLifetime = 5f;

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

    [Header("Fade and Scene Reset")]
    public Image fadeImage; // Fullscreen black image
    public float fadeDuration = 1f;

    [Header("UI Pop Animations")]
    public float popScale = 1.5f;
    public float popDuration = 0.2f;
    public Color popColor = Color.yellow;

    [Header("Sound Effects")]
    public AudioSource collectibleSFX;
    public AudioSource winSFX;
    public AudioSource boostSFX;
    public AudioSource gameOverSFX;
    public AudioSource engineAudioSource; // Engine loop
    public AudioClip engineClip;       
    [Range(0f, 1f)]
    public float engineVolume = 0.7f;

    [Header("Fall Reset")]
    public float fallYThreshold = -50f;   // height below which the level restarts
    public float fallRestartDelay = 1f;


    // Private fields

    private UnityEngine.Rigidbody rb;
    private SerialPort port;
    private bool portOpen = false;
    private float steeringValue = 0f; // Final processed steering value (-1 to 1)

    private float displayedSpeed = 0f; // Smoothed speed shown in UI
    private float lastDisplayedSpeed = 0f; // Last displayed speed (used to trigger pop animation)

    private float lastPortAttemptTime = 0f; // Time of last attempt to open COM port

    // Button states read from Arduino bitmask
    private bool btnForward = false;
    private bool btnBackward = false;
    private bool btnBrake = false;

    private int lastCollectibleCount = 0;
    private bool winnerShown = false; // Prevents win logic from triggering multiple times
    private bool gameOverShown = false; // Prevents game-over logic from triggering multiple times

    // Cached original scales for UI elements
    private Vector3 countTextOriginalScale;
    private Vector3 speedTextOriginalScale;
    private Vector3 winnerTextOriginalScale;
    private Vector3 gameOverTextOriginalScale;
    private Vector3 lastFixedPosition;

    private float speedMultiplier = 1f; // Used to reduce speed while braking
    private float boostTimer = 0f; // Tracks how long boost has been active
    private float boostCooldownTimer = 0f; // Cooldown timer between boosts
    private bool isBoosting = false; // Whether boost is currently active

    private Vector3 currentCameraOffset; // Current camera offset (smoothed)
    private Transform cameraTransform; // Reference to the main camera transform
    private Vector3 originalCameraLocalPos; // Camera position before any offsets

    private float lastRawSteering = 0f; // Raw steering input from last frame
    private float smoothedSteering = 0f; // Smoothed steering value

    private bool speedPopRunning = false; // Prevents overlapping speed pop animations


    // --- SETUP

    void Start()
    {
        // Rigidbody tuning
        rb = GetComponent<Rigidbody>();

        // Adjust the center of mass to improve vehicle stability
        rb.centerOfMass = new Vector3(0f, centerOfMassYOffset, 0f);

        // Set damping values to reduce unwanted motion
        rb.angularDamping = angularDragValue;
        rb.linearDamping = linearDragValue;

        // body mass
        rb.mass = 1500f;

        // Store the Rigidbody's initial position for later calculations
        lastFixedPosition = rb.position;

        // --- Engine audio setup

        // Ensure there is an AudioSource for the engine sounds
        if (engineAudioSource == null)
            engineAudioSource = gameObject.AddComponent<AudioSource>();

        // Assign the audio clip to the AudioSource
        engineAudioSource.clip = engineClip;

        // Loop the audio continuously
        engineAudioSource.loop = true;

        // Do not play automatically when the scene starts
        engineAudioSource.playOnAwake = false;

        // Set audio to 2D (non-spatialized) for engine sound
        engineAudioSource.spatialBlend = 0f; // 2D

        // Set the overall volume of the engine audio
        engineAudioSource.volume = engineVolume;

        // If an engine clip exists, configure initial idle sound
        if (engineClip != null)
        {
            engineAudioSource.pitch = 0.9f;   // idle pitch
            engineAudioSource.volume = engineVolume * 0.6f; // idle volume
            engineAudioSource.Play();
        }

        // --- Camera setup

        // Reference the main camera in the scene
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;

            // Store the original local position to reset camera after offsets
            originalCameraLocalPos = cameraTransform.localPosition;
        }
        // Initialize any camera offset applied during gameplay
        currentCameraOffset = Vector3.zero;

        // Open Arduino port
        OpenPort();

        // --- Initialise UI

        // Update the collectible counter UI
        if (countText != null) countText.text = $"Collectibles: {collectibles} / {totalCollectibles}";

        // Hide winner and game over messages at the start
        if (winnerText != null) winnerText.gameObject.SetActive(false);
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);

        // Store original scales for UI animations or scaling effects
        if (countText != null) countTextOriginalScale = countText.transform.localScale;
        if (speedText != null) speedTextOriginalScale = speedText.transform.localScale;
        if (winnerText != null) winnerTextOriginalScale = winnerText.transform.localScale;
        if (gameOverText != null) gameOverTextOriginalScale = gameOverText.transform.localScale;
    }

    // --- UPDATE LOOP (INPUT AND UI)

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

        // Determine if player is providing throttle input
        bool throttleInput = btnForward || btnBackward || Input.GetAxis("Vertical") != 0f || isBoosting;

        // Engine audio management
        if (engineAudioSource != null && engineClip != null)
        {
            float targetPitch;
            float targetVolume;

            if (isBoosting)
            {
                // Boosting: high pitch, louder
                targetPitch = 1.35f;
                targetVolume = engineVolume * 0.9f;
            }
            else if (throttleInput)
            {
                // Normal driving: medium pitch and volume
                targetPitch = 1.15f;
                targetVolume = engineVolume * 0.7f;
            }
            else
            {
                // Idle: low pitch, quieter
                targetPitch = 0.85f;
                targetVolume = engineVolume * 0.4f;
            }

            // Smoothly interpolate pitch and volume for natural audio transitions
            engineAudioSource.pitch =
                Mathf.Lerp(engineAudioSource.pitch, targetPitch, Time.deltaTime * 4f);

            engineAudioSource.volume =
                Mathf.Lerp(engineAudioSource.volume, targetVolume, Time.deltaTime * 4f);
        }

        // --- Fall off world check ---
        if (!gameOverShown && transform.position.y < fallYThreshold)
        {
            ShowGameOver(); // player fell off the map
            return;
        }

        // Collectibles UI update and sound
        if (countText != null && collectibles > lastCollectibleCount)
        {
            // Update collectibles counter text
            countText.text = $"Collectibles: {collectibles} / {totalCollectibles}";
            lastCollectibleCount = collectibles;

            // Trigger pop animation for text
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

            // Show winner text with pop animation
            if (winnerText != null)
            {
                winnerText.gameObject.SetActive(true);
                StartCoroutine(PopText(winnerText, winnerPopScale, winnerPopDuration, winnerColor));
            }

            // Play win sound
            if (winSFX != null)
                winSFX.Play();

            // Spawn confetti VFX and parent it to the player so it follows movement
            if (winConfettiVFX != null)
            {
                // Local offset above the car
                Vector3 localOffset = Vector3.up * 2f;

                GameObject confetti = Instantiate(
                    winConfettiVFX,
                    transform.position + localOffset,
                    winConfettiVFX.transform.rotation
                );

                // Parent to the player so it follows the car
                confetti.transform.SetParent(transform, worldPositionStays: true);

                // Ensure particle system plays
                ParticleSystem ps = confetti.GetComponent<ParticleSystem>();
                if (ps != null)
                    ps.Play();

                // Cleanup
                Destroy(confetti, confettiLifetime);
            }

            // Fade out background music
            if (MusicManager.Instance != null)
                MusicManager.Instance.FadeOut(2f);

            // Restart the game after a delay
            StartCoroutine(FadeAndRestart(4f));
        }


        // --- Game Over logic
        if (!gameOverShown)
        {
            // Check if car is flipped upside-down
            float upDot = Vector3.Dot(transform.up, Vector3.up);
            if (upDot < flipDotThreshold)
            {
                ShowGameOver();
            }
        }
    }

    // ---- PHYSICS LOOP
    void FixedUpdate()
    {
        // Extra gravity when airborne
        Vector3 gravityVelocity = Vector3.zero;

        // Apply additional downward force if the vehicle is not grounded
        if (!IsGrounded())
            gravityVelocity = Vector3.down * extraGravityForce * Time.fixedDeltaTime;

        // Handle movement and stabilization
        HandleMovement(gravityVelocity); // apply player input and extra gravity
        HandleAutoRight(); // auto-correct vehicle rotation if flipped or tilted

        // --- Speed calculation

        // Compute horizontal velocity based on position change
        Vector3 flatVel = new Vector3(rb.position.x - lastFixedPosition.x, 0f, rb.position.z - lastFixedPosition.z) / Time.fixedDeltaTime;

        // Convert Unity units per second to meters per second
        float actualSpeedMps = flatVel.magnitude * unityUnitsToMeters;

        // Convert m/s to mph (with optional display multiplier) and smooth the value
        float targetDisplaySpeed = actualSpeedMps * 2.23694f * displaySpeedMultiplier;
        displayedSpeed = Mathf.Lerp(displayedSpeed, targetDisplaySpeed, Time.fixedDeltaTime * speedSmoothFactor);

        // --- Speed UI
        if (speedText != null)
        {
            // Show speed in MPH (rounded)
            speedText.text = $"Speed: {displayedSpeed:F0} mph";

            // Trigger "pop" animation if speed changes significantly
            if (!speedPopRunning && Mathf.Abs(displayedSpeed - lastDisplayedSpeed) > speedPopThreshold)
            {
                StartCoroutine(PopText(speedText, speedPopScale, speedPopDuration, speedPopColor, () => speedPopRunning = false));
                speedPopRunning = true;
            }
            lastDisplayedSpeed = displayedSpeed;
        }
        // Save current position for next physics frame
        lastFixedPosition = rb.position;

        // ---- Send data to Arduino

        // Send current speed and collectibles count if port is open
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

    // Handles vehicle movement, turning, boost, and braking
    void HandleMovement(Vector3 extraVelocity = default)
    {
        // Get forward/backward input
        float moveVertical = Input.GetAxis("Vertical"); // keyboard/controller vertical input

        // Override input with Arduino buttons if pressed
        if (btnForward) moveVertical = 1f;
        if (btnBackward) moveVertical = -1f;

        // Get turning input
        float turnInput = portOpen ? steeringValue : Input.GetAxis("Horizontal");

        // Apply optional non-linear curve to steering input for smoother control
        if (useNonLinearCurve)
        {
            float t = turnInput;
            float a = steeringCurveFactor;
            turnInput = t * (a + (1f - a) * t * t);
        }

        // Set base movement speed
        float appliedSpeed = speed;

        // Boost logic
        float boostOffset = 0f;
        // Start boost if shift pressed, cooldown expired, and not currently boosting
        if (!isBoosting && boostCooldownTimer <= 0f && Input.GetKey(KeyCode.LeftShift))
        {
            isBoosting = true;
            boostTimer = 0f;

            // Play boost sound
            if (boostSFX != null)
                boostSFX.Play();
        }

        // Apply boost while active
        if (isBoosting)
        {
            boostTimer += Time.fixedDeltaTime;
            boostOffset = boostAcceleration; // additional speed during boost

            // Stop boost after duration expires
            if (boostTimer >= boostDuration)
            {
                isBoosting = false;
                boostTimer = 0f;
                boostCooldownTimer = boostCooldown; // start cooldown
            }
        }
        // Decrease cooldown timer if not boosting
        else if (boostCooldownTimer > 0f)
        {
            boostCooldownTimer -= Time.fixedDeltaTime;
            boostCooldownTimer = Mathf.Max(0f, boostCooldownTimer);
        }

        // Braking logic
        if (btnBrake || Input.GetKey(KeyCode.C))
        {
            speedMultiplier -= brakeDeceleration * Time.fixedDeltaTime; // reduce speed
            speedMultiplier = Mathf.Clamp(speedMultiplier, 0f, 1f);
        }
        else
        {
            speedMultiplier = 1f; // full speed if not braking
        }
        appliedSpeed *= speedMultiplier; // apply braking effect to speed

        // Calculate throttle and boost
        float throttle = Mathf.Clamp01(moveVertical); // normalized forward input
        float scaledBoost = boostOffset * throttle; // scale boost by throttle

        // Compute movement vector
        Vector3 movement;
        if (moveVertical > 0f)
            movement = transform.forward * (moveVertical * appliedSpeed + scaledBoost) * Time.fixedDeltaTime + extraVelocity;
        else
            movement = transform.forward * moveVertical * appliedSpeed * Time.fixedDeltaTime + extraVelocity;

        // Move Rigidbody to new position
        rb.MovePosition(rb.position + movement);

        // Compute and apply rotation
        float turnAmount = turnInput * rotationSpeed * Time.fixedDeltaTime; // rotation delta
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnAmount, 0f)); // apply rotation
    }

    // Automatically corrects the vehicle's rotation if flipped or too tilted
    private void HandleAutoRight()
    {
        Vector3 up = transform.up; // current up direction of the vehicle
        float tiltAngle = Vector3.Angle(up, Vector3.up); // angle between vehicle up and world up
        float upDot = Vector3.Dot(up, Vector3.up); // dot product to check if flipped
        bool isFlipped = upDot < flipDotThreshold; // true if upside-down
        bool isTooTilted = tiltAngle > tiltThreshold; // true if tilt exceeds allowed threshold

        // Only correct if flipped or too tilted
        if (!isFlipped && !isTooTilted) return;

        // Optionally correct only when airborne
        if (onlyWhenAirborne && IsGrounded()) return;

        // Preserve current yaw (rotation around Y-axis)
        float yaw = rb.rotation.eulerAngles.y;
        Quaternion target = Quaternion.Euler(0f, yaw, 0f); // upright rotation with current yaw

        // Smoothly interpolate rotation toward upright
        float step = restoreSpeed * Time.fixedDeltaTime;
        Quaternion slerped = Quaternion.Slerp(rb.rotation, target, step);

        // Limit maximum rotation change per frame for stability
        float maxStep = maxCorrectionPerSecond * Time.fixedDeltaTime;
        Quaternion finalRot = Quaternion.RotateTowards(rb.rotation, slerped, maxStep);

        // Apply corrected rotation to Rigidbody
        rb.MoveRotation(finalRot);

        // Dampen angular velocity on X and Z axes to reduce wobbling
        Vector3 localAngVel = transform.InverseTransformDirection(rb.angularVelocity);
        localAngVel.x *= 0.2f;
        localAngVel.z *= 0.2f;
        rb.angularVelocity = transform.TransformDirection(localAngVel);
    }

    // Checks if the vehicle is touching the ground
    private bool IsGrounded()
    {
        float rayLength = 1.2f; // distance to check below vehicle

        // Cast rays from center and four corners to detect ground
        Vector3[] offsets = { Vector3.zero, new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f),
                              new Vector3(0.5f, 0, -0.5f), new Vector3(-0.5f, 0, -0.5f) };

        foreach (var offset in offsets)
            if (Physics.Raycast(transform.position + offset, Vector3.down, rayLength))
                return true; // grounded if any ray hits

        return false; // not grounded
    }

    // Returns the original scale of a UI Text element (for animation)
    private Vector3 GetOriginalScale(TextMeshProUGUI text)
    {
        if (text == countText) return countTextOriginalScale;
        if (text == speedText) return speedTextOriginalScale;
        if (text == winnerText) return winnerTextOriginalScale;
        if (text == gameOverText) return gameOverTextOriginalScale;

        return Vector3.one; // fallback scale if text not recognised
    }

    // Pop a UI text element with scale and color animation
    private IEnumerator PopText(TextMeshProUGUI textElement, float scale, float duration, Color color, System.Action onComplete = null)
    {
        if (textElement == null) yield break; // exit if text element is null

        // Store original scale and color
        Vector3 originalScale = GetOriginalScale(textElement);
        Vector3 poppedScale = originalScale * scale; // target "popped" scale
        Color originalColor = textElement.color;

        // Set initial popped state
        textElement.transform.localScale = poppedScale;
        textElement.color = color;

        // Animate back to original scale and color over duration
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration; // normalised time
            textElement.transform.localScale = Vector3.Lerp(poppedScale, originalScale, t);
            textElement.color = Color.Lerp(color, originalColor, t);

            elapsed += Time.deltaTime;
            yield return null; // wait for next frame
        }

        // Ensure final values are exact
        textElement.transform.localScale = originalScale;
        textElement.color = originalColor;

        // Call optional completion callback
        onComplete?.Invoke();
    }

    // Update boost UI text based on boost state
    private void UpdateBoostUI()
    {
        if (boostStatusText == null) return;

        if (isBoosting)
        {
            // Player is actively boosting
            boostStatusText.text = "PEDAL TO THE METAL!";
            boostStatusText.color = boostActiveColor;
        }
        else if (boostCooldownTimer > 0f)
        {
            // Boost is on cooldown
            boostStatusText.text = $"BOOST READY IN: {boostCooldownTimer:F0}s";
            boostStatusText.color = boostCooldownColor;
        }
        else
        {
            // Boost is ready
            boostStatusText.text = "BOOST READY!";
            boostStatusText.color = boostReadyColor;
        }
    }

    // Toggle death camera state
    private bool deathCamActive = false;

    public void SetDeathCamActive(bool active)
    {
        deathCamActive = active; // enable or disable death cam behaviour
    }

    // Update camera position with boost offset
    private void UpdateCameraPosition()
    {
        if (cameraTransform == null || deathCamActive) return;  // skip if no camera or death cam active

        // Apply a small forward boost offset when boosting
        Vector3 boostOffsetLocal = isBoosting ? boostCameraOffset * 0.1f : Vector3.zero;

        // Smoothly interpolate camera offset
        currentCameraOffset = Vector3.Lerp(currentCameraOffset, boostOffsetLocal, Time.deltaTime * cameraLerpSpeed);

        // Update camera local position based on original position + current offset
        cameraTransform.localPosition = originalCameraLocalPos + currentCameraOffset;
    }

    // Fades the screen to black and restarts the current scene
    private IEnumerator FadeAndRestart(float delay)
    {
        if (fadeImage != null)
        {
            float elapsed = 0f; // track fade progress
            Color startColor = fadeImage.color; // current color
            Color targetColor = new Color(0f, 0f, 0f, 1f); // black with full opacity

            // Gradually interpolate the fadeImage color over fadeDuration
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeImage.color = Color.Lerp(startColor, targetColor, elapsed / fadeDuration);
                yield return null; // wait for next frame
            }
            // ensure final color is exactly black
            fadeImage.color = targetColor;
        }
        // Wait for remaining time if any before restarting
        yield return new WaitForSeconds(Mathf.Max(0f, delay - fadeDuration));

        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Handles Game Over sequence
    public void ShowGameOver()
    {
        if (gameOverShown) return; // prevent multiple triggers
        gameOverShown = true;

        // Activate and animate Game Over UI
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            StartCoroutine(PopText(gameOverText, gameOverPopScale, gameOverPopDuration, gameOverColor));
        }

        // Fade out background music
        if (MusicManager.Instance != null)
            MusicManager.Instance.FadeOut(2f);

        // Play Game Over sound effect
        float clipLength = 0f;
        if (gameOverSFX != null && gameOverSFX.clip != null)
        {
            gameOverSFX.volume = 1f; // ensure volume is audible
            gameOverSFX.spatialBlend = 0f; // 2D sound
            gameOverSFX.Play();
            clipLength = gameOverSFX.clip.length; // store duration
        }

        // Restart scene after the sound finishes, or at least 4 seconds
        StartCoroutine(FadeAndRestart(Mathf.Max(4f, clipLength)));
    }

    // Attempt to open the Arduino/COM port
    void OpenPort()
    {
        if (portOpen) return; // already open, no need to try again

        try
        {
            port = new SerialPort(portName, baudRate); // create new serial port object
            port.ReadTimeout = 50; // set a short read timeout (ms)
            port.Open(); // try to open the port
            portOpen = true; // mark port as open
        }
        catch
        {
            portOpen = false; // failed to open port
            Debug.LogWarning("Could not open COM port. Using keyboard fallback."); // fallback warning
        }
    }

    // Read input data from Arduino
    void ReadArduino()
    {
        if (!portOpen) return; // skip if port not open

        try
        {
            string line = port.ReadLine().Trim(); // read a line of data and trim whitespace
            string[] parts = line.Split(','); // expect "analogValue,buttonMask"
            if (parts.Length != 2) return; // invalid data, skip

            // Steering input
            int raw = int.Parse(parts[0]); // raw analog reading (0-1023)
            float normalized = Mathf.Clamp01(raw / 1023f); // normalise to 0-1
            float absoluteSteer = Mathf.Lerp(-1f, 1f, normalized); // map to -1 to 1

            // Only apply steering if vehicle is moving above threshold
            float speedThreshold = 0.1f;
            if (rb.linearVelocity.magnitude * unityUnitsToMeters > speedThreshold)
            {
                float target = 0f; // clamp to max
                if (absoluteSteer >= edgeThreshold) target = 1f; // clamp to max
                else if (absoluteSteer <= -edgeThreshold) target = -1f; // clamp to min
                else target = absoluteSteer; // within bounds

                // Determine smoothing speed based on whether at edges
                float smoothSpeed = (Mathf.Abs(target) == 1f) ? edgeSmoothSpeed : steeringDecay;

                // Smoothly move current steering value toward target
                smoothedSteering = Mathf.MoveTowards(smoothedSteering, target, smoothSpeed * Time.deltaTime);

                // Apply delta-based adjustment if movement exceeds deadzone
                float delta = absoluteSteer - lastRawSteering;
                lastRawSteering = absoluteSteer;

                if (Mathf.Abs(delta) > deadzone)
                    smoothedSteering += delta * steeringSensitivity * Time.deltaTime;

                // Clamp final steering value
                smoothedSteering = Mathf.Clamp(smoothedSteering, -1f, 1f);
                steeringValue = smoothedSteering; // assign to public steering value
            }

            // Button input
            int mask = int.Parse(parts[1]); // button mask from Arduino
            btnForward = (mask & 0b00000001) != 0; // forward button
            btnBackward = (mask & 0b00000100) != 0; // backward button
            btnBrake = (mask & 0b00010000) != 0; // brake button
        }
        // silently fail if parsing or reading fails
        catch { }
    }

    // Close port cleanly on application exit
    private void OnApplicationQuit()
    {
        if (port != null && port.IsOpen)
            port.Close();
    }
}
