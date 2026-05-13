using UnityEngine;
using System.Collections.Generic;

public class CustomLevelValidator : MonoBehaviour
{
    [SerializeField] private CustomLevelPopUp popUpManager;

    void Start()
    {
        if (popUpManager == null) Debug.LogError("CustomLevelPopUp not assigned!");
    }

    public bool ValidateMaze(MazeData mazeData)
    {
        if (mazeData == null || mazeData.cells == null)
        {
            ShowError("Invalid maze data!");
            return false;
        }

        if (!CheckSquareMaze(mazeData))
        {
            ShowError("Maze must be square (rows must equal columns)!");
            return false;
        }

        if (!CheckSizeAndCellCount(mazeData))
        {
            ShowError(mazeData.cells == null ? "Invalid maze data! Cell count does not match dimensions!" : "Maze size out of range! Must be between 7x7 and 11x11!");
            return false;
        }

        if (!CheckStartAndEnd(mazeData))
        {
            ShowError("Invalid start or end placement!");
            return false;
        }

        var (wallSuccess, wallMessage, _) = CheckWallCount(mazeData);
        if (!wallSuccess)
        {
            ShowError(wallMessage);
            return false;
        }

        if (!CheckWallDensity(mazeData))
        {
            ShowError("Too many cells with no walls!");
            return false;
        }

        return true;
    }

    private bool CheckSquareMaze(MazeData mazeData)
    {
        return mazeData.rows == mazeData.columns;
    }

    private bool CheckSizeAndCellCount(MazeData mazeData)
    {
        if (mazeData.rows < 7 || mazeData.rows > 43 || mazeData.columns < 7 || mazeData.columns > 43) return false;
        return mazeData.cells.GetLength(0) == mazeData.rows && mazeData.cells.GetLength(1) == mazeData.columns;
    }

    private bool CheckStartAndEnd(MazeData mazeData)
    {
        if (mazeData.start == null || mazeData.end == null || mazeData.start == mazeData.end) return false;
        int x = mazeData.start.x, y = mazeData.start.y;
        if (x < 0 || x >= mazeData.rows || y < 0 || y >= mazeData.columns) return false;
        bool hasExit = (!mazeData.cells[x, y].WallBack && x > 0) ||
                       (!mazeData.cells[x, y].WallRight && y < mazeData.columns - 1) ||
                       (!mazeData.cells[x, y].WallFront && x < mazeData.rows - 1) ||
                       (!mazeData.cells[x, y].WallLeft && y > 0);
        return hasExit;
    }

    private (bool success, string message, int wallDelta) CheckWallCount(MazeData mazeData)
    {
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

    private bool CheckWallDensity(MazeData mazeData)
    {
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

    private void ShowError(string message)
    {
        if (popUpManager != null)
        {
            popUpManager.ShowErrorPopUp(message);
        }
        else
        {
            Debug.LogError($"Validation Error: {message}");
        }
    }
}