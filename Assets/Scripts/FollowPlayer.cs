using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [Header("Float Settings")]
    [Tooltip("How quickly the camera catches up to its target local position (0 = no movement, 1 = instant).")]
    [Range(0f, 1f)]
    public float positionSmooth = 0.1f;

    [Tooltip("How quickly the camera catches up to its target rotation (0 = no movement, 1 = instant).")]
    [Range(0f, 1f)]
    public float rotationSmooth = 0.1f;

    [Header("Offsets")]
    [Tooltip("Optional rotation offset for a cinematic tilt (X = pitch, Y = yaw, Z = roll).")]
    public Vector3 rotationOffset = new Vector3(10f, 0f, 0f);

    [Tooltip("Optional local position offset for extra floatiness.")]
    public Vector3 positionOffset = Vector3.zero;

    private Vector3 targetLocalPosition;
    private Quaternion targetLocalRotation;

    void Start()
    {
        // Initialize target local position/rotation
        targetLocalPosition = transform.localPosition;
        targetLocalRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        // Update desired local position and rotation based on offsets
        targetLocalPosition = positionOffset;
        targetLocalRotation = Quaternion.Euler(rotationOffset);

        // Smoothly interpolate to target position and rotation
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, positionSmooth);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLocalRotation, rotationSmooth);
    }
}
