using UnityEngine;

public class ChaseTrigger : MonoBehaviour
{
    [Tooltip("Drag the enemy GameObject here (the object that has the EnemyAI component).")]
    public GameObject enemyObject;

    [Tooltip("Tag used to identify the player (default: Player).")]
    public string playerTag = "Player";

    private void Reset()
    {
        // Ensure there's a trigger collider so this works out of the box
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        if (enemyObject == null) return;

        // Call SetPlayerInRange(true) on any component that implements it.
        // DontRequireReceiver means there's no error if the method doesn't exist.
        enemyObject.SendMessage("SetPlayerInRange", true, SendMessageOptions.DontRequireReceiver);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        if (enemyObject == null) return;

        enemyObject.SendMessage("SetPlayerInRange", false, SendMessageOptions.DontRequireReceiver);
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag(playerTag);
    }

    // Optional: visualise the trigger in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.2f);
        var col = GetComponent<Collider>() as BoxCollider;
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
        }
        else
        {
            // fall back: draw a wire sphere
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
