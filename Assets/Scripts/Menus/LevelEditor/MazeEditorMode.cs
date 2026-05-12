using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MazeEditorMode : MonoBehaviour
{
    [SerializeField] private GameObject invalidSelectionMessage; // Changed to GameObject to include image background

    private MazeInputHandler inputHandler;
    private MazeData mazeData;
    private Button[,] cellButtons;
    private int rows, cols;
    private bool isEditingStartPoint = false;
    private Dictionary<Vector2Int, Color> originalCornerColors;
    private MazeGenerator mazeGenerator;

    void Awake()
    {
        originalCornerColors = new Dictionary<Vector2Int, Color>();
    }

    void Start()
    {
        if (invalidSelectionMessage != null)
        {
            invalidSelectionMessage.SetActive(false);
        }
        else
        {
            Debug.LogError("Invalid Selection Message is not assigned in the Inspector. Please assign the GameObject containing the warning UI.");
        }

        var controller = GetComponentInParent<MazeEditorController>();
        mazeGenerator = controller != null ? controller.GetMazeGenerator() : null;
        if (mazeGenerator == null)
        {
            Debug.LogError("MazeGenerator not found. Please ensure MazeEditorController and MazeGenerator are properly set up.");
        }

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
        if (mazeData == null || mazeData.cells == null)
        {
            Debug.LogWarning("Cannot enter Edit Start Point mode: mazeData or mazeData.cells is null.");
            return;
        }

        isEditingStartPoint = true;

        originalCornerColors.Clear();
        Vector2Int[] corners = GetCornerCells();
        foreach (Vector2Int corner in corners)
        {
            Image cellImage = cellButtons[corner.x, corner.y].GetComponent<Image>();
            originalCornerColors[corner] = cellImage.color;
            cellImage.color = Color.blue;
        }

        if (invalidSelectionMessage != null)
        {
            invalidSelectionMessage.SetActive(false);
        }
    }

    public void ExitEditStartPointMode()
    {
        isEditingStartPoint = false;

        foreach (var corner in originalCornerColors)
        {
            Vector2Int pos = corner.Key;
            Image cellImage = cellButtons[pos.x, pos.y].GetComponent<Image>();
            cellImage.color = corner.Value;
        }
        originalCornerColors.Clear();

        if (invalidSelectionMessage != null)
        {
            invalidSelectionMessage.SetActive(false);
        }

        UpdateGrid();
    }

    public void HandleStartPointSelection(int x, int y)
    {
        if (IsCornerCell(x, y))
        {
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    mazeData.cells[i, j].IsStart = false;
                }
            }

            mazeData.cells[x, y].IsStart = true;
            mazeData.start = new Vector2Int(x, y);

            if (mazeGenerator != null)
            {
                mazeGenerator.SetEndPointOppositeStart();
            }
            else
            {
                Debug.LogWarning("MazeGenerator is null. Cannot update end point.");
            }

            ExitEditStartPointMode();

            var editorController = GetComponentInParent<MazeEditorController>();
            if (editorController != null)
            {
                editorController.ExitEditStartPointMode();
            }
        }
        else
        {
            if (invalidSelectionMessage != null)
            {
                TMP_Text textComponent = invalidSelectionMessage.GetComponentInChildren<TMP_Text>();
                if (textComponent != null)
                {
                    textComponent.text = "Please select a corner cell!";
                }
                invalidSelectionMessage.SetActive(true);
            }
        }
    }

    public bool IsEditingStartPoint()
    {
        return isEditingStartPoint;
    }

    private void OnMoveModeChanged(bool isEnabled)
    {
        if (isEnabled && isEditingStartPoint)
        {
            ExitEditStartPointMode();
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

    private bool IsCornerCell(int x, int y)
    {
        return (x == 0 && y == 0) ||
               (x == 0 && y == cols - 1) ||
               (x == rows - 1 && y == 0) ||
               (x == rows - 1 && y == cols - 1);
    }

    private Vector2Int[] GetCornerCells()
    {
        return new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(0, cols - 1),
            new Vector2Int(rows - 1, 0),
            new Vector2Int(rows - 1, cols - 1)
        };
    }
}