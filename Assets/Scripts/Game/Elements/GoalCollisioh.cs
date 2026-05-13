using System;
using UnityEngine;

public class GoalCollision : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LevelManager levelManager = FindAnyObjectByType<LevelManager>();
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
}