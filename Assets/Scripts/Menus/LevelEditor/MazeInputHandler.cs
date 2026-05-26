using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MazeInputHandler : MonoBehaviour
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
    [SerializeField] private MazeGridRenderer gridRenderer; // Added for solution path hiding
    [SerializeField] private MazeEditorController editorController;

    #region UI for setting elements
    [SerializeField] private Toggle relaxToggle; // Toggle for Relax mode
    [SerializeField] private Toggle challengeToggle; // Toggle for Challenge mode
    [SerializeField] private GameObject elementTogglesPanel; // Panel with toggles and inputs
    [SerializeField] private Button dogButton;
    [SerializeField] private Button boneButton;
    [SerializeField] private Button shieldButton;
    [SerializeField] private Button starButton;
    [SerializeField] private Button slowButton;
    [SerializeField] private Button teleportButton;
    #endregion

    #region Prefabs for NPCs and Items
    [SerializeField] private GameObject dogPrefab;
    [SerializeField] private GameObject bonePrefab;
    [SerializeField] private GameObject shieldPrefab;
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private GameObject slowPrefab;
    [SerializeField] private GameObject teleportPrefab;
    #endregion

    #region Icons for NPCs and Items
    #endregion

    private Camera mainCamera;
    private MazeEditorMode editorMode;
    private MazeData mazeData;
    private Button[,] cellButtons;
    private int rows, cols;
    private bool isMoveMode = false;
    private bool isWallColorMode = false;
    private bool isSetElementsMode = false;
    private bool isPlaceDogMode = false;
    private bool isPlaceBoneMode = false;
    private bool isPlaceShieldMode = false;
    private bool isPlaceStarMode = false;
    private bool isPlaceSlowPotionMode = false;
    private bool isPlaceTeleporterMode = false;
    private bool isDragging = false;
    private HashSet<Vector2Int> processedCells;
    private Vector2Int? lastProcessedCell;
    private PointerEventData.InputButton? currentMouseButton;

    public enum WallDirection
    {
        Top, Right, Bottom, Left
    }

    void Awake()
    {
        processedCells = new HashSet<Vector2Int>();
    }

    void Start()
    {
        if (graphicRaycaster == null)
        {
            graphicRaycaster = GetComponentInParent<Canvas>()?.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
            {
                Debug.LogError("GraphicRaycaster not found. Please assign it in the Inspector.");
            }
        }

        if (wallDirectionDropdown != null)
        {
            wallDirectionDropdown.ClearOptions(); // Clear existing options
            wallDirectionDropdown.AddOptions(new List<string> { "Top", "Right", "Bottom", "Left" }); // Set options
            wallDirectionDropdown.value = 0; // Default to Top
        }

        if (addToggle == null)
        {
            Debug.LogError("Add Toggle is not assigned in the Inspector. Please assign the 'Add' toggle.");
        }

        if (gridRenderer == null)
        {
            Debug.LogError("MazeGridRenderer not assigned!");
        }

        if (moveButton != null)
        {
            moveButton.onClick.AddListener(OnMoveButtonClick);
        }
        if (editButton != null)
        {
            editButton.onClick.AddListener(OnEditButtonClick);
        }
        if (wallColorButton != null)
        {
            wallColorButton.onClick.AddListener(OnWallColorButtonClick);
        }

        // Initialize mode toggles
        if (relaxToggle != null && challengeToggle != null)
        {
            relaxToggle.onValueChanged.AddListener(OnRelaxToggleChanged);
            challengeToggle.onValueChanged.AddListener(OnChallengeToggleChanged);
            relaxToggle.isOn = true; // Default to Relax mode
            OnRelaxToggleChanged(true); // Initialize UI
        }
        else
        {
            Debug.LogError("Relax or Challenge toggle not assigned.");
        }

        // Initialize element panel and blocking panel
        if (elementTogglesPanel != null)
        {
            elementTogglesPanel.SetActive(false);
        }

        UpdateButtonAppearances();
        ApplyCurrentMode();
        editorMode = GetComponent<MazeEditorMode>();
    }

    public void Initialize(MazeData mazeData, Button[,] buttons)
    {
        this.mazeData = mazeData;
        this.cellButtons = buttons;
        this.rows = mazeData.rows;
        this.cols = mazeData.columns;

        if (editorMode != null)
        {
            editorMode.Initialize(mazeData, cellButtons);
        }

        ApplyCurrentMode();
        UpdateTogglesFromMazeData();
    }

    void Update()
    {
        if (isMoveMode) return;

        if (wallDirectionDropdown != null && addToggle != null)
        {
            int newIndex = wallDirectionDropdown.value;

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                newIndex = (int)WallDirection.Top; // Set to Top (0)
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                newIndex = (int)WallDirection.Left; // Set to Left (3)
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                newIndex = (int)WallDirection.Bottom; // Set to Bottom (2)
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                newIndex = (int)WallDirection.Right; // Set to Right (1)

            if (newIndex != wallDirectionDropdown.value)
            {
                wallDirectionDropdown.value = newIndex;
                Debug.Log($"Wall direction changed to: {(WallDirection)newIndex}");
            }
        }

        if (isDragging && graphicRaycaster != null && mazeData != null && wallDirectionDropdown != null && addToggle != null)
        {
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            graphicRaycaster.Raycast(pointerEventData, results);

            foreach (RaycastResult result in results)
            {
                Button hitButton = result.gameObject.GetComponent<Button>();
                if (hitButton != null)
                {
                    string[] nameParts = hitButton.name.Split('_');
                    if (nameParts.Length == 3 && int.TryParse(nameParts[1], out int x) && int.TryParse(nameParts[2], out int y))
                    {
                        Vector2Int currentCell = new Vector2Int(x, y);

                        if (processedCells.Contains(currentCell) || (lastProcessedCell.HasValue && lastProcessedCell.Value == currentCell))
                        {
                            continue;
                        }

                        bool addWall = true;
                        if (currentMouseButton == PointerEventData.InputButton.Left)
                        {
                            addWall = addToggle.isOn;
                        }
                        // else
                        // {
                        //     addWall = !addToggle.isOn;
                        // }

                        WallDirection direction = (WallDirection)wallDirectionDropdown.value;
                        ToggleWall(x, y, direction, addWall);
                        processedCells.Add(currentCell);
                        lastProcessedCell = currentCell;

                        OnWallToggled?.Invoke(x, y, direction);
                    }
                }
            }
        }
    }

    public void OnPointerDown(int x, int y, BaseEventData eventData)
    {

        if (mazeData == null || mazeData.cells == null || wallDirectionDropdown == null || addToggle == null)
        {
            Debug.LogWarning($"OnPointerDown failed: mazeData is {(mazeData == null ? "null" : "not null")}, mazeData.cells is {(mazeData?.cells == null ? "null" : "not null")}, wallDirectionDropdown is {(wallDirectionDropdown == null ? "null" : "not null")}, addToggle is {(addToggle == null ? "null" : "not null")}");
            return;
        }

        if (x < 0 || x >= rows || y < 0 || y >= cols)
        {
            Debug.LogError($"OnPointerDown received invalid coordinates: ({x}, {y}). Expected 0 <= x < {rows} and 0 <= y < {cols}.");
            return;
        }

        var pointerData = eventData as PointerEventData;

        if (editorController != null &&
            editorController.CurrentMode == MazeEditorMode.MazeEditorMode_Enum.SetElement)
        {
            if (pointerData.button == PointerEventData.InputButton.Left)
            {
                editorController.TryPlaceElement(new Vector2Int(x, y));
            }

            return;
        }
        
        if (pointerData == null)
        {
            Debug.LogWarning("OnPointerDown: PointerEventData is null.");
            return;
        }

        if (isMoveMode) return;

        if (editorMode != null && editorMode.IsEditingStartPoint())
        {
            if (pointerData.button == PointerEventData.InputButton.Left)
            {
                editorMode.HandleStartPointSelection(x, y);
                gridRenderer.HideSolution(); // Hide solution when setting start/end
            }
            return;
        }

        if (editorMode != null && editorMode.IsEditingWallColor())
        {
            if (pointerData.button == PointerEventData.InputButton.Left)
            {
                editorMode.SetSelectedWallCell(x, y);

                WallColorPopup popup = FindObjectOfType<WallColorPopup>();

                if (popup != null)
                {
                    popup.Open(x, y);
                }
            }

            return;
        }

        if (pointerData.button == PointerEventData.InputButton.Left || pointerData.button == PointerEventData.InputButton.Right)
        {
            isDragging = true;
            processedCells.Clear();
            lastProcessedCell = null;
            currentMouseButton = pointerData.button;

            bool addWall;
            if (pointerData.button == PointerEventData.InputButton.Left)
            {
                addWall = addToggle.isOn;
            }
            else
            {
                addWall = !addToggle.isOn;
            }

            WallDirection direction = (WallDirection)wallDirectionDropdown.value;
            ToggleWall(x, y, direction, addWall);
            processedCells.Add(new Vector2Int(x, y));
            lastProcessedCell = new Vector2Int(x, y);

            OnWallToggled?.Invoke(x, y, direction);
        }
    }

    public void OnPointerUp()
    {
        isDragging = false;
        processedCells.Clear();
        lastProcessedCell = null;
        currentMouseButton = null;
    }

    private void OnMoveButtonClick()
    {
        isMoveMode = true;
        isWallColorMode = false;
        if (editorMode != null) editorMode.ExitWallColorMode();

        UpdateButtonAppearances();
        ApplyCurrentMode();
        OnMoveModeChanged?.Invoke(isMoveMode);
    }

    private void OnEditButtonClick()
    {
        isMoveMode = false;
        isWallColorMode = false;
        if (editorMode != null) editorMode.ExitWallColorMode();

        UpdateButtonAppearances();
        ApplyCurrentMode();
        OnMoveModeChanged?.Invoke(isMoveMode);
    }

    private void OnWallColorButtonClick()
    {
        isMoveMode = false;
        isWallColorMode = true;
        if (editorMode != null) editorMode.EnterWallColorMode();

        UpdateButtonAppearances();
        ApplyCurrentMode();
        OnMoveModeChanged?.Invoke(isMoveMode); 
    }

    private void UpdateButtonAppearances()
    {
        if (moveButton != null)
        {
            Image moveImage = moveButton.GetComponent<Image>();
            if (moveImage != null) moveImage.color = isMoveMode ? Color.green : Color.white;
        }

        if (editButton != null)
        {
            Image editImage = editButton.GetComponent<Image>();
            // Update this line to check both booleans!
            if (editImage != null) editImage.color = (!isMoveMode && !isWallColorMode) ? Color.green : Color.white; 
        }

        // Add this block for the Wall Color Button
        if (wallColorButton != null)
        {
            Image colorImage = wallColorButton.GetComponent<Image>();
            if (colorImage != null) colorImage.color = isWallColorMode ? Color.green : Color.white;
        }
    }

    private void ApplyCurrentMode()
    {
        if (moveOverlay != null)
        {
            moveOverlay.gameObject.SetActive(isMoveMode);
        }
        if (scrollRect != null)
        {
            scrollRect.enabled = isMoveMode;
        }
    }

    private void ToggleWall(int x, int y, WallDirection direction, bool addWall)
    {
        if (mazeData == null || mazeData.cells == null)
        {
            Debug.LogError($"ToggleWall failed: mazeData is {(mazeData == null ? "null" : "not null")}, mazeData.cells is {(mazeData?.cells == null ? "null" : "not null")}");
            return;
        }

        if (x < 0 || x >= rows || y < 0 || y >= cols)
        {
            Debug.LogError($"ToggleWall received invalid coordinates: ({x}, {y}). Expected 0 <= x < {rows} and 0 <= y < {cols}.");
            return;
        }

        bool isBorderCell = false;
        switch (direction)
        {
            case WallDirection.Top:
                isBorderCell = (x == 0);
                break;
            case WallDirection.Right:
                isBorderCell = (y == cols - 1);
                break;
            case WallDirection.Bottom:
                isBorderCell = (x == rows - 1);
                break;
            case WallDirection.Left:
                isBorderCell = (y == 0);
                break;
        }

        if (isBorderCell) return;

        MazeData.CellData cell = mazeData.cells[x, y];
        switch (direction)
        {
            case WallDirection.Top:
                if (x > 0)
                {
                    cell.WallBack = addWall;
                    mazeData.cells[x - 1, y].WallFront = addWall;
                }
                else
                {
                    Debug.LogWarning($"ToggleWall: Attempted to toggle Top wall at border cell ({x}, {y}).");
                }
                break;
            case WallDirection.Right:
                if (y < cols - 1)
                {
                    cell.WallRight = addWall;
                    mazeData.cells[x, y + 1].WallLeft = addWall;
                }
                else
                {
                    Debug.LogWarning($"ToggleWall: Attempted to toggle Right wall at border cell ({x}, {y}).");
                }
                break;
            case WallDirection.Bottom:
                if (x < rows - 1)
                {
                    cell.WallFront = addWall;
                    mazeData.cells[x + 1, y].WallBack = addWall;
                }
                else
                {
                    Debug.LogWarning($"ToggleWall: Attempted to toggle Bottom wall at border cell ({x}, {y}).");
                }
                break;
            case WallDirection.Left:
                if (y > 0)
                {
                    cell.WallLeft = addWall;
                    mazeData.cells[x, y - 1].WallRight = addWall;
                }
                else
                {
                    Debug.LogWarning($"ToggleWall: Attempted to toggle Left wall at border cell ({x}, {y}).");
                }
                break;
        }
        gridRenderer.HideSolution(); // Hide solution after wall edit
    }

    private void OnRelaxToggleChanged(bool isOn)
    {
        if (!isOn)
            return;

        challengeToggle.isOn = false;

        if (mazeData != null)
            mazeData.mode = "Relax";

        elementTogglesPanel.SetActive(false);

        if (editorController != null)
        {
            editorController.DisableElementPlacement();
        }

        isSetElementsMode = false;

        Debug.LogError("Relax Mode Enabled");
    }

    private void OnChallengeToggleChanged(bool isOn)
    {
        if (!isOn)
            return;

        relaxToggle.isOn = false;

        if (mazeData != null)
            mazeData.mode = "Challenge";

        elementTogglesPanel.SetActive(true);

        Debug.LogError("Challenge Mode Enabled");
    }

    private (int dogAndShieldMin, int dogAndShieldMax, int bonesMin, int bonesMax, int specialCount) CalculateElementRanges()
    {
        if (mazeData == null)
        {
            Debug.LogWarning("Cannot calculate element ranges: MazeData is null.");
            return (0, 0, 0, 0, 0);
        }

        int size = mazeData.rows; // Assuming square maze
        int dogAndShieldMin = 2 + (size - 7);
        int dogAndShieldMax = Mathf.FloorToInt(size * size * 0.1f);
        int bonesMin = Mathf.FloorToInt(size * size * 0.1f);
        int bonesMax = Mathf.FloorToInt(size * size * 0.2f);
        int specialCount = Mathf.FloorToInt(size / 2f);

        return (dogAndShieldMin, dogAndShieldMax, bonesMin, bonesMax, specialCount);
    }

    private void ClampInputField(TMP_InputField inputField, string elementType)
    {
        if (mazeData == null || inputField == null || !int.TryParse(inputField.text, out int value))
        {
            inputField.text = "0";
            return;
        }

        var ranges = CalculateElementRanges();
        int minValue = 0;
        int maxValue = 0;

        switch (elementType)
        {
            case "Dog":
                minValue = ranges.dogAndShieldMin;
                maxValue = ranges.dogAndShieldMax;
                break;
            case "Bones":
                minValue = ranges.bonesMin;
                maxValue = ranges.bonesMax;
                break;
            case "Shield":
                minValue = ranges.dogAndShieldMin;
                maxValue = ranges.dogAndShieldMax;
                break;
            case "DogDetectionSize":
                minValue = 1;
                maxValue = mazeData.rows;
                break;
        }

        inputField.text = Mathf.Clamp(value, minValue, maxValue).ToString();
    }

    private void UpdateElementRanges()
    {
        var ranges = CalculateElementRanges();
        int dogAndShieldMin = ranges.dogAndShieldMin;
        int dogAndShieldMax = ranges.dogAndShieldMax;
        int bonesMin = ranges.bonesMin;
        int bonesMax = ranges.bonesMax;
        int specialCount = ranges.specialCount;
    }

    public void UpdateTogglesFromMazeData()
    {
        if (mazeData == null)
        {
            Debug.LogWarning("Cannot update toggles: MazeData is null.");
            return;
        }

        // Update mode toggles
        bool isChallengeMode = mazeData.mode == "Challenge";
        relaxToggle.isOn = !isChallengeMode; // Triggers OnRelaxToggleChanged
        challengeToggle.isOn = isChallengeMode; // Triggers OnChallengeToggleChanged
        if (string.IsNullOrEmpty(mazeData.mode) || (mazeData.mode != "Relax" && mazeData.mode != "Challenge"))
        {
            mazeData.mode = "Relax"; // Default for invalid/null mode
            relaxToggle.isOn = true;
        }

        // Count elements
        var elementCounts = mazeData.elements
            ?.GroupBy(e => e.elementType)
            ?.ToDictionary(g => g.Key, g => g.Count()) ?? new Dictionary<string, int>();

        UpdateElementRanges();
    }

    public void UpdateMazeDataWithToggles()
    {
        if (mazeData == null)
        {
            Debug.LogError("MazeData is null in UpdateMazeDataWithToggles.");
            return;
        }

        mazeData.mode = relaxToggle.isOn ? "Relax" : "Challenge";
        mazeData.elements.Clear();

        if (mazeData.mode == "Challenge")
        {
            var ranges = CalculateElementRanges();
            int dogAndShieldMin = ranges.dogAndShieldMin;
            int dogAndShieldMax = ranges.dogAndShieldMax;
            int bonesMin = ranges.bonesMin;
            int bonesMax = ranges.bonesMax;
            int specialCount = ranges.specialCount;
        }
    }
}