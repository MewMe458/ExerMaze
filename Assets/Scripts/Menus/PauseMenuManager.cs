using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Pickers;
using System;
using System.Threading.Tasks;
#endif

public class PauseMenuManager : BaseMenuManager
{
    protected override string GetMenuSceneName()
    {
        return "PauseMenu";
    }

    public void OnResumeButtonPressed()
    {
        if (levelManager != null)
        {
            levelManager.ResumeGame();
        }
        else
        {
            SceneManager.UnloadSceneAsync("PauseMenu"); // Fallback if LevelManager is not found
            Time.timeScale = 1f; // Resume time
            BLEManager.Instance?.bleConnect?.UpdateSensorStateOnBLE("start");
        }
    }

    public async void SaveMazeDetails()
    {
        Debug.Log("Save Button Clicked!");
        if (AutoMG3D_1010.Instance == null)
        {
            Debug.LogError("PauseMenuManager: Could not find the Maze Instance! Is the RandomLevel scene loaded?");
            return;
        }

        SaveMazeData data = AutoMG3D_1010.Instance.GetMazeSaveData();
        string json = JsonUtility.ToJson(data, true);
        
        #if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.SaveFilePanel("Save Maze Details", "", "maze_save.json", "json");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);
            Debug.Log("Editor: Maze saved to " + path);
        }
        #elif ENABLE_WINMD_SUPPORT
        await SaveFileUWP(json);
        #endif
    }

#if ENABLE_WINMD_SUPPORT
    private async Task SaveFileUWP(string content)
    {
        // We move everything inside the UI Thread block to ensure the Picker 
        // is created on the same thread that displays it.
        UnityEngine.WSA.Application.InvokeOnUIThread(async () => 
        {
            try 
            {
                FileSavePicker savePicker = new FileSavePicker();

                // Get the window handle for the current view
                // This is required for the picker to know which window to 'pop up' over.
                var window = Windows.UI.Core.CoreWindow.GetForCurrentThread();
                
                if (window != null)
                {
                    savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                    savePicker.FileTypeChoices.Add("JSON File", new List<string>() { ".json" });
                    savePicker.SuggestedFileName = "maze_save";

                    // Open the picker
                    StorageFile file = await savePicker.PickSaveFileAsync();

                    if (file != null)
                    {
                        // 1. Write the content to the file
                        await FileIO.WriteTextAsync(file, content);

                        // 2. Get the full directory path
                        string savedPath = file.Path;

                        // 3. Output to the console as requested (Red Error icon)
                        Debug.LogError("File saved in " + savedPath);
                    }
                    else
                    {
                        Debug.LogError("Save operation cancelled by user.");
                    }
                }
                else
                {
                    Debug.LogError("UWP Save Error: Could not find the active UI Window.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("UWP Save Error: " + ex.Message);
            }
        }, false);

        // We return a completed task because the logic is handled inside the InvokeOnUIThread
        await Task.CompletedTask;
    }
#endif
}
