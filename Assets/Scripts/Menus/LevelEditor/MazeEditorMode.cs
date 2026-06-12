using UnityEngine;
using UnityEngine.UI;

public class MazeEditorMode : MonoBehaviour
{
    private MazeInputHandler inputHandler;
    private MazeData mazeData;
    private Button[,] cellButtons;
    private int rows, cols;
    private bool isEditingWallColor = false;
    private int globalMaterialIndex = -1; 
    private MazeGenerator mazeGenerator;

    void Start()
    {
        var controller = GetComponentInParent<MazeEditorController>();
        mazeGenerator = controller != null ? controller.GetMazeGenerator() : null;

        inputHandler = GetComponent<MazeInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.OnMoveModeChanged += OnMoveModeChanged;
        }
    }

    public void Initialize(MazeData mazeData, Button[,] buttons)
    {
        this.mazeData = mazeData;
        this.cellButtons = buttons;
        if (mazeData != null)
        {
            this.rows = mazeData.rows;
            this.cols = mazeData.columns;
        }
    }

    public void EnterEditStartPointMode() { } 

    public void ExitEditStartPointMode()
    {
        UpdateGrid();
    }

    public bool IsEditingStartPoint()
    {
        var controller = GetComponentInParent<MazeEditorController>();
        if (controller == null) return false;
        return controller.SelectedElementType == "StartPoint" || controller.SelectedElementType == "EndPoint";
    }

    private void OnMoveModeChanged(bool isEnabled)
    {
        if (isEnabled)
        {
            var controller = GetComponentInParent<MazeEditorController>();
            if(controller != null) controller.DisableElementPlacement();
            ExitWallColorMode();
        }
    }

    public void UpdateGrid()
    {
        // Safety check to ensure we have valid grid configurations initialized
        if (mazeData == null || mazeData.cells == null) return;

        var gridRenderer = GetComponent<MazeGridRenderer>();
        if (gridRenderer != null)
        {
            gridRenderer.UpdateGrid(mazeData);
        }
    }

    public void EnterWallColorMode() => isEditingWallColor = true;

    public void ExitWallColorMode()
    {
        isEditingWallColor = false;
        globalMaterialIndex = -1;
    }

    public bool IsEditingWallColor() => isEditingWallColor;
    public void SetGlobalMaterialIndex(int index) => globalMaterialIndex = index;
    public int GetGlobalMaterialIndex() => globalMaterialIndex;

    public void ApplyColorToCell(int x, int y)
    {
        if (mazeData == null || globalMaterialIndex < 0) return;
        mazeData.cells[x, y].MaterialIndex = globalMaterialIndex;
        UpdateGrid();
    }

    public enum MazeEditorMode_Enum
    {
        View, EditWalls, SetStart, SetEnd, SetElement
    }
}