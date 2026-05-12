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
    [SerializeField] private Image editOverlay;
    [SerializeField] private Image moveOverlay;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private MazeGridRenderer gridRenderer; // Added for solution path hiding

    #region UI for setting elements
    [SerializeField] private Toggle relaxToggle; // Toggle for Relax mode
    [SerializeField] private Toggle challengeToggle; // Toggle for Challenge mode
    [SerializeField] private Button showElementPanelButton; // Button to show/hide element panel
    [SerializeField] private GameObject elementTogglesPanel; // Panel with toggles and inputs
    [SerializeField] private GameObject editingElementPanel; // Blocking panel
    [SerializeField] private Toggle dogToggle; // Dog NPC toggle
    [SerializeField] private TMP_InputField dogCountInput; // Dog count input
    [SerializeField] private TMP_Text dogRangeText; // Dog range text (Min-Max)
    [SerializeField] private TMP_InputField dogDetectionSizeInput; // Dog detection range input
    [SerializeField] private TMP_Text dogDetectionSizeRange; // Dog detection range text (Min-Max)
    [SerializeField] private Toggle bonesToggle; // Bones toggle
    [SerializeField] private TMP_InputField bonesCountInput; // Bones count input
    [SerializeField] private TMP_Text bonesRangeText; // Bones range text
    [SerializeField] private Toggle shieldToggle; // Shield toggle
    [SerializeField] private TMP_InputField shieldCountInput; // Shield count input
    [SerializeField] private TMP_Text shieldRangeText; // Shield range text
    [SerializeField] private Toggle specialToggle; // Special item toggle
    [SerializeField] private TMP_Text specialCountText; // Special item fixed count
    private bool isElementPanelVisible = false; // Tracks element panel visibility
    #endregion

    private MazeEditorMode editorMode;
    private MazeData mazeData;
    private Button[,] cellButtons;
    private int rows, cols;
    private bool isMoveMode = false;
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

        // Initialize show element panel button
        if (showElementPanelButton != null)
        {
            showElementPanelButton.onClick.AddListener(OnShowElementPanelButton);
            showElementPanelButton.gameObject.SetActive(false); // Hidden by default
        }
        else
        {
            Debug.LogError("Show Element Panel Button not assigned.");
        }

        // Initialize element toggles and inputs
        if (dogToggle != null)
        {
            dogToggle.isOn = false;
            dogToggle.onValueChanged.AddListener(OnDogToggleChanged);
        }
        if (bonesToggle != null)
        {
            bonesToggle.isOn = false;
            bonesToggle.interactable = dogToggle.isOn;
            bonesToggle.onValueChanged.AddListener(OnBonesToggleChanged);
        }
        if (shieldToggle != null)
        {
            shieldToggle.isOn = false;
            shieldToggle.interactable = dogToggle.isOn;
            shieldToggle.onValueChanged.AddListener(OnShieldToggleChanged);
        }
        if (specialToggle != null)
        {
            specialToggle.isOn = false;
            specialToggle.onValueChanged.AddListener(OnSpecialToggleChanged);
        }

        // Add OnEndEdit listeners for input fields
        if (dogCountInput != null)
        {
            dogCountInput.onEndEdit.AddListener((value) => ClampInputField(dogCountInput, "Dog"));
            dogCountInput.gameObject.SetActive(false); // Hidden initially
        }
        if(dogDetectionSizeInput != null)
        {
            dogDetectionSizeInput.onEndEdit.AddListener((value) => ClampInputField(dogDetectionSizeInput, "DogDetectionSize"));
            dogDetectionSizeInput.gameObject.SetActive(false); // Hidden initially
        }
        if (bonesCountInput != null)
        {
            bonesCountInput.onEndEdit.AddListener((value) => ClampInputField(bonesCountInput, "Bones"));
            bonesCountInput.gameObject.SetActive(false); // Hidden initially
        }
        if (shieldCountInput != null)
        {
            shieldCountInput.onEndEdit.AddListener((value) => ClampInputField(shieldCountInput, "Shield"));
            shieldCountInput.gameObject.SetActive(false); // Hidden initially
        }

        // Initialize element panel and blocking panel
        if (elementTogglesPanel != null)
        {
            elementTogglesPanel.SetActive(false);
        }
        if (editingElementPanel != null)
        {
            editingElementPanel.SetActive(false);
        }
        if (specialCountText != null)
        {
            specialCountText.gameObject.SetActive(true);
            specialCountText.text = "0";
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

                        bool addWall;
                        if (currentMouseButton == PointerEventData.InputButton.Left)
                        {
                            addWall = addToggle.isOn;
                        }
                        else
                        {
                            addWall = !addToggle.isOn;
                        }

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
        UpdateButtonAppearances();
        ApplyCurrentMode();
        OnMoveModeChanged?.Invoke(isMoveMode);
    }

    private void OnEditButtonClick()
    {
        isMoveMode = false;
        UpdateButtonAppearances();
        ApplyCurrentMode();
        OnMoveModeChanged?.Invoke(isMoveMode);
    }

    private void UpdateButtonAppearances()
    {
        if (moveButton != null)
        {
            Image moveImage = moveButton.GetComponent<Image>();
            if (moveImage != null)
            {
                moveImage.color = isMoveMode ? Color.green : Color.white;
            }
            TMP_Text moveText = moveButton.GetComponentInChildren<TMP_Text>();
            if (moveText != null)
            {
                moveText.text = "Move";
            }
        }

        if (editButton != null)
        {
            Image editImage = editButton.GetComponent<Image>();
            if (editImage != null)
            {
                editImage.color = !isMoveMode ? Color.green : Color.white;
            }
            TMP_Text editText = editButton.GetComponentInChildren<TMP_Text>();
            if (editText != null)
            {
                editText.text = "Edit";
            }
        }
    }

    private void ApplyCurrentMode()
    {
        if (moveOverlay != null)
        {
            moveOverlay.gameObject.SetActive(isMoveMode);
        }
        if (editOverlay != null)
        {
            editOverlay.gameObject.SetActive(isMoveMode);
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
        if (!isOn) return;
        challengeToggle.isOn = false; // Ensure mutual exclusivity
        showElementPanelButton.gameObject.SetActive(false);
        elementTogglesPanel.SetActive(false);
        editingElementPanel.SetActive(false);
        isElementPanelVisible = false;
    }

    private void OnChallengeToggleChanged(bool isOn)
    {
        if (!isOn) return;
        relaxToggle.isOn = false; // Ensure mutual exclusivity
        showElementPanelButton.gameObject.SetActive(true);
        elementTogglesPanel.SetActive(isElementPanelVisible);
        editingElementPanel.SetActive(isElementPanelVisible);
    }

    private void OnShowElementPanelButton()
    {
        // Clamp input fields before toggling panel visibility
        if (dogToggle.isOn)
            ClampInputField(dogCountInput, "Dog");
        if (bonesToggle.isOn)
            ClampInputField(bonesCountInput, "Bones");
        if (shieldToggle.isOn)
            ClampInputField(shieldCountInput, "Shield");

        isElementPanelVisible = !isElementPanelVisible;
        elementTogglesPanel.SetActive(isElementPanelVisible);
        editingElementPanel.SetActive(isElementPanelVisible);
    }

    private void OnDogToggleChanged(bool isOn)
    {
        bonesToggle.interactable = isOn;
        shieldToggle.interactable = isOn;
        if (!isOn)
        {
            bonesToggle.isOn = false; // Uncheck Bones toggle
            shieldToggle.isOn = false; // Uncheck Shield toggle
        }
        dogCountInput.gameObject.SetActive(isOn);
        dogDetectionSizeInput.gameObject.SetActive(isOn);
        dogDetectionSizeRange.gameObject.SetActive(isOn);
        bonesCountInput.gameObject.SetActive(isOn && bonesToggle.isOn);
        shieldCountInput.gameObject.SetActive(isOn && shieldToggle.isOn);
        UpdateElementRanges();
    }

    private void OnBonesToggleChanged(bool isOn)
    {
        bonesCountInput.gameObject.SetActive(isOn);
        UpdateElementRanges();
    }

    private void OnShieldToggleChanged(bool isOn)
    {
        shieldCountInput.gameObject.SetActive(isOn);
        UpdateElementRanges();
    }

    private void OnSpecialToggleChanged(bool isOn)
    {
        UpdateElementRanges();
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

        if (dogRangeText != null)
            dogRangeText.text = $"({dogAndShieldMin}-{dogAndShieldMax})";
        if (dogDetectionSizeRange != null)
            dogDetectionSizeRange.text = $"(1-{(mazeData != null ? mazeData.rows : 0)})";
        if (bonesRangeText != null)
            bonesRangeText.text = $"({bonesMin}-{bonesMax})";
        if (shieldRangeText != null)
            shieldRangeText.text = $"({dogAndShieldMin}-{dogAndShieldMax})";
        if (specialCountText != null)
            specialCountText.text = mazeData != null ? specialCount.ToString() : "0";

        // Clamp input values
        if (dogCountInput != null && int.TryParse(dogCountInput.text, out int dogCount))
            dogCountInput.text = Mathf.Clamp(dogCount, dogAndShieldMin, dogAndShieldMax).ToString();
        if (dogDetectionSizeInput != null && int.TryParse(dogDetectionSizeInput.text, out int dogRange)) 
            dogDetectionSizeInput.text = Mathf.Clamp(dogRange, 1, mazeData.rows).ToString();
        if (bonesCountInput != null && int.TryParse(bonesCountInput.text, out int bonesCount))
            bonesCountInput.text = Mathf.Clamp(bonesCount, bonesMin, bonesMax).ToString();
        if (shieldCountInput != null && int.TryParse(shieldCountInput.text, out int shieldCount))
            shieldCountInput.text = Mathf.Clamp(shieldCount, dogAndShieldMin, dogAndShieldMax).ToString();
    }

    public void UpdateTogglesFromMazeData()
    {
        if (mazeData == null)
        {
            Debug.LogWarning("Cannot update toggles: MazeData is null.");
            // Hide input fields when no maze is loaded
            if (dogCountInput != null) dogCountInput.gameObject.SetActive(false);
            if (bonesCountInput != null) bonesCountInput.gameObject.SetActive(false);
            if (shieldCountInput != null) shieldCountInput.gameObject.SetActive(false);
            if (specialCountText != null) specialCountText.text = "0";
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
            ?.GroupBy(e => e.type)
            ?.ToDictionary(g => g.Key, g => g.Count()) ?? new Dictionary<string, int>();

        // Update element toggles and inputs
        dogToggle.isOn = elementCounts.ContainsKey("Dog") && elementCounts["Dog"] > 0;
        bonesToggle.isOn = dogToggle.isOn && elementCounts.ContainsKey("Bones") && elementCounts["Bones"] > 0;
        shieldToggle.isOn = dogToggle.isOn && elementCounts.ContainsKey("Shield") && elementCounts["Shield"] > 0;
        specialToggle.isOn = elementCounts.ContainsKey("Special") && elementCounts["Special"] > 0;

        dogCountInput.text = dogToggle.isOn ? elementCounts.GetValueOrDefault("Dog", 0).ToString() : "0";
        dogDetectionSizeInput.text = mazeData != null ? Mathf.FloorToInt(mazeData.rows / 2).ToString() : "1";
        bonesCountInput.text = bonesToggle.isOn ? elementCounts.GetValueOrDefault("Bones", 0).ToString() : "0";
        shieldCountInput.text = shieldToggle.isOn ? elementCounts.GetValueOrDefault("Shield", 0).ToString() : "0";

        // Update interactability and visibility
        bonesToggle.interactable = dogToggle.isOn;
        shieldToggle.interactable = dogToggle.isOn;
        //if (!dogToggle.isOn)
        //{
        //    bonesToggle.isOn = false;
        //    shieldToggle.isOn = false;
        //}
        dogCountInput.gameObject.SetActive(dogToggle.isOn);
        bonesCountInput.gameObject.SetActive(dogToggle.isOn && bonesToggle.isOn);
        shieldCountInput.gameObject.SetActive(dogToggle.isOn && shieldToggle.isOn);
        if (specialCountText != null)
            specialCountText.gameObject.SetActive(true);

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

            if (dogToggle.isOn && int.TryParse(dogCountInput.text, out int dogCount))
            {
                dogCount = Mathf.Clamp(dogCount, dogAndShieldMin, dogAndShieldMax);
                int dogDetectionSize = int.TryParse(dogDetectionSizeInput.text, out int range) ? 
                    Mathf.Clamp(range, 1, mazeData.rows) : mazeData.rows;
                Debug.Log($"Setting dogDetectionSize: {dogDetectionSize} for {dogCount} Dogs");
                for (int i = 0; i < dogCount; i++)
                    mazeData.elements.Add(new MazeData.ElementData { type = "Dog", detectionSize = (float)dogDetectionSize });
                Debug.Log($"Elements after adding Dogs: {string.Join(", ", mazeData.elements.Select(e => $"type={e.type}, detectionSize={e.detectionSize}"))}");
            }

            if (bonesToggle.isOn && int.TryParse(bonesCountInput.text, out int bonesCount))
            {
                bonesCount = Mathf.Clamp(bonesCount, bonesMin, bonesMax);
                for (int i = 0; i < bonesCount; i++)
                    mazeData.elements.Add(new MazeData.ElementData { type = "Bones", detectionSize = 0f });
            }

            if (shieldToggle.isOn && int.TryParse(shieldCountInput.text, out int shieldCount))
            {
                shieldCount = Mathf.Clamp(shieldCount, dogAndShieldMin, dogAndShieldMax);
                for (int i = 0; i < shieldCount; i++)
                    mazeData.elements.Add(new MazeData.ElementData { type = "Shield", detectionSize = 0f });
            }

            if (specialToggle.isOn)
            {
                for (int i = 0; i < specialCount; i++)
                    mazeData.elements.Add(new MazeData.ElementData { type = "Special", detectionSize = 0f });
            }
        }
    }
}