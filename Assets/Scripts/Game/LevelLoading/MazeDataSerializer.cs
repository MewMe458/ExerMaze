using UnityEngine;

public static class MazeDataSerializer
{
    public static MazeData Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("Invalid maze file text: Empty or null");
            return null;
        }

        try
        {
            MazeData mazeData = JsonUtility.FromJson<MazeData>(json);
            if (mazeData == null)
            {
                Debug.LogError("Failed to deserialize maze file");
                return null;
            }

            // Validate rows, columns, and cellsSerialized
            if (mazeData.rows <= 0 || mazeData.columns <= 0)
            {
                Debug.LogError($"Invalid maze dimensions: rows={mazeData.rows}, columns={mazeData.columns}");
                return null;
            }

            if (mazeData.cellsSerialized == null || mazeData.cellsSerialized.Count != mazeData.rows * mazeData.columns)
            {
                Debug.LogError($"Invalid cellsSerialized: expected {mazeData.rows * mazeData.columns}, got {(mazeData.cellsSerialized != null ? mazeData.cellsSerialized.Count : 0)}");
                return null;
            }

            mazeData.RestoreAfterDeserialization();
            if (mazeData.cells == null)
            {
                Debug.LogError("Failed to restore cells after deserialization");
                return null;
            }

            return mazeData;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error deserializing maze data: {ex.Message}");
            return null;
        }
    }
}