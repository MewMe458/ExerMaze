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
        // Reloads the random level scene with existing GameManager dimensions
        SceneManager.LoadScene("RandomLevel"); 
    }

    public void OnClickStop()
    {
        // Finally show the summary screen
        SceneManager.UnloadSceneAsync("ContinueChoiceScene");
        SceneManager.LoadScene("LevelCompleteMenu", LoadSceneMode.Additive); 
    }
}
