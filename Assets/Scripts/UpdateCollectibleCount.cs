using UnityEngine;
using TMPro;
// Required for Type handling (GetType)
using System;

public class UpdateCollectibleCount : MonoBehaviour
{
    // Reference to the TextMeshProUGUI component where the count will be displayed
    private TextMeshProUGUI collectibleText;

    void Start()
    {
        // Get the TextMeshProUGUI component attached to this GameObject
        collectibleText = GetComponent<TextMeshProUGUI>();

        // Ensure the component exists; if not, log an error
        if (collectibleText == null)
        {
            Debug.LogError("UpdateCollectibleCount script requires a TextMeshProUGUI component on the same GameObject.");
            return;
        }

        // Perform an initial update of the collectible count display
        UpdateCollectibleDisplay();
    }

    void Update()
    {
        // Continuously update the collectible count every frame
        UpdateCollectibleDisplay();
    }

    // Updates the on-screen collectible count
    private void UpdateCollectibleDisplay()
    {
        int totalCollectibles = 0;

        // --- Count objects of type 'Collectible' ---
        Type collectibleType = Type.GetType("Collectible");
        if (collectibleType != null)
        {
            // Find all objects of this type currently in the scene
            totalCollectibles += UnityEngine.Object.FindObjectsByType(collectibleType, FindObjectsSortMode.None).Length;
        }

        // Optionally, check and count objects of type Collectible2D as well if needed
        Type collectible2DType = Type.GetType("Collectible2D");
        if (collectible2DType != null)
        {
            totalCollectibles += UnityEngine.Object.FindObjectsByType(collectible2DType, FindObjectsSortMode.None).Length;
        }

        // --- Update the TextMeshProUGUI display ---
        collectibleText.text = $"Collectibles remaining: {totalCollectibles}";
    }
}
