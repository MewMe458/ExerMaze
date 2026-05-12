using UnityEngine;

#if ENABLE_WINMD_SUPPORT
using Windows.UI.ViewManagement;
using Windows.Foundation;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
#endif


public class ResolutionManager : MonoBehaviour
{
    public int minWidth = 800;
    public int minHeight = 600;
    public int defaultWindowedWidth = 1280;
    public int defaultWindowedHeight = 720;

    void Start()
    {
#if ENABLE_WINMD_SUPPORT && UNITY_WSA && !UNITY_EDITOR
    var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

    if (dispatcher != null)
    {
        _ = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
            var view = ApplicationView.GetForCurrentView();
            view.SetPreferredMinSize(new Size(minHeight, minWidth));
        });
    }
#endif

        // Load fullscreen preference (this is safe to call on Unity's thread)
        bool isFullScreen = PlayerPrefs.GetInt("fullscreen", 1) == 1;
        SetFullscreen(isFullScreen);
    }


    void Update()
    {
        // Toggle fullscreen mode with F11
        if (Input.GetKeyDown(KeyCode.F11))
        {
            bool isFullScreen = Screen.fullScreen;
            SetFullscreen(!isFullScreen);
        }

        // Auto-correct fullscreen resolution if it gets out of sync
        if (Screen.fullScreen)
        {
            int screenWidth = Display.main.systemWidth;
            int screenHeight = Display.main.systemHeight;

            if (Screen.width != screenWidth || Screen.height != screenHeight)
            {
                Screen.SetResolution(screenWidth, screenHeight, FullScreenMode.FullScreenWindow);
            }
        }
    }

    void SetFullscreen(bool fullscreen)
    {
        if (fullscreen)
        {
            int screenWidth = Display.main.systemWidth;
            int screenHeight = Display.main.systemHeight;
            Screen.SetResolution(screenWidth, screenHeight, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(defaultWindowedWidth, defaultWindowedHeight, false);
        }

        PlayerPrefs.SetInt("fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}
