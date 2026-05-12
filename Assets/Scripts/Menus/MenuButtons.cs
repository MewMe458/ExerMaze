using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtons : MonoBehaviour
{
    public void PlayButton()
    {
        SceneManager.LoadScene("LevelSelectMenu");
        Debug.Log("PlayButton");
    }

    public void AboutButton()
    {
        SceneManager.LoadScene("AboutScene");
        Debug.Log("AboutButton");
    }

    public void SettingsButton()
    {
        SceneManager.LoadScene("SettingsScene");
        Debug.Log("SettingsButton");
    }

    public void EditorButton()
    {
        SceneManager.LoadScene("LevelEditor");
        Debug.Log("EditorButton");
    }

    public void QuitButton()
    {
        BLEManager.Instance?.bleConnect?.Disconnect();
        SoundManager.Instance.StopBGM();
        Application.Quit();
        Debug.Log("QuitButton");
    }
}
