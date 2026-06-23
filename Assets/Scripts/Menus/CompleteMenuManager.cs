using UnityEngine;
using TMPro;

public class CompleteMenuManager : BaseMenuManager
{
    public TMP_Text finalScoreText;
    public TMP_Text finalTimeText;
    public TMP_Text finalStepCountText;

    protected override string GetMenuSceneName()
    {
        return "LevelCompleteMenu";
    }

    protected override void Start()
    {
        base.Start();
        
        if (finalScoreText == null || finalTimeText == null || finalStepCountText == null)
        {
            Debug.LogError("CompleteMenuManager: One or more UI Text components are not assigned!");
            return;
        }

        // 1. Fetch Accumulated Score
        if (finalScoreText != null)
        {
            finalScoreText.text = GPXDataPersistence.TotalScoreAccumulated.ToString();
        }

        // 2. Fetch Accumulated Time
        if (finalTimeText != null)
        {
            float elapsedTime = GameManager.Instance != null ? GameManager.Instance.AccumulatedTime : 0f;
            
            int hours = Mathf.FloorToInt(elapsedTime / 3600f);
            int minutes = Mathf.FloorToInt((elapsedTime % 3600f) / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            
            if (hours >= 1)
            {
                finalTimeText.text = string.Format("{0}:{1:00}:{2:00}", hours, minutes, seconds);
            }
            else
            {
                finalTimeText.text = string.Format("{0}:{1:00}", minutes, seconds);
            }
        }

        // 3. Fetch Accumulated Steps
        if (finalStepCountText != null)
        {
            int finalSteps = GameManager.Instance != null ? GameManager.Instance.AccumulatedSteps : 0;
            finalStepCountText.text = finalSteps.ToString();
        }
    }
}