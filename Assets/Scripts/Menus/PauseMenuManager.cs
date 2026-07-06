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
            SceneManager.UnloadSceneAsync("PauseMenu"); 
            Time.timeScale = 1f; 
            BLEManager.Instance?.bleConnect?.UpdateSensorStateOnBLE("start");
        }
    }

    public async void SaveMazeDetails()
    {
        Debug.Log("Save Button Clicked!");
        string activeScene = SceneManager.GetActiveScene().name;

        if (activeScene != "DefaultLevel" && activeScene != "CustomLevel" && activeScene != "RandomLevel")
        {
            Debug.LogError("PauseMenuManager: Not in a valid maze scene to save.");
            return;
        }

        SaveMazeData data;

        if (AutoMG3D_1010.Instance != null)
        {
            data = AutoMG3D_1010.Instance.GetMazeSaveData();
        }
        else
        {
            data = CaptureGlobalMazeData();
        }

        // Store scene context and level identifiers
        data.sceneName = activeScene;
        if (GameManager.Instance != null)
        {
            data.levelName = GameManager.Instance.CurrentLevelName;
            data.customLevelPath = GameManager.Instance.CurrentCustomLevelPath;
            
            // Capture Random Level Continuous State
            data.isContinuingSession = GameManager.Instance.IsContinuingSession;

            // Capture Custom Level Campaign State (Assuming GameManager has these exposed)
            // If your GameManager uses slightly different names for the queue/index, update them here.
            if (GameManager.Instance.CustomLevelQueue != null)
            {
                data.customLevelQueue = new List<string>(GameManager.Instance.CustomLevelQueue);
                data.currentCustomLevelIndex = GameManager.Instance.CurrentCustomLevelIndex;
            }
        }

        string json = JsonUtility.ToJson(data, true);
        
        #if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.SaveFilePanel("Save Maze Details", "", "maze_save.fitmazesaved", "fitmazesaved");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);
            Debug.Log("Editor: Maze saved to " + path);
        }
        #elif ENABLE_WINMD_SUPPORT
        await SaveFileUWP(json);
        #endif
    }

    private SaveMazeData CaptureGlobalMazeData()
    {
        SaveMazeData data = new SaveMazeData();
        if (GameManager.Instance != null)
        {
            data.width = GameManager.Instance.MazeWidth;
            data.depth = GameManager.Instance.MazeDepth;
        }

        CaptureObjects(data.walls, "Wall");
        CaptureObjects(data.npcs, "NPC");
        CaptureObjects(data.collectibles, "Collectibles");
        CaptureObjects(data.endGoal, "MazeGoal");

        GameObject floorObj = GameObject.Find("Maze Floor");
        if (floorObj != null)
        {
            data.floor = new ObjectData
            {
                type = "Floor",
                position = new SerializableVector3(floorObj.transform.position),
                rotation = new SerializableVector3(floorObj.transform.eulerAngles),
                scale = new SerializableVector3(floorObj.transform.localScale)
            };
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            data.playerData = new ObjectData
            {
                type = "Player",
                position = new SerializableVector3(playerObj.transform.position),
                rotation = new SerializableVector3(playerObj.transform.eulerAngles),
                scale = new SerializableVector3(playerObj.transform.localScale)
            };
        }

        return data;
    }

    private void CaptureObjects(List<ObjectData> list, string tag)
    {
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag(tag))
        {
            int matIdx = -1; 
            list.Add(new ObjectData
            {
                type = obj.name.Replace("(Clone)", "").Trim(),
                position = new SerializableVector3(obj.transform.position),
                rotation = new SerializableVector3(obj.transform.eulerAngles),
                scale = new SerializableVector3(obj.transform.localScale),
                materialIndex = matIdx
            });
        }
    }

#if ENABLE_WINMD_SUPPORT
    private async Task SaveFileUWP(string content)
    {
        UnityEngine.WSA.Application.InvokeOnUIThread(async () => 
        {
            try 
            {
                FileSavePicker savePicker = new FileSavePicker();
                var window = Windows.UI.Core.CoreWindow.GetForCurrentThread();
                
                if (window != null)
                {
                    savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                    savePicker.FileTypeChoices.Add("FitMaze Saved File", new List<string>() { ".fitmazesaved" });
                    savePicker.SuggestedFileName = "maze_save";

                    StorageFile file = await savePicker.PickSaveFileAsync();

                    if (file != null)
                    {
                        await FileIO.WriteTextAsync(file, content);
                        string savedPath = file.Path;
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
        await Task.CompletedTask;
    }
#endif
}