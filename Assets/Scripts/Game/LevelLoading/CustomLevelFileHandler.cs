using UnityEngine;
using System;
using System.IO;
using System.Threading.Tasks;
#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Pickers;
#endif

public class CustomLevelFileHandler : MonoBehaviour
{
    [SerializeField] private CustomLevelValidator validator;
    [SerializeField] private CustomLevelPopUp popUpManager;

    public delegate void MazeLoadedHandler(MazeData mazeData);
    public event MazeLoadedHandler OnMazeLoaded;

    public delegate void MazeLoadedWithPathHandler(MazeData mazeData, string filePath);
    public event MazeLoadedWithPathHandler OnMazeLoadedWithPath;

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
                    // Copy file to Application.persistentDataPath or get existing file path
                    string copiedFilePath = await CopyFileToPersistentDataPath(file);
                    if (string.IsNullOrEmpty(copiedFilePath))
                    {
                        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                        {
                            popUpManager?.ShowErrorPopUp("Failed to copy maze file to persistent storage.");
                            OnMazeLoaded?.Invoke(null);
                            OnMazeLoadedWithPath?.Invoke(null, null);
                        }, false);
                        return;
                    }
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        ProcessMazeFile(json, copiedFilePath);
                    }, false);
                }
                else
                {
                    Debug.Log("Load operation canceled by user.");
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        OnMazeLoaded?.Invoke(null);
                        OnMazeLoadedWithPath?.Invoke(null, null);
                    }, false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading maze file: {ex.Message}");
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    popUpManager?.ShowErrorPopUp("Failed to load maze file: " + ex.Message);
                    OnMazeLoaded?.Invoke(null);
                    OnMazeLoadedWithPath?.Invoke(null, null);
                }, false);
            }
        }, false);
#else
        Debug.LogError("File picker is only supported on UWP.");
        OnMazeLoaded?.Invoke(null);
        OnMazeLoadedWithPath?.Invoke(null, null);
#endif
    }

#if ENABLE_WINMD_SUPPORT
    private async Task<string> CopyFileToPersistentDataPath(StorageFile sourceFile)
    {
        try
        {
            string fileName = sourceFile.Name;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            // Check if file already exists
            string existingFilePath = null;
            try
            {
                StorageFile existingFile = await localFolder.GetFileAsync(fileName);
                if (existingFile != null)
                {
                    existingFilePath = existingFile.Path;
                    Debug.Log($"File already exists at: {existingFilePath}");
                    return existingFilePath; // Reuse existing file
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                // File doesn't exist, proceed with copy
            }

            // Copy file if it doesn't exist
            StorageFile copiedFile = await sourceFile.CopyAsync(localFolder, fileName, NameCollisionOption.FailIfExists);
            string copiedFilePath = copiedFile.Path;
            Debug.Log($"File copied to: {copiedFilePath}");

            // Verify file exists
            bool fileExists = await VerifyFileExists(copiedFile);
            if (!fileExists)
            {
                Debug.LogError($"Copied file not found at: {copiedFilePath}");
                return null;
            }

            return copiedFilePath;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error copying file to persistent data path: {ex.Message}");
            return null; // Return null to indicate failure
        }
    }

    private async Task<bool> VerifyFileExists(StorageFile file)
    {
        try
        {
            // Attempt to open the file to confirm it exists and is accessible
            await FileIO.ReadTextAsync(file);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error verifying file existence: {ex.Message}");
            return false;
        }
    }
#endif

    private MazeData ProcessMazeFile(string json, string filePath, bool invokeEvents = true)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            if (invokeEvents)
            {
                popUpManager?.ShowErrorPopUp("Invalid file path for maze file.");
                OnMazeLoaded?.Invoke(null);
                OnMazeLoadedWithPath?.Invoke(null, null);
            }
            return null;
        }

        // IMPROVEMENT: Use standard JsonUtility to ensure compatibility with MazeData structure
        MazeData mazeData = null;
        try 
        {
            mazeData = JsonUtility.FromJson<MazeData>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"JSON Parsing Error: {ex.Message}");
        }

        if (mazeData != null)
        {
            // Now it is safe to restore because we added null checks in MazeData.cs
            mazeData.RestoreAfterDeserialization(); 

            if (validator != null && !validator.ValidateMaze(mazeData))
            {
                if (invokeEvents)
                {
                    popUpManager?.ShowErrorPopUp("Invalid maze file format or size.");
                    OnMazeLoaded?.Invoke(null);
                    OnMazeLoadedWithPath?.Invoke(null, null);
                }
                return null;
            }
            else
            {
                if (invokeEvents)
                {
                    OnMazeLoaded?.Invoke(mazeData);
                    OnMazeLoadedWithPath?.Invoke(mazeData, filePath);
                    Debug.Log("Maze file loaded and textures restored successfully.");
                }
                return mazeData;
            }
        }
        else
        {
            if (invokeEvents)
            {
                popUpManager?.ShowErrorPopUp("Failed to deserialize maze file.");
                OnMazeLoaded?.Invoke(null);
                OnMazeLoadedWithPath?.Invoke(null, null);
            }
            return null;
        }
    }
}