using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    // Stores the camera’s original local position so we can return to it after shaking
    private Vector3 originalPos;

    private void Awake()
    {
        // Assign this instance so it can be accessed globally
        Instance = this;

        // Save the starting local position of the camera
        originalPos = transform.localPosition;
    }

    // Public method to start a camera shake.
    // duration  = how long the shake lasts (seconds)
    // magnitude = how strong the shake is
    public void Shake(float duration, float magnitude)
    {
        // Start the shake coroutine
        StartCoroutine(DoShake(duration, magnitude));
    }

    // Coroutine that actually performs the shake over time
    private System.Collections.IEnumerator DoShake(float duration, float magnitude)
    {
        // Track how long we’ve been shaking
        float elapsed = 0f;

        // Continue shaking until the duration has elapsed
        while (elapsed < duration)
        {
            // Generate random offsets for X and Y based on magnitude
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // Apply the random offset to the camera’s local position
            transform.localPosition = originalPos + new Vector3(x, y, 0);

            // Increase elapsed time
            elapsed += Time.deltaTime;

            // Wait until the next frame
            yield return null;
        }

        // Reset the camera back to its original position after shaking
        transform.localPosition = originalPos;
    }
}
