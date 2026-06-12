using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EditorBackButton : MonoBehaviour
{
    [SerializeField] Button backButton;
    [SerializeField] GameObject backPanel;
    [SerializeField] GameObject backConfirmPopUp;
    [SerializeField] GameObject backSavePopUp;
    [SerializeField] Button yesBackButton;
    [SerializeField] Button noBackButton;
    [SerializeField] Button yesSaveButton;
    [SerializeField] Button noSaveButton;
    [SerializeField] MazeFileHandler mazeFileHandler;
    [SerializeField] MazeEditorController mazeEditorController;

    private bool isExporting = false;

    void Awake()
    {
        if (backButton == null)
        {
            Debug.LogError("BackButton not assigned in EditorBackButton.");
            return;
        }
        if (backPanel == null)
        {
            Debug.LogError("BackPanel not assigned in EditorBackButton.");
            return;
        }
        if (backConfirmPopUp == null || backSavePopUp == null)
        {
            Debug.LogError("One or both pop-ups not assigned in EditorBackButton.");
            return;
        }
        if (mazeFileHandler == null)
        {
            Debug.LogError("MazeFileHandler not assigned in EditorBackButton.");
            return;
        }
        if (mazeEditorController == null)
        {
            Debug.LogError("MazeEditorController not assigned in EditorBackButton.");
            return;
        }

        // Set up listeners for the back button
        backButton.onClick.AddListener(OnBackButtonClicked);

        // Set up listeners for BackConfirmPopUp buttons
        if (yesBackButton != null) yesBackButton.onClick.AddListener(OnYesBack);
        if (noBackButton != null) noBackButton.onClick.AddListener(OnNoBack);

        // Set up listeners for BackSavePopUp buttons
        if (yesSaveButton != null) yesSaveButton.onClick.AddListener(OnYesSave);
        if (noSaveButton != null) noSaveButton.onClick.AddListener(OnNoSave);

        // Initially hide panels and pop-ups
        backPanel.SetActive(false);
        backConfirmPopUp.SetActive(false);
        backSavePopUp.SetActive(false);

        // Hook into the maze exported event
        mazeFileHandler.OnMazeExported += OnMazeExportComplete;
    }

    void OnDestroy()
    {
        if (mazeFileHandler != null)
        {
            mazeFileHandler.OnMazeExported -= OnMazeExportComplete;
        }
    }

    void OnBackButtonClicked()
    {
        backPanel.SetActive(true);
        backConfirmPopUp.SetActive(true);
        backSavePopUp.SetActive(false);
    }

    void OnYesBack()
    {
        backConfirmPopUp.SetActive(false);
        backSavePopUp.SetActive(true);
    }

    void OnNoBack()
    {
        backConfirmPopUp.SetActive(false);
        backSavePopUp.SetActive(false);
        backPanel.SetActive(false);
    }

    void OnYesSave()
    {
        // Fix: Use .CurrentMaze instead of GetCurrentMazeData()
        MazeData currentMazeData = mazeEditorController.CurrentMaze;
        if (currentMazeData != null)
        {
            isExporting = true;
            mazeFileHandler.ExportMazeFile(currentMazeData); 
        }
        else
        {
            Debug.LogWarning("Current maze data is null, navigating to Menu.");
            isExporting = true; 
            OnMazeExportComplete(false); 
        }
    }

    void OnNoSave()
    {
        backConfirmPopUp.SetActive(false);
        backSavePopUp.SetActive(false);
        backPanel.SetActive(false);
        SceneManager.LoadScene("Menu");
    }

    void OnMazeExportComplete(bool success)
    {
        if (isExporting)
        {
            isExporting = false;
            if (success)
            {
                Debug.Log("Export successful, navigating to Menu.");
            }
            else
            {
                Debug.LogWarning("Export failed or canceled, navigating to Menu.");
            }
            backConfirmPopUp.SetActive(false);
            backSavePopUp.SetActive(false);
            backPanel.SetActive(false);
            SceneManager.LoadScene("Menu");
        }
    }
}