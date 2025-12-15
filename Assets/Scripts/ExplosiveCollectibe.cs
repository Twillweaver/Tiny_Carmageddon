using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ExplosiveCollectible : MonoBehaviour
{
    [Header("Explosion Effect")]
    public GameObject explosionEffect;

    [Header("Camera Death Movement")]
    public float pullUpHeight = 5f;
    public float cameraPitchAngle = 45f;
    public float movementSpeed = 2f;
    public float orbitSpeed = 50f;

    [Header("Restart Delay")]
    public float restartDelay = 2f;

    [Header("Sound Effects")]
    public AudioSource gameOverSFX; // scene-persistent AudioSource

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController_Arduino pc = other.GetComponent<PlayerController_Arduino>();

        // --- Spawn death camera ---
        Camera originalCam = Camera.main;
        Camera deathCam = null;
        if (originalCam != null)
        {
            GameObject deathCamObj = new GameObject("DeathCamera");
            deathCam = deathCamObj.AddComponent<Camera>();
            deathCamObj.AddComponent<AudioListener>();

            deathCam.transform.position = originalCam.transform.position;
            deathCam.transform.rotation = originalCam.transform.rotation;
            deathCam.fieldOfView = originalCam.fieldOfView;
            deathCam.clearFlags = originalCam.clearFlags;
            deathCam.backgroundColor = originalCam.backgroundColor;
            deathCam.nearClipPlane = originalCam.nearClipPlane;
            deathCam.farClipPlane = originalCam.farClipPlane;

            originalCam.gameObject.SetActive(false);
        }

        // --- Trigger Game Over via PlayerController (handles sound & UI) ---
        if (pc != null)
            pc.ShowGameOver();

        // --- Disable player scripts ---
        MonoBehaviour[] playerScripts = other.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour s in playerScripts)
            s.enabled = false;

        // --- Spawn explosion ---
        if (explosionEffect != null)
            Instantiate(explosionEffect, other.transform.position, Quaternion.identity);

        // --- Start cinematic ---
        if (deathCam != null)
            StartCoroutine(CinematicCamera(deathCam.transform, other.transform));

        // --- Restart is handled by PlayerController ShowGameOver ---
    }


    private IEnumerator DisablePlayerScripts(GameObject player, float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (MonoBehaviour s in player.GetComponents<MonoBehaviour>())
            s.enabled = false;
    }

    private IEnumerator CinematicCamera(Transform cam, Transform player)
    {
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

        float elapsed = 0f;
        while (elapsed < restartDelay)
        {
            elapsed += Time.deltaTime;

            cam.RotateAround(player.position, Vector3.up, orbitSpeed * Time.deltaTime);

            Vector3 pos = cam.position;
            pos.y = player.position.y + pullUpHeight;
            cam.position = pos;

            cam.LookAt(player.position + Vector3.up);

            yield return null;
        }
    }

    private IEnumerator Restart(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
