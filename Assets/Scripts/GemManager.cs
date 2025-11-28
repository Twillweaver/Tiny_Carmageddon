using UnityEngine;

public class GemManager : MonoBehaviour
{
    public GemSpawner spawner;
    public int gemsCollected = 0;

    public void AddGem()
    {
        gemsCollected++;

        if (gemsCollected % 3 == 0)
        {
            // Every 3 gems, spawn a new one
            spawner.RespawnGem();
        }
    }
}
