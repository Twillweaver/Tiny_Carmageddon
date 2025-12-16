using UnityEngine;

public class Collectible : MonoBehaviour
{
// How fast the collectible spins around each axis (degrees per second)
    [Header("Rotation Speed")]
    public float RotationSpeedX = 0f;
    public float RotationSpeedY = 50f;
    public float RotationSpeedZ = 0f;

    [Header("Collectible Effect")]
    public GameObject onCollectEffect;

    private void Update()
    {
        // Rotate the collectible every frame
        // Multiply by Time.deltaTime so rotation is framerate independent
        transform.Rotate(RotationSpeedX * Time.deltaTime,
                         RotationSpeedY * Time.deltaTime,
                         RotationSpeedZ * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered the trigger is the player
        if (other.CompareTag("Player"))
        {
            // Increment player's collectible count
            // Assumes the player has a PlayerController_Arduino script
            PlayerController_Arduino pc = other.GetComponent<PlayerController_Arduino>();
            if (pc != null)
                pc.collectibles++;

            // Spawn the onCollectEffect prefab at the collectible's position and rotation
            // For example, a sparkle or particle effect
            if (onCollectEffect != null)
                Instantiate(onCollectEffect, transform.position, transform.rotation);

            // Remove the collectible from the scene
            Destroy(gameObject);
        }
    }
}
