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
        if (defaultLevelButton != null)
            defaultLevelButton.onClick.AddListener(LoadDefaultLevelSelect);
        else
            Debug.LogWarning("Default level button not assigned");

        if (customLevelButton != null)
            customLevelButton.onClick.AddListener(LoadCustomLevelSelect);
        else
            Debug.LogWarning("Custom level button not assigned");
        if (randomLevelButton != null)
            randomLevelButton.onClick.AddListener(LoadRandomLevelSelect);
        else
            Debug.LogWarning("Random level button not assigned");
        if (loadLevelButton != null)
            loadLevelButton.onClick.AddListener(LoadMaze);
        else
            Debug.LogWarning("Random level button not assigned");
    }
    public void LoadDefaultLevelSelect()
    {
        SceneManager.LoadSceneAsync("DefaultLevelSelect");
    }

    public void LoadCustomLevelSelect()
    {
        SceneManager.LoadSceneAsync("CustomLevelSelect");
    }

    public void LoadRandomLevelSelect()
    {
        SceneManager.LoadSceneAsync("RandomLevelSelect");
    }

    // Change to async void so we can await the file picker
    public void LoadMaze()
    {
        #if UNITY_EDITOR
        // Editor is synchronous, so this works fine as-is
        string path = UnityEditor.EditorUtility.OpenFilePanel("Select Maze Save File", "", "json");
        if (!string.IsNullOrEmpty(path))
        {
            string json = File.ReadAllText(path);
            ProcessAndLoad(json);
        }

        #elif ENABLE_WINMD_SUPPORT
        // UWP is Asynchronous - we must wait for the UI thread
        LoadFileUWP();

        #else
        // Fallback for standard builds
        string fallbackPath = Path.Combine(Application.persistentDataPath, "maze_save.json");
        if (File.Exists(fallbackPath))
        {
            ProcessAndLoad(File.ReadAllText(fallbackPath));
        }
        #endif
    }

    // A helper method to keep the code clean
    private void ProcessAndLoad(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        SaveMazeData data = JsonUtility.FromJson<SaveMazeData>(json);
        
        // Set the static data for the next scene to read
        MazeSaveHolder.LoadedData = data;
        MazeSaveHolder.HasLoadedData = true;

        Debug.Log("Success! Loading scene...");
        SceneManager.LoadScene("RandomLevel");
    }

    #if ENABLE_WINMD_SUPPORT
    private void LoadFileUWP()
    {
        // 1. Move to Windows UI Thread to show the Picker
        UnityEngine.WSA.Application.InvokeOnUIThread(async () => 
        {
            try 
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".json");

                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    string json = await FileIO.ReadTextAsync(file);
                    
                    // 2. CRITICAL: Move BACK to the Unity App Thread to load the scene
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
