using UnityEngine;

public class GemManager : MonoBehaviour
{
    // Reference to a GemSpawner object which handles spawning new gems
    public GemSpawner spawner;

    // Tracks how many gems the player has collected
    public int gemsCollected = 0;

    // Call this method whenever the player collects a gem
    public void AddGem()
    {
        // Increment the collected gem count
        gemsCollected++;

        // Check if the player has collected a multiple of 3 gems
        if (gemsCollected % 3 == 0)
        {
            // Every 3 gems collected, tell the spawner to spawn a new gem
            spawner.RespawnGem();
        }
    }
}
