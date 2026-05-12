using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IncreaseWidth_MS : MonoBehaviour
{
    [SerializeField] Button upButtonWidth;
    [SerializeField] TMP_Text widthText;
    [SerializeField] TMP_Text warningText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (upButtonWidth != null)
        {
            upButtonWidth.onClick.AddListener(IncreaseWidth);
        }
        else
        {
            Debug.Log("No Up Button Width found in " + SceneManager.GetActiveScene().name);
        }
    }

    void IncreaseWidth()
    {
        string stringWidth = widthText.text;
        int initialIntWidth = int.Parse(stringWidth);
        int newIntWidth = initialIntWidth + 12;

        widthText.SetText(newIntWidth.ToString());
    }
}