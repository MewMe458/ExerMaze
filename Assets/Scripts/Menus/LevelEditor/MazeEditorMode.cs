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
    private bool isEditingStartPoint = false;
    private bool isEditingWallColor = false;
    
    // 🔥 Track global chosen material texture index
    private int globalMaterialIndex = -1; 
    private MazeGenerator mazeGenerator;

    void Awake()
    {
    }

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

    public void EnterEditStartPointMode()
    {
        isEditingStartPoint = true;
    }

    public void ExitEditStartPointMode()
    {
        isEditingStartPoint = false;
        UpdateGrid();
    }

    public bool IsEditingStartPoint()
    {
        return isEditingStartPoint;
    }

    private void OnMoveModeChanged(bool isEnabled)
    {
        if (isEnabled)
        {
            ExitEditStartPointMode();
            ExitWallColorMode(); // 🔥 Automatically clear wall painter mode when leaving
        }
    }

    private void UpdateGrid()
    {
        var gridRenderer = GetComponent<MazeGridRenderer>();
        if (gridRenderer != null)
        {
            gridRenderer.UpdateGrid(mazeData);
        }
    }

    public void EnterWallColorMode()
    {
        isEditingWallColor = true;
    }

    public void ExitWallColorMode()
    {
        isEditingWallColor = false;
        globalMaterialIndex = -1; // Reset selection
    }

    public bool IsEditingWallColor()
    {
        return isEditingWallColor;
    }

    // 🔥 Added to set and get the color selected from popup palette
    public void SetGlobalMaterialIndex(int index)
    {
        globalMaterialIndex = index;
    }

    public int GetGlobalMaterialIndex()
    {
        return globalMaterialIndex;
    }

    public void ApplyColorToCell(int x, int y)
    {
        if (mazeData == null || globalMaterialIndex < 0) return;

        mazeData.cells[x, y].MaterialIndex = globalMaterialIndex;

        // Force GridRenderer to update this cell representation
        var gridRenderer = GetComponent<MazeGridRenderer>();
        if (gridRenderer != null)
        {
            gridRenderer.UpdateGrid(mazeData); 
        }
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