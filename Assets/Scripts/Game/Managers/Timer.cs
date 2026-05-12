using UnityEngine;

public class Timer : MonoBehaviour
{
    private float elapsedTime = 0f;
    private bool isTimerActive = false;
    private bool isTimerRunning = false;

    private void Update()
    {
        isTimerRunning = CanTimerRun();
        if (isTimerRunning)
        {
            elapsedTime += Time.deltaTime;
        }
    }

    private bool CanTimerRun()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.InGame)
            return false;
        return isTimerActive;
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
    }

    public void StartTimer()
    {
        isTimerActive = true;
    }

    public void StopTimer()
    {
        isTimerActive = false;
    }

    public float GetElapsedTime()
    {
        return elapsedTime;
    }

    public void ReduceTime(float amount)
    {
        if (amount < 0f)
        {
            Debug.LogWarning("Timer: Attempted to reduce time by negative amount, ignoring");
            return;
        }
        if (elapsedTime < amount)
        {
            elapsedTime = 0f;
            Debug.Log("Timer: Time set to 0 as elapsed time was less than reduction amount");
        }
        else
        {
            elapsedTime -= amount;
            Debug.Log($"Timer: Reduced time by {amount} seconds, new time: {elapsedTime}");
        }
    }
}