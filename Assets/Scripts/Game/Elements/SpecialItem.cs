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
    public event Action<string> OnSpecialItemEffect;

    private void Awake()
    {
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

        ItemCollision.OnItemCollected += HandleItemCollected;
    }

    private void OnDestroy()
    {
        ItemCollision.OnItemCollected -= HandleItemCollected;
    }

    private void HandleItemCollected(string itemType)
    {
        if (itemType == "Special" && levelManager.CurrentLevelState == LevelManager.LevelState.Playing)
        {
            levelManager.AddScore(50); // Add +50 for every Special Item
            string baseMessage = "Special Item: +50 Score";
            bool hasDogs = levelManager.CheckIfLevelHasDog();
            int effect = hasDogs ? UnityEngine.Random.Range(0, 5) : UnityEngine.Random.Range(0, 3);
            //int effect = 2; // For testing purpose, change to any case that needs to be tested
            switch (effect)
            {
                case 0: // Score Bonus
                    int baseAmount = UnityEngine.Random.Range(5, 11);
                    int scoreAmount = baseAmount * 10; // Score is always a multiple of 10
                    levelManager.AddScore(scoreAmount);
                    OnSpecialItemEffect?.Invoke($"{baseMessage}, You got {scoreAmount} score bonus!");
                    Debug.Log($"Special Item: Added {scoreAmount} score");
                    break;
                case 1: // Time Reduction
                    timer.ReduceTime(minusTimeAmount);
                    OnSpecialItemEffect?.Invoke($"{baseMessage}, Time reduced by {minusTimeAmount} seconds!");
                    Debug.Log($"Special Item: Reduced time by {minusTimeAmount} seconds");
                    break;
                case 2: // Goal Hint Reveal
                    GoalLocationMarker goalMarker = FindAnyObjectByType<GoalLocationMarker>();
                    if (goalMarker != null)
                    {
                        goalMarker.ActivateHint();
                        OnSpecialItemEffect?.Invoke($"{baseMessage}, Goal hint revealed!");
                        Debug.Log("Special Item: Goal hint revealed");
                    }
                    else
                    {
                        Debug.LogWarning("Special Item: GoalLocationMarker not found");
                    }
                    break;
                case 3: // Add Bones
                    inventoryManager.AddItem("Bones");
                    OnSpecialItemEffect?.Invoke($"{baseMessage}, You got one bone!");
                    Debug.Log($"Special Item: Added one bone");
                    break;
                case 4: // Shield Extension
                    shieldPowerUp.AddShieldTime();
                    OnSpecialItemEffect?.Invoke($"{baseMessage}, Shield activated!");
                    Debug.Log("Special Item: Extended shield");
                    break;
            }
        }
    }
}