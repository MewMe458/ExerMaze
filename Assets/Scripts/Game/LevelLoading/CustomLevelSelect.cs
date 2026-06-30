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
    [SerializeField] private CustomLevelPopUp popUpManager;
    [SerializeField] private CustomLevelFileHandler fileHandler;

    private List<LevelInfoObject> levelInfoObjects = new List<LevelInfoObject>();
    
    // --- UPDATED: MULTI-SELECTION LIST MANAGEMENT ---
    private List<LevelInfoObject> selectedLevelInfos = new List<LevelInfoObject>();
    // ------------------------------------------------

    private string saveFilePath;

    void Awake()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "custom_levels.json");

        if (contentParent == null) Debug.LogError("Content Parent not assigned!");
        if (levelInfoPrefab == null) Debug.LogError("Level Info Prefab not assigned!");
        if (addButton == null) Debug.LogError("Add Button not assigned!");
        if (openButton == null) Debug.LogError("Open Button not assigned!");
        if (deleteButton == null) Debug.LogError("Delete Button not assigned!");
        if (popUpManager == null) Debug.LogError("PopUp Manager not assigned!");
        if (fileHandler == null) Debug.LogError("File Handler not assigned!");

        addButton.onClick.AddListener(OnAddButtonClicked);
        openButton.onClick.AddListener(OnOpenButtonClicked);
        deleteButton.onClick.AddListener(OnDeleteButtonClicked);

        openButton.interactable = false;
        deleteButton.interactable = false;

        fileHandler.OnMazeLoadedWithPath += OnMazeFileLoadedWithPath;
    }

    void OnDestroy()
    {
        if (fileHandler != null)
        {
            fileHandler.OnMazeLoadedWithPath -= OnMazeFileLoadedWithPath;
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
                levelInfo.Initialize(data.levelName, data.date, data.size, data.mode, normalizedPath, this);
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
                filePath = levelInfo.FilePath
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

            bool fileExists = File.Exists(normalizedPath);
            Debug.Log($"Checking file existence at {normalizedPath}: {fileExists}");
            if (!fileExists)
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
            levelInfo.Initialize(levelName, date, size, mode, normalizedPath, this);
            levelInfoObjects.Add(levelInfo);
            SaveLevelInfoData();
        }
    }

    // Helper to extract and safely load MazeData directly from a file path
    private bool PreLoadMazeDataIntoGameManager(string path)
    {
        try
        {
            if (!File.Exists(path)) return false;
            string json = File.ReadAllText(path);
            MazeData data = MazeDataSerializer.Deserialize(json);
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

        // Verify all chosen files are still valid
        List<string> verifiedPaths = new List<string>();
        foreach (var info in selectedLevelInfos)
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
        SaveLevelInfoData();

        // Pass the entire sequence list to the GameManager playlist queue
        GameManager.Instance.SetupCustomLevelQueue(verifiedPaths);

        // Advance to take out the first level of the playlist queue
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
        
        // Confirm delete for the most recently selected item or first item
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

    // --- UPDATED: TOGGLE MULTI-SELECTION LIST ENTRY ---
    public void OnLevelInfoSelected(LevelInfoObject levelInfo)
    {
        if (!selectedLevelInfos.Contains(levelInfo))
        {
            selectedLevelInfos.Add(levelInfo);
            Debug.Log($"Selected level added to playlist queue: {levelInfo.LevelName}. Total items: {selectedLevelInfos.Count}");
        }
        UpdateButtonInteractability();
    }

    public void OnLevelInfoDeselected(LevelInfoObject levelInfo)
    {
        if (selectedLevelInfos.Contains(levelInfo))
        {
            selectedLevelInfos.Remove(levelInfo);
            Debug.Log($"Selected level removed from playlist queue: {levelInfo.LevelName}. Total items: {selectedLevelInfos.Count}");
        }
        UpdateButtonInteractability();
    }
    // --------------------------------------------------

    private void UpdateButtonInteractability()
    {
        openButton.interactable = selectedLevelInfos.Count > 0;
        deleteButton.interactable = selectedLevelInfos.Count > 0;
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