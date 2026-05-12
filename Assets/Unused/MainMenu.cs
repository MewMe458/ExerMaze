using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void LoadPlayScene()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void LoadSettingScene()
    {

    }
}
