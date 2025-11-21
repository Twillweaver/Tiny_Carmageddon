using UnityEngine;
using System.IO.Ports;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController_Arduino : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5.0f;
    public float rotationSpeed = 200.0f;

    [Header("Jump")]
    public float jumpForce = 5.0f;

    [Header("Arduino")]
    public string portName = "COM3";
    public int baudRate = 115200;
    public float deadzone = 0.02f;
    public bool useNonLinearCurve = true;
    public float portRetryInterval = 2f;

    [Header("Speed Display Scaling")]
    public float speedScale = 60f; // Scale factor to “fake” speed for OLED
    public float speedSmoothFactor = 5f; // Smooth displayed speed

    private Rigidbody rb;
    private SerialPort port;
    private bool portOpen = false;
    private float steeringValue = 0f;
    private float displayedSpeed = 0f;
    private float lastPortAttemptTime = 0f;
    private float speedMultiplier = 1f;
    private float boostTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.4f, 0f);
        rb.angularDamping = 1.5f;
        rb.linearDamping = 0.5f;
        rb.mass = 1200f;

        OpenPort();
    }

    void Update()
    {
        // Retry COM port if not open
        if (!portOpen && Time.time - lastPortAttemptTime > portRetryInterval)
        {
            OpenPort();
            lastPortAttemptTime = Time.time;
        }

        ReadArduino();

        if (Input.GetButtonDown("Jump"))
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

        // Send “faked” speed to Arduino for OLED
        if (portOpen && port != null && port.IsOpen)
        {
            float actualSpeed = rb.linearVelocity.magnitude * speedScale; // scale for tiny car
            displayedSpeed = Mathf.Lerp(displayedSpeed, actualSpeed, Time.deltaTime * speedSmoothFactor);
            port.WriteLine(displayedSpeed.ToString("F1"));
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        float moveVertical = Input.GetAxis("Vertical");
        float turn = portOpen ? steeringValue : Input.GetAxis("Horizontal");

        float appliedSpeed = speed;

        if (Input.GetKey(KeyCode.LeftShift))
            appliedSpeed *= 1.1f;

        if (Input.GetKey(KeyCode.C))
        {
            speedMultiplier -= 1f * Time.fixedDeltaTime;
            speedMultiplier = Mathf.Clamp(speedMultiplier, 0f, 1f);
        }
        else
            speedMultiplier = 1f;

        appliedSpeed *= speedMultiplier;

        Vector3 movement = transform.forward * moveVertical * appliedSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);

        float turnAmount = turn * rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnAmount, 0f));
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
            Debug.Log("COM port opened: " + portName);
        }
        catch
        {
            portOpen = false;
            Debug.LogWarning("Could not open COM port. Keyboard fallback active.");
        }
    }

    void ReadArduino()
    {
        if (!portOpen) return;

        try
        {
            string line = port.ReadLine().Trim();
            int raw = int.Parse(line);
            float normalized = Mathf.Clamp01(raw / 1023f);
            float rawSteering = Mathf.Lerp(-1f, 1f, normalized);

            if (Mathf.Abs(rawSteering) < deadzone)
                rawSteering = 0f;

            if (useNonLinearCurve)
                rawSteering = Mathf.Sign(rawSteering) * Mathf.Pow(Mathf.Abs(rawSteering), 1.2f);

            steeringValue = rawSteering;
        }
        catch { }
    }

    private void OnApplicationQuit()
    {
        if (port != null && port.IsOpen)
            port.Close();
    }
}
