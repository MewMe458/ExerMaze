using UnityEngine;

public class KeyNPC : NPCBase
{
    //private InventoryManager inventoryManager;
    //private bool isInteracting;

    //protected override void Start()
    //{
    //    base.Start();
    //    if (string.IsNullOrEmpty(currentState))
    //    {
    //        currentState = "NoFood";
    //        Debug.Log("KeyNPC: Initialized state to NoFood");
    //    }
    //    inventoryManager = FindAnyObjectByType<InventoryManager>();
    //    if (inventoryManager == null)
    //    {
    //        Debug.LogError("KeyNPC: InventoryManager not found", gameObject);
    //    }
    //}

    //public override string GetState()
    //{
    //    Debug.Log($"KeyNPC: Returning state {currentState}, Food: {inventoryManager?.HasItem("Food", 1)}");
    //    return currentState;
    //}

    //protected override void StartDialogue()
    //{
    //    if (isInteracting)
    //    {
    //        Debug.Log("KeyNPC: Already interacting, ignoring StartDialogue");
    //        return;
    //    }
    //    isInteracting = true;

    //    Debug.Log($"KeyNPC: StartDialogue, currentState: {currentState}, Food: {inventoryManager?.HasItem("Food", 1)}");

    //    if (currentState == "NoFood" && inventoryManager != null && inventoryManager.HasItem("Food", 1))
    //    {
    //        currentState = "HasFood";
    //        Debug.Log("KeyNPC: Updated state to HasFood");
    //    }

    //    if (currentState == "HasFood")
    //    {
    //        if (inventoryManager != null)
    //        {
    //            inventoryManager.RemoveItem("Food", 1);
    //            inventoryManager.AddItem("Key");
    //            base.StartDialogue(); // Show HasFood dialogue
    //            SetState("Traded");
    //            Debug.Log("KeyNPC: Traded Food for Key, set state to Traded");
    //            isInteracting = false;
    //            return;
    //        }
    //    }

    //    base.StartDialogue(); // Show NoFood or Traded dialogue
    //    isInteracting = false;
    //}
}