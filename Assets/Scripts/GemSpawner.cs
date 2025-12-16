using UnityEngine;

public class GemSpawner : MonoBehaviour
{
    // The area within which gems can spawn
    public BoxCollider spawnArea;
    // Reference to the gem to spawn/position
    public Transform gem;

    [Header("Raycast Settings")]
    public LayerMask floorMask;       // Only allow spawning on these layers (Floor)
    public float raycastHeight = 2f;  // How high above to cast from
    public float safeAboveGround = 0.3f; // Final gem height above the hit point
    public int maxAttempts = 20;      // Prevent infinite loops

    // Main method to place or reposition a gem
    public void RespawnGem()
    {
        // Get the bounds of the spawn area
        Bounds b = spawnArea.bounds;

        // Try multiple times to find a valid spawn location
        for (int i = 0; i < maxAttempts; i++)
        {
            // Step 1 – pick a random XZ position within the spawn area
            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);

            // Start the raycast from above the spawn area
            Vector3 origin = new Vector3(x, b.max.y + raycastHeight, z);

            // Step 2 – raycast downward to find the floor
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f, floorMask))
            {
                // Step 3 – calculate candidate position slightly above the floor
                Vector3 candidatePos = hit.point + Vector3.up * safeAboveGround;

                // Step 4 – check if there’s enough space for the gem at this position
                if (!Physics.CheckSphere(candidatePos, 0.2f)) // radius depends on gem size
                {
                    // Place the gem and exit the loop
                    gem.position = candidatePos;
                    return;
                }
            }
        }

        // If a valid position was not found after all attempts
        Debug.LogWarning("GemSpawner could not find a valid spawn location.");
    }
}
