using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5.0f;
    public float rotationSpeed = 100.0f;

    [Header("Jump")]
    public float jumpForce = 5.0f;

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
    public float brakeDeceleration = 1f; // speed multiplier reduced per second when C held

    private Rigidbody rb;
    private float boostTimer = 0f;
    private float speedMultiplier = 1f; // 1 = full speed, 0 = stopped

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, centerOfMassYOffset, 0f);
        rb.angularDamping = angularDragValue;
        rb.linearDamping = linearDragValue;
        rb.mass = 1200f; // heavy car
    }

    private void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
    }

    private void FixedUpdate()
    {
        HandleMovement();

        // Extra gravity when airborne
        if (!IsGrounded())
        {
            rb.AddForce(Vector3.down * extraGravityForce, ForceMode.Acceleration);
        }

        HandleAutoRight();
    }

    private void HandleMovement()
    {
        float moveVertical = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");

        float appliedSpeed = speed;

        // ----------------------
        // Shift boost
        // ----------------------
        if (Input.GetKey(KeyCode.LeftShift) && boostTimer < boostDuration)
        {
            appliedSpeed *= boostMultiplier;
            boostTimer += Time.fixedDeltaTime;
        }
        else if (!Input.GetKey(KeyCode.LeftShift))
        {
            boostTimer = 0f;
        }

        // ----------------------
        // C Brake
        // ----------------------
        if (Input.GetKey(KeyCode.C))
        {
            speedMultiplier -= brakeDeceleration * Time.fixedDeltaTime;
            speedMultiplier = Mathf.Clamp(speedMultiplier, 0f, 1f);
        }
        else
        {
            speedMultiplier = 1f; // reset multiplier when released
        }

        // Apply multiplier
        appliedSpeed *= speedMultiplier;

        // Move forward/back
        Vector3 movement = transform.forward * moveVertical * appliedSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);

        // Turn
        float turnAmount = turn * rotationSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
    }

    private void HandleAutoRight()
    {
        Vector3 up = transform.up;
        float tiltAngle = Vector3.Angle(up, Vector3.up);
        float upDot = Vector3.Dot(up, Vector3.up);

        bool isFlipped = upDot < flipDotThreshold;
        bool isTooTilted = tiltAngle > tiltThreshold;

        if (!isFlipped && !isTooTilted)
            return;

        if (onlyWhenAirborne && IsGrounded())
            return;

        float yaw = rb.rotation.eulerAngles.y;
        Quaternion target = Quaternion.Euler(0f, yaw, 0f);

        float step = restoreSpeed * Time.fixedDeltaTime;
        Quaternion slerped = Quaternion.Slerp(rb.rotation, target, step);

        float maxStep = maxCorrectionPerSecond * Time.fixedDeltaTime;
        Quaternion finalRot = Quaternion.RotateTowards(rb.rotation, slerped, maxStep);

        rb.MoveRotation(finalRot);

        // Damp roll + pitch
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
}
