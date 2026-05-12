#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
//#define ENABLE_WINMD_SUPPORT
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

#if ENABLE_WINMD_SUPPORT
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
#endif

public class BluetoothLEClient : MonoBehaviour
{
#if ENABLE_WINMD_SUPPORT
    private BluetoothLEAdvertisementWatcher watcher;
    private Dictionary<ulong, string> scannedDevices = new Dictionary<ulong, string>();
    private ulong selectedDeviceAddress;
    private bool isScanning = false;
    private BluetoothLEDevice connectedDevice;
#endif

    public static BluetoothLEClient Instance { get; private set; }

    // UI References
    public Button scanButton;
    public Button stopScanButton;
    public Button connectButton;
    public Button disconnectButton;
    public TMP_Text statusText;
    public Transform deviceListParent; // Parent to hold the dynamic buttons
    public GameObject deviceButtonPrefab; // Prefab for device buttons

    // UUIDs for testing
    public string targetServiceUUID = "94727bab-f1fe-4104-981c-83cd6c8636aa";
    public string targetStepCharUUID = "49832218-a470-4611-8a24-35c9a3ae5427";
    public string targetTurnCharUUID = "f4660c4e-2d9a-4b46-aa68-00eb8e46e290";

    private List<GameObject> deviceButtons = new List<GameObject>();

    public event Action<string> OnGameStepDataUpdated;
    public event Action<int> OnTurnStateUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Ensure there's only one instance
        }
    }

    void Start()
    {
#if ENABLE_WINMD_SUPPORT
        InitializeWatcher();
        SetupButtons();
#else
        Debug.Log("Bluetooth LE not supported on this platform.");
        statusText.text = "BLE not supported on this platform.";
#endif
    }

#if ENABLE_WINMD_SUPPORT
    void SetupButtons()
    {
        scanButton.onClick.AddListener(StartScan);
        stopScanButton.onClick.AddListener(StopScan);
        connectButton.onClick.AddListener(ConnectToSelectedDevice);
    }

    void InitializeWatcher()
    {
        watcher = new BluetoothLEAdvertisementWatcher();
        watcher.ScanningMode = BluetoothLEScanningMode.Active;

        watcher.Received += OnAdvertisementReceived;
        watcher.Stopped += OnAdvertisementStopped;

        statusText.text = "Ready to scan BLE devices.";
        Debug.Log("BLE Watcher initialized.");
    }

    public void StartScan()
    {
        if (isScanning) return;

        Debug.Log("Starting BLE scan...");
        statusText.text = "Scanning for BLE devices...";
        scannedDevices.Clear();
        ClearDeviceButtons();

        watcher.Start();
        isScanning = true;

        // Stop scan after 5 seconds
        Invoke(nameof(StopScan), 5f);
    }

    public void StopScan()
    {
        if (!isScanning) return;

        Debug.Log("Stopping BLE scan...");
        watcher.Stop();
        isScanning = false;

        statusText.text = $"Scan completed. Devices found: {scannedDevices.Count}";

        foreach (var device in scannedDevices)
        {
            CreateDeviceButton(device.Key, device.Value);
        }
    }

    private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    {
        string deviceName = args.Advertisement.LocalName;
        if (string.IsNullOrEmpty(deviceName))
        {
            deviceName = "Unnamed Device";
        }

        // Check for manufacturer data
        var manufacturerSections = args.Advertisement.ManufacturerData;
        if (manufacturerSections == null || manufacturerSections.Count == 0)
        {
            Debug.Log($"No manufacturer data for device: {args.BluetoothAddress:X}");
            return; // Skip devices without manufacturer data
        }

        foreach (var manufacturerData in manufacturerSections)
        {
            ushort manufacturerId = manufacturerData.CompanyId;
            var data = new byte[manufacturerData.Data.Length];
            using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(manufacturerData.Data))
            {
                reader.ReadBytes(data);
            }

            string manufacturerString = System.Text.Encoding.UTF8.GetString(data);

            // Log manufacturer details for debugging
            Debug.Log($"Manufacturer ID: {manufacturerId:X4}, Data: {manufacturerString}");

            // Filter devices based on "controller"
            if (manufacturerString.Contains("controller") && !scannedDevices.ContainsKey(args.BluetoothAddress))
            {
                Debug.Log($"Device matched with controller: {args.BluetoothAddress:X}");
                scannedDevices[args.BluetoothAddress] = deviceName;

                CreateDeviceButton(args.BluetoothAddress, deviceName);
                return; // Break loop once the desired device is found
            }
        }

        Debug.Log($"Device {args.BluetoothAddress:X} does not match the required manufacturer data.");
    }


    private void OnAdvertisementStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
    {
        Debug.Log("Watcher stopped. Reason: " + args.Error);
        statusText.text = "Watcher stopped.";
    }

    private void CreateDeviceButton(ulong address, string name)
    {
        GameObject buttonObj = Instantiate(deviceButtonPrefab, deviceListParent);
        buttonObj.GetComponentInChildren<TMP_Text>().text = $"{name} ({address:X})";
        buttonObj.GetComponent<Button>().onClick.AddListener(() => SelectDevice(address, name));

        deviceButtons.Add(buttonObj);
    }

    private void SelectDevice(ulong address, string name)
    {
        selectedDeviceAddress = address;
        statusText.text = $"Selected Device: {name} ({address:X})";
    }

    private void ConnectToSelectedDevice()
    {
        if (selectedDeviceAddress == 0)
        {
            statusText.text = "No device selected. Please select a device first.";
            return;
        }

        ConnectToDevice(selectedDeviceAddress);
    }

    private async void ConnectToDevice(ulong address)
    {
        try
        {
            Debug.Log($"Attempting to connect to device: {address:X}");
            statusText.text = "Connecting to device...";

            connectedDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(address);

            if (connectedDevice != null)
            {
                Debug.Log($"Successfully connected to: {connectedDevice.Name}, Address: {address:X}");
                statusText.text = $"Connected to: {connectedDevice.Name}";

                // Check for specific service and characteristics
                await CheckForTargetServiceAndCharacteristics();
            }
            else
            {
                statusText.text = "Failed to connect or connection dropped.";
                Debug.LogError("Connection failed or device disconnected.");
            }
        }
        catch (Exception e)
        {
            statusText.text = $"Error while connecting: {e.Message}";
            Debug.LogError($"Error while connecting: {e}");
        }
    }

    private async Task CheckForTargetServiceAndCharacteristics()
    {
        GattDeviceServicesResult servicesResult = await connectedDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
        if (servicesResult.Status == GattCommunicationStatus.Success)
        {
            foreach (var service in servicesResult.Services)
            {
                if (service.Uuid.ToString() == targetServiceUUID)
                {
                    Debug.Log($"Target Service Found: {service.Uuid}");
                    statusText.text += $"\nTarget Service Found: {service.Uuid}";

                    GattCharacteristicsResult charResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    if (charResult.Status == GattCommunicationStatus.Success)
                    {
                        foreach (var characteristic in charResult.Characteristics)
                        {
                            if (characteristic.Uuid.ToString() == targetStepCharUUID)
                            {
                                Debug.Log($"Step Characteristic Found: {characteristic.Uuid}");
                                await SubscribeToNotifications(characteristic);
                            }
                            else if (characteristic.Uuid.ToString() == targetTurnCharUUID)
                            {
                                Debug.Log($"Turn Characteristic Found: {characteristic.Uuid}");
                                await SubscribeToNotifications(characteristic);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Target service or characteristics not found.");
            statusText.text += "\nTarget service/characteristics not found.";
        }
    }

    private async Task SubscribeToNotifications(GattCharacteristic characteristic)
    {
        // Validate CCCD existence
        var descriptors = await characteristic.GetDescriptorsAsync(BluetoothCacheMode.Uncached);
        if (!descriptors.Descriptors.Any(d => d.Uuid == GattDescriptorUuids.ClientCharacteristicConfiguration))
        {
            Debug.LogError("CCCD not found for the characteristic.");
            statusText.text += "\nCCCD not found for the characteristic.";
            return;
        }

        // Enable notifications if supported
        GattCharacteristicProperties charProperties = characteristic.CharacteristicProperties;
        if (charProperties.HasFlag(GattCharacteristicProperties.Notify))
        {
            Debug.Log("Characteristic supports Notify.");
            statusText.text += "\nChar has notify";

            try
            {
                // Attempt to enable notifications
                GattCommunicationStatus notifyStatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
                Debug.Log($"Notify status: {notifyStatus}");
                statusText.text += $"\nNotify status: {notifyStatus}";

                if (notifyStatus == GattCommunicationStatus.Success)
                {
                    Debug.Log("Successfully enabled notifications for the characteristic.");
                    statusText.text += "\nNotification enabled.";

                    // Subscribe to notifications
                    characteristic.ValueChanged += (sender, args) =>
                    {
                        try
                        {
                            var reader = Windows.Storage.Streams.DataReader.FromBuffer(args.CharacteristicValue);
                            byte[] input = new byte[reader.UnconsumedBufferLength];
                            reader.ReadBytes(input);

                            // Convert the byte array to a string (or other desired format)
                            string receivedData = System.Text.Encoding.UTF8.GetString(input);
                            Debug.Log($"Received notification: {receivedData}");

                            // Ensure Unity operations happen on the main thread
                            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                            {
                                // Update UI or invoke the appropriate event
                                if (characteristic.Uuid.ToString() == targetStepCharUUID)
                                {
                                    OnGameStepDataUpdated?.Invoke(receivedData);
                                }
                                else if (characteristic.Uuid.ToString() == targetTurnCharUUID)
                                {
                                    // Attempt to parse the received data into an integer
                                    if (int.TryParse(receivedData, out int turnState))
                                    {
                                        OnTurnStateUpdated?.Invoke(turnState);
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"Invalid turn state data: {receivedData}");
                                    }
                                }
                            }, false);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error processing notification: {ex.Message}");
                        }
                    };
                }
                else
                {
                    Debug.LogError($"Failed to enable notifications. GattCommunicationStatus: {notifyStatus}");
                    statusText.text += $"\nFailed to enable notifications. Status: {notifyStatus}";
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error enabling notifications: {ex.Message}");
                Debug.LogError(ex); // Log stack trace for deeper analysis
                statusText.text += $"\nError enabling notifications: {ex.Message}";
            }
        }
        else
        {
            Debug.LogError("Characteristic does not support Notify.");
            statusText.text += "\nCharacteristic does not support Notify.";
        }
    }

#endif

    private void ClearDeviceButtons()
    {
        foreach (var button in deviceButtons)
        {
            Destroy(button);
        }
        deviceButtons.Clear();
    }
}
