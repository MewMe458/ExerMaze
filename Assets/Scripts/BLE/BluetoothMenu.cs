using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BluetoothMenu : MonoBehaviour
{
    public static BluetoothMenu Instance;
    public GameObject BluetoothMenuCanvas;
    public GameObject BluetoothMenuPanel;
    public TextMeshProUGUI BluetoothMenuButtonText;
    private bool PanelIsVisible;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(BluetoothMenuCanvas);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PanelIsVisible = BluetoothMenuPanel.activeSelf;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded; // Handle additive scene unloading
        UpdateCanvasVisibility(); // Set initial visibility
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded; // Prevent leaks
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateCanvasVisibility();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        UpdateCanvasVisibility();
    }

    private void UpdateCanvasVisibility()
    {
        if (BluetoothMenuCanvas == null)
        {
            Debug.LogError("BluetoothMenuCanvas is not assigned!");
            return;
        }

        // Check all loaded scenes
        bool shouldHide = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            //Debug.Log($"Checking scene: {scene.name}");
            if (scene.isLoaded && (scene.name == "LevelEditor" || scene.name == "DefaultLevel" || scene.name=="CustomLevel"))
            {
                shouldHide = true;
                break;
            }
        }

        BluetoothMenuCanvas.SetActive(!shouldHide);

        //Debug.Log($"BluetoothMenuCanvas {(BluetoothMenuCanvas.activeSelf ? "shown" : "hidden")} in current scenes");
    }

    public void TooglePanel()
    {
        PanelIsVisible = !PanelIsVisible;
        BluetoothMenuPanel.SetActive(PanelIsVisible && BluetoothMenuCanvas.activeSelf);

        if (PanelIsVisible == true) BluetoothMenuButtonText.text = "Back to\nMenu";
        else BluetoothMenuButtonText.text = "Controller\nMenu";
    }
}
