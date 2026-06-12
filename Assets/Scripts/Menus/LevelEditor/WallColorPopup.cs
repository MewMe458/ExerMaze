using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class WallColorPopup : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform contentParent;
    [SerializeField] private Button materialButtonPrefab;
    // 🔥 ADDED: Reference to your close/cancel button so we can listen to clicks
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

        // 🔥 ADDED: Hook up the Cancel button functionality automatically on startup
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
                
                // Save selection down to the Mode tracker and close
                if (editorMode != null)
                {
                    editorMode.SetGlobalMaterialIndex(selectedMaterialIndex);
                }
                Close();
            });
        }
    }

    // ⚙️ UPDATED: Cancel handler to reset both the logic mode and the UI buttons
    private void OnCancelPressed()
    {
        // Find or check the input handler to update button highlights
        if (editorMode != null)
        {
            // First drop out of custom wall painting states
            editorMode.ExitWallColorMode(); 

            // Find the input handler sitting on the controller structure to update visual elements
            MazeInputHandler handler = editorMode.GetComponent<MazeInputHandler>();
            if (handler != null)
            {
                handler.ForceReturnToEditMode();
            }
            else
            {
                // Fallback attempt if it's structured differently in your hierarchy
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