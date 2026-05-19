using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class WallColorPopup : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform contentParent;
    [SerializeField] private Button materialButtonPrefab;
    [SerializeField] private Button setButton;
    [SerializeField] private Button cancelButton;

    private List<WallMaterialData> wallMaterials;

    private int selectedMaterialIndex = -1;

    private MazeGridRenderer gridRenderer;
    private MazeEditorMode editorMode;

    void Start()
    {
        root.SetActive(false);

        gridRenderer = FindFirstObjectByType<MazeGridRenderer>();
        editorMode = FindFirstObjectByType<MazeEditorMode>();

        if (gridRenderer != null)
        {
            wallMaterials = gridRenderer.wallMaterials;
        }
        else 
        {
            wallMaterials = new List<WallMaterialData>();
        }

        GenerateMaterialButtons();

        setButton.onClick.AddListener(SetMaterial);
        cancelButton.onClick.AddListener(Close);
    }

    void GenerateMaterialButtons()
    {
        // Clear old buttons first
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < wallMaterials.Count; i++)
        {
            int index = i;

            Button btn = Instantiate(materialButtonPrefab, contentParent);

            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            Image img = btn.GetComponent<Image>();

            // SET TEXT
            if (txt != null)
            {
                txt.text = wallMaterials[index].materialName;
            }

            // SET PREVIEW IMAGE
            if (img != null)
            {
                if (wallMaterials[index].previewSprite != null)
                {
                    img.sprite = wallMaterials[index].previewSprite;
                    img.color = Color.white;
                }
                else
                {
                    img.sprite = null;
                    img.color = wallMaterials[index].previewColor;
                }
            }

            btn.onClick.RemoveAllListeners();

            btn.onClick.AddListener(() =>
            {
                selectedMaterialIndex = index;

                Debug.Log("Selected Material: " + wallMaterials[index].materialName);
            });
        }
    }

    public void Open(int x, int y)
    {
        selectedMaterialIndex = -1;
        root.SetActive(true);
    }

    public void Close()
    {
        root.SetActive(false);
    }

    void SetMaterial()
    {
        if (selectedMaterialIndex < 0)
            return;

        Vector2Int pos = editorMode.GetSelectedWallCell();

        MazeData mazeData = gridRenderer.GetMazeData();

        mazeData.cells[pos.x, pos.y].MaterialIndex = selectedMaterialIndex;

        gridRenderer.UpdateGrid(mazeData);

        Close();
    }
}