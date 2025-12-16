using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [Header("Float Settings")]
    [Tooltip("How quickly the camera catches up to its target local position (0 = no movement, 1 = instant).")]
    [Range(0f, 1f)]
    // Controls how fast the camera moves to its target position
    public float positionSmooth = 0.1f;

    [Tooltip("How quickly the camera catches up to its target rotation (0 = no movement, 1 = instant).")]
    [Range(0f, 1f)]
    // Controls how fast the camera rotates to its target rotation
    public float rotationSmooth = 0.1f;

    [Header("Offsets")]
    [Tooltip("Optional rotation offset for a cinematic tilt (X = pitch, Y = yaw, Z = roll).")]
    // Tilt/pitch the camera for cinematic effect
    public Vector3 rotationOffset = new Vector3(10f, 0f, 0f);

    [Tooltip("Optional local position offset for extra floatiness.")]
    // Small position offset to make camera feel "floaty"
    public Vector3 positionOffset = Vector3.zero;

    // Target position the camera is moving toward
    private Vector3 targetLocalPosition;
    // Target rotation the camera is rotating toward
    private Quaternion targetLocalRotation;

    void Start()
    {
        // Initialize the camera's current local position and rotation as starting targets
        targetLocalPosition = transform.localPosition;
        targetLocalRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        // Update target local position/rotation every frame based on the configured offsets
        targetLocalPosition = positionOffset;
        targetLocalRotation = Quaternion.Euler(rotationOffset);

        // Smoothly interpolate to target position and rotation
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, positionSmooth);

        // Smoothly rotate the camera toward the target rotation
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLocalRotation, rotationSmooth);
    }
}
