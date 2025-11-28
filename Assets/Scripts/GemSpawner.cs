using UnityEngine;

public class GemSpawner : MonoBehaviour
{
    public BoxCollider spawnArea;
    public Transform gem;

    [Header("Raycast Settings")]
    public LayerMask floorMask;       // Only allow spawning on these layers (e.g., Floor)
    public float raycastHeight = 2f;  // How high above to cast from
    public float safeAboveGround = 0.3f; // Final gem height above the hit point
    public int maxAttempts = 20;      // Prevent infinite loops

    public void RespawnGem()
    {
        Bounds b = spawnArea.bounds;

        for (int i = 0; i < maxAttempts; i++)
        {
            // Step 1 – pick a random XZ position
            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);

            Vector3 origin = new Vector3(x, b.max.y + raycastHeight, z);

            // Step 2 – raycast downward to find floor
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f, floorMask))
            {
                // Step 3 – ensure the gem would not overlap something
                Vector3 candidatePos = hit.point + Vector3.up * safeAboveGround;

                // Sphere check at gem position
                if (!Physics.CheckSphere(candidatePos, 0.2f)) // radius depends on gem size
                {
                    gem.position = candidatePos;
                    return;
                }
            }
        }

        Debug.LogWarning("GemSpawner could not find a valid spawn location.");
    }
}
