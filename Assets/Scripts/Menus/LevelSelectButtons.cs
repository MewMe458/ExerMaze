using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Pickers;
using System;
using System.Threading.Tasks;
#endif

public class LevelSelectButtons : MonoBehaviour
{
    [SerializeField] private Button defaultLevelButton;
    [SerializeField] private Button customLevelButton;
    [SerializeField] private Button randomLevelButton;
    [SerializeField] private Button loadLevelButton;

    void Awake()
    {
        if (defaultLevelButton != null) defaultLevelButton.onClick.AddListener(LoadDefaultLevelSelect);
        if (customLevelButton != null) customLevelButton.onClick.AddListener(LoadCustomLevelSelect);
        if (randomLevelButton != null) randomLevelButton.onClick.AddListener(LoadRandomLevelSelect);
        if (loadLevelButton != null) loadLevelButton.onClick.AddListener(LoadMaze);
    }
    
    public void LoadDefaultLevelSelect() => SceneManager.LoadSceneAsync("DefaultLevelSelect");
    public void LoadCustomLevelSelect() => SceneManager.LoadSceneAsync("CustomLevelSelect");
    public void LoadRandomLevelSelect() => SceneManager.LoadSceneAsync("ShapeMazeSelect");

    public void LoadMaze()
    {
        #if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Select Maze Save File", "", "fitmazesaved");
        if (!string.IsNullOrEmpty(path))
        {
            string json = File.ReadAllText(path);
            ProcessAndLoad(json);
        }
        #elif ENABLE_WINMD_SUPPORT
        LoadFileUWP();
        #endif
    }

    private void ProcessAndLoad(string json)
    {
        SaveMazeData data = JsonUtility.FromJson<SaveMazeData>(json);
        if (data == null)
        {
            Debug.LogError("Failed to parse save file data.");
            return;
        }

        MazeSaveHolder.LoadedData = data;
        MazeSaveHolder.HasLoadedData = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetMazeSize(data.width, data.depth);
            
            // Restore saved shape back into GameManager context
            if (!string.IsNullOrEmpty(data.mazeShape))
            {
                GameManager.Instance.SetMazeShapeFromString(data.mazeShape);
            }

            if (!string.IsNullOrEmpty(data.levelName))
                GameManager.Instance.SetCurrentLevelName(data.levelName);
            else
                GameManager.Instance.ClearCurrentLevelName();

            if (!string.IsNullOrEmpty(data.customLevelPath))
                GameManager.Instance.CurrentCustomLevelPath = data.customLevelPath;
            else
                GameManager.Instance.ClearCurrentCustomLevelPath();

            GameManager.Instance.IsContinuingSession = data.isContinuingSession;

            if (data.customLevelQueue != null)
            {
                GameManager.Instance.CustomLevelQueue = new System.Collections.Generic.List<string>(data.customLevelQueue);
                GameManager.Instance.CurrentCustomLevelIndex = data.currentCustomLevelIndex;
            }
        }

        string targetScene = !string.IsNullOrEmpty(data.sceneName) ? data.sceneName : "RandomLevel";

        Debug.Log($"Success! Loading scene context: {targetScene}...");
        SceneManager.LoadScene(targetScene);
    }

    #if ENABLE_WINMD_SUPPORT
    private void LoadFileUWP()
    {
        // UWP File Picker Code remains completely identical
        UnityEngine.WSA.Application.InvokeOnUIThread(async () => 
        {
            try 
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".fitmazesaved");

                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    string json = await FileIO.ReadTextAsync(file);
                    UnityEngine.WSA.Application.InvokeOnAppThread(() => 
                    {
                        ProcessAndLoad(json);
                    }, false);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.WSA.Application.InvokeOnAppThread(() => {
                    Debug.LogError("UWP Load Error: " + ex.Message);
                }, false);
            }
        }, false);
    }
    #endif
}