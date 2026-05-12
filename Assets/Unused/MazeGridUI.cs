using UnityEngine;

public class MazeGridUI : MonoBehaviour
{
    [SerializeField] private MazeGridRenderer gridRenderer;
    [SerializeField] private MazeEditorMode editorMode;

    void Start()
    {
        gridRenderer = gridRenderer ?? GetComponent<MazeGridRenderer>();
        editorMode = editorMode ?? GetComponent<MazeEditorMode>();

        if (gridRenderer == null || editorMode == null)
        {
            Debug.LogError($"Missing components: gridRenderer is {(gridRenderer == null ? "null" : "not null")}, editorMode is {(editorMode == null ? "null" : "not null")}");
        }
    }

    public void InitializeGrid(MazeData mazeData)
    {
        if (gridRenderer != null)
        {
            gridRenderer.InitializeGrid(mazeData);
        }
    }

    public void UpdateGrid(MazeData mazeData)
    {
        if (gridRenderer != null)
        {
            gridRenderer.UpdateGrid(mazeData);
        }
    }

    public void SetZoom(float zoom)
    {
        if (gridRenderer != null)
        {
            gridRenderer.SetZoom(zoom);
        }
    }

    public void ZoomIn()
    {
        if (gridRenderer != null)
        {
            gridRenderer.ZoomIn();
        }
    }

    public void ZoomOut()
    {
        if (gridRenderer != null)
        {
            gridRenderer.ZoomOut();
        }
    }

    public void EnterEditStartPointMode()
    {
        if (editorMode != null)
        {
            editorMode.EnterEditStartPointMode();
        }
    }

    public void ExitEditStartPointMode()
    {
        if (editorMode != null)
        {
            editorMode.ExitEditStartPointMode();
        }
    }
}