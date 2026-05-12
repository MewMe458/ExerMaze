using UnityEngine;
using System.Collections;
using System;

public class ShieldPowerUp : MonoBehaviour
{
    private float shieldDuration = 10f;
    private float remainingTime = 0f;
    private bool isShieldActive = false;

    public event Action OnShieldActivated;
    public event Action<float> OnShieldTick;
    public event Action OnShieldDeactivated;

    private void Awake()
    {
        ItemCollision.OnItemCollected += HandleShieldObtained;
    }

    private void OnDestroy()
    {
        ItemCollision.OnItemCollected -= HandleShieldObtained;
    }

    private void HandleShieldObtained(string itemType)
    {
        if (itemType == "Shield")
        {
            LevelUIManager uiManager = FindAnyObjectByType<LevelUIManager>();
            uiManager?.ShowLevelMessage("Shield activated!");
            AddShieldTime();
        }
    }

    public void AddShieldTime()
    {
        if (!isShieldActive)
        {
            isShieldActive = true;
            remainingTime = shieldDuration;
            OnShieldActivated?.Invoke();
            StartCoroutine(ShieldCountdown());
            Debug.Log("Shield activated");
        }
        else
        {
            remainingTime += shieldDuration;
            Debug.Log($"Shield time extended, remaining: {remainingTime}s");
        }
    }

    public bool IsShieldActive()
    {
        return isShieldActive;
    }

    public void ResetShield()
    {
        isShieldActive = false;
        remainingTime = 0f;
        StopAllCoroutines();
        OnShieldDeactivated?.Invoke();
        Debug.Log("Shield reset");
    }

    private IEnumerator ShieldCountdown()
    {
        while (remainingTime > 0)
        {
            OnShieldTick?.Invoke(remainingTime);
            remainingTime -= 1f;
            yield return new WaitForSeconds(1f);
        }
        isShieldActive = false;
        OnShieldDeactivated?.Invoke();
        Debug.Log("Shield deactivated");
    }
}