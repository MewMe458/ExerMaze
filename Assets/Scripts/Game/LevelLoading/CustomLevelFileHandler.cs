using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Pickers;
#endif

[System.Serializable]
public class CampaignData
{
    public List<string> levelPaths = new List<string>();
}

public class CustomLevelFileHandler : MonoBehaviour
{
    [SerializeField] private CustomLevelValidator validator;
    [SerializeField] private CustomLevelPopUp popUpManager;

    public delegate void MazeLoadedHandler(MazeData mazeData);
    public event MazeLoadedHandler OnMazeLoaded;

    public delegate void MazeLoadedWithPathHandler(MazeData mazeData, string filePath);
    public event MazeLoadedWithPathHandler OnMazeLoadedWithPath;

    public delegate void CampaignLoadedWithPathHandler(CampaignData campaignData, string filePath);
    public event CampaignLoadedWithPathHandler OnCampaignLoadedWithPath;

    // NEW: Event triggered when the user successfully chooses a path and saves a campaign
    public delegate void CampaignSavedWithPathHandler(string externalPath, string json);
    public event CampaignSavedWithPathHandler OnCampaignSavedWithPath;

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
                openPicker.FileTypeFilter.Add(".fitmaze");
                
                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    Debug.Log($"File selected: {file.Path}");
                    string json = await FileIO.ReadTextAsync(file);
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

    public void LoadCampaignFile()
    {
#if ENABLE_WINMD_SUPPORT
        Debug.Log("Opening file picker for loading campaign...");
        UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
        {
            try
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".fitcampaign");
                
                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    Debug.Log($"Campaign file selected: {file.Path}");
                    string json = await FileIO.ReadTextAsync(file);
                    string copiedFilePath = await CopyFileToPersistentDataPath(file);
                    if (string.IsNullOrEmpty(copiedFilePath))
                    {
                        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                        {
                            popUpManager?.ShowErrorPopUp("Failed to copy campaign file to persistent storage.");
                            OnCampaignLoadedWithPath?.Invoke(null, null);
                        }, false);
                        return;
                    }
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        ProcessCampaignFile(json, copiedFilePath);
                    }, false);
                }
                else
                {
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        OnCampaignLoadedWithPath?.Invoke(null, null);
                    }, false);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    popUpManager?.ShowErrorPopUp("Failed to load campaign file: " + ex.Message);
                    OnCampaignLoadedWithPath?.Invoke(null, null);
                }, false);
            }
        }, false);
#else
        Debug.LogError("File picker is only supported on UWP.");
        OnCampaignLoadedWithPath?.Invoke(null, null);
#endif
    }

    // NEW: FileSavePicker integration for exporting campaign files
    public void SaveCampaignFile(string json, string defaultName)
    {
#if ENABLE_WINMD_SUPPORT
        Debug.Log("Opening file save picker for campaign...");
        UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
        {
            try
            {
                FileSavePicker savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("FitMaze Campaign", new List<string>() { ".fitcampaign" });
                savePicker.SuggestedFileName = defaultName;

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    Debug.Log($"Save destination selected by user: {file.Path}");
                    await FileIO.WriteTextAsync(file, json);

                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        OnCampaignSavedWithPath?.Invoke(file.Path, json);
                    }, false);
                }
                else
                {
                    Debug.Log("Campaign save operation canceled by user.");
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        OnCampaignSavedWithPath?.Invoke(null, null);
                    }, false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error picking save location or writing file: {ex.Message}");
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    popUpManager?.ShowErrorPopUp("Failed to save campaign file: " + ex.Message);
                    OnCampaignSavedWithPath?.Invoke(null, null);
                }, false);
            }
        }, false);
#else
        Debug.LogError("File save picker is only supported on UWP.");
        OnCampaignSavedWithPath?.Invoke(null, null);
#endif
    }

#if ENABLE_WINMD_SUPPORT
    private async Task<string> CopyFileToPersistentDataPath(StorageFile sourceFile)
    {
        try
        {
            string fileName = sourceFile.Name;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            string existingFilePath = null;
            try
            {
                StorageFile existingFile = await localFolder.GetFileAsync(fileName);
                if (existingFile != null)
                {
                    existingFilePath = existingFile.Path;
                    Debug.Log($"File already exists at: {existingFilePath}");
                    return existingFilePath;
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                // Proceed with copy
            }

            StorageFile copiedFile = await sourceFile.CopyAsync(localFolder, fileName, NameCollisionOption.FailIfExists);
            string copiedFilePath = copiedFile.Path;
            Debug.Log($"File copied to: {copiedFilePath}");

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
            return null;
        }
    }

    private async Task<bool> VerifyFileExists(StorageFile file)
    {
        try
        {
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
            mazeData.RestoreAfterDeserialization(); 

            if (validator != null && !validator.ValidateMaze(mazeData))
            {
                if (invokeEvents)
                {
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

    private void ProcessCampaignFile(string json, string filePath)
    {
        try
        {
            CampaignData campaignData = JsonUtility.FromJson<CampaignData>(json);
            if (campaignData != null)
            {
                OnCampaignLoadedWithPath?.Invoke(campaignData, filePath);
                Debug.Log("Campaign file loaded successfully.");
            }
            else
            {
                popUpManager?.ShowErrorPopUp("Failed to deserialize campaign file.");
                OnCampaignLoadedWithPath?.Invoke(null, null);
            }
        }
        catch (Exception ex)
        {
            popUpManager?.ShowErrorPopUp("Invalid campaign file format: " + ex.Message);
            OnCampaignLoadedWithPath?.Invoke(null, null);
        }
    }
}