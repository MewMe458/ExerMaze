using UnityEngine;
using System.Collections.Generic;
using System;

public class MazeGenerator : MonoBehaviour
{
    private MazeData mazeData;

    public MazeData GenerateEmpty(int rows, int columns)
    {
        mazeData = new MazeData
        {
            mode = "Relax",
            rows = rows,
            columns = columns,
            cells = new MazeData.CellData[rows, columns],
            start = Vector2Int.zero,
            end = new Vector2Int(0, columns - 1),
            elements = new List<MazeData.ElementData>()
        };

        // Initialize all cells with walls
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                mazeData.cells[x, y] = new MazeData.CellData
                {
                    WallRight = true,
                    WallFront = true,
                    WallLeft = true,
                    WallBack = true,
                    IsVisited = false,
                    IsGoal = false,
                    IsStart = false
                };
            }
        }

        // Set start point in a random corner and end point on the opposite side
        SetStartAndEndPoints();

        return mazeData;
    }

    public MazeData GenerateRandom(int rows, int columns)
    {
        mazeData = new MazeData
        {
            mode = "Relax",
            rows = rows,
            columns = columns,
            cells = new MazeData.CellData[rows, columns],
            elements = new List<MazeData.ElementData>()
        };

        // Initialize all cells with walls
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                mazeData.cells[x, y] = new MazeData.CellData
                {
                    WallRight = true,
                    WallFront = true,
                    WallLeft = true,
                    WallBack = true,
                    IsVisited = false,
                    IsGoal = false,
                    IsStart = false
                };
            }
        }

        // 🔥 NEW: DFS Generation instead of Kruskal
        GenerateMazeDFS(0, 0);

        // Optional: add loops like AutoMG3D
        AddExtraConnections(0.25f);

        FixOpenCells();

        ReduceStraightPaths();

        // Start/End
        SetStartAndEndPoints();

        return mazeData;
    }

    private void GenerateMazeDFS(int startX, int startY)
    {
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(startX, startY));
        mazeData.cells[startX, startY].IsVisited = true;

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Pop();

            List<Vector2Int> neighbors = GetUnvisitedNeighbors(current);

            if (neighbors.Count > 0)
            {
                stack.Push(current);

                Vector2Int chosen = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];

                RemoveWallBetween(current, chosen);

                mazeData.cells[chosen.x, chosen.y].IsVisited = true;

                stack.Push(chosen);
            }
        }
    }

    private List<Vector2Int> GetUnvisitedNeighbors(Vector2Int cell)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (var dir in directions)
        {
            Vector2Int next = cell + dir;

            if (IsInBounds(next) && !mazeData.cells[next.x, next.y].IsVisited)
            {
                neighbors.Add(next);
            }
        }

        return neighbors;
    }

    private void RemoveWallBetween(Vector2Int a, Vector2Int b)
    {
        int dx = b.x - a.x;
        int dy = b.y - a.y;

        if (dx == 1) // down
        {
            mazeData.cells[a.x, a.y].WallFront = false;
            mazeData.cells[b.x, b.y].WallBack = false;
        }
        else if (dx == -1) // up
        {
            mazeData.cells[a.x, a.y].WallBack = false;
            mazeData.cells[b.x, b.y].WallFront = false;
        }
        else if (dy == 1) // right
        {
            mazeData.cells[a.x, a.y].WallRight = false;
            mazeData.cells[b.x, b.y].WallLeft = false;
        }
        else if (dy == -1) // left
        {
            mazeData.cells[a.x, a.y].WallLeft = false;
            mazeData.cells[b.x, b.y].WallRight = false;
        }
    }

    private bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < mazeData.rows &&
            pos.y >= 0 && pos.y < mazeData.columns;
    }

    private void AddExtraConnections(float chance)
    {
        for (int x = 0; x < mazeData.rows; x++)
        {
            for (int y = 0; y < mazeData.columns; y++)
            {
                Vector2Int current = new Vector2Int(x, y);

                Vector2Int[] directions =
                {
                    Vector2Int.up,
                    Vector2Int.down,
                    Vector2Int.left,
                    Vector2Int.right
                };

                foreach (var dir in directions)
                {
                    if (UnityEngine.Random.value > chance)
                        continue;

                    Vector2Int neighbor = current + dir;

                    if (!IsInBounds(neighbor))
                        continue;

                    RemoveWallBetween(current, neighbor);
                }
            }
        }
    }

    private void FixOpenCells()
    {
        for (int x = 0; x < mazeData.rows; x++)
        {
            for (int y = 0; y < mazeData.columns; y++)
            {
                var cell = mazeData.cells[x, y];

                bool noWalls = !cell.WallBack && !cell.WallRight &&
                            !cell.WallFront && !cell.WallLeft;

                if (noWalls)
                {
                    // Add ONE random wall back
                    AddRandomWall(x, y);
                }
            }
        }
    }

    private void ReduceStraightPaths()
    {
        for (int x = 1; x < mazeData.rows - 1; x++)
        {
            for (int y = 1; y < mazeData.columns - 1; y++)
            {
                var cell = mazeData.cells[x, y];

                // Horizontal straight corridor
                if (!cell.WallLeft && !cell.WallRight &&
                    cell.WallFront && cell.WallBack)
                {
                    if (UnityEngine.Random.value < 0.3f)
                    {
                        AddRandomWall(x, y);
                    }
                }

                // Vertical straight corridor
                if (!cell.WallFront && !cell.WallBack &&
                    cell.WallLeft && cell.WallRight)
                {
                    if (UnityEngine.Random.value < 0.3f)
                    {
                        AddRandomWall(x, y);
                    }
                }
            }
        }
    }

    private void AddRandomWall(int x, int y)
    {
        List<Action> possibleWalls = new List<Action>();

        if (x > 0)
            possibleWalls.Add(() => {
                mazeData.cells[x, y].WallBack = true;
                mazeData.cells[x - 1, y].WallFront = true;
            });

        if (x < mazeData.rows - 1)
            possibleWalls.Add(() => {
                mazeData.cells[x, y].WallFront = true;
                mazeData.cells[x + 1, y].WallBack = true;
            });

        if (y > 0)
            possibleWalls.Add(() => {
                mazeData.cells[x, y].WallLeft = true;
                mazeData.cells[x, y - 1].WallRight = true;
            });

        if (y < mazeData.columns - 1)
            possibleWalls.Add(() => {
                mazeData.cells[x, y].WallRight = true;
                mazeData.cells[x, y + 1].WallLeft = true;
            });

        if (possibleWalls.Count > 0)
        {
            possibleWalls[UnityEngine.Random.Range(0, possibleWalls.Count)]();
        }
    }

    private int Find(int x, int[] parent)
    {
        if (parent[x] != x)
        {
            parent[x] = Find(parent[x], parent); // Path compression
        }
        return parent[x];
    }

    private void SetStartAndEndPoints()
    {
        // Define the four corners
        Vector2Int[] corners = new Vector2Int[]
        {
            new Vector2Int(0, 0),                    // Top-left
            new Vector2Int(0, mazeData.columns - 1), // Top-right
            new Vector2Int(mazeData.rows - 1, 0),    // Bottom-left
            new Vector2Int(mazeData.rows - 1, mazeData.columns - 1) // Bottom-right
        };

        // Randomly select a corner for the start point
        int startCornerIndex = UnityEngine.Random.Range(0, 4);
        mazeData.start = corners[startCornerIndex];
        mazeData.cells[mazeData.start.x, mazeData.start.y].IsStart = true;

        // Set the end point on the opposite side
        SetEndPointOppositeStart();
    }

    public void SetEndPointOppositeStart()
    {
        // Clear the previous end point
        for (int x = 0; x < mazeData.rows; x++)
        {
            for (int y = 0; y < mazeData.columns; y++)
            {
                mazeData.cells[x, y].IsGoal = false;
            }
        }

        // Set the end point exactly opposite the start point
        if (mazeData.start == new Vector2Int(0, 0)) // Top-left
        {
            // End at bottom-right
            mazeData.end = new Vector2Int(mazeData.rows - 1, mazeData.columns - 1);
        }
        else if (mazeData.start == new Vector2Int(0, mazeData.columns - 1)) // Top-right
        {
            // End at bottom-left
            mazeData.end = new Vector2Int(mazeData.rows - 1, 0);
        }
        else if (mazeData.start == new Vector2Int(mazeData.rows - 1, 0)) // Bottom-left
        {
            // End at top-right
            mazeData.end = new Vector2Int(0, mazeData.columns - 1);
        }
        else if (mazeData.start == new Vector2Int(mazeData.rows - 1, mazeData.columns - 1)) // Bottom-right
        {
            // End at top-left
            mazeData.end = new Vector2Int(0, 0);
        }

        mazeData.cells[mazeData.end.x, mazeData.end.y].IsGoal = true;
    }
}