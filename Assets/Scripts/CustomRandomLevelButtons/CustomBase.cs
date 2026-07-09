using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CustomBase : MonoBehaviour
{
    [SerializeField] Button upButtonBase;
    [SerializeField] Button downButtonBase;
    [SerializeField] TMP_Text baseText;
    [SerializeField] TMP_Text warningText;
    [SerializeField] private Button enterButton;
    [SerializeField] private TMP_Text baseTMP;

    void Awake()
    {
        if (upButtonBase != null)
        {
            upButtonBase.onClick.AddListener(IncreaseBase);
        }
        else
        {
            Debug.Log("No Up Button Base found in " + SceneManager.GetActiveScene().name);
        }

        if (downButtonBase != null)
        {
            downButtonBase.onClick.AddListener(DecreaseBase);
        }
        else
        {
            Debug.Log("No Down Button Base found in " + SceneManager.GetActiveScene().name);
        }

        if (enterButton != null)
        {
            enterButton.onClick.AddListener(() => SelectLevel());
        }
        else
            Debug.LogWarning("Enter button not assigned");
    }

    void DecreaseBase()
    {
        string stringBase = baseText.text;
        int initialIntBase = int.Parse(stringBase);
        int newIntBase = initialIntBase - 6;

        if (newIntBase < 6)
        {
            warningText.text = "Base cannot go below 6.";
            baseText.SetText(initialIntBase.ToString());
        }
        else
        {
            baseText.SetText(newIntBase.ToString());
        }
    }

    void IncreaseBase()
    {
        string stringBase = baseText.text;
        int initialIntBase = int.Parse(stringBase);
        int newIntBase = initialIntBase + 6;

        baseText.SetText(newIntBase.ToString());
    }

    private void SelectLevel()
    {
        if (!int.TryParse(baseTMP.text, out int triangleBase))
        {
            Debug.LogWarning("Invalid base value.");
            return;
        }

        // Target folder structure created one time here upon submission validation
        if (ScreenshotManager.Instance != null)
        {
            ScreenshotManager.Instance.CreateSessionSubfolder();
        }

        GameManager.Instance.SetMazeSize(triangleBase * 2, triangleBase * 2, GameManager.MazeShape.Triangle);
        SceneManager.LoadScene("RandomLevel");
    }
}
