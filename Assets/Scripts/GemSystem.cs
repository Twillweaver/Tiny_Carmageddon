using UnityEngine;

public class GemSystem : MonoBehaviour
{
    [Header("Gem Settings")]
    public Transform gemPrefab;               // The gem object
    public int gemsPerSpawnTrigger = 3;       // Spawn a new gem every 3 collected

    [Header("Spawn Zones")]
    public BoxCollider[] spawnZones;          // Assign 1 or more BoxColliders in the scene

    [Header("Raycast Settings")]
    public LayerMask floorMask;               // Must match floor layer
    public float raycastHeight = 2f;          // Start raycast above spawn zone
    public float safeAboveGround = 0.2f;      // Lift gem above floor
    public int maxAttempts = 25;              // Max random attempts per spawn

    private int collectedSinceLastSpawn = 0;

    void Start()
    {
        RespawnGem(); // Spawn first gem
    }

    public void OnGemCollected()
    {
        collectedSinceLastSpawn++;

        if (collectedSinceLastSpawn >= gemsPerSpawnTrigger)
        {
            collectedSinceLastSpawn = 0;
            RespawnGem();
        }
    }

    public void RespawnGem()
    {
        if (TryGetValidSpawnPoint(out Vector3 spawnPoint))
        {
            Transform newGem = Instantiate<Transform>(gemPrefab);
            if (newGem.gameObject.activeSelf == false)
                newGem.gameObject.SetActive(true);

            newGem.position = spawnPoint;
            Debug.Log("Gem spawned at: " + spawnPoint);
        }
        else
        {
            Debug.LogWarning("GemSystem: Could not find a valid spawn point!");
        }
    }

    private bool TryGetValidSpawnPoint(out Vector3 point)
    {
        point = Vector3.zero;

        if (spawnZones == null || spawnZones.Length == 0)
        {
            Debug.LogWarning("GemSystem: No spawn zones assigned!");
            return false;
        }

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Pick a random spawn zone
            BoxCollider zone = spawnZones[Random.Range(0, spawnZones.Length)];
            Bounds b = zone.bounds;

            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);
            Vector3 origin = new Vector3(x, b.max.y, z);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 20f, floorMask))
            {
                // Optionally, add overlap check here to avoid furniture
                point = hit.point + Vector3.up * safeAboveGround;
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnZones == null) return;

        Gizmos.color = Color.yellow;
        foreach (BoxCollider zone in spawnZones)
        {
            if (zone != null)
                Gizmos.DrawWireCube(zone.bounds.center, zone.bounds.size);
        }
    }
}
