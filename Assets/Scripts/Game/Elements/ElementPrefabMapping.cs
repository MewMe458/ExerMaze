using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ElementPrefabPair
{
    public string type;
    public GameObject prefab;
}

public class ElementPrefabMapping : MonoBehaviour
{
    [SerializeField] private List<ElementPrefabPair> elementPrefabs = new List<ElementPrefabPair>();

    public GameObject GetPrefabForType(string type)
    {
        ElementPrefabPair pair = elementPrefabs.Find(p => p.type == type);
        if (pair != null && pair.prefab != null)
        {
            return pair.prefab;
        }
        Debug.LogWarning($"No prefab found for element type: {type}");
        return null;
    }
}