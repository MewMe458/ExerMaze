using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EditorBackButton : MonoBehaviour
{
    [SerializeField] Button backButton;
    [SerializeField] GameObject backPanel;
    [SerializeField] GameObject backConfirmPopUp;
    [SerializeField] GameObject backSavePopUp;
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
        Button yesBackButton = backConfirmPopUp.transform.Find("BackYes")?.GetComponent<Button>();
        Button noBackButton = backConfirmPopUp.transform.Find("BackNo")?.GetComponent<Button>();
        if (yesBackButton == null || noBackButton == null)
        {
            Debug.LogError("One or both of the buttons not found in BackConfirmPopUp.");
            return;
        }
        yesBackButton.onClick.AddListener(OnYesBack);
        noBackButton.onClick.AddListener(OnNoBack);

        // Set up listeners for BackSavePopUp buttons
        Button yesSaveButton = backSavePopUp.transform.Find("BackSaveYes")?.GetComponent<Button>();
        Button noSaveButton = backSavePopUp.transform.Find("BackSaveNo")?.GetComponent<Button>();
        if (yesSaveButton == null || noSaveButton == null)
        {
            Debug.LogError("One or both of the buttons not found in SaveConfirmPopUp.");
            return;
        }
        yesSaveButton.onClick.AddListener(OnYesSave);
        noSaveButton.onClick.AddListener(OnNoSave);

        // Ensure pop-ups and panel are initially hidden
        backConfirmPopUp.SetActive(false);
        backSavePopUp.SetActive(false);
        backPanel.SetActive(false);

        // Subscribe to export event
        mazeFileHandler.OnMazeExported += OnMazeExportComplete;
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (mazeFileHandler != null)
        {
            mazeFileHandler.OnMazeExported -= OnMazeExportComplete;
        }
    }

    void OnBackButtonClicked()
    {
        // Check if there's a maze generated
        MazeData currentMazeData = mazeEditorController.GetCurrentMazeData();
        if (currentMazeData == null)
        {
            Debug.Log("No maze data, navigating to Menu.");
            SceneManager.LoadScene("Menu");
            return;
        }

        // Show pop-ups for non-null maze
        backPanel.SetActive(true);
        backConfirmPopUp.SetActive(true);
        backSavePopUp.SetActive(false); // Ensure second pop-up is hidden
    }

    void OnYesBack()
    {
        // Hide first pop-up, show second pop-up
        backConfirmPopUp.SetActive(false);
        backSavePopUp.SetActive(true);
    }

    void OnNoBack()
    {
        // Hide both pop-ups and panel, stay in editor
        backConfirmPopUp.SetActive(false);
        backSavePopUp.SetActive(false);
        backPanel.SetActive(false);
    }

    void OnYesSave()
    {
        // Save the level using MazeFileHandler
        MazeData currentMazeData = mazeEditorController.GetCurrentMazeData();
        if (currentMazeData != null)
        {
            isExporting = true;
            mazeFileHandler.ExportMazeFile(currentMazeData); // Non-blocking
        }
        else
        {
            Debug.LogWarning("Current maze data is null, navigating to Menu.");
            isExporting = true; // Ensure OnMazeExportComplete processes navigation
            OnMazeExportComplete(false); // Proceed as if export failed
        }
    }

    void OnNoSave()
    {
        // Hide pop-ups and navigate to Menu without saving
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