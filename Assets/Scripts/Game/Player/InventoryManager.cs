using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    private Dictionary<string, int> items = new Dictionary<string, int>(); // Tracks item types and counts
    private string[] nonInventoryItems = { "Shield", "Special" }; // Items that should not be added to inventory
    public event Action<string, int> OnItemAdded;
    public event Action<string, int> OnItemRemoved;
    private LevelManager levelManager; // Reference to LevelManager for state checks

    private void Start()
    {
        // Subscribe to item collection event
        ItemCollision.OnItemCollected += AddItem;
        // Find LevelManager for state checks
        levelManager = FindAnyObjectByType<LevelManager>();
        if (levelManager == null)
        {
            Debug.LogError("InventoryManager: LevelManager not found in scene", gameObject);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        ItemCollision.OnItemCollected -= AddItem;
    }

    // Add an item to the inventory
    public void AddItem(string itemType)
    {
        if (levelManager != null && levelManager.CurrentLevelState != LevelManager.LevelState.Playing)
        //    && levelManager.CurrentLevelState != LevelManager.LevelState.Interacting)
        {
            Debug.Log($"Ignoring item {itemType} collection: Level state is {levelManager.CurrentLevelState}");
            return;
        }
        if (nonInventoryItems.Contains(itemType))
        {
            //if (itemType == "Special")
            //{
            //    LevelUIManager uiManager = FindFirstObjectByType<LevelUIManager>();
            //    uiManager?.ShowLevelMessage("Special Item Collected");
            //}
            Debug.Log($"Ignoring item {itemType}: Not added to inventory (filtered)");
            return;
        }
        if (!items.ContainsKey(itemType))
        {
            items[itemType] = 0;
        }
        items[itemType]++;
        Debug.Log($"Added {itemType} to inventory. Count: {items[itemType]}");
        OnItemAdded?.Invoke(itemType, items[itemType]);
    }

    public void RemoveItem(string itemType, int count = 1)
    {
        if (levelManager != null && levelManager.CurrentLevelState != LevelManager.LevelState.Playing)
            //&& levelManager.CurrentLevelState != LevelManager.LevelState.Interacting)
        {
            Debug.Log($"Ignoring item {itemType} removal: Level state is {levelManager.CurrentLevelState}");
            return;
        }
        if (nonInventoryItems.Contains(itemType))
        {
            Debug.Log($"Ignoring item {itemType}: Not added to inventory (filtered)");
            return;
        }
        if (items.ContainsKey(itemType) && items[itemType] >= count)
        {
            items[itemType] -= count;
            int newCount = items[itemType];
            if (items[itemType] == 0)
            {
                items.Remove(itemType);
            }
            Debug.Log($"Removed {count} {itemType} from inventory. New count: {newCount}");
            OnItemRemoved?.Invoke(itemType, newCount);
        }
        else
        {
            Debug.LogWarning($"Cannot remove {count} {itemType}: Insufficient quantity or item not found");
        }
    }

    // Check if the inventory has at least the required count of an item
    public bool HasItem(string itemType, int requiredCount = 1)
    {
        return items.TryGetValue(itemType, out int count) && count >= requiredCount;
    }

    // Get the current count of an item
    public int GetItemCount(string itemType)
    {
        return items.TryGetValue(itemType, out int count) ? count : 0;
    }

    // Clear all items (called when starting a new level)
    public void ClearItems()
    {
        items.Clear();
        Debug.Log("Inventory cleared");
    }
}