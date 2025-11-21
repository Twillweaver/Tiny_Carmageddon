using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform followTarget;       // The object the camera follows
    public Vector3 followOffset = Vector3.zero;  // Optional offset from target

    [Header("Rotation")]
    public float yaw;                    // Y rotation (mouse X)
    public float pitch;                  // X rotation (mouse Y)
    public float sensitivity = 150f;     // Mouse sensitivity
    public float minPitch = -40f;        // Clamp pitch rotation
    public float maxPitch = 80f;

    [Header("Zoom")]
    public float distance = 5f;          // Camera distance
    public float minDistance = 2f;
    public float maxDistance = 12f;
    public float zoomSpeed = 5f;

    [Header("Collision")]
    public LayerMask collisionMask;      // Layers camera should avoid
    public float collisionBuffer = 0.2f; // Prevent clipping into objects

    void LateUpdate()
    {
        if (followTarget == null)
            return;

        // --- Update rotation from mouse input ---
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * sensitivity * Time.deltaTime;
        pitch -= mouseY * sensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // --- Zoom using scroll wheel ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // --- Base rotation ---
        Quaternion rotation = Quaternion.AngleAxis(yaw, Vector3.up);
        rotation *= Quaternion.AngleAxis(pitch, Vector3.right);

        // --- Start from target position ---
        Vector3 targetPos = followTarget.position + followOffset;

        // --- Calculate intended camera position (before collision test) ---
        Vector3 intendedPos = targetPos - rotation * Vector3.forward * distance;

        // --- Collision handling using Raycast ---
        Ray ray = new Ray(targetPos, intendedPos - targetPos);
        float rayDistance = Vector3.Distance(targetPos, intendedPos);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, collisionMask))
        {
            // Move camera closer if something is in the way
            float correctedDist = hit.distance - collisionBuffer;
            intendedPos = targetPos + (intendedPos - targetPos).normalized * correctedDist;
        }

        // --- Apply final camera transform ---
        transform.position = intendedPos;
        transform.rotation = rotation;
    }
}
