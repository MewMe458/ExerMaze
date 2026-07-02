using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System;

[System.Serializable]
public class LevelInfoData
{
    public string levelName;
    public string date;
    public string size;
    public string mode;
    public string filePath;
    public bool isCampaign;
}

[System.Serializable]
public class LevelInfoDataList
{
    public List<LevelInfoData> levels = new List<LevelInfoData>();
}

public class CustomLevelSelect : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject levelInfoPrefab;
    [SerializeField] private Button addButton;
    [SerializeField] private Button openButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button addCampaignButton;
    [SerializeField] private Button saveAsCampaign;
    [SerializeField] private CustomLevelPopUp popUpManager;
    [SerializeField] private CustomLevelFileHandler fileHandler;

    private List<LevelInfoObject> levelInfoObjects = new List<LevelInfoObject>();
    private List<LevelInfoObject> selectedLevelInfos = new List<LevelInfoObject>();

    private string saveFilePath;

    void Awake()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "custom_levels.json");

        if (contentParent == null) Debug.LogError("Content Parent not assigned!");
        if (levelInfoPrefab == null) Debug.LogError("Level Info Prefab not assigned!");
        if (addButton == null) Debug.LogError("Add Button not assigned!");
        if (openButton == null) Debug.LogError("Open Button not assigned!");
        if (deleteButton == null) Debug.LogError("Delete Button not assigned!");
        if (addCampaignButton == null) Debug.LogError("Add Campaign Button not assigned!");
        if (saveAsCampaign == null) Debug.LogError("Save As Campaign Button not assigned!");
        if (popUpManager == null) Debug.LogError("PopUp Manager not assigned!");
        if (fileHandler == null) Debug.LogError("File Handler not assigned!");

        addButton.onClick.AddListener(OnAddButtonClicked);
        openButton.onClick.AddListener(OnOpenButtonClicked);
        deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        addCampaignButton.onClick.AddListener(OnAddCampaignButtonClicked);
        saveAsCampaign.onClick.AddListener(OnSaveAsCampaignClicked);

        openButton.interactable = false;
        deleteButton.interactable = false;
        saveAsCampaign.interactable = false;

        fileHandler.OnMazeLoadedWithPath += OnMazeFileLoadedWithPath;
        fileHandler.OnCampaignLoadedWithPath += OnCampaignFileLoadedWithPath;
        fileHandler.OnCampaignSavedWithPath += OnCampaignFileSavedWithPath; // NEW
    }

    void OnDestroy()
    {
        if (fileHandler != null)
        {
            fileHandler.OnMazeLoadedWithPath -= OnMazeFileLoadedWithPath;
            fileHandler.OnCampaignLoadedWithPath -= OnCampaignFileLoadedWithPath;
            fileHandler.OnCampaignSavedWithPath -= OnCampaignFileSavedWithPath; // NEW
        }
    }

    void Start()
    {
        LoadLevelInfoData();
        GameManager.Instance.SetGameState(GameManager.GameState.Menu);
    }

    private void LoadLevelInfoData()
    {
        levelInfoObjects.Clear();
        selectedLevelInfos.Clear();
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            LevelInfoDataList dataList = JsonUtility.FromJson<LevelInfoDataList>(json);
            foreach (var data in dataList.levels)
            {
                string normalizedPath = NormalizePath(data.filePath);
                GameObject instance = Instantiate(levelInfoPrefab, contentParent);
                LevelInfoObject levelInfo = instance.GetComponent<LevelInfoObject>();
                levelInfo.Initialize(data.levelName, data.date, data.size, data.mode, normalizedPath, this, data.isCampaign);
                levelInfoObjects.Add(levelInfo);
            }
        }
    }

    private void SaveLevelInfoData()
    {
        LevelInfoDataList dataList = new LevelInfoDataList();
        foreach (var levelInfo in levelInfoObjects)
        {
            LevelInfoData data = new LevelInfoData
            {
                levelName = levelInfo.LevelName,
                date = levelInfo.Date,
                size = levelInfo.Size,
                mode = levelInfo.Mode,
                filePath = levelInfo.FilePath,
                isCampaign = levelInfo.IsCampaign
            };
            dataList.levels.Add(data);
        }
        string json = JsonUtility.ToJson(dataList, true);
        File.WriteAllText(saveFilePath, json);
    }

    private void OnAddButtonClicked()
    {
        fileHandler.LoadMazeFile();
    }

    private void OnAddCampaignButtonClicked()
    {
        fileHandler.LoadCampaignFile();
    }

    // UPDATED: Now generates JSON payload data and triggers native OS Save Dialog Picker
    private void OnSaveAsCampaignClicked()
    {
        if (selectedLevelInfos.Count == 0) return;

        CampaignData campaignData = new CampaignData();
        foreach (var info in selectedLevelInfos)
        {
            if (!info.IsCampaign)
            {
                campaignData.levelPaths.Add(info.FilePath);
            }
        }

        if (campaignData.levelPaths.Count == 0)
        {
            popUpManager.ShowErrorPopUp("Please select custom levels to form a campaign.");
            return;
        }

        string defaultCampaignName = $"Campaign_{DateTime.Now:yyyyMMdd_HHmmss}";
        string json = JsonUtility.ToJson(campaignData, true);

        // Call fileHandler to trigger native UI thread dialogue window
        fileHandler.SaveCampaignFile(json, defaultCampaignName);
    }

    // NEW: Asynchronous callback handling data alignment after the player chooses an export location
    private void OnCampaignFileSavedWithPath(string externalPath, string json)
    {
        if (string.IsNullOrEmpty(externalPath) || string.IsNullOrEmpty(json))
        {
            // Operation was cancelled by user or encountered error handled by popup
            return;
        }

        // Mirrors file locally in sandboxed persistentDataPath to guarantee loading accessibility later
        string campaignFolder = Path.Combine(Application.persistentDataPath, "Campaigns");
        if (!Directory.Exists(campaignFolder))
        {
            Directory.CreateDirectory(campaignFolder);
        }

        string fileName = Path.GetFileName(externalPath);
        string localSandboxPath = NormalizePath(Path.Combine(campaignFolder, fileName));

        try
        {
            // Save the secondary tracking copy locally
            File.WriteAllText(localSandboxPath, json);

            CampaignData campaignData = JsonUtility.FromJson<CampaignData>(json);
            string campaignDisplayName = Path.GetFileNameWithoutExtension(externalPath);
            string date = DateTime.Now.ToString("dd/MM/yyyy, h:mm tt");
            string size = $"{campaignData.levelPaths?.Count ?? 0} Levels";
            string mode = "Campaign";

            // Spawn the UI listing element mapped to the stable local sandbox copy path
            GameObject instance = Instantiate(levelInfoPrefab, contentParent);
            LevelInfoObject levelInfo = instance.GetComponent<LevelInfoObject>();
            levelInfo.Initialize(campaignDisplayName, date, size, mode, localSandboxPath, this, true);
            levelInfoObjects.Add(levelInfo);
            SaveLevelInfoData();

            Debug.Log($"Campaign master file exported to: {externalPath}");
            Debug.Log($"Campaign local runner track saved to: {localSandboxPath}");
            
            // Clear selections
            foreach (var info in new List<LevelInfoObject>(selectedLevelInfos))
            {
                info.Deselect();
            }
        }
        catch (Exception ex)
        {
            popUpManager.ShowErrorPopUp($"Failed to mirror campaign tracking data layout: {ex.Message}");
        }
    }

    private void OnMazeFileLoadedWithPath(MazeData mazeData, string filePath)
    {
        if (mazeData != null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                popUpManager.ShowErrorPopUp("Failed to retrieve file path for the loaded maze.");
                return;
            }

            string normalizedPath = NormalizePath(filePath);

            if (!File.Exists(normalizedPath))
            {
                popUpManager.ShowErrorPopUp($"Maze file not found at {normalizedPath}. Please try adding the file again.");
                return;
            }

            if (levelInfoObjects.Exists(level => level.FilePath == normalizedPath))
            {
                popUpManager.ShowErrorPopUp("This maze file is already added.");
                return;
            }

            string levelName = Path.GetFileNameWithoutExtension(normalizedPath);
            string date = DateTime.Now.ToString("dd/MM/yyyy, h:mm tt");
            string size = $"{mazeData.rows}x{mazeData.columns}";
            string mode = mazeData.mode ?? "Relax";

            GameObject instance = Instantiate(levelInfoPrefab, contentParent);
            LevelInfoObject levelInfo = instance.GetComponent<LevelInfoObject>();
            levelInfo.Initialize(levelName, date, size, mode, normalizedPath, this, false);
            levelInfoObjects.Add(levelInfo);
            SaveLevelInfoData();
        }
    }

    private void OnCampaignFileLoadedWithPath(CampaignData campaignData, string filePath)
    {
        if (campaignData != null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                popUpManager.ShowErrorPopUp("Failed to retrieve file path for the loaded campaign.");
                return;
            }

            string normalizedPath = NormalizePath(filePath);

            if (levelInfoObjects.Exists(level => level.FilePath == normalizedPath))
            {
                popUpManager.ShowErrorPopUp("This campaign file is already added.");
                return;
            }

            string campaignName = Path.GetFileNameWithoutExtension(normalizedPath);
            string date = DateTime.Now.ToString("dd/MM/yyyy, h:mm tt");
            string size = $"{campaignData.levelPaths?.Count ?? 0} Levels";
            string mode = "Campaign";

            GameObject instance = Instantiate(levelInfoPrefab, contentParent);
            LevelInfoObject levelInfo = instance.GetComponent<LevelInfoObject>();
            levelInfo.Initialize(campaignName, date, size, mode, normalizedPath, this, true);
            levelInfoObjects.Add(levelInfo);
            SaveLevelInfoData();
        }
    }

    private bool PreLoadMazeDataIntoGameManager(string path)
    {
        try
        {
            if (!File.Exists(path)) return false;
            string json = File.ReadAllText(path);
            MazeData data = JsonUtility.FromJson<MazeData>(json);
            if (data != null)
            {
                data.RestoreAfterDeserialization();
                GameManager.Instance.LoadedMazeData = data;
                GameManager.Instance.SetMazeSize(data.rows, data.columns);
                Debug.Log($"Successfully pre-loaded MazeData: {path}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to pre-load maze data: {ex.Message}");
        }
        return false;
    }

    private void OnOpenButtonClicked()
    {
        if (selectedLevelInfos.Count == 0) return;

        List<string> verifiedPaths = new List<string>();
        foreach (var info in selectedLevelInfos)
        {
            if (info.IsCampaign)
            {
                if (File.Exists(info.FilePath))
                {
                    try
                    {
                        string json = File.ReadAllText(info.FilePath);
                        CampaignData campaignData = JsonUtility.FromJson<CampaignData>(json);
                        if (campaignData != null && campaignData.levelPaths != null)
                        {
                            foreach (string path in campaignData.levelPaths)
                            {
                                string normPath = NormalizePath(path);
                                if (File.Exists(normPath))
                                {
                                    verifiedPaths.Add(normPath);
                                }
                                else
                                {
                                    popUpManager.ShowErrorPopUp($"Campaign custom level file not found at: {normPath}");
                                    return;
                                }
                            }
                            info.UpdateDate();
                        }
                    }
                    catch (Exception ex)
                    {
                        popUpManager.ShowErrorPopUp($"Failed to read campaign file data sequence: {ex.Message}");
                        return;
                    }
                }
                else
                {
                    popUpManager.ShowErrorPopUp($"Campaign reference file not found at {info.FilePath}");
                    return;
                }
            }
            else
            {
                if (info.IsFileValid())
                {
                    info.UpdateDate();
                    verifiedPaths.Add(info.FilePath);
                }
                else
                {
                    popUpManager.ShowErrorPopUp($"File not found at {info.FilePath}");
                    return;
                }
            }
        }
        SaveLevelInfoData();

        if (ScreenshotManager.Instance != null)
        {
            ScreenshotManager.Instance.CreateSessionSubfolder();
        }

        GameManager.Instance.SetupCustomLevelQueue(verifiedPaths);

        if (GameManager.Instance.AdvanceToNextCustomLevel())
        {
            if (PreLoadMazeDataIntoGameManager(GameManager.Instance.CurrentCustomLevelPath))
            {
                SceneManager.LoadScene("CustomLevel");
            }
            else
            {
                popUpManager.ShowErrorPopUp("Failed to open the first selected level file.");
            }
        }
    }

    private void OnDeleteButtonClicked()
    {
        if (selectedLevelInfos.Count == 0) return;
        
        LevelInfoObject primaryTarget = selectedLevelInfos[selectedLevelInfos.Count - 1];
        popUpManager.ShowDeleteConfirmation(primaryTarget.LevelName, OnDeleteConfirmed);
    }

    private void OnDeleteConfirmed(bool confirmed)
    {
        if (confirmed && selectedLevelInfos.Count > 0)
        {
            LevelInfoObject target = selectedLevelInfos[selectedLevelInfos.Count - 1];
            if (File.Exists(target.FilePath))
            {
                try
                {
                    File.Delete(target.FilePath);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to delete file {target.FilePath}: {ex.Message}");
                }
            }

            levelInfoObjects.Remove(target);
            selectedLevelInfos.Remove(target);
            Destroy(target.gameObject);
            UpdateButtonInteractability();
            SaveLevelInfoData();
        }
    }

    public void OnLevelInfoSelected(LevelInfoObject levelInfo)
    {
        if (!selectedLevelInfos.Contains(levelInfo))
        {
            selectedLevelInfos.Add(levelInfo);
            Debug.Log($"Selected item added to playlist queue: {levelInfo.LevelName}. Total items: {selectedLevelInfos.Count}");
        }
        UpdateButtonInteractability();
    }

    public void OnLevelInfoDeselected(LevelInfoObject levelInfo)
    {
        if (selectedLevelInfos.Contains(levelInfo))
        {
            selectedLevelInfos.Remove(levelInfo);
            Debug.Log($"Selected item removed from playlist queue: {levelInfo.LevelName}. Total items: {selectedLevelInfos.Count}");
        }
        UpdateButtonInteractability();
    }

    private void UpdateButtonInteractability()
    {
        bool elementsSelected = selectedLevelInfos.Count > 0;
        openButton.interactable = elementsSelected;
        deleteButton.interactable = elementsSelected;
        
        bool canSaveCampaign = false;
        if (elementsSelected)
        {
            canSaveCampaign = true;
            foreach (var info in selectedLevelInfos)
            {
                if (info.IsCampaign) { canSaveCampaign = false; break; }
            }
        }
        saveAsCampaign.interactable = canSaveCampaign;
    }

    public void ShowErrorFromExternal(string message)
    {
        popUpManager.ShowErrorPopUp(message);
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        return path.Replace('/', '\\').Replace("\\\\", "\\");
    }
}