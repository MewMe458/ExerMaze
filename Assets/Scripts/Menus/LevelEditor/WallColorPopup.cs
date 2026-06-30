using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class WallColorPopup : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform contentParent;
    [SerializeField] private Button materialButtonPrefab;
    [SerializeField] private Button cancelButton; 

    private MazeGridRenderer gridRenderer;
    private MazeEditorMode editorMode;
    private List<WallMaterialData> wallMaterials;
    private int selectedMaterialIndex = -1;

    void Start()
    {
        var controller = GetComponentInParent<MazeEditorController>();
        gridRenderer = controller != null ? controller.GetComponentInChildren<MazeGridRenderer>() : null;
        editorMode = controller != null ? controller.GetComponentInChildren<MazeEditorMode>() : null;

        if (gridRenderer != null)
        {
            wallMaterials = gridRenderer.wallMaterials;
            PopulateMaterials();
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelPressed);
        }
    }

    void PopulateMaterials()
    {
        if (wallMaterials == null || materialButtonPrefab == null || contentParent == null)
            return;

        // Clear existing children
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < wallMaterials.Count; i++)
        {
            int index = i; // Avoid closure issues
            Button btn = Instantiate(materialButtonPrefab, contentParent);
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            Image img = btn.GetComponent<Image>();

            // 🎨 UPDATED: Prioritize displaying the wall texture image asset over raw colors
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

            // --- TEXT BORDER / WRAPPER CONFIGURATION ---
            if (txt != null)
            {
                txt.text = wallMaterials[index].materialName;

                // Create a solid white wrapper background object
                GameObject bgObj = new GameObject("TextBackground");
                bgObj.transform.SetParent(btn.transform, false);

                Image bgImage = bgObj.AddComponent<Image>();
                bgImage.color = Color.white;
                bgImage.raycastTarget = false;

                // Structure the wrapper layout overlaying the button center
                RectTransform bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = new Vector2(0.05f, 0.2f);
                bgRect.anchorMax = new Vector2(0.95f, 0.8f);
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                // Re-parent the existing text into the white border background
                txt.transform.SetParent(bgObj.transform, false);

                // Configure text styling for baseline contrast against white background
                txt.color = Color.black;
                txt.fontSize = 12;
                txt.fontStyle = FontStyles.Bold;

                RectTransform txtRect = txt.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.offsetMin = Vector2.zero;
                txtRect.offsetMax = Vector2.zero;
            }
            // --------------------------------------------

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                selectedMaterialIndex = index;
                Debug.Log("Selected Material: " + wallMaterials[index].materialName);
                
                if (editorMode != null)
                {
                    editorMode.SetGlobalMaterialIndex(selectedMaterialIndex);
                }
                Close();
            });
        }
    }

    private void OnCancelPressed()
    {
        if (editorMode != null)
        {
            editorMode.ExitWallColorMode(); 

            MazeInputHandler handler = editorMode.GetComponent<MazeInputHandler>();
            if (handler != null)
            {
                handler.ForceReturnToEditMode();
            }
            else
            {
                handler = FindObjectOfType<MazeInputHandler>();
                if (handler != null) handler.ForceReturnToEditMode();
            }
        }
        
        Close();
    }

    public void Open()
    {
        selectedMaterialIndex = -1;
        root.SetActive(true);
    }

    public void Close()
    {
        root.SetActive(false);
    }
}