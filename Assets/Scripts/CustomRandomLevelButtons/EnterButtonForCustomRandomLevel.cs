using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EnterButtonForCustomRandomLevel : MonoBehaviour
{
    [SerializeField] private Button enterButton;
    [SerializeField] private TMP_Text widthTMP;
    [SerializeField] private TMP_Text depthTMP;

    public string levelName;

    void Awake()
    {
        if (enterButton != null)
        {
            enterButton.onClick.AddListener(() => SelectLevel());
        }
        else
            Debug.LogWarning("Enter button not assigned");
    }

    private void SelectLevel()
    {
        if (!int.TryParse(widthTMP.text, out int width))
        {
            Debug.LogWarning("Invalid width value.");
            return;
        }

        if (!int.TryParse(depthTMP.text, out int depth))
        {
            Debug.LogWarning("Invalid depth value.");
            return;
        }

        GameManager.Instance.SetMazeSize(width, depth);
        SceneManager.LoadScene("RandomLevel");
    }
}
