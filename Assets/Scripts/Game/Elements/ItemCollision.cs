using UnityEngine;
using System;

public class ItemCollision : MonoBehaviour
{
    [SerializeField] private string itemType; // Set in Unity Editor, e.g., "SpeedBoost", "Key"

    // Event triggered when an item is collected
    public static event Action<string> OnItemCollected;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

            if (transform.parent != null && transform.parent.CompareTag("Collectibles"))
            {
                Destroy(transform.parent.gameObject); // Destroy parent GameObject if exist and has the correct tag
            }
            else
            {
                Debug.LogWarning($"ItemCollision: No parent found for {gameObject.name}, destroying self", gameObject);
                Destroy(gameObject); // Fallback to destroy self
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