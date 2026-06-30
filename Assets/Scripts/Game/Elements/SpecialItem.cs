using UnityEngine;
using System;

public class SpecialItem : MonoBehaviour
{
    private LevelManager levelManager;
    private InventoryManager inventoryManager;
    private ShieldPowerUp shieldPowerUp;
    private LevelUIManager uiManager;
    private Timer timer;

    [SerializeField] private float minusTimeAmount = 10f;

    private bool isCollected = false; 

    private void Awake()
    {
        // Safety Check: Ensure the collider is actually a trigger to prevent physics glitches
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"SpecialItem on {gameObject.name} does NOT have 'Is Trigger' enabled! Fixing it automatically.", gameObject);
            col.isTrigger = true;
        }

        // Safety Check: If there's a Rigidbody, make it kinematic so physics forces don't launch the player
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        levelManager = FindAnyObjectByType<LevelManager>();
        if (levelManager == null) Debug.LogError("SpecialItem: LevelManager not found");

        inventoryManager = FindAnyObjectByType<InventoryManager>();
        if (inventoryManager == null) Debug.LogError("SpecialItem: InventoryManager not found");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        shieldPowerUp = player?.GetComponent<ShieldPowerUp>();
        if (shieldPowerUp == null) Debug.LogError("SpecialItem: ShieldPowerUp not found on player");

        uiManager = FindAnyObjectByType<LevelUIManager>();
        if (uiManager == null) Debug.LogError("SpecialItem: LevelUIManager not found");

        timer = FindAnyObjectByType<Timer>();
        if (timer == null) Debug.LogError("SpecialItem: Timer not found");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            isCollected = true;
            HandleItemCollected();
        }
    }

    private void HandleItemCollected()
    {
        // Safety check to ensure levelManager isn't null before checking state
        if (levelManager != null && levelManager.CurrentLevelState == LevelManager.LevelState.Playing)
        {
            levelManager.AddScore(50); 
            string baseMessage = "Special Item: +50 Score";
            bool hasDogs = levelManager.CheckIfLevelHasDog();
            int effect = hasDogs ? UnityEngine.Random.Range(0, 5) : UnityEngine.Random.Range(0, 3);
            
            string effectMessage = baseMessage;

            switch (effect)
            {
                case 0: 
                    int baseAmount = UnityEngine.Random.Range(5, 11);
                    int scoreAmount = baseAmount * 10; 
                    levelManager.AddScore(scoreAmount);
                    effectMessage = $"{baseMessage}, You got {scoreAmount} score bonus!";
                    Debug.Log($"Special Item: Added {scoreAmount} score");
                    break;
                case 1: 
                    if (timer != null) timer.ReduceTime(minusTimeAmount);
                    effectMessage = $"{baseMessage}, Time reduced by {minusTimeAmount} seconds!";
                    Debug.Log($"Special Item: Reduced time by {minusTimeAmount} seconds");
                    break;
                case 2: 
                    GoalLocationMarker goalMarker = FindAnyObjectByType<GoalLocationMarker>();
                    if (goalMarker != null)
                    {
                        goalMarker.ActivateHint();
                        effectMessage = $"{baseMessage}, Goal hint revealed!";
                        Debug.Log("Special Item: Goal hint revealed");
                    }
                    else
                    {
                        Debug.LogWarning("Special Item: GoalLocationMarker not found");
                    }
                    break;
                case 3: 
                    if (inventoryManager != null) inventoryManager.AddItem("Bones");
                    effectMessage = $"{baseMessage}, You got one bone!";
                    Debug.Log($"Special Item: Added one bone");
                    break;
                case 4: 
                    if (shieldPowerUp != null) shieldPowerUp.AddShieldTime();
                    effectMessage = $"{baseMessage}, Shield activated!";
                    Debug.Log("Special Item: Extended shield");
                    break;
            }

            // Direct Call completely eliminates the registration/timing issues
            if (uiManager != null)
            {
                uiManager.ShowLevelMessage(effectMessage);
            }

            Destroy(gameObject);
        }
    }
}