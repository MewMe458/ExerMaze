using UnityEngine;
using System.Collections;
using System;

public class SlowPotion : MonoBehaviour
{
    public float slowMultiplier = 0.5f;
    public float slowDuration = 10f;
    public float remainingTime = 0f;
    private bool isSlowActive = false;

    private void Awake()
    {
        ItemCollision.OnItemCollected += HandleSlowPotionObtained;
    }

    private void OnDestroy()
    {
        ItemCollision.OnItemCollected -= HandleSlowPotionObtained;
    }

    private void HandleSlowPotionObtained(string itemType)
    {
        if (itemType == "SlowPotion")
        {
            LevelUIManager uIManager = FindAnyObjectByType<LevelUIManager>();
            uIManager?.ShowLevelMessage("You're now slowed!");
            AddSlowTime();
        }
    }

    private void AddSlowTime()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        CharacterMovement movement = player.GetComponent<CharacterMovement>();
        if (movement == null) return;

        if (!isSlowActive)
        {
            isSlowActive = true;
            remainingTime = slowDuration;
            StartCoroutine(ApplySlow(movement));
        }
        else
        {
            remainingTime += slowDuration;
        }
    }

    private IEnumerator ApplySlow(CharacterMovement movement)
    {
        movement.SetSpeedMultiplier(slowMultiplier);

        while (remainingTime > 0f)
        {
            remainingTime -= 1f;
            yield return new WaitForSeconds(1f);
        }

        movement.ResetSpeedMultiplier();
        isSlowActive = false;
    }
}