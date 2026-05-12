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
    private LevelInfoObject selectedLevelInfo = null;
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
                // Normalize path for consistency
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

            // Normalize path for consistency
            string normalizedPath = NormalizePath(filePath);

            // Debug file existence
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

    private void OnOpenButtonClicked()
    {
        if (selectedLevelInfo == null) return;

        if (selectedLevelInfo.IsFileValid())
        {
            selectedLevelInfo.UpdateDate();
            SaveLevelInfoData();

            // --- NEW: LOAD DATA INTO GAMEMANAGER BEFORE SCENE LOAD ---
            try 
            {
                string json = File.ReadAllText(selectedLevelInfo.FilePath);
                MazeData data = MazeDataSerializer.Deserialize(json);
                
                if (data != null)
                {
                    data.RestoreAfterDeserialization(); // Reconstruct the 2D array
                    GameManager.Instance.LoadedMazeData = data;
                    GameManager.Instance.SetMazeSize(data.rows, data.columns);
                    Debug.Log($"Successfully pre-loaded MazeData with {data.rows} rows.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to pre-load maze data: {ex.Message}");
            }
            // ---------------------------------------------------------

            GameManager.Instance.CurrentCustomLevelPath = selectedLevelInfo.FilePath;
            SceneManager.LoadScene("CustomLevel");
        }
        else
        {
            popUpManager.ShowErrorPopUp($"File not found at {selectedLevelInfo.FilePath}");
        }
    }

    private void OnDeleteButtonClicked()
    {
        if (selectedLevelInfo == null) return;
        popUpManager.ShowDeleteConfirmation(selectedLevelInfo.LevelName, OnDeleteConfirmed);
    }

    private void OnDeleteConfirmed(bool confirmed)
    {
        if (confirmed && selectedLevelInfo != null)
        {
            if (File.Exists(selectedLevelInfo.FilePath))
            {
                try
                {
                    File.Delete(selectedLevelInfo.FilePath);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to delete file {selectedLevelInfo.FilePath}: {ex.Message}");
                }
            }

            levelInfoObjects.Remove(selectedLevelInfo);
            Destroy(selectedLevelInfo.gameObject);
            selectedLevelInfo = null;
            UpdateButtonInteractability();
            SaveLevelInfoData();
        }
    }

    public void OnLevelInfoSelected(LevelInfoObject levelInfo)
    {
        if (selectedLevelInfo != null && selectedLevelInfo != levelInfo)
        {
            selectedLevelInfo.Deselect();
        }
        selectedLevelInfo = levelInfo;
        UpdateButtonInteractability();
    }

    public void OnLevelInfoDeselected(LevelInfoObject levelInfo)
    {
        if (selectedLevelInfo == levelInfo)
        {
            selectedLevelInfo = null;
            UpdateButtonInteractability();
        }
    }

    private void UpdateButtonInteractability()
    {
        openButton.interactable = selectedLevelInfo != null;
        deleteButton.interactable = selectedLevelInfo != null;
    }

    public void ShowErrorFromExternal(string message)
    {
        popUpManager.ShowErrorPopUp(message);
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        // Replace forward slashes with backslashes for Windows
        return path.Replace('/', '\\').Replace("\\\\", "\\");
    }
}