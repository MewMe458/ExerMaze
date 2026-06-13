using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MazeGridRenderer : MonoBehaviour
{
    [SerializeField] private GridLayoutGroup gridLayoutGroup;
    [SerializeField] private Button cellButtonPrefab;
    [SerializeField] private Slider zoomSlider;
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button zoomOutButton;
    [SerializeField] private Button recenterButton;
    [SerializeField] private TMP_Text zoomSizeText;
    [SerializeField] private RectTransform contentRectTransform;
    // [SerializeField] private List<Color> materialPreviewColors;
    [SerializeField] public List<WallMaterialData> wallMaterials;

    [Header("Element Prefabs")]
    public GameObject dogNPCPrefab;
    public GameObject bonePrefab;
    public GameObject shieldPrefab;
    public GameObject starPrefab;
    public GameObject slowPotionPrefab;
    public GameObject teleporterPrefab;

    [Header("Element Icons")]
    [SerializeField] private Sprite dogIcon;
    [SerializeField] private Sprite boneIcon;
    [SerializeField] private Sprite shieldIcon;
    [SerializeField] private Sprite starIcon;
    [SerializeField] private Sprite slowPotionIcon;
    [SerializeField] private Sprite teleporterIcon;

    // Keeps track of spawned elements so they can be removed if overridden
    private Dictionary<Vector2Int, GameObject> spawnedElements = new Dictionary<Vector2Int, GameObject>();

    private MazeInputHandler inputHandler;
    private Button[,] cellButtons;
    private float baseCellSize = 75f;
    private float zoomLevel = 1f;
    private float minZoom = 0.5f;
    private float maxZoom = 2f;
    private float zoomScrollSpeed = 0.1f;
    private int rows, cols;
    private Sprite[] wallSprites;
    private MazeData mazeData;
    private Dictionary<Vector2Int, Color> originalColors;
    private bool isSolutionVisible = false;
    private List<Vector2Int> currentSolutionPath = null;

    void Awake()
    {
        wallSprites = new Sprite[16];
        for (int i = 0; i < 16; i++)
        {
            string binary = System.Convert.ToString(i, 2).PadLeft(4, '0');
            string spriteName = $"Walls_{binary}";
            wallSprites[i] = Resources.Load<Sprite>($"Sprites/{spriteName}");
            if (wallSprites[i] == null)
            {
                Debug.LogError($"Failed to load sprite: Sprites/{spriteName}. Ensure the sprite exists in Resources/Sprites.");
            }
        }
        originalColors = new Dictionary<Vector2Int, Color>();
    }

    void Start()
    {
        if (gridLayoutGroup == null) Debug.LogError("Grid Layout Group not assigned!");
        if (cellButtonPrefab == null) Debug.LogError("Cell Button Prefab not assigned!");
        if (zoomSlider == null) Debug.LogError("Zoom Slider not assigned!");
        if (zoomInButton == null) Debug.LogError("Zoom In Button not assigned!");
        if (zoomOutButton == null) Debug.LogError("Zoom Out Button not assigned!");
        if (recenterButton == null) Debug.LogError("Recenter Button not assigned!");
        if (zoomSizeText == null) Debug.LogError("Zoom Size Text not assigned!");
        if (contentRectTransform == null) Debug.LogError("Content Rect Transform not assigned!");

        zoomSlider.minValue = minZoom;
        zoomSlider.maxValue = maxZoom;
        zoomSlider.value = zoomLevel;
        zoomSlider.onValueChanged.AddListener(SetZoom);
        zoomInButton.onClick.AddListener(ZoomIn);
        zoomOutButton.onClick.AddListener(ZoomOut);
        recenterButton.onClick.AddListener(ResetToCenter);
        UpdateZoomText();

        if (System.Array.Exists(wallSprites, sprite => sprite == null))
        {
            Debug.LogError("One or more wall sprites failed to load. Check Resources/Sprites folder.");
        }

        inputHandler = GetComponent<MazeInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.OnWallToggled += UpdateAffectedCells;
        }
        else
        {
            Debug.LogError("Maze Input Handler not found on this GameObject!");
        }
    }

    void Update()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (scrollDelta != 0f && zoomSlider != null)
        {
            float newZoom = zoomSlider.value + scrollDelta * zoomScrollSpeed * (maxZoom - minZoom);
            SetZoom(Mathf.Clamp(newZoom, minZoom, maxZoom));
        }

        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals))
        {
            ZoomIn();
        }
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.Underscore))
        {
            ZoomOut();
        }
    }

    public void InitializeGrid(MazeData mazeData)
    {
        if (mazeData == null || mazeData.cells == null)
        {
            Debug.LogError("MazeData or MazeData.cells is null in InitializeGrid.");
            return;
        }

        ResetSolutionVisibility();
        ClearAllElements(); // 🔥 FIX 1: Clear tracking dictionary references from the previous maze

        this.mazeData = mazeData;

        foreach (Transform child in gridLayoutGroup.transform)
        {
            Destroy(child.gameObject);
        }

        rows = mazeData.rows;
        cols = mazeData.columns;
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = cols;

        if (mazeData.cells.GetLength(0) != rows || mazeData.cells.GetLength(1) != cols)
        {
            Debug.LogError($"MazeData.cells dimensions ({mazeData.cells.GetLength(0)}x{mazeData.cells.GetLength(1)}) do not match rows ({rows}) and cols ({cols}).");
            return;
        }

        RectTransform gridPanel = gridLayoutGroup.GetComponent<RectTransform>();

        if (zoomSlider == null || zoomSlider.value == zoomLevel)
        {
            float panelWidth = gridPanel.rect.width;
            float panelHeight = gridPanel.rect.height;
            float referenceSize = 10 * baseCellSize;
            zoomLevel = Mathf.Min(panelWidth / referenceSize, panelHeight / referenceSize);
            zoomLevel = Mathf.Clamp(zoomLevel, minZoom, maxZoom);
        }

        if (zoomSlider != null)
        {
            zoomLevel = zoomSlider.value;
            zoomSlider.value = zoomLevel;
        }

        gridLayoutGroup.cellSize = new Vector2(baseCellSize * zoomLevel, baseCellSize * zoomLevel);
        gridLayoutGroup.spacing = new Vector2(2f, 2f);

        if (contentRectTransform != null)
        {
            RectTransform viewportRect = contentRectTransform.parent.GetComponent<RectTransform>();
            float minSize = Mathf.Max(viewportRect.rect.width, viewportRect.rect.height) * 1.5f;
            float contentWidth = cols * baseCellSize * zoomLevel;
            float contentHeight = rows * baseCellSize * zoomLevel;
            contentRectTransform.sizeDelta = new Vector2(Mathf.Max(minSize, contentWidth), Mathf.Max(minSize, contentHeight));
        }

        cellButtons = new Button[rows, cols];
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                int currentX = x;
                int currentY = y;

                Button cellButton = Instantiate(cellButtonPrefab, gridLayoutGroup.transform, false);
                cellButton.name = $"Cell_{currentX}_{currentY}";
                Image cellImage = cellButton.GetComponent<Image>();
                
                cellButtons[currentX, currentY] = cellButton; 
                cellImage.sprite = GetSpriteForCell(mazeData.cells[currentX, currentY]);
                cellImage.color = GetCellColor(currentX, currentY);

                EventTrigger trigger = cellButton.gameObject.AddComponent<EventTrigger>();
                EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
                pointerDownEntry.eventID = EventTriggerType.PointerDown;
                pointerDownEntry.callback.AddListener((eventData) => inputHandler?.OnPointerDown(currentX, currentY, eventData));
                trigger.triggers.Add(pointerDownEntry);

                EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
                pointerUpEntry.eventID = EventTriggerType.PointerUp;
                pointerUpEntry.callback.AddListener((eventData) => inputHandler?.OnPointerUp());
                trigger.triggers.Add(pointerUpEntry);
            }
        }
        RefreshAllCellTexts();

        gridPanel.anchorMin = new Vector2(0.5f, 0.5f);
        gridPanel.anchorMax = new Vector2(0.5f, 0.5f);
        gridPanel.pivot = new Vector2(0.5f, 0.5f);
        gridPanel.anchoredPosition = Vector2.zero;

        UpdateZoomText();

        if (inputHandler != null)
            inputHandler.Initialize(mazeData, cellButtons);

        // 🔥 FIX 2: Iterate through the loaded challenge elements and draw their icons
        if (mazeData.elements != null)
        {
            foreach (var element in mazeData.elements)
            {
                DrawElement(element);
            }
        }
    }

    public void UpdateGrid(MazeData mazeData)
    {
        if (mazeData == null || mazeData.cells == null)
        {
            Debug.LogError("MazeData or MazeData.cells is null in UpdateGrid.");
            return;
        }

        this.mazeData = mazeData;

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                Image cellImage = cellButtons[x, y].GetComponent<Image>();
                cellImage.sprite = GetSpriteForCell(mazeData.cells[x, y]);
                cellImage.color = GetCellColor(x, y);
            }
        }

        // 🔥 FIX: Refresh texts when the grid updates completely
        RefreshAllCellTexts();
    }

    private void UpdateAffectedCells(int x, int y, MazeInputHandler.WallDirection direction)
    {
        if (cellButtons == null || x < 0 || x >= rows || y < 0 || y >= cols)
        {
            Debug.LogError($"UpdateAffectedCells received invalid coordinates: ({x}, {y}). Expected 0 <= x < {rows} and 0 <= y < {cols}. cellButtons is {(cellButtons == null ? "null" : "not null")}");
            return;
        }

        Image cellImage = cellButtons[x, y].GetComponent<Image>();
        if (cellImage != null)
        {
            cellImage.sprite = GetSpriteForCell(mazeData.cells[x, y]);
        }
        else
        {
            Debug.LogWarning($"UpdateAffectedCells: Cell button at ({x}, {y}) has no Image component.");
        }

        switch (direction)
        {
            case MazeInputHandler.WallDirection.Top:
                if (x > 0)
                {
                    Image neighborImage = cellButtons[x - 1, y].GetComponent<Image>();
                    if (neighborImage != null)
                    {
                        neighborImage.sprite = GetSpriteForCell(mazeData.cells[x - 1, y]);
                    }
                }
                break;
            case MazeInputHandler.WallDirection.Right:
                if (y < cols - 1)
                {
                    Image neighborImage = cellButtons[x, y + 1].GetComponent<Image>();
                    if (neighborImage != null)
                    {
                        neighborImage.sprite = GetSpriteForCell(mazeData.cells[x, y + 1]);
                    }
                }
                break;
            case MazeInputHandler.WallDirection.Bottom:
                if (x < rows - 1)
                {
                    Image neighborImage = cellButtons[x + 1, y].GetComponent<Image>();
                    if (neighborImage != null)
                    {
                        neighborImage.sprite = GetSpriteForCell(mazeData.cells[x + 1, y]);
                    }
                }
                break;
            case MazeInputHandler.WallDirection.Left:
                if (y > 0)
                {
                    Image neighborImage = cellButtons[x, y - 1].GetComponent<Image>();
                    if (neighborImage != null)
                        neighborImage.sprite = GetSpriteForCell(mazeData.cells[x, y - 1]);
                }
                break;
        }

        RefreshAllCellTexts();
    }

    public void SetZoom(float zoom)
    {
        zoomLevel = Mathf.Clamp(zoom, minZoom, maxZoom);
        gridLayoutGroup.cellSize = new Vector2(baseCellSize * zoomLevel, baseCellSize * zoomLevel);

        if (contentRectTransform != null)
        {
            RectTransform viewportRect = contentRectTransform.parent.GetComponent<RectTransform>();
            float minSize = Mathf.Max(viewportRect.rect.width, viewportRect.rect.height) * 1.5f;
            float contentWidth = cols * baseCellSize * zoomLevel;
            float contentHeight = rows * baseCellSize * zoomLevel;
            contentRectTransform.sizeDelta = new Vector2(Mathf.Max(minSize, contentWidth), Mathf.Max(minSize, contentHeight));
        }

        if (zoomSlider != null)
        {
            zoomSlider.value = zoomLevel;
        }
        UpdateZoomText();
    }

    public void ZoomIn()
    {
        zoomLevel = Mathf.Min(zoomLevel + 0.1f, maxZoom);
        SetZoom(zoomLevel);
    }

    public void ZoomOut()
    {
        zoomLevel = Mathf.Max(zoomLevel - 0.1f, minZoom);
        SetZoom(zoomLevel);
    }

    public void ResetToCenter()
    {
        if (gridLayoutGroup != null)
        {
            RectTransform gridPanel = gridLayoutGroup.GetComponent<RectTransform>();
            gridPanel.anchoredPosition = Vector2.zero;
        }
    }

    public void ShowSolution(List<Vector2Int> path)
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("Cannot show solution: Path is null or empty.");
            return;
        }

        if (isSolutionVisible)
        {
            HideSolution();
            return;
        }

        originalColors.Clear();
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                Image cellImage = cellButtons[x, y].GetComponent<Image>();
                originalColors[new Vector2Int(x, y)] = cellImage.color;
            }
        }

        currentSolutionPath = new List<Vector2Int>(path);
        foreach (Vector2Int cell in path)
        {
            if (cell.x >= 0 && cell.x < rows && cell.y >= 0 && cell.y < cols)
            {
                Image cellImage = cellButtons[cell.x, cell.y].GetComponent<Image>();
                if (!mazeData.cells[cell.x, cell.y].IsStart && !mazeData.cells[cell.x, cell.y].IsGoal)
                {
                    cellImage.color = Color.cyan;
                }
            }
        }
        isSolutionVisible = true;
    }

    public void HideSolution()
    {
        if (!isSolutionVisible && originalColors.Count == 0)
            return;

        foreach (var entry in originalColors)
        {
            Vector2Int pos = entry.Key;
            if (pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < cols)
            {
                Image cellImage = cellButtons[pos.x, pos.y].GetComponent<Image>();
                cellImage.color = entry.Value;
            }
        }
        originalColors.Clear();
        currentSolutionPath = null;
        isSolutionVisible = false;
    }

    public void ResetSolutionVisibility()
    {
        if (isSolutionVisible || originalColors.Count > 0)
        {
            originalColors.Clear();
            currentSolutionPath = null;
            isSolutionVisible = false;
        }
    }

    public Color GetCellColor(int x, int y)
    {
        // We removed the green and red returns here so they don't paint the button background.
        int matIndex = mazeData.cells[x, y].MaterialIndex;
        if (wallMaterials != null && matIndex >= 0 && matIndex < wallMaterials.Count)
        {
            return wallMaterials[matIndex].previewColor;
        }

        return Color.white;
    }

    public void RefreshAllCellTexts()
    {
        if (cellButtons == null || mazeData == null) return;

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                UpdateCellText(x, y);
            }
        }
    }

    private void UpdateCellText(int x, int y)
    {
        if (cellButtons == null || cellButtons[x, y] == null) return;

        Button cellButton = cellButtons[x, y];
        TMP_Text cellText = cellButton.GetComponentInChildren<TMP_Text>();

        bool isStart = mazeData.cells[x, y].IsStart;
        bool isGoal = mazeData.cells[x, y].IsGoal;

        if (isStart || isGoal)
        {
            if (cellText == null)
            {
                GameObject textObj = new GameObject("Label");
                textObj.transform.SetParent(cellButton.transform, false);
                cellText = textObj.AddComponent<TextMeshProUGUI>();
                
                cellText.alignment = TextAlignmentOptions.Center;
                cellText.fontSize = 16; // Bumped up slightly for readability
                cellText.fontStyle = FontStyles.Bold; // Make it stand out without a background color
                cellText.raycastTarget = false;

                RectTransform rect = textObj.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            // Apply colors to the TEXT instead of the cell background
            if (isStart)
            {
                cellText.text = "Start";
                cellText.color = Color.green;
            }
            else if (isGoal)
            {
                cellText.text = "Goal";
                cellText.color = Color.red;
            }
        }
        else
        {
            if (cellText != null)
            {
                Destroy(cellText.gameObject);
            }
        }
    }

    public Button[,] GetCellButtons()
    {
        return cellButtons;
    }

    public MazeData GetMazeData()
    {
        return mazeData;
    }

    private Sprite GetSpriteForCell(MazeData.CellData cell)
    {
        int index = (cell.WallBack ? 1 : 0) << 3 |
                    (cell.WallRight ? 1 : 0) << 2 |
                    (cell.WallFront ? 1 : 0) << 1 |
                    (cell.WallLeft ? 1 : 0);
        if (wallSprites != null && index >= 0 && index < wallSprites.Length && wallSprites[index] != null)
        {
            return wallSprites[index];
        }
        Debug.LogWarning("Sprite not found for index: " + index + ". Using default sprite.");
        return wallSprites != null && wallSprites.Length > 0 && wallSprites[15] != null ? wallSprites[15] : null;
    }

    // This method handles drawing the specific element
    public void DrawElement(MazeData.ElementData element)
    {
        RemoveElementVisual(element.position);

        Button cellButton =
            cellButtons[element.position.x, element.position.y];

        GameObject iconObj = new GameObject(
            element.elementType);

        iconObj.transform.SetParent(cellButton.transform, false);

        RectTransform rect =
            iconObj.AddComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        Image img = iconObj.AddComponent<Image>();

        img.sprite = GetIconForElementType(element.elementType);

        img.raycastTarget = false;

        spawnedElements[element.position] = iconObj;
    }

    private Sprite GetIconForElementType(string type)
    {
        switch (type)
        {
            case "DogNPC":
                return dogIcon;

            case "Bones":
                return boneIcon;

            case "Shield":
                return shieldIcon;

            case "Special":
                return starIcon;

            case "SlowPotion":
                return slowPotionIcon;

            case "Teleporter":
                return teleporterIcon;
        }

        return null;
    }

    public void DestroyElementAt(int x, int y)
    {
        // Search through your active instantiated element prefabs container.
        // If you are storing them inside a list or dictionary, find it by position:
        
        // Example assuming your element prefabs are children of a specific container or spawned cells:
        string expectedName = $"Element_{x}_{y}"; 
        
        // Look through spawned children to clean up the hierarchy
        foreach (Transform child in transform) 
        {
            // Adjust this naming condition based on how your script names spawned prefabs
            if (child.name.Contains($"({x}, {y})") || child.name == expectedName)
            {
                Destroy(child.gameObject);
                break;
            }
        }
        
        // Alternately, if your script completely clears and recreates the grid on changes, 
        // you can simply call your existing regeneration/render method:
        // UpdateGrid(currentMazeData); 
    }

    public void RemoveElementVisual(Vector2Int position)
    {
        if (spawnedElements.TryGetValue(position, out GameObject elementObj))
        {
            Destroy(elementObj);
            spawnedElements.Remove(position);
        }
    }

    public void ClearAllElements()
    {
        foreach (var element in spawnedElements.Values)
        {
            Destroy(element);
        }
        spawnedElements.Clear();
    }

    private GameObject GetPrefabForElementType(string type)
    {
        switch (type)
        {
            case "DogNPC": return dogNPCPrefab;
            case "Bones": return bonePrefab;
            case "Shield": return shieldPrefab;
            case "Special": return starPrefab;
            case "SlowPotion": return slowPotionPrefab;
            case "Teleporter": return teleporterPrefab;
            default: return null;
        }
    }

    private void UpdateZoomText()
    {
        if (zoomSizeText != null)
        {
            zoomSizeText.text = $"{zoomLevel:F1}x";
        }
    }
}