using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ExplosiveCollectible : MonoBehaviour
{
    [Header("Explosion Effect")]
    public GameObject explosionEffect;

    [Header("Camera Death Movement")]
    public float pullUpHeight = 5f;          // Height camera moves up
    public float cameraPitchAngle = 45f;     // Tilt down angle
    public float movementSpeed = 2f;         // Upward movement speed
    public float orbitSpeed = 50f;           // Degrees per second

    [Header("Restart Delay")]
    public float restartDelay = 2f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // --- Spawn explosion ---
        if (explosionEffect != null)
            Instantiate(explosionEffect, other.transform.position, Quaternion.identity);

        // --- Disable player scripts ---
        MonoBehaviour[] playerScripts = other.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour s in playerScripts)
            s.enabled = false;

        // --- Create independent death camera ---
        Camera originalCam = Camera.main;
        if (originalCam != null)
        {
            // Create new camera object
            GameObject deathCamObj = new GameObject("DeathCamera");
            Camera deathCam = deathCamObj.AddComponent<Camera>();

            // Copy settings from original camera
            deathCam.transform.position = originalCam.transform.position;
            deathCam.transform.rotation = originalCam.transform.rotation;
            deathCam.fieldOfView = originalCam.fieldOfView;
            deathCam.clearFlags = originalCam.clearFlags;
            deathCam.backgroundColor = originalCam.backgroundColor;
            deathCam.nearClipPlane = originalCam.nearClipPlane;
            deathCam.farClipPlane = originalCam.farClipPlane;

            // Disable original camera
            originalCam.gameObject.SetActive(false);

            // Start cinematic sequence
            StartCoroutine(CinematicCamera(deathCam.transform, other.transform));
        }

        // --- Restart scene ---
        StartCoroutine(Restart());
    }

    private IEnumerator CinematicCamera(Transform cam, Transform player)
    {
        // 1. Smooth rise and tilt
        Vector3 startPos = cam.position;
        Vector3 targetPos = startPos + new Vector3(0, pullUpHeight, 0);
        Quaternion startRot = cam.rotation;
        Quaternion targetRot = Quaternion.Euler(cameraPitchAngle, cam.eulerAngles.y, 0);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * movementSpeed;
            cam.position = Vector3.Lerp(startPos, targetPos, t);
            cam.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        // 2. Orbit around player until restart
        float elapsed = 0f;
        while (elapsed < restartDelay)
        {
            elapsed += Time.deltaTime;

            // Rotate around player
            cam.RotateAround(player.position, Vector3.up, orbitSpeed * Time.deltaTime);

            // Maintain height
            Vector3 pos = cam.position;
            pos.y = player.position.y + pullUpHeight;
            cam.position = pos;

            // Look at player
            cam.LookAt(player.position + Vector3.up);

            yield return null;
        }
    }

    private IEnumerator Restart()
    {
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
