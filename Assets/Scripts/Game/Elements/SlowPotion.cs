using UnityEngine;
using System.Collections;
using System;

public class SlowPotion : MonoBehaviour
{
    public float slowMultiplier = 0.5f;
    public float slowDuration = 10f;
    
    // Static state tracks remaining time globally so it survives when individual potion objects are deleted
    private static float remainingTime = 0f;
    private static bool isSlowActive = false;
    private static Coroutine activeSlowCoroutine = null;

    private void Awake()
    {
        // Global listener hook
        ItemCollision.OnItemCollected += HandleSlowPotionObtained;
    }

    private void OnDestroy()
    {
        // Clean up tracking subscription safely
        ItemCollision.OnItemCollected -= HandleSlowPotionObtained;
    }

    private void HandleSlowPotionObtained(string itemType)
    {
        // 🛠️ FIX: Accommodates both exact matches and typos across your systems safely
        if (itemType == "SlowPotion" || itemType == "SlowPotions")
        {
            LevelUIManager uIManager = FindAnyObjectByType<LevelUIManager>();
            if (uIManager != null)
            {
                uIManager.ShowLevelMessage("You're now slowed!");
            }

            AddSlowTime();
        }
    }

    private void AddSlowTime()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("SlowPotion: Player object not found via tag!");
            return;
        }

        CharacterMovement movement = player.GetComponent<CharacterMovement>();
        if (movement == null) return;

        // 🛠️ FIX: Instead of running the coroutine on this potion instance (which gets destroyed), 
        // we run it through a MonoBehavior instance attached to the persistent Player object!
        if (!isSlowActive)
        {
            isSlowActive = true;
            remainingTime = slowDuration;
            activeSlowCoroutine = movement.StartCoroutine(ApplySlow(movement));
        }
        else
        {
            remainingTime += slowDuration;
            Debug.Log($"Slow active! Time extended. Remaining time: {remainingTime}s");
        }
    }

    private IEnumerator ApplySlow(CharacterMovement movement)
    {
        movement.SetSpeedMultiplier(slowMultiplier);
        Debug.Log($"Slow applied! Multiplier set to: {slowMultiplier}");

        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
            yield return null; // Precision countdown frame-by-frame
        }

        // Reset speed states cleanly
        movement.ResetSpeedMultiplier();
        isSlowActive = false;
        remainingTime = 0f;
        activeSlowCoroutine = null;
        Debug.Log("Slow effect worn off. Speed restored to normal.");
    }
}