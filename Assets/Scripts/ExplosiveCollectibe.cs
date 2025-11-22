using UnityEngine;
using UnityEngine.SceneManagement;

public class ExplosiveCollectible : MonoBehaviour
{
    [Header("Explosion Effect")]
    public GameObject explosionEffect;   // particle prefab

    [Header("Restart Delay")]
    public float restartDelay = 1f;      // time before scene reload

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Spawn explosion effect
            if (explosionEffect != null)
                Instantiate(explosionEffect, other.transform.position, Quaternion.identity);

            // Destroy the player
            Destroy(other.gameObject);

            // Restart the scene
            StartCoroutine(RestartLevel());
        }
    }

    private System.Collections.IEnumerator RestartLevel()
    {
        yield return new WaitForSeconds(restartDelay);

        // Reload the current active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
