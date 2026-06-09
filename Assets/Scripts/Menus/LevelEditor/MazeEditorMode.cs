using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MazeEditorMode : MonoBehaviour
{
    [SerializeField] private GameObject invalidSelectionMessage;

    private MazeInputHandler inputHandler;
    private MazeData mazeData;
    private Button[,] cellButtons;
    private int rows, cols;
    private bool isEditingWallColor = false;
    
    private int globalMaterialIndex = -1; 
    private MazeGenerator mazeGenerator;

    void Start()
    {
        if (invalidSelectionMessage != null)
        {
            invalidSelectionMessage.SetActive(false);
        }

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
        this.rows = mazeData.rows;
        this.cols = mazeData.columns;
    }

    // 🔥 Simplified: We only need to tell the grid to refresh
    public void EnterEditStartPointMode() { } 

    public void ExitEditStartPointMode()
    {
        UpdateGrid();
    }

    // 🔥 This is now handled by MazeEditorController's Enum state
    public bool IsEditingStartPoint()
    {
        var controller = GetComponentInParent<MazeEditorController>();
        if (controller == null) return false;

        // Returns true if either editor mode tool is currently operating the grid placement sequence
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
        View,
        EditWalls,
        SetStart,
        SetEnd,
        SetElement
    }
}