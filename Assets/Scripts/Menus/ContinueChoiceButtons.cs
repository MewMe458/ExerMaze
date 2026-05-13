using UnityEngine;
using UnityEngine.SceneManagement;

public class ContinueChoice : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClickContinue()
    {
        GPXDataPersistence.IsContinuingSession = true;
        Time.timeScale = 1f;

        // ADD THESE LINES: Clear the custom level references in GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearCurrentLevelName();        // [cite: 92]
            GameManager.Instance.ClearCurrentCustomLevelPath();  // [cite: 93]
        }

        // Now load the RandomLevel scene
        SceneManager.LoadScene("RandomLevel"); // [cite: 77]
    }

    public void OnClickStop()
    {
        // Finally show the summary screen
        SceneManager.UnloadSceneAsync("ContinueChoiceScene");
        SceneManager.LoadScene("LevelCompleteMenu", LoadSceneMode.Additive); 
    }
}
