using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BLEUI : MonoBehaviour
{
    public Button scanButton;
    public Button stopScanButton;
    public Button connectButton;
    public Button disconnectButton;
    public TMP_Text statusText;
    public Transform deviceListParent; // Parent to hold the dynamic buttons
    public GameObject deviceButtonPrefab; // Prefab for device buttons

    private BLEConnect bleConnect;
    private ulong selectedDeviceAddress; // Store the selected device's address

    public void Initialize(BLEConnect scanner)
    {
        bleConnect = scanner;

        // Set up button listeners
        scanButton.onClick.AddListener(bleConnect.StartScan);
        stopScanButton.onClick.AddListener(bleConnect.StopScan);
        connectButton.onClick.AddListener(OnConnectButtonClicked);
        disconnectButton.onClick.AddListener(OnDisconnectButtonClicked);

        // Subscribe to events
        //bleConnect.OnDeviceScanned += CreateDeviceButton;
        bleConnect.OnStatusUpdated += UpdateStatusText;
        bleConnect.OnScanCompleted += OnScanCompleted;

        // Disable buttons if BLE is not supported
        //#if !ENABLE_WINMD_SUPPORT
        //        scanButton.interactable = false;
        //        stopScanButton.interactable = false;
        //        connectButton.interactable = false;
        //        statusText.text = "Bluetooth LE is not supported on this platform.";
        //#endif
        Debug.Log("BLEUI initialized");
    }

    private void OnScanCompleted(Dictionary<ulong, string> scannedDevices)
    {
        // Clear existing device buttons
        ClearDeviceButtons();

        // Create buttons for the scanned devices
        foreach (var device in scannedDevices)
        {
            CreateDeviceButton(device.Key, device.Value);
        }
    }

    private void ClearDeviceButtons()
    {
        // Destroy all existing device buttons
        foreach (Transform child in deviceListParent)
        {
            Destroy(child.gameObject);
        }
    }

    private void CreateDeviceButton(ulong address, string name)
    {
        // Create a button for the discovered device
        GameObject buttonObj = Instantiate(deviceButtonPrefab, deviceListParent);
        TMP_Text textComponent = buttonObj.GetComponentInChildren<TMP_Text>();

        // Set the button text
        textComponent.text = $"{name} ({address:X})";

        // Force the text to recalculate its size
        LayoutRebuilder.ForceRebuildLayoutImmediate(textComponent.rectTransform);

        // Get the preferred height of the text
        float preferredHeight = textComponent.preferredHeight;

        // Adjust the button height dynamically
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(buttonRect.sizeDelta.x, Mathf.Max(preferredHeight + 20, 60)); // Minimum height 60

        // Add a listener to select the device when the button is clicked
        Button buttonComponent = buttonObj.GetComponent<Button>();
        buttonComponent.onClick.AddListener(() => SelectDevice(address, name));

        // Ensure the button is interactable
        buttonComponent.interactable = true;
    }


    private void SelectDevice(ulong address, string name)
    {
        // Store the selected device's address
        selectedDeviceAddress = address;
        statusText.text = $"Selected Device: {name} ({address:X})";
    }

    private void OnConnectButtonClicked()
    {
        if (selectedDeviceAddress == 0)
        {
            statusText.text = "No device selected. Please select a device first.";
            return;
        }

        if (bleConnect.IsDeviceConnected())
        {
            statusText.text = "Can only connect to one device at a time.";
            return;
        }

        bleConnect.ConnectToDevice(selectedDeviceAddress);
    }

    private void OnDisconnectButtonClicked()
    {
        if (bleConnect == null || !bleConnect.IsDeviceConnected())
        {
            statusText.text = "No device to disconnect";
            return;
        }
        bleConnect.Disconnect();
    }

    private void UpdateStatusText(string status)
    {
        statusText.text = status;
    }
}
