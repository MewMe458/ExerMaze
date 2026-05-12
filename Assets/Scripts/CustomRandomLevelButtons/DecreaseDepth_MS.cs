using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DecreaseDepth_MS : MonoBehaviour
{
    [SerializeField] Button downButtonDepth;
    [SerializeField] TMP_Text depthText;
    [SerializeField] TMP_Text warningText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (downButtonDepth != null)
        {
            downButtonDepth.onClick.AddListener(DecreaseDepth);
        }
        else
        {
            Debug.Log("No Up Button Depth found in " + SceneManager.GetActiveScene().name);
        }
    }

    void DecreaseDepth()
    {
        string stringDepth = depthText.text;
        int initialIntDepth = int.Parse(stringDepth);
        int newIntDepth = initialIntDepth - 12;

        if (newIntDepth < 12)
        {
            warningText.text = "Depth cannot go below 12.";
            depthText.SetText(initialIntDepth.ToString());
        }
        else
        {
            depthText.SetText(newIntDepth.ToString());
        }
    }
}