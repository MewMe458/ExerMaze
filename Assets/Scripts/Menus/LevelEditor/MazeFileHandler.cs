using UnityEngine;
using System;
using System.Collections.Generic;
#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Pickers;
#endif

public class MazeFileHandler : MonoBehaviour
{
    [SerializeField] private MazeInputHandler inputHandler;
    [SerializeField] private MazeValidator validator;

    [Header("Texture Matching Settings")]
    [SerializeField] private Material[] wallMaterials;
    [SerializeField] private int wallRegionSize = 6;

    public delegate void MazeLoadedHandler(MazeData mazeData);
    public event MazeLoadedHandler OnMazeLoaded;

    public delegate void MazeLoadedWithPathHandler(MazeData mazeData, string filePath);
    public event MazeLoadedWithPathHandler OnMazeLoadedWithPath;

    public delegate void MazeExportedHandler(bool success);
    public event MazeExportedHandler OnMazeExported;

    void Start()
    {
        if (validator == null) Debug.LogError("Maze Validator not assigned!");
    }

    public void LoadMazeFile()
    {
#if ENABLE_WINMD_SUPPORT
        Debug.Log("Opening file picker for loading maze...");
        UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
        {
            try
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".json");
                StorageFile file = await openPicker.PickSingleFileAsync();

                if (file != null)
                {
                    Debug.Log($"File selected for load: {file.Path}");
                    string fileContent = await FileIO.ReadTextAsync(file);
                    
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        try
                        {
                            MazeData loadedMazeData = JsonUtility.FromJson<MazeData>(fileContent);
                            if (loadedMazeData != null)
                            {
                                loadedMazeData.RestoreAfterDeserialization();
                                OnMazeLoaded?.Invoke(loadedMazeData);
                                OnMazeLoadedWithPath?.Invoke(loadedMazeData, file.Path);
                            }
                        }
                        catch (Exception innerEx)
                        {
                            Debug.LogError($"Error parsing or processing loaded JSON: {innerEx.Message}");
                        }
                    }, false);
                }
                else
                {
                    Debug.Log("Load operation canceled by user.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to pick or read file: {ex.Message}");
            }
        }, false);
#else
        Debug.LogError("File open picker is only supported on UWP platforms.");
#endif
    }

    public void ExportMazeFile(MazeData mazeData)
    {
        if (mazeData == null)
        {
            Debug.LogError("Cannot export null maze data.");
            OnMazeExported?.Invoke(false);
            return;
        }

        // Fix: Removed deprecated inputHandler.UpdateMazeDataWithToggles() call since toggle tracking is reactive.

        var validationResult = validator.ValidateMaze(mazeData, false, false);
        if (!validationResult.success)
        {
            Debug.LogWarning("Exporting an invalid/unsolvable maze layout.");
        }

#if ENABLE_WINMD_SUPPORT
        Debug.Log("Opening file picker for saving maze...");
        mazeData.PrepareForSerialization();

        UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
        {
            try
            {
                FileSavePicker savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("JSON File", new List<string>() { ".json" });
                
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                savePicker.SuggestedFileName = $"maze_{timestamp}";
                
                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    Debug.Log($"File selected: {file.Path}");
                    string json = JsonUtility.ToJson(mazeData, true);
                    await FileIO.WriteTextAsync(file, json);
                    
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        OnMazeExported?.Invoke(true);
                        Debug.Log("Maze file successfully exported.");
                    }, false);
                }
                else
                {
                    Debug.Log("Export operation canceled by user.");
                    OnMazeExported?.Invoke(false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Export operation failed: {ex.Message}");
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    OnMazeExported?.Invoke(false);
                }, false);
            }
            finally
            {
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    mazeData.RestoreAfterDeserialization();
                }, false);
            }
        }, false);
#else
        Debug.LogError("File picker is only supported on UWP.");
        OnMazeExported?.Invoke(false);
#endif
    }
}