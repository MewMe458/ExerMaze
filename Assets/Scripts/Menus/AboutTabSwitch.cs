using UnityEngine;
using UnityEngine.UI;

public class AboutTabSwitch : MonoBehaviour
{
    public Button[] tabButtons;
    public GameObject[] tabContents;
    private Color normalColor = Color.white; // Normal color for active tab
    private Color dimmedColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
    private int defaultTabIndex = 0; // Index of the default tab to show

    void Start()
    {
        if (tabButtons == null || tabContents == null || tabButtons.Length != tabContents.Length)
        {
            Debug.LogError("Tab buttons and contents are null or their length are not matched.");
            return;
        }

        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i; // Capture index for listener
            tabButtons[i].onClick.AddListener(() => SwitchTab(index));
        }

        // Activate default tab
        defaultTabIndex = Mathf.Clamp(defaultTabIndex, 0, tabButtons.Length - 1);
        SwitchTab(defaultTabIndex);
    }

    private void SwitchTab(int activeIndex)
    {
        // Validate index
        if (activeIndex < 0 || activeIndex >= tabButtons.Length)
        {
            Debug.LogError($"Invalid tab index {activeIndex}");
            return;
        }

        // Update panels and button colors
        for (int i = 0; i < tabButtons.Length; i++)
        {
            bool isActive = (i == activeIndex);
            tabContents[i].SetActive(isActive);
            tabButtons[i].image.color = isActive ? normalColor : dimmedColor;
        }

        Debug.Log($"Switched to tab {activeIndex}");
    }
}
