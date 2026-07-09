using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CustomRadius : MonoBehaviour
{
    [SerializeField] Button upButtonRadius;
    [SerializeField] Button downButtonRadius;
    [SerializeField] TMP_Text radiusText;
    [SerializeField] TMP_Text warningText;
    [SerializeField] private Button enterButton;
    [SerializeField] private TMP_Text radiusTMP;

    void Awake()
    {
        if (upButtonRadius != null)
        {
            upButtonRadius.onClick.AddListener(IncreaseRadius);
        }
        else
        {
            Debug.Log("No Up Button Radius found in " + SceneManager.GetActiveScene().name);
        }

        if (downButtonRadius != null)
        {
            downButtonRadius.onClick.AddListener(DecreaseRadius);
        }
        else
        {
            Debug.Log("No Down Button Radius found in " + SceneManager.GetActiveScene().name);
        }

        if (enterButton != null)
        {
            enterButton.onClick.AddListener(() => SelectLevel());
        }
        else
            Debug.LogWarning("Enter button not assigned");
    }

    void DecreaseRadius()
    {
        string stringRadius = radiusText.text;
        int initialIntRadius = int.Parse(stringRadius);
        int newIntRadius = initialIntRadius - 6;

        if (newIntRadius < 6)
        {
            warningText.text = "Radius cannot go below 6.";
            radiusText.SetText(initialIntRadius.ToString());
        }
        else
        {
            radiusText.SetText(newIntRadius.ToString());
        }
    }

    void IncreaseRadius()
    {
        string stringRadius = radiusText.text;
        int initialIntRadius = int.Parse(stringRadius);
        int newIntRadius = initialIntRadius + 6;

        radiusText.SetText(newIntRadius.ToString());
    }

    private void SelectLevel()
    {
        if (!int.TryParse(radiusTMP.text, out int radius))
        {
            Debug.LogWarning("Invalid radius value.");
            return;
        }

        // Target folder structure created one time here upon submission validation
        if (ScreenshotManager.Instance != null)
        {
            ScreenshotManager.Instance.CreateSessionSubfolder();
        }

        GameManager.Instance.SetMazeSize(radius * 2, radius * 2, GameManager.MazeShape.Circle);
        SceneManager.LoadScene("RandomLevel");
    }
}
