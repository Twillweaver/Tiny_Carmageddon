using UnityEngine;

public class GemPickup : MonoBehaviour
{
    public GemSystem gemSystem;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            gemSystem.OnGemCollected();
            gameObject.SetActive(false); // Hide until respawned
        }
    }
}
