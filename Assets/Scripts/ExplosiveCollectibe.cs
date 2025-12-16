using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ExplosiveCollectible : MonoBehaviour
{
    [Header("Explosion Effect")]
    // Visual explosion effect when player dies
    public GameObject explosionEffect;

    [Header("Camera Death Movement")]
    // How high the death camera lifts
    public float pullUpHeight = 5f;
    // Camera tilt angle for cinematic view
    public float cameraPitchAngle = 45f;
    // Speed of initial camera lift
    public float movementSpeed = 2f;
    // Speed of orbiting around the player
    public float orbitSpeed = 50f;

    [Header("Restart Delay")]
    // Time camera orbits before the scene can restart
    public float restartDelay = 2f;

    [Header("Sound Effects")]
    // scene persistent game over sound
    public AudioSource gameOverSFX;

    // Called when another collider enters the enemy's trigger collider
    private void OnTriggerEnter(Collider other)
    {
        // Only kill the player
        if (!other.CompareTag("Player")) return;

        PlayerController_Arduino pc = other.GetComponent<PlayerController_Arduino>();

        // --- Notify the PlayerController that the player is dead ---
        // This handles UI, sounds, and any internal player death logic
        if (pc != null)
            pc.ShowGameOver();

        // --- Spawn explosion effect at the player's position ---
        if (explosionEffect != null)
            Instantiate(explosionEffect, other.transform.position, Quaternion.identity);

        // --- Spawn cinematic death camera ---
        Camera originalCam = Camera.main;
        if (originalCam != null)
            // Disable the main camera
            originalCam.gameObject.SetActive(false);

        GameObject deathCamObj = new GameObject("DeathCamera");
        Camera deathCam = deathCamObj.AddComponent<Camera>();
        // handles the sound in the death cam
        deathCamObj.AddComponent<AudioListener>();

        // Copy original camera settings to maintain visual consistency
        if (originalCam != null)
        {
            deathCam.transform.position = originalCam.transform.position;
            deathCam.transform.rotation = originalCam.transform.rotation;
            deathCam.fieldOfView = originalCam.fieldOfView;
            deathCam.clearFlags = originalCam.clearFlags;
            deathCam.backgroundColor = originalCam.backgroundColor;
            deathCam.nearClipPlane = originalCam.nearClipPlane;
            deathCam.farClipPlane = originalCam.farClipPlane;
        }

        // --- Notify PlayerController to stop controlling camera ---
        if (pc != null)
            pc.SetDeathCamActive(true);

        // --- Start cinematic camera orbit ---
        StartCoroutine(CinematicCamera(deathCam.transform, other.transform));
    }

    // Coroutine to handle camera lift and orbit around the player
    private IEnumerator CinematicCamera(Transform cam, Transform player)
    {
        // --- Initial camera lift and tilt ---
        Vector3 startPos = cam.position;
        Vector3 targetPos = startPos + Vector3.up * pullUpHeight;
        Quaternion startRot = cam.rotation;
        Quaternion targetRot = Quaternion.Euler(cameraPitchAngle, cam.eulerAngles.y, 0);

        float t = 0f;
        while (t < 1f)
        {
            // interpolation, smooth move up and tilt
            t += Time.deltaTime * movementSpeed;
            cam.position = Vector3.Lerp(startPos, targetPos, t);
            cam.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        // --- Orbit around the player's position ---
        float elapsed = 0f;
        while (elapsed < restartDelay)
        {
            elapsed += Time.deltaTime;

            // Rotate camera around the player horizontally
            cam.RotateAround(player.position, Vector3.up, orbitSpeed * Time.deltaTime);

            // Maintain consistent height relative to the player
            Vector3 pos = cam.position;
            pos.y = player.position.y + pullUpHeight;
            cam.position = pos;

            // Always look at the player
            cam.LookAt(player.position + Vector3.up);

            yield return null;
        }
    }

}
