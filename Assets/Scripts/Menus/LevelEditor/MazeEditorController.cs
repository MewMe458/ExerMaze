using UnityEngine;
using UnityEngine.UI;

public class MazeEditorController : MonoBehaviour
{
    [SerializeField] private Slider sizeSlider;
    [SerializeField] private Button generateEmptyButton;
    [SerializeField] private Button generateRandomButton;
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button zoomOutButton;
    [SerializeField] private Button setStartPointButton;
    [SerializeField] private Button loadMazeButton;
    [SerializeField] private Button exportMazeButton;
    [SerializeField] private Button validateButton;
    [SerializeField] private Button showSolutionButton;
    [SerializeField] private MazeGenerator mazeGenerator;
    [SerializeField] private MazeInputHandler inputHandler;
    [SerializeField] private MazeValidator validator;
    [SerializeField] private MazeFileHandler fileHandler;
    [SerializeField] private MazeGridRenderer gridRenderer;
    [SerializeField] private MazeEditorMode editorMode;

    private MazeData currentMazeData;
    private bool isEditingStartPoint = false;
    private Color originalButtonColor;

    void Start()
    {
        if (sizeSlider == null) Debug.LogError("Size Slider not assigned!");
        if (generateEmptyButton == null) Debug.LogError("Generate Empty Button not assigned!");
        if (generateRandomButton == null) Debug.LogError("Generate Random Button not assigned!");
        if (zoomInButton == null) Debug.LogError("Zoom In Button not assigned!");
        if (zoomOutButton == null) Debug.LogError("Zoom Out Button not assigned!");
        if (setStartPointButton == null) Debug.LogError("Set Start Point Button not assigned!");
        if (showSolutionButton == null) Debug.LogError("Show Solution Button not assigned!");
        if (loadMazeButton == null) Debug.LogError("Load Maze Button not assigned!");
        if (exportMazeButton == null) Debug.LogError("Export Maze Button not assigned!");
        if (validateButton == null) Debug.LogError("Validate Button not assigned!");
        if (mazeGenerator == null) Debug.LogError("Maze Generator not assigned!");
        if (validator == null) Debug.LogError("Maze Validator not assigned!");
        if (fileHandler == null) Debug.LogError("Maze File Handler not assigned!");
        if (gridRenderer == null) Debug.LogError("Maze Grid Renderer not assigned!");
        if (editorMode == null) Debug.LogError("Maze Editor Mode not assigned!");

        generateEmptyButton.onClick.AddListener(OnGenerateEmpty);
        generateRandomButton.onClick.AddListener(OnGenerateRandom);
        zoomInButton.onClick.AddListener(() => gridRenderer.ZoomIn());
        zoomOutButton.onClick.AddListener(() => gridRenderer.ZoomOut());
        showSolutionButton.onClick.AddListener(OnShowSolution);
        loadMazeButton.onClick.AddListener(OnLoadMaze);
        exportMazeButton.onClick.AddListener(OnExportMaze);
        validateButton.onClick.AddListener(OnValidateMaze);

        if (setStartPointButton != null)
        {
            setStartPointButton.onClick.AddListener(OnSetStartPoint);
            Image buttonImage = setStartPointButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                originalButtonColor = buttonImage.color;
            }
            else
            {
                Debug.LogWarning("Set Start Point button has no Image component. Color changes will not be applied.");
                originalButtonColor = Color.white;
            }
        }

        // Subscribe to file handler events
        fileHandler.OnMazeLoaded += OnMazeLoaded;
        fileHandler.OnMazeExported += OnMazeExported;
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (fileHandler != null)
        {
            fileHandler.OnMazeLoaded -= OnMazeLoaded;
            fileHandler.OnMazeExported -= OnMazeExported;
        }
    }

    void OnGenerateEmpty()
    {
        gridRenderer.ResetSolutionVisibility();
        int size = Mathf.RoundToInt(sizeSlider.value);
        currentMazeData = mazeGenerator.GenerateEmpty(size, size);
        gridRenderer.InitializeGrid(currentMazeData);
        isEditingStartPoint = false;
        UpdateButtonColor();
        editorMode.ExitEditStartPointMode();
    }

    void OnGenerateRandom()
    {
        gridRenderer.ResetSolutionVisibility();
        int size = Mathf.RoundToInt(sizeSlider.value);
        currentMazeData = mazeGenerator.GenerateRandom(size, size);
        gridRenderer.InitializeGrid(currentMazeData);
        isEditingStartPoint = false;
        UpdateButtonColor();
        editorMode.ExitEditStartPointMode();
    }

    void OnSetStartPoint()
    {
        if (currentMazeData == null)
        {
            Debug.LogWarning("Cannot set start point: No maze data available.");
            return;
        }

        gridRenderer.ResetSolutionVisibility();
        isEditingStartPoint = !isEditingStartPoint;

        if (isEditingStartPoint)
        {
            editorMode.EnterEditStartPointMode();
            UpdateButtonColor();
        }
        else
        {
            editorMode.ExitEditStartPointMode();
            UpdateButtonColor();
        }
    }

    void OnShowSolution()
    {
        if (currentMazeData == null)
        {
            Debug.LogWarning("Cannot show solution: No maze data available.");
            return;
        }

        var result = validator.ValidateMaze(currentMazeData, true, false); // Warning UI for invalid, no valid UI
        if (result.success)
        {
            gridRenderer.ShowSolution(result.path);
        }
        else
        {
            gridRenderer.HideSolution();
            Debug.Log("No valid path.");
        }
    }

    void OnLoadMaze()
    {
        gridRenderer.ResetSolutionVisibility();
        fileHandler.LoadMazeFile(); // Non-blocking
    }

    void OnExportMaze()
    {
        if (currentMazeData == null)
        {
            Debug.LogWarning("Cannot export maze: No maze data available.");
            return;
        }

        gridRenderer.ResetSolutionVisibility();
        fileHandler.ExportMazeFile(currentMazeData); // Non-blocking
    }

    void OnValidateMaze()
    {
        if (currentMazeData == null)
        {
            Debug.LogWarning("Cannot validate maze: No maze data available.");
            return;
        }

        validator.ValidateMaze(currentMazeData, true, true); // Show both warning and valid UI
        gridRenderer.ResetSolutionVisibility();
    }

    void OnMazeLoaded(MazeData mazeData)
    {
        if (mazeData != null)
        {
            currentMazeData = mazeData;
            gridRenderer.InitializeGrid(currentMazeData);
            inputHandler.Initialize(currentMazeData, gridRenderer.GetCellButtons());
            isEditingStartPoint = false;
            UpdateButtonColor();
            editorMode.ExitEditStartPointMode();
            Debug.Log("Maze loaded successfully.");
        }
        else
        {
            Debug.LogWarning("Failed to load maze or operation canceled.");
        }
    }

    void OnMazeExported(bool success)
    {
        if (success)
        {
            Debug.Log("Maze exported successfully.");
        }
        else
        {
            Debug.LogWarning("Export failed or canceled.");
        }
    }

    public void ExitEditStartPointMode()
    {
        isEditingStartPoint = false;
        UpdateButtonColor();
    }

    private void UpdateButtonColor()
    {
        if (setStartPointButton != null)
        {
            Image buttonImage = setStartPointButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = isEditingStartPoint ? Color.yellow : originalButtonColor;
            }
        }
    }

    public MazeData GetCurrentMazeData()
    {
        return currentMazeData;
    }

    public MazeGenerator GetMazeGenerator()
    {
        return mazeGenerator;
    }
}