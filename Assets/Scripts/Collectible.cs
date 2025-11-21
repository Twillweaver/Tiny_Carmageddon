using UnityEngine;

public class Collectible : MonoBehaviour
{
    [Header("Rotation Speed")]
    public float RotationSpeedX = 0f;
    public float RotationSpeedY = 50f;
    public float RotationSpeedZ = 0f;

    [Header("Collectible Effect")]
    public GameObject onCollectEffect;

    private void Update()
    {
        transform.Rotate(RotationSpeedX * Time.deltaTime,
                         RotationSpeedY * Time.deltaTime,
                         RotationSpeedZ * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Increment player's collectible count
            PlayerController_Arduino pc = other.GetComponent<PlayerController_Arduino>();
            if (pc != null)
                pc.collectibles++;

            // Spawn effect
            if (onCollectEffect != null)
                Instantiate(onCollectEffect, transform.position, transform.rotation);

            // Destroy this collectible
            Destroy(gameObject);
        }
    }
}
