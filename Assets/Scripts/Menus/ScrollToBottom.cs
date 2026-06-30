using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScrollToBottom : MonoBehaviour
{
    [Header("Scroll Setup")]
    [SerializeField] private ScrollRect settingsScrollRect;

    private void Start()
    {
        // Check if we came from the Menu popup prompt
        if (PlayerPrefs.GetInt("ScrollToScreenshotSection", 0) == 1)
        {
            // Reset the flag immediately so normal visits don't force-scroll
            PlayerPrefs.SetInt("ScrollToScreenshotSection", 0);
            PlayerPrefs.Save();

            if (settingsScrollRect != null)
            {
                // UI Canvas calculations sometimes take a frame to settle; 
                // a Coroutine ensures it scrolls cleanly at the end of the frame.
                StartCoroutine(ScrollToBottomRoutine());
            }
        }
    }

    private IEnumerator ScrollToBottomRoutine()
    {
        yield return new WaitForEndOfFrame();
        // 0 means bottom, 1 means top for vertical normalized position
        settingsScrollRect.verticalNormalizedPosition = 0f; 
    }
}
