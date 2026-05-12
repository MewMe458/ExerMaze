using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MazeGenerator3D))]
public class MazeGenerator3DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MazeGenerator3D mazeGenerator = (MazeGenerator3D)target;

        // Draw default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Maze Controls", EditorStyles.boldLabel);

        // Button layout
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Generate Maze", GUILayout.Height(30)))
        {
            mazeGenerator.GenerateMaze();
        }

        if (GUILayout.Button("Clear Maze", GUILayout.Height(30)))
        {
            mazeGenerator.ClearMaze();
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Size Presets", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Small (5x5x5)"))
        {
            mazeGenerator.Width = 5;
            mazeGenerator.Height = 5;
            mazeGenerator.Depth = 5;
            EditorUtility.SetDirty(mazeGenerator);
        }

        if (GUILayout.Button("Medium (10x10x10)"))
        {
            mazeGenerator.Width = 10;
            mazeGenerator.Height = 10;
            mazeGenerator.Depth = 10;
            EditorUtility.SetDirty(mazeGenerator);
        }

        if (GUILayout.Button("Large (15x15x15)"))
        {
            mazeGenerator.Width = 15;
            mazeGenerator.Height = 15;
            mazeGenerator.Depth = 15;
            EditorUtility.SetDirty(mazeGenerator);
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Maze Information", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            $"Status: {mazeGenerator.GetMazeInfo()}",
            MessageType.Info
        );

        // Warning for large mazes
        if (mazeGenerator.Width * mazeGenerator.Height * mazeGenerator.Depth > 1000)
        {
            EditorGUILayout.HelpBox(
                "Large maze detected! Consider using iterative generation.",
                MessageType.Warning
            );
        }
    }
}

#region ReadOnly Attribute

// Custom attribute for read-only fields in Inspector
public class ReadOnlyAttribute : PropertyAttribute { }

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label
    )
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}

#endregion
