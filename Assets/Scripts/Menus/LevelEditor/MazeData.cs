using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MazeData
{
    public string mode;
    public int rows;
    public int columns;
    public CellData[,] cells;
    public List<SerializableCellData> cellsSerialized;
    public Vector2Int start;
    public Vector2Int end;
    public List<ElementData> elements = new List<ElementData>();

    public void PrepareForSerialization()
    {
        cellsSerialized = new List<SerializableCellData>();
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                cellsSerialized.Add(new SerializableCellData
                {
                    x = x,
                    y = y,
                    IsVisited = cells[x, y].IsVisited,
                    WallRight = cells[x, y].WallRight,
                    WallFront = cells[x, y].WallFront,
                    WallLeft = cells[x, y].WallLeft,
                    WallBack = cells[x, y].WallBack,
                    IsGoal = cells[x, y].IsGoal,
                    IsStart = cells[x, y].IsStart,
                    MaterialIndex = cells[x, y].MaterialIndex // Store the texture ID
                });
            }
        }
        cells = null;
    }

    public void RestoreAfterDeserialization()
    {
        // Ensure rows and columns are valid to avoid creating a null or empty array
        if (rows <= 0 || columns <= 0)
        {
            Debug.LogError("Restore failed: Invalid maze dimensions.");
            return;
        }

        cells = new CellData[rows, columns];

        // CRITICAL FIX: Check if the serialized list exists before looping
        if (cellsSerialized == null)
        {
            Debug.LogError("Restore: cellsSerialized list is null. Initializing empty maze.");
            // InitializeEmptyCells();
            return;
        }

        foreach (var serializedCell in cellsSerialized)
        {
            // Safety check to ensure indices are within array bounds
            if (serializedCell.x >= 0 && serializedCell.x < rows && 
                serializedCell.y >= 0 && serializedCell.y < columns)
            {
                cells[serializedCell.x, serializedCell.y] = new CellData
                {
                    IsVisited = serializedCell.IsVisited,
                    WallRight = serializedCell.WallRight,
                    WallFront = serializedCell.WallFront,
                    WallLeft = serializedCell.WallLeft,
                    WallBack = serializedCell.WallBack,
                    IsGoal = serializedCell.IsGoal,
                    IsStart = serializedCell.IsStart,
                    MaterialIndex = serializedCell.MaterialIndex 
                };
            }
        }
        cellsSerialized = null; // Clear to save memory
    }

    private void InitializeEmptyCells()
    {
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                cells[x, y] = new CellData();
            }
        }
    }

    [Serializable]
    public class CellData
    {
        public bool IsVisited = false;
        public bool WallRight = true;
        public bool WallFront = true;
        public bool WallLeft = true;
        public bool WallBack = true;
        public bool IsGoal = false;
        public bool IsStart = false;
        public int MaterialIndex = -1; // Added to track wall texture
    }

    [Serializable]
    public class SerializableCellData
    {
        public int x;
        public int y;
        public bool IsVisited;
        public bool WallRight;
        public bool WallFront;
        public bool WallLeft;
        public bool WallBack;
        public bool IsGoal;
        public bool IsStart;
        public int MaterialIndex; // Added for serialization
    }

    [Serializable]
    public class ElementData
    {
        public Vector2Int position;
        public string elementType;
        public float detection = 0f;
    }
}