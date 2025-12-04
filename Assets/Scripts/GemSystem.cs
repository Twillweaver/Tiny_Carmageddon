using UnityEngine;

public class GemSystem : MonoBehaviour
{
    [Header("Gem Settings")]
    public Transform gemPrefab;                     // Prefab used for new gems
    public int gemsPerSpawnTrigger = 3;             // How many pickups before a new one is spawned

    [Header("Spawn Zones")]
    public BoxCollider[] spawnZones;                // Areas where new gems may spawn

    [Header("Raycast Settings")]
    public LayerMask floorMask;                     // Must match floor layer
    public float raycastHeight = 2f;                // Height to raycast downward from
    public float safeAboveGround = 0.2f;            // Lift above detected floor
    public int maxAttempts = 25;

    [Header("Behaviour Matching")]
    public Transform referenceGem;                  // Drag one existing gem here
    public bool matchReferenceHeight = true;
    public bool matchReferenceRotation = true;

    [Header("Optional Rotate Script")]
    public bool copyRotateScript = true;            // Automatically add/clone rotate script?
    public string rotateScriptName = "GemRotate";   // Name of your rotation script (case-sensitive)

    private int collectedSinceLastSpawn = 0;

    void Start()
    {
       
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
            Transform newGem = Instantiate(gemPrefab);
            newGem.position = spawnPoint;

            // -------------------------------------------------------------
            // HEIGHT MATCHING
            // -------------------------------------------------------------
            if (matchReferenceHeight && referenceGem != null)
            {
                Vector3 pos = newGem.position;
                pos.y = referenceGem.position.y;
                newGem.position = pos;
            }

            // -------------------------------------------------------------
            // ROTATION MATCHING
            // -------------------------------------------------------------
            if (matchReferenceRotation && referenceGem != null)
            {
                newGem.rotation = referenceGem.rotation;
            }
            else
            {
                newGem.rotation = Quaternion.identity;
            }

            // -------------------------------------------------------------
            // ROTATE SCRIPT HANDLING
            // -------------------------------------------------------------
            if (copyRotateScript)
            {
                System.Type rotateType = System.Type.GetType(rotateScriptName);

                if (rotateType != null)
                {
                    Component existing = newGem.GetComponent(rotateType);

                    // If the spawned prefab has no rotate script, add one.
                    if (existing == null)
                    {
                        existing = newGem.gameObject.AddComponent(rotateType);
                    }

                    // Try copying values from the reference gem's script.
                    if (referenceGem != null)
                    {
                        Component referenceComp = referenceGem.GetComponent(rotateType);
                        if (referenceComp != null)
                        {
                            CopyComponentValues(referenceComp, existing);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("GemSystem: No script found named " + rotateScriptName);
                }
            }

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
            BoxCollider zone = spawnZones[Random.Range(0, spawnZones.Length)];
            Bounds b = zone.bounds;

            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);

            Vector3 origin = new Vector3(x, b.max.y + raycastHeight, z);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 20f, floorMask))
            {
                Vector3 result = hit.point + Vector3.up * safeAboveGround;

                // If height matching is on, the spawn height will be overridden later.
                point = result;
                return true;
            }
        }

        return false;
    }

    // -------------------------------------------------------------
    // Copies serializable fields from one component to another
    // -------------------------------------------------------------
    private void CopyComponentValues(Component source, Component target)
    {
        var type = source.GetType();
        var fields = type.GetFields();

        foreach (var field in fields)
        {
            if (field.IsPublic || field.IsDefined(typeof(SerializeField), true))
            {
                field.SetValue(target, field.GetValue(source));
            }
        }
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
