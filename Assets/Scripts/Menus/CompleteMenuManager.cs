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

        LevelManager levelManager = FindFirstObjectByType<LevelManager>();

        if (levelManager != null && finalScoreText != null)
        {
            finalScoreText.text = levelManager.GetFinalScore().ToString();
        }
        else
        {
            Debug.LogWarning("CompleteMenuManager: LevelManager or finalScoreText not found");
            if (finalScoreText != null) finalScoreText.text = "N/A";
        }

        if (levelManager != null && finalTimeText != null)
        {
            float elapsedTime = levelManager.GetFinalTime();
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
        else
        {
            Debug.LogWarning("CompleteMenuManager: LevelManager or finalTimeText not found");
            if (finalTimeText != null) finalTimeText.text = "N/A";
        }

        if (levelManager != null && finalScoreText != null)
        {
            int finalSteps = levelManager.GetFinalStepCount();
            finalStepCountText.text = finalSteps.ToString();
        }
        else
        {
            Debug.LogWarning("CompleteMenuManager: LevelManager or finalScoreText not found");
            if (finalTimeText != null) finalScoreText.text = "N/A";
        }
    }
}
