using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IncreaseDepth_MS : MonoBehaviour
{
    [SerializeField] Button upButtonDepth;
    [SerializeField] TMP_Text depthText;
    [SerializeField] TMP_Text warningText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (upButtonDepth != null)
        {
            upButtonDepth.onClick.AddListener(IncreaseDepth);
        }
        else
        {
            Debug.Log("No Up Button Depth found in " + SceneManager.GetActiveScene().name);
        }
    }

    void IncreaseDepth()
    {
        string stringDepth = depthText.text;
        int initialIntDepth = int.Parse(stringDepth);
        int newIntDepth = initialIntDepth + 12;

        depthText.SetText(newIntDepth.ToString());
    }
}