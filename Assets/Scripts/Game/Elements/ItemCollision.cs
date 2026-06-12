using UnityEngine;
using System;

public class ItemCollision : MonoBehaviour
{
    [SerializeField] private string itemType; // Set in Unity Editor, e.g., "Bones", "Shield", "SlowPotion"

    // Event triggered when an item is collected
    public static event Action<string> OnItemCollected;

    void Start()
    {
        if (string.IsNullOrEmpty(itemType))
        {
            Debug.LogWarning($"Item type not set for {gameObject.name}", gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!string.IsNullOrEmpty(itemType))
            {
                OnItemCollected?.Invoke(itemType); // Trigger event with item type
                Debug.Log($"Collected {itemType}");
            }
            else
            {
                Debug.LogWarning("Item collected but itemType is not set", gameObject);
            }

            // 🛠️ FIX: Robust cleanup logic for both traditional levels and Custom Level Loader instances
            if (transform.parent != null && transform.parent.CompareTag("Collectibles"))
            {
                // Traditional level setup
                Destroy(transform.parent.gameObject); 
            }
            else if (gameObject.CompareTag("LevelObject"))
            {
                // Custom loader base element setup
                Destroy(gameObject);
            }
            else if (transform.parent != null && transform.parent.CompareTag("LevelObject"))
            {
                // Custom loader nested child setup (destroys the whole spawned element container)
                Destroy(transform.parent.gameObject);
            }
            else
            {
                // Safe absolute fallback: Destroy the local transform hierarchy root 
                // so no orphaned floating visual effects/circles stay in the air.
                Debug.LogWarning($"ItemCollision: Cleared object via hierarchy root fallback for {gameObject.name}");
                Destroy(transform.root.gameObject == other.transform.root.gameObject ? gameObject : transform.root.gameObject);
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPickupSound(); // Play pickup sound
            }
            else
            {
                Debug.LogWarning("SoundManager instance not found, cannot play pickup sound");
            }
        }
    }
}