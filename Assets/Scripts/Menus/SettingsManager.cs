#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Storage.AccessCache;
#endif
using UnityEngine;
using UnityEngine.UI; // For Button
using TMPro;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting;

public class SettingsManager : MonoBehaviour
{
    #region Screenshot directory fields
    [SerializeField] private TMP_Text directoryText;
    [SerializeField] private TMP_Dropdown coordinatesDropdown;
    private ScreenshotManager screenshotManager;
    private string defaultDirectory;
    private const string DIRECTORY_KEY = "ScreenshotDirectory";
    private const string FOLDER_TOKEN_KEY = "ScreenshotFolderToken";
    #endregion

    #region GPS Tracking fields
    [SerializeField] private Button editNameButton; // Button to edit the name
    [SerializeField] private Button deleteButton; // Button to delete the coordinate
    [SerializeField] private TMP_InputField nameInputField; // Input field for editing the name of saved coordinates
    [SerializeField] private Button saveButton; // Save button for the new name
    [SerializeField] private Button cancelButton; // Cancel button for editing

    private const string SELECTED_COORD_INDEX_KEY = "SelectedCoordinateIndex";
    private const int NAME_CHARACTER_LIMIT = 20; // Limit for coordinate names

    private int currentEditIndex = -1; // Track which coordinate is being edited

    [SerializeField] private Toggle characterTrackingToggle;
    [SerializeField] private Toggle realLifeTrackingToggle;
    [SerializeField] private ToggleGroup trackingModeToggleGroup;
    [SerializeField] private Slider stepLengthSlider;
    [SerializeField] private TMP_Text stepLengthText;
    #endregion

    #region Sound settings fields
    [SerializeField] private Slider masterSlider;
    [SerializeField] private TMP_Text masterValueText;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TMP_Text bgmValueText;
    [SerializeField] private Slider uiSlider;
    [SerializeField] private TMP_Text uiValueText;
    [SerializeField] private Slider characterSlider;
    [SerializeField] private TMP_Text characterValueText;
    [SerializeField] private Slider effectSlider;
    [SerializeField] private TMP_Text effectValueText;
    [SerializeField] private Toggle soundToggle;
    #endregion

    void Start()
    {
        screenshotManager = ScreenshotManager.Instance;
        if (screenshotManager == null)
        {
            UnityEngine.Debug.LogError("SettingsManager: ScreenshotManager not found.");
            if (directoryText != null) directoryText.text = "Error: ScreenshotManager not found";
            return;
        }

        if (coordinatesDropdown == null || editNameButton == null || deleteButton == null ||
            nameInputField == null || saveButton == null || cancelButton == null)
        {
            UnityEngine.Debug.LogError("SettingsManager: One or more UI elements are not assigned in the Inspector.");
            return;
        }

        if (masterSlider == null || masterValueText == null || bgmSlider == null || bgmValueText == null ||
            uiSlider == null || uiValueText == null || characterSlider == null || characterValueText == null ||
            effectSlider == null || effectValueText == null || soundToggle == null)
        {
            UnityEngine.Debug.LogError("SettingsManager: One or more sound UI elements are not assigned in the Inspector.");
            return;
        }

        defaultDirectory = GetDefaultDirectory();

        // Initialize screenshot folder
        if (!PlayerPrefs.HasKey(DIRECTORY_KEY))
        {
            UnityEngine.Debug.LogError("First-time user detected. Setting default screenshot directory.");
            PlayerPrefs.SetString(DIRECTORY_KEY, defaultDirectory);
            PlayerPrefs.Save();
        }

        string currentPath = PlayerPrefs.GetString(DIRECTORY_KEY, defaultDirectory);

        #if ENABLE_WINMD_SUPPORT
            string folderToken = PlayerPrefs.GetString(FOLDER_TOKEN_KEY, "");
            if (!string.IsNullOrEmpty(folderToken))
            {
                screenshotManager.SetScreenshotFolder(currentPath, null);
            }
            else
            {
                screenshotManager.SetScreenshotFolder(currentPath, null);
            }
        #else
            if (!Directory.Exists(currentPath))
            {
                Directory.CreateDirectory(currentPath);
            }
            screenshotManager.SetScreenshotFolder(currentPath, null);
        #endif

        UpdateDirectoryText();
        SetupCoordinatesDropdown();

        // Set up the input field character limit
        nameInputField.characterLimit = NAME_CHARACTER_LIMIT;

        // Initially hide the edit UI
        SetEditMode(false);

        // Set up button listeners
        editNameButton.onClick.AddListener(EditCoordinateName);
        deleteButton.onClick.AddListener(DeleteCoordinate);
        saveButton.onClick.AddListener(SaveNewName);
        cancelButton.onClick.AddListener(CancelEdit);

        // Initialize sound settings
        InitializeSoundSettings();

        // Subscribe to coordinate save events
        GPXCoordinate.OnCoordinateSaved += RefreshCoordinatesDropdown;
        InitializeTrackingSettings();
    }

    void OnDestroy()
    {
        GPXCoordinate.OnCoordinateSaved -= RefreshCoordinatesDropdown;
        GPXCoordinate.OnSettingsChanged -= UpdateTrackingUI;
    }

    #region Screenshot directory methods
    private string GetDefaultDirectory()
    {
    #if ENABLE_WINMD_SUPPORT
        string resultPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
    #else
        string resultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "FitMazeScreenshots"
        );

        if (!Directory.Exists(resultPath))
        {
            Directory.CreateDirectory(resultPath);
        }
    #endif

        UnityEngine.Debug.Log($"Default directory set to: {resultPath}");
        return resultPath;
    }

    public void ChangeDirectory()
    {
        if (screenshotManager == null)
        {
            UnityEngine.Debug.LogError("Cannot change directory: ScreenshotManager is not assigned.");
            return;
        }

#if ENABLE_WINMD_SUPPORT
        UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
        {
            try
            {
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                folderPicker.FileTypeFilter.Add("*");
                UnityEngine.Debug.Log("Displaying folder picker...");
                StorageFolder folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    UnityEngine.Debug.Log($"Folder selected: {folder.Path}");
                    string newDirectory = folder.Path;
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        string token = StorageApplicationPermissions.FutureAccessList.Add(folder);
                        PlayerPrefs.SetString(FOLDER_TOKEN_KEY, token);
                        PlayerPrefs.SetString(DIRECTORY_KEY, newDirectory);
                        PlayerPrefs.Save();
                        screenshotManager.SetScreenshotFolder(newDirectory, folder);
                        UpdateDirectoryText();
                    }, false);
                }
                else
                {
                    UnityEngine.Debug.Log("Folder selection canceled by user.");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error selecting folder: {ex.Message}\n{ex.StackTrace}");
            }
        }, false);
#endif
    }

#if ENABLE_WINMD_SUPPORT
    private void SetScreenshotDirectory(string directory, StorageFolder folder = null)
    {
        if (string.IsNullOrEmpty(directory))
        {
            directory = defaultDirectory;
        }

        try
        {
            screenshotManager.SetScreenshotFolder(directory, folder ?? null);
            UnityEngine.Debug.Log($"Directory set: {directory}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to set directory '{directory}': {ex.Message}");
            string fallbackPath = GetDefaultDirectory();
            screenshotManager.SetScreenshotFolder(fallbackPath, null);
        }
    }
#endif

    private void UpdateDirectoryText()
    {
        if (directoryText == null)
        {
            UnityEngine.Debug.LogError("DirectoryText is not assigned in the Inspector.");
            return;
        }

        if (screenshotManager == null)
        {
            directoryText.text = "Error: ScreenshotManager not found";
            return;
        }

        directoryText.text = screenshotManager.GetScreenshotFolder();
    }
    #endregion

    #region GPS Tracking methods
    private void SetupCoordinatesDropdown()
    {
        if (coordinatesDropdown == null)
        {
            UnityEngine.Debug.LogError("CoordinatesDropdown is not assigned in the Inspector.");
            return;
        }

        RefreshCoordinatesDropdown();
    }

    private void RefreshCoordinatesDropdown()
    {
        var coordinates = GPXCoordinate.GetSavedCoordinates();
        UnityEngine.Debug.Log($"Refreshing dropdown with {coordinates.Count} coordinates");
        coordinatesDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var coord in coordinates)
        {
            options.Add($"{coord.name} (Lat: {coord.latitude}, Lon: {coord.longitude})");
        }
        coordinatesDropdown.AddOptions(options);

        int selectedIndex = PlayerPrefs.GetInt(SELECTED_COORD_INDEX_KEY, 0);
        coordinatesDropdown.value = Mathf.Clamp(selectedIndex, 0, coordinates.Count - 1);
        coordinatesDropdown.onValueChanged.RemoveAllListeners();
        coordinatesDropdown.onValueChanged.AddListener(OnCoordinateSelected);
        OnCoordinateSelected(coordinatesDropdown.value);
        coordinatesDropdown.RefreshShownValue();
    }

    private void OnCoordinateSelected(int index)
    {
        GPXCoordinate.SetInitialFromSaved(index);
        PlayerPrefs.SetInt(SELECTED_COORD_INDEX_KEY, index);
        PlayerPrefs.Save();
        UnityEngine.Debug.Log($"SettingsManager: Selected coordinate index {index}");

        // Show/hide Edit and Delete buttons based on whether the selected coordinate is "Default"
        bool isDefaultCoordinate = index == 0 && GPXCoordinate.GetSavedCoordinates()[index].name == "Default";
        editNameButton.gameObject.SetActive(!isDefaultCoordinate);
        deleteButton.gameObject.SetActive(!isDefaultCoordinate);
    }

    private void EditCoordinateName()
    {
        currentEditIndex = coordinatesDropdown.value;
        var selectedCoord = GPXCoordinate.GetSavedCoordinates()[currentEditIndex];
        nameInputField.text = selectedCoord.name;

        // Hide dropdown and buttons, show edit UI
        SetEditMode(true);
    }

    private void SaveNewName()
    {
        string newName = nameInputField.text.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            UnityEngine.Debug.LogWarning("Cannot save empty name for coordinate.");
            return;
        }

        var coordinates = GPXCoordinate.GetSavedCoordinates();
        if (currentEditIndex >= 0 && currentEditIndex < coordinates.Count)
        {
            var coord = coordinates[currentEditIndex];
            GPXCoordinate.UpdateCoordinateName(currentEditIndex, newName);
            UnityEngine.Debug.Log($"Updated coordinate name to: {newName} (Lat: {coord.latitude}, Lon: {coord.longitude})");
        }

        // Exit edit mode and refresh UI
        SetEditMode(false);
        RefreshCoordinatesDropdown();
        coordinatesDropdown.value = currentEditIndex; // Restore selection
    }

    private void DeleteCoordinate()
    {
        int indexToDelete = coordinatesDropdown.value;
        var coordinates = GPXCoordinate.GetSavedCoordinates();
        if (indexToDelete == 0 && coordinates[indexToDelete].name == "Default")
        {
            UnityEngine.Debug.LogWarning("Cannot delete the default coordinate.");
            return;
        }

        if (indexToDelete >= 0 && indexToDelete < coordinates.Count)
        {
            var coord = coordinates[indexToDelete];
            GPXCoordinate.DeleteCoordinate(indexToDelete);
            UnityEngine.Debug.Log($"Deleted coordinate: {coord.name} (Lat: {coord.latitude}, Lon: {coord.longitude})");

            // Refresh dropdown and reset selection
            RefreshCoordinatesDropdown();
            coordinatesDropdown.value = Mathf.Min(indexToDelete, coordinates.Count - 2); // Adjust selection
        }
    }

    private void CancelEdit()
    {
        SetEditMode(false);
    }

    private void SetEditMode(bool isEditing)
    {
        coordinatesDropdown.gameObject.SetActive(!isEditing);
        editNameButton.gameObject.SetActive(!isEditing && coordinatesDropdown.value != 0);
        deleteButton.gameObject.SetActive(!isEditing && coordinatesDropdown.value != 0);
        nameInputField.gameObject.SetActive(isEditing);
        saveButton.gameObject.SetActive(isEditing);
        cancelButton.gameObject.SetActive(isEditing);

        if (!isEditing)
        {
            currentEditIndex = -1;
            nameInputField.text = "";
        }
    }

    private void InitializeTrackingSettings()
    {
        if (characterTrackingToggle == null || realLifeTrackingToggle == null || trackingModeToggleGroup == null ||
            stepLengthSlider == null || stepLengthText == null)
        {
            UnityEngine.Debug.LogError("SettingsManager: One or more tracking UI elements are not assigned in the Inspector.");
            return;
        }

        // Initialize toggles
        characterTrackingToggle.isOn = GPXCoordinate.CurrentTrackingMode == GPXCoordinate.TrackingMode.CharacterTracking;
        realLifeTrackingToggle.isOn = GPXCoordinate.CurrentTrackingMode == GPXCoordinate.TrackingMode.RealLifeTracking;

        // Ensure toggles are in the toggle group
        characterTrackingToggle.group = trackingModeToggleGroup;
        realLifeTrackingToggle.group = trackingModeToggleGroup;

        // Add toggle listeners
        characterTrackingToggle.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                GPXCoordinate.SetTrackingMode(GPXCoordinate.TrackingMode.CharacterTracking);
            }
        });
        realLifeTrackingToggle.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                GPXCoordinate.SetTrackingMode(GPXCoordinate.TrackingMode.RealLifeTracking);
            }
        });

        // Initialize slider
        stepLengthSlider.minValue = 65f;
        stepLengthSlider.maxValue = 80f;
        stepLengthSlider.value = GPXCoordinate.StepLength * 100f; // Convert meters to slider value
        stepLengthSlider.wholeNumbers = true; // Ensure integer steps
        UpdateStepLengthText();
        stepLengthSlider.onValueChanged.AddListener((value) =>
        {
            GPXCoordinate.SetStepLength(value);
            UpdateStepLengthText();
        });

        // Subscribe to settings changes
        GPXCoordinate.OnSettingsChanged += UpdateTrackingUI;
    }

    private void UpdateTrackingUI()
    {
        characterTrackingToggle.isOn = GPXCoordinate.CurrentTrackingMode == GPXCoordinate.TrackingMode.CharacterTracking;
        realLifeTrackingToggle.isOn = GPXCoordinate.CurrentTrackingMode == GPXCoordinate.TrackingMode.RealLifeTracking;
        stepLengthSlider.value = GPXCoordinate.StepLength * 100f; // Convert meters to slider value
        UpdateStepLengthText();
    }

    private void UpdateStepLengthText()
    {
        if (stepLengthText != null)
        {
            stepLengthText.text = $"{GPXCoordinate.StepLength:F2} m";
        }
    }
    #endregion

    #region Sound settings methods
    private void InitializeSoundSettings()
    {
        SoundManager manager = SoundManager.Instance;
        if (manager == null) return;


        masterSlider.onValueChanged.RemoveAllListeners();
        bgmSlider.onValueChanged.RemoveAllListeners();
        uiSlider.onValueChanged.RemoveAllListeners();
        characterSlider.onValueChanged.RemoveAllListeners();
        effectSlider.onValueChanged.RemoveAllListeners();
        soundToggle.onValueChanged.RemoveAllListeners();


        masterSlider.value = manager.MasterVolume * 100;
        bgmSlider.value = manager.BGMVolume * 100;
        uiSlider.value = manager.UIVolume * 100;
        characterSlider.value = manager.CharacterVolume * 100;
        effectSlider.value = manager.EffectVolume * 100;
        soundToggle.isOn = manager.IsSoundEnabled;

        UpdateSoundText();


        masterSlider.onValueChanged.AddListener(value =>
        {
            manager.SetMasterVolume(value / 100f);
            UpdateSoundText();
        });

        bgmSlider.onValueChanged.AddListener(value =>
        {
            manager.SetBGMVolume(value / 100f);
            UpdateSoundText();
        });

        uiSlider.onValueChanged.AddListener(value =>
        {
            manager.SetUIVolume(value / 100f);
            UpdateSoundText();
        });

        characterSlider.onValueChanged.AddListener(value =>
        {
            manager.SetCharacterVolume(value / 100f);
            UpdateSoundText();
        });

        effectSlider.onValueChanged.AddListener(value =>
        {
            manager.SetEffectVolume(value / 100f);
            UpdateSoundText();
        });

        soundToggle.onValueChanged.AddListener(value =>
        {
            manager.SetSoundEnabled(value);
        });
    }

    private void UpdateSoundText()
    {
        if (masterValueText != null) masterValueText.text = Mathf.RoundToInt(masterSlider.value).ToString();
        if (bgmValueText != null) bgmValueText.text = Mathf.RoundToInt(bgmSlider.value).ToString();
        if (uiValueText != null) uiValueText.text = Mathf.RoundToInt(uiSlider.value).ToString();
        if (characterValueText != null) characterValueText.text = Mathf.RoundToInt(characterSlider.value).ToString();
        if (effectValueText != null) effectValueText.text = Mathf.RoundToInt(effectSlider.value).ToString();
    }
    #endregion
}