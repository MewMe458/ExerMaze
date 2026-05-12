using UnityEngine;

public class BLEManager : MonoBehaviour
{
    public static BLEManager Instance { get; set; }

    public BLEConnect bleConnect; // Scanning and connecting
    public BLEUI bluetoothUI; // UI updates
    public BLEDataHandler bleDataHandler; // Data handling

    void Awake()
    {
        Debug.Log("BLEManager Awake called.");

        // Singleton pattern to ensure only one instance exists
        if (Instance == null)
        {
            Debug.Log("Setting BLEManager instance.");
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Debug.Log("Duplicate BLEManager instance found. Destroying this instance.");
            Destroy(gameObject); // Ensure only one instance exists
        }
    }

    void Start()
    {
        Debug.Log("BLEManager initialized.");

        // Initialize the components
        if (bleConnect == null)
        {
            Debug.LogError("bleConnect is not assigned in the Inspector.");
        }
        if (bluetoothUI == null)
        {
            Debug.LogError("bluetoothUI is not assigned in the Inspector.");
        }
        if (bleDataHandler == null)
        {
            Debug.LogError("bleDataHandler is not assigned in the Inspector.");
        }

        bluetoothUI?.Initialize(bleConnect);
        bleDataHandler?.Initialize(bleConnect);
        bleConnect?.InitializeWatcher();
    }
}