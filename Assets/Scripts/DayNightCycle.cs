using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Day-Night Cycle Settings")]
    [Tooltip("Duration of a full day in seconds")]
    // How long it takes for a complete day-night cycle (full 360 degree rotation of the sun)
    public float dayDurationInSeconds = 60f;

    [Tooltip("Rotation axis for the sun (e.g., X: 1, Y: 0, Z: 0 for X-axis rotation)")]
    // Axis around which the sun rotates (Vector3.right = X-axis rotation)
    public Vector3 rotationAxis = Vector3.right;

    [Header("Light Temperature Settings")]
    [Tooltip("Color of the light in the morning")]
    // Light color at the start of the day
    public Color morningColor = new Color(0.5f, 0.7f, 1f); // soft blue light
    [Tooltip("Color of the light in the afternoon")]
    // Light color in the afternoon
    public Color afternoonColor = new Color(1f, 0.6f, 0.3f); // warm orange light

    private Light directionalLight;
    // Calculated rotation speed to complete 360 degree in dayDurationInSeconds
    private float rotationSpeed;

    void Start()
    {
        // Get the Light component attached to the Directional Light
        directionalLight = GetComponent<Light>();

        // If there is no Light, display an error and disable the script
        if (directionalLight == null)
        {
            Debug.LogError("No Light component found on the GameObject. Please attach this script to a Directional Light.");
            enabled = false;
            return;
        }

        // Calculate the rotation speed based on the duration of the day
        rotationSpeed = 360f / dayDurationInSeconds;
    }

    void Update()
    {
        // Rotate the light around the selected axis
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);

        // Calculate the normalized time of the day (0 to 1)
        // Mathf.PingPong ensures it goes up and down, simulating day and night
        float timeNormalized = Mathf.PingPong(Time.time / dayDurationInSeconds, 1);

        // Interpolate between morning and afternoon colors based on the time of day
        directionalLight.color = Color.Lerp(morningColor, afternoonColor, timeNormalized);
    }
}