using System;
using UnityEngine;

public class GoalCollision : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LevelManager levelManager = FindAnyObjectByType<LevelManager>();
            //InventoryManager inventoryManager = FindAnyObjectByType<InventoryManager>();
            //if (levelManager == null || inventoryManager == null) return;
            //if (inventoryManager.HasItem("Key", 1))
            //{
            //    levelManager.CompleteLevel();
            //}
            //else
            //{
            //    levelManager.ShowGoalMessage("You need a key!");
            //}
            if (levelManager != null)
            {
                levelManager.CompleteLevel();
            }
            else
            {
                Debug.LogWarning("LevelManager not found in the scene.");
            }
        }
    }
    //private void OnTriggerExit(Collider other)
    //{
    //    if (other.CompareTag("Player"))
    //    {
    //        LevelManager levelManager = FindAnyObjectByType<LevelManager>();
    //        if (levelManager != null)
    //            levelManager.HideGoalMessage();
    //    }
    //}
}