using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;
#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
#endif

public class LevelInfoObject : MonoBehaviour
{
    private TMP_Text levelNameText;
    private TMP_Text dateText;
    private TMP_Text sizeText;
    private TMP_Text modeText;
    private Button selectButton;
    private Outline outline;

    private string levelName;
    private string date;
    private string size;
    private string mode;
    private string filePath;
    private CustomLevelSelect manager;
    private bool isSelected = false;

    public string LevelName => levelName;
    public string Date => date;
    public string Size => size;
    public string Mode => mode;
    public string FilePath => filePath;

    void Awake()
    {
        // Fetch components dynamically
        selectButton = GetComponent<Button>();
        outline = GetComponent<Outline>();
        levelNameText = transform.Find("LevelNameText")?.GetComponent<TMP_Text>();
        dateText = transform.Find("DateText")?.GetComponent<TMP_Text>();
        sizeText = transform.Find("SizeText")?.GetComponent<TMP_Text>();
        modeText = transform.Find("ModeText")?.GetComponent<TMP_Text>();

        if (levelNameText == null) Debug.LogError("LevelNameText child not found or missing TMP_Text!");
        if (dateText == null) Debug.LogError("DateText child not found or missing TMP_Text!");
        if (sizeText == null) Debug.LogError("SizeText child not found or missing TMP_Text!");
        if (modeText == null) Debug.LogError("ModeText child not found or missing TMP_Text!");
        if (selectButton == null) Debug.LogError("Select Button component not found on root!");
        if (outline == null) Debug.LogError("Outline component not found on root!");

        selectButton.onClick.AddListener(OnSelectButtonClicked);
        outline.enabled = false; // Outline off by default
    }

    public void Initialize(string levelName, string date, string size, string mode, string filePath, CustomLevelSelect manager)
    {
        this.levelName = levelName;
        this.date = date;
        this.size = size;
        this.mode = mode;
        this.filePath = filePath;
        this.manager = manager;

        levelNameText.text = levelName;
        dateText.text = date;
        sizeText.text = size;
        modeText.text = mode;
    }

    private void OnSelectButtonClicked()
    {
        if (isSelected)
        {
            Deselect();
        }
        else
        {
            Select();
        }
    }

    public void Select()
    {
        isSelected = true;
        outline.enabled = true;
        manager.OnLevelInfoSelected(this);
    }

    public void Deselect()
    {
        isSelected = false;
        outline.enabled = false;
        manager.OnLevelInfoDeselected(this);
    }

    public bool IsFileValid()
    {
#if ENABLE_WINMD_SUPPORT
        try
        {
            var task = StorageFile.GetFileFromPathAsync(filePath).AsTask();
            task.Wait();
            return task.Result != null;
        }
        catch
        {
            return false;
        }
#else
        return File.Exists(filePath);
#endif
    }

    public void UpdateDate()
    {
        date = DateTime.Now.ToString("dd/MM/yyyy, h:mm tt");
        dateText.text = date;
    }
}