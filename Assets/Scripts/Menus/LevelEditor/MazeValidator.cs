using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MazeValidator : MonoBehaviour
{
    [SerializeField] private GameObject warningMessage;
    [SerializeField] private Button warningOkButton;
    [SerializeField] private GameObject validMessage;
    [SerializeField] private Button validOkButton;
    [SerializeField] private Pathfinder pathfinder;

    void Start()
    {
        if (warningMessage == null) Debug.LogError("Validation Warning Message not assigned!");
        if (warningOkButton == null) Debug.LogError("Warning Ok Button not assigned!");
        if (validMessage == null) Debug.LogError("Validation Valid Message not assigned!");
        if (validOkButton == null) Debug.LogError("Valid Ok Button not assigned!");
        if (pathfinder == null) Debug.LogError("Pathfinder not assigned!");

        if (warningOkButton != null)
        {
            warningOkButton.onClick.AddListener(HideWarning);
        }
        if (validOkButton != null)
        {
            validOkButton.onClick.AddListener(HideValidMessage);
        }

        HideWarning();
        HideValidMessage();
    }

    public (bool success, List<Vector2Int> path) ValidateMaze(MazeData mazeData, bool showUIMessages = true, bool showValidMessage = true)
    {
        HideWarning();
        HideValidMessage();

        if (!CheckSquareMaze(mazeData) || !CheckSizeAndCellCount(mazeData) || !CheckStartAndEnd(mazeData))
            {
                if (showUIMessages) ShowWarning("Basic structural validation failed.");
                return (false, null);
            }

        var (path, pathLength, turns) = pathfinder.FindPath(mazeData, mazeData.start, mazeData.end);
        if (path == null || path.Count == 0)
        {
            if (showUIMessages) ShowWarning("No valid path found from start to end!");
            return (false, null);
        }

        if (showUIMessages && showValidMessage)
            {
                ShowValidMessage($"Valid Maze! Path found with {pathLength} steps and {turns} turns.");
            }

            return (true, path);
    }

    public bool CheckSquareMaze(MazeData mazeData)
    {
        if (mazeData == null) return false;
        return mazeData.rows == mazeData.columns;
    }

    public bool CheckSizeAndCellCount(MazeData mazeData)
    {
        if (mazeData == null || mazeData.cells == null) return false;
        if (mazeData.rows < 7 || mazeData.rows > 43 || mazeData.columns < 7 || mazeData.columns > 43) return false;
        return mazeData.cells.GetLength(0) == mazeData.rows && mazeData.cells.GetLength(1) == mazeData.columns;
    }

    public bool CheckStartAndEnd(MazeData mazeData)
    {
        if (mazeData == null || mazeData.cells == null) return false;
        if (mazeData.start == null || mazeData.end == null || mazeData.start == mazeData.end) return false;
        int x = mazeData.start.x, y = mazeData.start.y;
        if (x < 0 || x >= mazeData.rows || y < 0 || y >= mazeData.columns) return false;
        bool hasExit = (!mazeData.cells[x, y].WallBack && x > 0) ||
                       (!mazeData.cells[x, y].WallRight && y < mazeData.columns - 1) ||
                       (!mazeData.cells[x, y].WallFront && x < mazeData.rows - 1) ||
                       (!mazeData.cells[x, y].WallLeft && y > 0);
        return hasExit;
    }

    public (bool success, string message, int wallDelta) CheckWallCount(MazeData mazeData)
    {
        if (mazeData == null || mazeData.cells == null)
            return (false, "Invalid maze data!", 0);

        int rows = mazeData.rows, cols = mazeData.columns;
        int wallCount = 0;
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                if (x < rows - 1 && mazeData.cells[x, y].WallFront) wallCount++;
                if (y < cols - 1 && mazeData.cells[x, y].WallRight) wallCount++;
            }
        }
        int totalWalls = (rows * (cols - 1)) + (cols * (rows - 1));
        float wallPercentage = (float)wallCount / totalWalls;

        if (wallPercentage >= 0.2f && wallPercentage <= 0.7f)
        {
            return (true, "", 0);
        }
        else if (wallPercentage < 0.2f)
        {
            int minWalls = Mathf.CeilToInt(totalWalls * 0.2f);
            int wallsNeeded = minWalls - wallCount;
            return (false, $"Not enough walls, should be between 20-70% ({wallsNeeded})", wallsNeeded);
        }
        else
        {
            int maxWalls = Mathf.FloorToInt(totalWalls * 0.7f);
            int wallsExcess = wallCount - maxWalls;
            return (false, $"Too many walls, should be between 20-70% ({wallsExcess})", -wallsExcess);
        }
    }

    public bool CheckWallDensity(MazeData mazeData)
    {
        if (mazeData == null || mazeData.cells == null) return false;
        int rows = mazeData.rows, cols = mazeData.columns;
        int noWallCells = 0;
        int maxNoWallCells = rows == 7 ? 5 : 9;
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                bool noWalls = !mazeData.cells[x, y].WallBack &&
                               !mazeData.cells[x, y].WallRight &&
                               !mazeData.cells[x, y].WallFront &&
                               !mazeData.cells[x, y].WallLeft;
                if (noWalls) noWallCells++;
                if (noWallCells > maxNoWallCells) return false;
            }
        }
        return true;
    }

    public void ShowWarning(string message)
    {
        if (warningMessage == null)
        {
            Debug.LogError("Validation warning message not assigned!");
            return;
        }
        Debug.Log($"ShowWarning: {message}");
        HideValidMessage();
        warningMessage.SetActive(true);
        GameObject validationPanel = warningMessage.transform.parent.gameObject;
        if (validationPanel != null)
        {
            validationPanel.SetActive(true);
            Debug.Log("ValidationPanel activated.");
        }
        else
        {
            Debug.LogError("ValidationPanel not found!");
        }
        TMP_Text text = warningMessage.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = message;
        }
        else
        {
            Debug.LogError("Validation warning message has no TMP_Text component!");
        }
    }

    private void HideWarning()
    {
        if (warningMessage != null)
        {
            warningMessage.SetActive(false);
            if (validMessage != null && !validMessage.activeSelf)
            {
                GameObject validationPanel = warningMessage.transform.parent.gameObject;
                if (validationPanel != null)
                {
                    validationPanel.SetActive(false);
                }
            }
        }
    }

    public void ShowValidMessage(string message)
    {
        if (validMessage == null)
        {
            Debug.LogError("Validation valid message not assigned!");
            return;
        }
        Debug.Log($"ShowValidMessage: {message}");
        HideWarning();
        validMessage.SetActive(true);
        GameObject validationPanel = validMessage.transform.parent.gameObject;
        if (validationPanel != null)
        {
            validationPanel.SetActive(true);
            Debug.Log("ValidationPanel activated.");
        }
        else
        {
            Debug.LogError("ValidationPanel not found!");
        }
        TMP_Text text = validMessage.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = message;
        }
        else
        {
            Debug.LogError("Validation valid message has no TMP_Text component!");
        }
    }

    private void HideValidMessage()
    {
        if (validMessage != null)
        {
            validMessage.SetActive(false);
            if (warningMessage != null && !warningMessage.activeSelf)
            {
                GameObject validationPanel = validMessage.transform.parent.gameObject;
                if (validationPanel != null)
                {
                    validationPanel.SetActive(false);
                }
            }
        }
    }
}