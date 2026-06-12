using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MazeInputHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public event Action<int, int, WallDirection> OnWallToggled;
    public event Action<bool> OnMoveModeChanged;

    [SerializeField] private GraphicRaycaster graphicRaycaster;
    [SerializeField] private Toggle addToggle;
    [SerializeField] private TMP_Dropdown wallDirectionDropdown;
    [SerializeField] private Button editButton;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button wallColorButton;
    [SerializeField] private Image editOverlay;
    [SerializeField] private Image moveOverlay;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private MazeGridRenderer gridRenderer; 
    [SerializeField] private MazeEditorController editorController;

    [Header("UI for setting elements")]
    [SerializeField] private Toggle relaxToggle; 
    [SerializeField] private Toggle challengeToggle; 
    [SerializeField] private GameObject elementTogglesPanel; 

    private MazeEditorMode editorMode;
    private MazeData mazeData;
    private Button[,] cellButtons;
    private int rows, cols;
    private bool isMoveMode = false;
    private bool isWallColorMode = false;
    private bool isDeleteElementMode = false;
    private bool isDragging = false;
    private HashSet<Vector2Int> processedCells;
    private Vector2Int? lastProcessedCell;

    public enum WallDirection { Top, Right, Bottom, Left }

    void Awake() => processedCells = new HashSet<Vector2Int>();

    void Start()
    {
        if (graphicRaycaster == null)
            graphicRaycaster = GetComponentInParent<Canvas>()?.GetComponent<GraphicRaycaster>();

        if (wallDirectionDropdown != null)
        {
            wallDirectionDropdown.ClearOptions(); 
            wallDirectionDropdown.AddOptions(new List<string> { "Top", "Right", "Bottom", "Left" }); 
            wallDirectionDropdown.value = 0; 
        }

        if (moveButton != null) moveButton.onClick.AddListener(OnMoveButtonClick);
        if (editButton != null) editButton.onClick.AddListener(OnEditButtonClick);
        if (wallColorButton != null) wallColorButton.onClick.AddListener(OnWallColorButtonClick);

        if (relaxToggle != null && challengeToggle != null)
        {
            relaxToggle.onValueChanged.AddListener(OnRelaxToggleChanged);
            challengeToggle.onValueChanged.AddListener(OnChallengeToggleChanged);
            relaxToggle.isOn = true; 
        }

        UpdateButtonAppearances();
        ApplyCurrentMode();
        editorMode = GetComponent<MazeEditorMode>();
    }

    void Update()
    {
        HandleWASDShortcutInterception();
        if (isMoveMode) return;

        if (isDragging && graphicRaycaster != null && mazeData != null && wallDirectionDropdown != null)
        {
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            List<RaycastResult> results = new List<RaycastResult>();
            graphicRaycaster.Raycast(pointerEventData, results);

            foreach (RaycastResult result in results)
            {
                Button hitButton = result.gameObject.GetComponent<Button>();
                if (hitButton != null && hitButton.name.StartsWith("Cell_"))
                {
                    string[] nameParts = hitButton.name.Split('_');
                    if (nameParts.Length == 3 && int.TryParse(nameParts[1], out int x) && int.TryParse(nameParts[2], out int y))
                    {
                        Vector2Int currentCell = new Vector2Int(x, y);
                        if (processedCells.Contains(currentCell) || (lastProcessedCell.HasValue && lastProcessedCell.Value == currentCell)) continue;

                        WallDirection direction = (WallDirection)wallDirectionDropdown.value;
                        bool addWall = !GetCurrentWallState(x, y, direction);

                        ToggleWall(x, y, direction, addWall);
                        processedCells.Add(currentCell);
                        lastProcessedCell = currentCell;

                        OnWallToggled?.Invoke(x, y, direction);
                    }
                }
            }
        }
    }

    private void HandleWASDShortcutInterception()
    {
        bool isWPressed = Input.GetKeyDown(KeyCode.W);
        bool isAPressed = Input.GetKeyDown(KeyCode.A);
        bool isSPressed = Input.GetKeyDown(KeyCode.S);
        bool isDPressed = Input.GetKeyDown(KeyCode.D);

        if (isWPressed || isAPressed || isSPressed || isDPressed)
        {
            bool isWallColorModeActive = editorMode != null && editorMode.IsEditingWallColor();
            if (isMoveMode || isDeleteElementMode || isWallColorModeActive)
            {
                ForceReturnToEditMode();
            }

            if (wallDirectionDropdown != null)
            {
                if (isWPressed) SetWallDirectionDropdownValue(WallDirection.Top);    
                if (isDPressed) SetWallDirectionDropdownValue(WallDirection.Right);  
                if (isSPressed) SetWallDirectionDropdownValue(WallDirection.Bottom); 
                if (isAPressed) SetWallDirectionDropdownValue(WallDirection.Left);   
            }
        }
    }

    private void SetWallDirectionDropdownValue(WallDirection direction)
    {
        if (wallDirectionDropdown != null)
        {
            wallDirectionDropdown.value = (int)direction;
            wallDirectionDropdown.RefreshShownValue();
        }
    }

    public void OnPointerDown(int x, int y, BaseEventData eventData)
    {
        if (mazeData == null || mazeData.cells == null) return;
        var pointerData = eventData as PointerEventData;
        if (pointerData == null || isMoveMode) return;

        if (editorController != null && !string.IsNullOrEmpty(editorController.SelectedElementType))
        {
            if (pointerData.button == PointerEventData.InputButton.Left)
                editorController.TryPlaceElement(new Vector2Int(x, y));
            return; 
        }

        if (editorMode != null && editorMode.IsEditingStartPoint())
        {
            if (pointerData.button == PointerEventData.InputButton.Left && gridRenderer != null)
                gridRenderer.HideSolution();
            return;
        }

        if (editorMode != null && editorMode.IsEditingWallColor())
        {
            if (editorMode.GetGlobalMaterialIndex() < 0)
            {
                WallColorPopup popup = FindObjectOfType<WallColorPopup>();
                if (popup != null) popup.Open();
                return;
            }
            editorMode.ApplyColorToCell(x, y);
            return;
        }

        if (isDeleteElementMode)
        {
            MazeData.ElementData elementToRemove = mazeData.elements.Find(e => e.position.x == x && e.position.y == y);
            if (elementToRemove != null)
            {
                mazeData.elements.Remove(elementToRemove);
                if (gridRenderer != null)
                {
                    gridRenderer.DestroyElementAt(x, y);
                    gridRenderer.HideSolution();
                }
            }
            return; 
        }

        if (pointerData.button == PointerEventData.InputButton.Left || pointerData.button == PointerEventData.InputButton.Right)
        {
            isDragging = true;
            processedCells.Clear();
            lastProcessedCell = new Vector2Int(x, y);

            WallDirection direction = (WallDirection)wallDirectionDropdown.value;
            ToggleWall(x, y, direction, !GetCurrentWallState(x, y, direction));
            processedCells.Add(lastProcessedCell.Value);

            OnWallToggled?.Invoke(x, y, direction);
        }
    }

    public void OnPointerUp() => isDragging = false;

    public void OnPointerEnter(int x, int y, BaseEventData eventData)
    {
        if (mazeData == null || mazeData.cells == null || isMoveMode) return;
        if (editorMode != null && editorMode.IsEditingWallColor() && Input.GetMouseButton(0) && editorMode.GetGlobalMaterialIndex() >= 0)
        {
            editorMode.ApplyColorToCell(x, y);
        }
    }

    public void OnBeginDrag(PointerEventData eventData) { if (isMoveMode && scrollRect != null) scrollRect.OnBeginDrag(eventData); }
    public void OnDrag(PointerEventData eventData) { if (isMoveMode && scrollRect != null) scrollRect.OnDrag(eventData); }
    public void OnEndDrag(PointerEventData eventData) { if (isMoveMode && scrollRect != null) scrollRect.OnEndDrag(eventData); }

    public void SetDeleteElementMode(bool enabled)
    {
        isDeleteElementMode = enabled;
        if (enabled)
        {
            isMoveMode = false;
            if (editorMode != null) editorMode.ExitWallColorMode();
        }
    }

    public void OnMoveButtonClick()
    {
        isMoveMode = true;
        isDeleteElementMode = false;
        if (editorMode != null) editorMode.ExitWallColorMode();
        UpdateButtonAppearances();
        ApplyCurrentMode();
        OnMoveModeChanged?.Invoke(true);
    }

    private void OnEditButtonClick()
    {
        isMoveMode = false;
        isWallColorMode = false;
        if (editorMode != null) editorMode.ExitWallColorMode();
        UpdateButtonAppearances();
        ApplyCurrentMode();
        OnMoveModeChanged?.Invoke(false);
    }

    private void OnWallColorButtonClick()
    {
        if (editorMode == null) return;

        isMoveMode = false;
        isWallColorMode = true;
        
        // Fix: Do not call ExitEditStartPointMode() here anymore to prevent null errors.
        editorMode.EnterWallColorMode();
        
        UpdateButtonAppearances();
        ApplyCurrentMode();

        WallColorPopup popup = FindObjectOfType<WallColorPopup>();
        if (popup != null) popup.Open();
    }

    private void UpdateButtonAppearances()
    {
        if (moveButton != null && moveButton.GetComponent<Image>() != null)
            moveButton.GetComponent<Image>().color = isMoveMode ? Color.green : Color.white;
        if (editButton != null && editButton.GetComponent<Image>() != null)
            editButton.GetComponent<Image>().color = (!isMoveMode && !isWallColorMode) ? Color.green : Color.white;
        if (wallColorButton != null && wallColorButton.GetComponent<Image>() != null)
            wallColorButton.GetComponent<Image>().color = isWallColorMode ? Color.green : Color.white;
    }

    private void ApplyCurrentMode()
    {
        if (moveOverlay != null) moveOverlay.gameObject.SetActive(isMoveMode);
        if (scrollRect != null) scrollRect.enabled = isMoveMode;
    }

    private void ToggleWall(int x, int y, WallDirection direction, bool addWall)
    {
        if (mazeData == null || mazeData.cells == null) return;
        switch (direction)
        {
            case WallDirection.Top: if (x > 0) { mazeData.cells[x, y].WallBack = addWall; mazeData.cells[x - 1, y].WallFront = addWall; } break;
            case WallDirection.Right: if (y < cols - 1) { mazeData.cells[x, y].WallRight = addWall; mazeData.cells[x, y + 1].WallLeft = addWall; } break;
            case WallDirection.Bottom: if (x < rows - 1) { mazeData.cells[x, y].WallFront = addWall; mazeData.cells[x + 1, y].WallBack = addWall; } break;
            case WallDirection.Left: if (y > 0) { mazeData.cells[x, y].WallLeft = addWall; mazeData.cells[x, y - 1].WallRight = addWall; } break;
        }
        if (gridRenderer != null) gridRenderer.HideSolution(); 
    }

    private void OnRelaxToggleChanged(bool isOn)
    {
        if (!isOn) return;
        challengeToggle.isOn = false;
        if (mazeData != null) mazeData.mode = "Relax";
        if (elementTogglesPanel != null) elementTogglesPanel.SetActive(false);
        if (editorController != null) editorController.DisableElementPlacement();
    }

    private void OnChallengeToggleChanged(bool isOn)
    {
        if (!isOn) return;
        relaxToggle.isOn = false;
        if (mazeData != null) mazeData.mode = "Challenge";
        if (elementTogglesPanel != null) elementTogglesPanel.SetActive(true);
    }

    public void UpdateTogglesFromMazeData()
    {
        if (mazeData == null) return;
        bool isChallengeMode = mazeData.mode == "Challenge";
        relaxToggle.isOn = !isChallengeMode;
        challengeToggle.isOn = isChallengeMode;
    }

    public void ForceReturnToEditMode()
    {
        isMoveMode = false;
        isDeleteElementMode = false;
        if (editorMode != null) editorMode.ExitWallColorMode();
        OnEditButtonClick();
    }

    private bool GetCurrentWallState(int x, int y, WallDirection direction)
    {
        if (mazeData == null || mazeData.cells == null) return false;
        MazeData.CellData cell = mazeData.cells[x, y];
        return direction switch
        {
            WallDirection.Top => cell.WallBack,
            WallDirection.Right => cell.WallRight,
            WallDirection.Bottom => cell.WallFront,
            WallDirection.Left => cell.WallLeft,
            _ => false
        };
    }

    public void Initialize(MazeData data, Button[,] buttons)
    {
        this.mazeData = data;
        this.cellButtons = buttons;
        if (data != null)
        {
            this.rows = data.rows;
            this.cols = data.columns;
            UpdateTogglesFromMazeData();
        }
    }
}