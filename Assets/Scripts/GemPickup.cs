using UnityEngine;

public class GemPickup : MonoBehaviour
{
    // Reference to the GemSystem that manages gem collection and spawning
    public GemSystem gemSystem;

    // This method is called automatically when another collider enters this object's trigger collider
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger has the "Player" tag
        if (other.CompareTag("Player"))
        {
            // Notify the GemSystem that this gem has been collected
            gemSystem.OnGemCollected();

            // Deactivate this gem object so it disappears until the GemSystem respawns it
            gameObject.SetActive(false);
        }
    }
}
