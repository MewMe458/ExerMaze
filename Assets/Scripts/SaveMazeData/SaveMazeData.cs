using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SerializableVector3
{
    public float x, y, z;
    public SerializableVector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[System.Serializable]
public class ObjectData
{
    public string type;
    public SerializableVector3 position;
    public SerializableVector3 rotation;
    public SerializableVector3 scale; 
    public int materialIndex = -1;    
}

[System.Serializable]
public class SaveMazeData
{
    public string sceneName; 
    public string levelName;       
    public string customLevelPath; 
    public string mazeShape; 

    // Campaign and Continuous Session Tracking
    public List<string> customLevelQueue = new List<string>();
    public int currentCustomLevelIndex = 0;
    public bool isContinuingSession = false;

    public int width;
    public int depth;
    public List<ObjectData> walls = new List<ObjectData>();
    public List<ObjectData> npcs = new List<ObjectData>();
    public List<ObjectData> collectibles = new List<ObjectData>();
    public List<ObjectData> endGoal = new List<ObjectData>();
    
    public ObjectData floor; // Left for backwards compatibility with older saves
    public List<ObjectData> floors = new List<ObjectData>(); // NEW: Captures all segmented mask floor tiles
    
    public ObjectData playerData;
}