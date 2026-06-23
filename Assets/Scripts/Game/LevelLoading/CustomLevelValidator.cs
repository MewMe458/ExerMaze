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
            ShowError(mazeData.cells == null ? "Invalid maze data! Cell count does not match dimensions!" : "Maze size out of range! Must be between 7x7 and 43x43!");
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

    // 1. Relaxed wall minimums from 20% to 10% to allow for more open concept mazes
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

        if (wallPercentage >= 0.1f && wallPercentage <= 0.8f) // Relaxed range
        {
            return (true, "", 0);
        }
        else if (wallPercentage < 0.1f)
        {
            int minWalls = Mathf.CeilToInt(totalWalls * 0.1f);
            int wallsNeeded = minWalls - wallCount;
            return (false, $"Not enough walls, should be between 10-80% ({wallsNeeded})", wallsNeeded);
        }
        else
        {
            int maxWalls = Mathf.FloorToInt(totalWalls * 0.8f);
            int wallsExcess = wallCount - maxWalls;
            return (false, $"Too many walls, should be between 10-80% ({wallsExcess})", -wallsExcess);
        }
    }

    // 2. Changed hardcoded '9' to a dynamic percentage (allow 25% of the maze to be empty)
    private bool CheckWallDensity(MazeData mazeData)
    {
        int rows = mazeData.rows, cols = mazeData.columns;
        int noWallCells = 0;
        
        // Dynamically calculate based on grid size instead of a hard limit
        int maxNoWallCells = Mathf.FloorToInt(rows * cols * 0.25f); 
        
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

    // 3. Fixed logic flaw: The original code only checked if the START cell had an exit, ignoring the END cell.
    private bool CheckStartAndEnd(MazeData mazeData)
    {
        if (mazeData.start == null || mazeData.end == null || mazeData.start == mazeData.end) return false;
        
        // Check Start Exit
        int sx = mazeData.start.x, sy = mazeData.start.y;
        if (sx < 0 || sx >= mazeData.rows || sy < 0 || sy >= mazeData.columns) return false;
        bool startHasExit = (!mazeData.cells[sx, sy].WallBack && sx > 0) ||
                            (!mazeData.cells[sx, sy].WallRight && sy < mazeData.columns - 1) ||
                            (!mazeData.cells[sx, sy].WallFront && sx < mazeData.rows - 1) ||
                            (!mazeData.cells[sx, sy].WallLeft && sy > 0);

        // Check End Exit
        int ex = mazeData.end.x, ey = mazeData.end.y;
        if (ex < 0 || ex >= mazeData.rows || ey < 0 || ey >= mazeData.columns) return false;
        bool endHasExit = (!mazeData.cells[ex, ey].WallBack && ex > 0) ||
                          (!mazeData.cells[ex, ey].WallRight && ey < mazeData.columns - 1) ||
                          (!mazeData.cells[ex, ey].WallFront && ex < mazeData.rows - 1) ||
                          (!mazeData.cells[ex, ey].WallLeft && ey > 0);

        return startHasExit && endHasExit;
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