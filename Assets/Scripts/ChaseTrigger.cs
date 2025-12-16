using UnityEngine;

public class ChaseTrigger : MonoBehaviour
{
    // Reference to the enemy GameObject that should start/stop chasing the player.
    // This object is expected to have a script (e.g. EnemyAI) that responds to SetPlayerInRange(bool).
    [Tooltip("Enemy")]
    public GameObject enemyObject;

    // Tag used to identify the player when they enter the trigger.
    // Default is "Player".
    [Tooltip("Player Tag")]
    public string playerTag = "Player";

    private void Reset()
    {
        // This runs automatically when the component is first added
        // or when the Reset option is used in the Inspector.

        // Ensure there's a trigger collider so this works out of the box
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // If no collider exists, add a BoxCollider and set it as a trigger
            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
        }
        else
        {
            // If a collider already exists, just make sure it is a trigger
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore anything that is not the player
        if (!IsPlayer(other)) return;

        // If no enemy has been assigned, do nothing
        if (enemyObject == null) return;

        // Notify the enemy that the player has entered the chase range
        // SendMessage allows loose coupling — the enemy does not need
        // to explicitly reference this script
        // DontRequireReceiver prevents errors if the method doesn't exist
        // Call SetPlayerInRange(true) on any component that implements it
        // DontRequireReceiver means there's no error if the method doesn't exist
        enemyObject.SendMessage("SetPlayerInRange", true, SendMessageOptions.DontRequireReceiver);
    }

    private void OnTriggerExit(Collider other)
    {
        // Ignore anything that is not the player
        if (!IsPlayer(other)) return;

        // If no enemy has been assigned, do nothing
        if (enemyObject == null) return;

        // Notify the enemy that the player has left the chase range
        enemyObject.SendMessage("SetPlayerInRange", false, SendMessageOptions.DontRequireReceiver);
    }

    // Helper method to check whether the collider belongs to the player
    private bool IsPlayer(Collider other)
    {
        return other.CompareTag(playerTag);
    }

    // Draw a visual representation of the trigger area in the Scene view
    // This only appears when the GameObject is selected
    private void OnDrawGizmosSelected()
    {
        // Semi-transparent green color for visibility
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.2f);

        // Attempt to draw the BoxCollider shape if one exists
        var col = GetComponent<Collider>() as BoxCollider;
        if (col != null)
        {
            // Match the collider's transform so the gizmo lines up correctly
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
        }
        else
        {
            // Fallback visual if no BoxCollider exists
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
