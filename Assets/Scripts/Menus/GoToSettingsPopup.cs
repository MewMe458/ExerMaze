using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GoToSettingsPopup : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject goToSettingsPopup;
    [SerializeField] private Button okButton;

    // Static variables persist across scene loads during gameplay
    private static bool hasShownPopup = false;

    private void Start()
    {
        // 2) Check if the popup has already been displayed during this game session
        if (hasShownPopup)
        {
            goToSettingsPopup.SetActive(false);
        }
        else
        {
            goToSettingsPopup.SetActive(true);
            hasShownPopup = true; // Mark it as shown so it won't appear again
        }

        // Assign the button listener dynamically if not set in inspector
        if (okButton != null)
        {
            okButton.onClick.AddListener(OnOkButtonPressed);
        }
    }

    private void OnOkButtonPressed()
    {
        // Set a flag that the Settings scene can read to know it needs to scroll down
        PlayerPrefs.SetInt("ScrollToScreenshotSection", 1);
        PlayerPrefs.Save();

        // 1) Load the SettingsScene
        SceneManager.LoadScene("SettingsScene");
    }
}