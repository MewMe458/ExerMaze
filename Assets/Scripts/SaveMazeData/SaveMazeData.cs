using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using Unity.AI.Navigation;
using Unity.Collections;
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
    public SerializableVector3 scale; // Added scale
    public int materialIndex = -1;    // Added to track wallMaterials array
}

[System.Serializable]
public class SaveMazeData
{
    public int width;
    public int depth;
    public List<ObjectData> walls = new List<ObjectData>();
    public List<ObjectData> npcs = new List<ObjectData>();
    public List<ObjectData> collectibles = new List<ObjectData>();
    public List<ObjectData> endGoal = new List<ObjectData>();
    public ObjectData floor; // Added specific field for floor
    public ObjectData playerData;
}