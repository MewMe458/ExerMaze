using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class MazeEditorController : MonoBehaviour
{
    [SerializeField] private Slider sizeSlider;
    [SerializeField] private Button generateEmptyButton;
    [SerializeField] private Button generateRandomButton;
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button zoomOutButton;
    [SerializeField] private Button setStartPointButton;
    [SerializeField] private Button setEndPointButton;
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

    [Header("Challenge Element Buttons")]
    [SerializeField] private Button dogButton;
    [SerializeField] private Button boneButton;
    [SerializeField] private Button shieldButton;
    [SerializeField] private Button starButton;
    [SerializeField] private Button slowButton;
    [SerializeField] private Button teleportButton;
    [SerializeField] private Button deleteElementButton;

    private Dictionary<Button, string> elementButtonMap;
    private Button currentSelectedButton;

    private MazeData currentMazeData;
    private bool isEditingStartPoint = false;
    private bool isDeleteElementMode = false;
    private bool isEditingEndPoint = false;
    private Color originalEndButtonColor;
    private Color originalButtonColor;

    public MazeData CurrentMaze => currentMazeData;
    public MazeEditorMode.MazeEditorMode_Enum CurrentMode {get; private set;}
    public string SelectedElementType {get; private set;}
    public bool IsChallengeMode => CurrentMaze != null && CurrentMaze.mode == "Challenge";

    public void OnSetEndPointClick() => OnSetEndPoint();
    public void SelectDogNPC() => SelectElement(dogButton, "DogNPC");
    public void SelectBone() => SelectElement(boneButton, "Bones");
    public void SelectShield() => SelectElement(shieldButton, "Shield");
    public void SelectStar() => SelectElement(starButton, "Special");
    public void SelectSlowPotion() => SelectElement(slowButton, "SlowPotion");
    public void SelectTeleporter() => SelectElement(teleportButton, "Teleporter");
    public void SelectDeleteElement() => SelectElement(deleteElementButton, "Delete");

    private void SelectElement(Button clickedButton, string elementType)
    {
        if (IsChallengeMode == false)
        {
            Debug.Log("Cannot place elements in Relax mode.");
            return;
        }

        ResetElementButtonVisuals();
        currentSelectedButton = clickedButton;

        Image img = clickedButton.GetComponent<Image>();
        if (img != null)
            img.color = Color.green;

        CurrentMode = MazeEditorMode.MazeEditorMode_Enum.SetElement;
        SelectedElementType = elementType;

        Debug.Log($"Selected Element: {elementType}");
    }

    public void DisableElementPlacement()
    {
        CurrentMode = MazeEditorMode.MazeEditorMode_Enum.EditWalls;
        SelectedElementType = string.Empty;
        ResetElementButtonVisuals();
    }

    private void ResetElementButtonVisuals()
    {
        Button[] buttons =
        {
            dogButton, boneButton, shieldButton, starButton, slowButton, teleportButton, deleteElementButton
        };

        foreach (Button btn in buttons)
        {
            if (btn == null) continue;
            Image img = btn.GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }
    }

    public bool IsEditingStartPointMode() => isEditingStartPoint;

    public void TryPlaceElement(Vector2Int cellPosition)
    {
        if (currentMazeData == null) return;

        if (SelectedElementType == "StartPoint")
        {
            for (int x = 0; x < currentMazeData.rows; x++)
            {
                for (int y = 0; y < currentMazeData.columns; y++)
                {
                    currentMazeData.cells[x, y].IsStart = false;
                }
            }
            currentMazeData.start = cellPosition;
            currentMazeData.cells[cellPosition.x, cellPosition.y].IsStart = true;

            gridRenderer.UpdateGrid(currentMazeData);
            return;
        }

        if (SelectedElementType == "EndPoint")
        {
            for (int x = 0; x < currentMazeData.rows; x++)
            {
                for (int y = 0; y < currentMazeData.columns; y++)
                {
                    currentMazeData.cells[x, y].IsGoal = false;
                }
            }

            currentMazeData.end = cellPosition;
            currentMazeData.cells[cellPosition.x, cellPosition.y].IsGoal = true;

            gridRenderer.UpdateGrid(currentMazeData);
            return;
        }

        var existing = CurrentMaze.elements.FirstOrDefault(e => e.position == cellPosition);

        if (SelectedElementType == "Delete")
        {
            if (existing != null)
            {
                CurrentMaze.elements.Remove(existing);
                gridRenderer.RemoveElementVisual(cellPosition);
                gridRenderer.HideSolution();
            }
            return;
        }

        if (cellPosition == currentMazeData.start || cellPosition == currentMazeData.end)
        {
            Debug.LogWarning("Cannot modify elements on Start/Goal positions.");
            return;
        }

        if (!IsChallengeMode || string.IsNullOrEmpty(SelectedElementType))
            return;

        if (existing != null)
        {
            CurrentMaze.elements.Remove(existing);
            gridRenderer.RemoveElementVisual(cellPosition);
        }

        MazeData.ElementData element = new MazeData.ElementData
        {
            position = cellPosition,
            elementType = SelectedElementType
        };
        CurrentMaze.elements.Add(element);
        gridRenderer.DrawElement(element);
    }

    void Start()
    {
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
            originalButtonColor = buttonImage != null ? buttonImage.color : Color.white;
        }

        if (setEndPointButton != null)
        {
            setEndPointButton.onClick.AddListener(OnSetEndPoint);
            Image buttonImage = setEndPointButton.GetComponent<Image>();
            if (buttonImage != null) originalEndButtonColor = buttonImage.color;
        }

        if (deleteElementButton != null)
        {
            deleteElementButton.onClick.RemoveAllListeners();
            deleteElementButton.onClick.AddListener(SelectDeleteElement);
        }

        ResetElementButtonVisuals();
        fileHandler.OnMazeLoaded += OnMazeLoaded;
        fileHandler.OnMazeExported += OnMazeExported;
    }

    void OnDestroy()
    {
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
        inputHandler.Initialize(currentMazeData, gridRenderer.GetCellButtons());
        editorMode.Initialize(currentMazeData, gridRenderer.GetCellButtons()); // Sync Editor Mode Reference
        
        isEditingStartPoint = false;
        isEditingEndPoint = false;
        UpdateButtonColor();
        UpdateEndButtonColor();
    }

    void OnGenerateRandom()
    {
        gridRenderer.ResetSolutionVisibility();
        int size = Mathf.RoundToInt(sizeSlider.value);
        currentMazeData = mazeGenerator.GenerateRandom(size, size);
        
        gridRenderer.InitializeGrid(currentMazeData);
        inputHandler.Initialize(currentMazeData, gridRenderer.GetCellButtons());
        editorMode.Initialize(currentMazeData, gridRenderer.GetCellButtons()); // Sync Editor Mode Reference
        
        isEditingStartPoint = false;
        isEditingEndPoint = false;
        UpdateButtonColor();
        UpdateEndButtonColor();
    }

    void OnSetStartPoint()
    {
        gridRenderer.ResetSolutionVisibility();
        isEditingStartPoint = !isEditingStartPoint;

        if (isEditingStartPoint)
        {
            isEditingEndPoint = false;
            UpdateEndButtonColor();
            CurrentMode = MazeEditorMode.MazeEditorMode_Enum.SetElement;
            SelectedElementType = "StartPoint";
            Debug.Log("Start Point Mode Active: Click a cell to set Start.");
        }
        else
        {
            SelectedElementType = "";
        }
        UpdateButtonColor();
    }

    void OnSetEndPoint()
    {
        if (currentMazeData == null) return;

        gridRenderer.ResetSolutionVisibility();
        isEditingEndPoint = !isEditingEndPoint;
        
        if (isEditingEndPoint)
        {
            isEditingStartPoint = false;
            UpdateButtonColor();
            ResetElementButtonVisuals();

            CurrentMode = MazeEditorMode.MazeEditorMode_Enum.SetElement;
            SelectedElementType = "EndPoint";
            Debug.Log("End Goal Mode Active: Click a cell to set Goal.");
        }
        else
        {
            SelectedElementType = "";
        }

        UpdateEndButtonColor();
    }

    void OnShowSolution()
    {
        if (currentMazeData == null) return;
        var result = validator.ValidateMaze(currentMazeData, true, false);
        if (result.success) gridRenderer.ShowSolution(result.path);
        else gridRenderer.HideSolution();
    }

    void OnLoadMaze()
    {
        gridRenderer.ResetSolutionVisibility();
        fileHandler.LoadMazeFile();
    }

    void OnExportMaze()
    {
        if (currentMazeData == null) return;
        gridRenderer.ResetSolutionVisibility();
        fileHandler.ExportMazeFile(currentMazeData);
    }

    void OnValidateMaze()
    {
        if (currentMazeData == null) return;
        validator.ValidateMaze(currentMazeData, true, true);
        gridRenderer.ResetSolutionVisibility();
    }

    void OnMazeLoaded(MazeData mazeData)
    {
        if (mazeData != null)
        {
            currentMazeData = mazeData;
            gridRenderer.InitializeGrid(currentMazeData);
            inputHandler.Initialize(currentMazeData, gridRenderer.GetCellButtons());
            editorMode.Initialize(currentMazeData, gridRenderer.GetCellButtons()); // Sync Editor Mode Reference
            
            isEditingStartPoint = false;
            isEditingEndPoint = false;
            UpdateButtonColor();
            UpdateEndButtonColor();
            Debug.Log("Maze loaded successfully.");
        }
    }

    void OnMazeExported(bool success) { }

    private void UpdateButtonColor()
    {
        if (setStartPointButton != null)
        {
            Image buttonImage = setStartPointButton.GetComponent<Image>();
            if (buttonImage != null) buttonImage.color = isEditingStartPoint ? Color.yellow : originalButtonColor;
        }
    }

    private void UpdateEndButtonColor()
    {
        if (setEndPointButton != null)
        {
            Image buttonImage = setEndPointButton.GetComponent<Image>();
            if (buttonImage != null) buttonImage.color = isEditingEndPoint ? Color.yellow : originalEndButtonColor;
        }
    }

    public MazeGenerator GetMazeGenerator() => mazeGenerator;
}