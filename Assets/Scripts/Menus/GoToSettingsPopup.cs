using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GoToSettingsPopup : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject goToSettingsPopup;
    [SerializeField] private Button okButton;

    private const string DIRECTORY_KEY = "ScreenshotDirectory";

    private void Start()
    {
        // Check if the screenshot folder directory has already been configured/set
        if (PlayerPrefs.HasKey(DIRECTORY_KEY))
        {
            goToSettingsPopup.SetActive(false);
        }
        else
        {
            goToSettingsPopup.SetActive(true);
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

        // Load the SettingsScene
        SceneManager.LoadScene("SettingsScene");
    }
}