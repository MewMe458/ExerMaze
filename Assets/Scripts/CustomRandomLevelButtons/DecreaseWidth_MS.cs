using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DecreaseWidth_MS : MonoBehaviour
{
    [SerializeField] Button downButtonWidth;
    [SerializeField] TMP_Text widthText;
    [SerializeField] TMP_Text warningText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (downButtonWidth != null)
        {
            downButtonWidth.onClick.AddListener(DecreaseWidth);
        }
        else
        {
            Debug.Log("No Up Button Width found in " + SceneManager.GetActiveScene().name);
        }
    }

    void DecreaseWidth()
    {
        string stringWidth = widthText.text;
        int initialIntWidth = int.Parse(stringWidth);
        int newIntWidth = initialIntWidth - 12;

        if (newIntWidth < 12)
        {
            warningText.text = "Width cannot go below 12.";
            widthText.SetText(initialIntWidth.ToString());
        }
        else
        {
            widthText.SetText(newIntWidth.ToString());
        }
    }
}