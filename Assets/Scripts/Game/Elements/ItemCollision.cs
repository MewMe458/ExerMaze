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

        // Double-check Collider configuration
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"ItemCollision on {gameObject.name} does NOT have 'Is Trigger' enabled! Fixing it.", gameObject);
            col.isTrigger = true;
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

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPickupSound(); // Play pickup sound
            }
            else
            {
                Debug.LogWarning("SoundManager instance not found, cannot play pickup sound");
            }

            // 🛠️ FIXED CLEANUP LOGIC: Prevents destroying the whole maze/root environment
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
                // Custom loader nested child setup
                Destroy(transform.parent.gameObject);
            }
            else
            {
                // SAFE FALLBACK: Only destroy this specific item, never the transform.root!
                Debug.Log($"ItemCollision: Safely destroying individual item instance: {gameObject.name}");
                Destroy(gameObject);
            }
        }
    }
}