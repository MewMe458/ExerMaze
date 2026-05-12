using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Pickers;
using System;
using System.Threading.Tasks;
#endif

public class RandomLevelSelect : MonoBehaviour
{
    [SerializeField] private Button level12x12Button;
    [SerializeField] private Button level24x24Button;
    [SerializeField] private Button level36x36Button;
    [SerializeField] private Button customRandomLevelButton;

    public string levelName;

    void Awake()
    {
        if (level12x12Button != null)
            level12x12Button.onClick.AddListener(() => SelectLevel(12, 12));
        else
            Debug.LogWarning("Level 12x12 button not assigned");

        if (level24x24Button != null)
            level24x24Button.onClick.AddListener(() => SelectLevel(24, 24));
        else
            Debug.LogWarning("Level 24x24 button not assigned");

        if (level36x36Button != null)
            level36x36Button.onClick.AddListener(() => SelectLevel(36, 36));
        else
            Debug.LogWarning("Level 36x36 button not assigned");
        if (customRandomLevelButton != null)
        {
            customRandomLevelButton.onClick.AddListener(() => ToCustomRandomLevelSelectScene());
        }
        else
            Debug.LogWarning("Custom Random Level button not assigned");
    }

    private void SelectLevel(int width, int depth)
    {
        GameManager.Instance.SetMazeSize(width, depth);
        SceneManager.LoadScene("RandomLevel");
    }

    private void ToCustomRandomLevelSelectScene()
    {
        SceneManager.LoadSceneAsync("CustomRandomLevelSelect");
    }
}