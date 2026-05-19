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

    // Match these to AutoMG3D_1010 settings
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
                    Debug.Log($"File selected: {file.Path}");
                    string json = await FileIO.ReadTextAsync(file);
                    string filePath = file.Path;
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        MazeData mazeData = JsonUtility.FromJson<MazeData>(json);
                        if (mazeData != null)
                        {
                            mazeData.RestoreAfterDeserialization(); // Restore cells array
                            if (validator.CheckSquareMaze(mazeData) && validator.CheckSizeAndCellCount(mazeData))
                            {
                                OnMazeLoaded?.Invoke(mazeData);
                                OnMazeLoadedWithPath?.Invoke(mazeData, filePath);
                                Debug.Log("Maze file loaded successfully.");
                            }
                            else
                            {
                                validator?.ShowWarning("Invalid maze file format or size.");
                                OnMazeLoaded?.Invoke(null);
                                OnMazeLoadedWithPath?.Invoke(null, null);
                            }
                        }
                        else
                        {
                            validator?.ShowWarning("Failed to deserialize maze file.");
                            OnMazeLoaded?.Invoke(null);
                            OnMazeLoadedWithPath?.Invoke(null, null);
                        }
                    }, false);
                }
                else
                {
                    Debug.Log("Load operation canceled by user.");
                    OnMazeLoaded?.Invoke(null);
                    OnMazeLoadedWithPath?.Invoke(null, null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading maze file: {ex.Message}");
                validator?.ShowWarning("Failed to load maze file: " + ex.Message);
                OnMazeLoaded?.Invoke(null);
                OnMazeLoadedWithPath?.Invoke(null, null);
            }
        }, false);
#else
        Debug.LogError("File picker is only supported on UWP.");
        OnMazeLoaded?.Invoke(null);
        OnMazeLoadedWithPath?.Invoke(null, null);
#endif
    }

    public void ExportMazeFile(MazeData mazeData)
    {
        if (mazeData == null)
        {
            validator?.ShowWarning("Cannot export: No maze data!");
            OnMazeExported?.Invoke(false);
            return;
        }

        inputHandler.UpdateMazeDataWithToggles();

        // // IMPROVEMENT: Apply the Wall Texture Algorithm before exporting
        // ApplyWallTextureAlgorithm(mazeData);

        mazeData.PrepareForSerialization();

        // Debug: Log the state of mazeData.cells before serialization
        Debug.Log($"Before export: cells null? {mazeData.cells == null}, rows: {mazeData.rows}, columns: {mazeData.columns}");
        if (mazeData.cells != null)
        {
            Debug.Log($"cells dimensions: {mazeData.cells.GetLength(0)}x{mazeData.cells.GetLength(1)}");
            for (int x = 0; x < mazeData.rows; x++)
            {
                for (int y = 0; y < mazeData.columns; y++)
                {
                    if (mazeData.cells[x, y] != null)
                    {
                        Debug.Log($"Cell[{x},{y}]: WallRight={mazeData.cells[x, y].WallRight}, WallFront={mazeData.cells[x, y].WallFront}, WallLeft={mazeData.cells[x, y].WallLeft}, WallBack={mazeData.cells[x, y].WallBack}, IsStart={mazeData.cells[x, y].IsStart}, IsGoal={mazeData.cells[x, y].IsGoal}");
                    }
                    else
                    {
                        Debug.LogWarning($"Cell[{x},{y}] is null!");
                    }
                }
            }
        }

        // Prepare cells for serialization
        // mazeData.PrepareForSerialization();

#if ENABLE_WINMD_SUPPORT
    Debug.Log("Opening file picker for saving maze...");
    UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
    {
        try
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Maze File", new[] { ".json" });
            string timestamp = DateTime.Now.ToString("ddMMyy_HHmmss");
            savePicker.SuggestedFileName = $"maze{timestamp}";
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                Debug.Log($"File selected: {file.Path}");
                string json = JsonUtility.ToJson(mazeData, true);
                Debug.Log($"Serialized JSON:\n{json}");
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
            Debug.Log($"Export operation failed or canceled: {ex.Message}");
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                Debug.Log("Invoking OnMazeExported with false for error.");
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