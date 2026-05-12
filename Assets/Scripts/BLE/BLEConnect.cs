using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

#if ENABLE_WINMD_SUPPORT
using Windows.Foundation;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
#endif

public class BLEConnect : MonoBehaviour
{
#if ENABLE_WINMD_SUPPORT
    private BluetoothLEAdvertisementWatcher watcher;
    private Dictionary<ulong, string> scannedDevices = new Dictionary<ulong, string>();
    private ulong selectedDeviceAddress;
    private bool isScanning = false;
    private BluetoothLEDevice connectedDevice;
    private List<(GattCharacteristic characteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> handler)> subscribedCharacteristics 
      = new List<(GattCharacteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs>)>();
#endif

    // UUIDs for service and characteristics
    private string targetServiceUUID = "94727bab-f1fe-4104-981c-83cd6c8636aa";
    private string targetStepCharUUID = "49832218-a470-4611-8a24-35c9a3ae5427";
    private string targetTurnCharUUID = "f4660c4e-2d9a-4b46-aa68-00eb8e46e290";
    private string targetMapCharUUID = "da0805d7-016e-4cac-9366-b3aea37a1921";
    private string targetToggleSensorCharUUID = "601484c4-c192-4c5c-bca0-391feab42071";
    private string targetPauseCharUUID = "38d4b4d2-a43b-4a0b-8971-1032ae2a366e";
    private string targetScreenshotCharUUID = "086ca2e7-1794-489f-aa01-b03dc91379cf";

    public event Action<ulong, string> OnDeviceScanned;
    public event Action<Dictionary<ulong, string>> OnScanCompleted;
    public event Action<string> OnStatusUpdated;
    public event Action<string> OnGameStepDataUpdated;
    public event Action<int> OnTurnStateUpdated;
    public event Action<string> OnMapCoordinateReceived;
    public event Action OnPauseRequested;
    public event Action OnScreenshotRequested;

    public void InitializeWatcher()
    {
#if ENABLE_WINMD_SUPPORT
        watcher = new BluetoothLEAdvertisementWatcher();
        watcher.ScanningMode = BluetoothLEScanningMode.Active;

        watcher.Received += OnAdvertisementReceived;
        watcher.Stopped += OnAdvertisementStopped;

        OnStatusUpdated?.Invoke("Ready to scan BLE devices.");
        Debug.Log("Watcher initialized");
#else
        OnStatusUpdated?.Invoke("BLE not supported on this platform.");
#endif
    }

    public void StartScan()
    {
#if ENABLE_WINMD_SUPPORT
        if (isScanning) return;

        scannedDevices.Clear();
        watcher.Start();
        isScanning = true;

        OnStatusUpdated?.Invoke("Scanning for BLE devices...");

        // Stop scan after 5 seconds
        Invoke(nameof(StopScan), 5f);
#else
        OnStatusUpdated?.Invoke("Start Scan: BLE not supported on this platform.");
#endif
    }

    public void StopScan()
    {
#if ENABLE_WINMD_SUPPORT
        if (!isScanning) return;

        watcher.Stop();
        isScanning = false;

        OnStatusUpdated?.Invoke($"Scan completed. Devices found: {scannedDevices.Count}");

        // Notify subscribers that the scan is complete
        OnScanCompleted?.Invoke(scannedDevices);
#else
        OnStatusUpdated?.Invoke("Stop Scan: BLE not supported on this platform.");
#endif
    }

    public bool IsDeviceConnected()
    {
#if ENABLE_WINMD_SUPPORT
        return connectedDevice != null;
#else
        return false;
#endif
    }

    public async void ConnectToDevice(ulong address)
    {
#if ENABLE_WINMD_SUPPORT
        try
        {
            OnStatusUpdated?.Invoke($"Connecting to device ({address:X})...");
            connectedDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(address);
            if (connectedDevice != null)
            {
                string deviceName = connectedDevice.Name ?? "Unknown Device";
                bool success = await CheckForTargetServiceAndCharacteristics();
                if (success)
                {
                    OnStatusUpdated?.Invoke($"Connected to: {deviceName}");
                }
                else
                {
                    connectedDevice?.Dispose();
                    connectedDevice = null;
                    OnStatusUpdated?.Invoke("Failed to connect: Service or characteristics not found.");
                }
            }
            else
            {
                OnStatusUpdated?.Invoke("Failed to connect: Device not found.");
            }
        }
        catch (Exception e)
        {
            connectedDevice?.Dispose();
            connectedDevice = null;
            OnStatusUpdated?.Invoke($"Connection failed: {e.Message}");
        }
#else
        OnStatusUpdated?.Invoke("BLE not supported on this platform.");
#endif
    }

    public async void Disconnect()
    {
#if ENABLE_WINMD_SUPPORT
    if (connectedDevice == null)
    {
        OnStatusUpdated?.Invoke("No device connected.");
        return;
    }

    OnStatusUpdated?.Invoke("Disconnecting...");
    try
    {
        // Unsubscribe from all characteristic notifications
        foreach (var (characteristic, handler) in subscribedCharacteristics)
        {
            characteristic.ValueChanged -= handler;
            Debug.Log($"Unsubscribed from characteristic: {characteristic.Uuid}");
        }
        subscribedCharacteristics.Clear();

        // Dispose of the connected device
        connectedDevice?.Dispose();
        connectedDevice = null;
        selectedDeviceAddress = 0;

        OnStatusUpdated?.Invoke("Disconnected from device.");
        Debug.Log("BLE device disconnected and resources cleaned up.");
    }
    catch (Exception ex)
    {
        OnStatusUpdated?.Invoke($"Disconnect failed: {ex.Message}");
        Debug.LogError($"Exception in Disconnect: {ex.Message}");
    }
#else
        OnStatusUpdated?.Invoke("Disconnect: BLE not supported on this platform.");
#endif
    }

#if ENABLE_WINMD_SUPPORT
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
            //Debug.Log($"No manufacturer data for device: {args.BluetoothAddress:X}");
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

                // Notify subscribers that a device has been scanned
                OnDeviceScanned?.Invoke(args.BluetoothAddress, deviceName);
                return; // Break loop once the desired device is found
            }
        }

        Debug.Log($"Device {args.BluetoothAddress:X} does not match the required manufacturer data.");
    }

    private void OnAdvertisementStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
    {
        if (isScanning)
        {
            OnStatusUpdated?.Invoke("Watcher stopped unexpectedly.");
        }
        isScanning = false;
    }
#endif

    private async Task<bool> CheckForTargetServiceAndCharacteristics()
    {
#if ENABLE_WINMD_SUPPORT
        GattDeviceServicesResult servicesResult = await connectedDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
        if (servicesResult.Status == GattCommunicationStatus.Success)
        {
            foreach (var service in servicesResult.Services)
            {
                if (service.Uuid.ToString() == targetServiceUUID)
                {
                    GattCharacteristicsResult charResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    if (charResult.Status == GattCommunicationStatus.Success)
                    {
                        foreach (var characteristic in charResult.Characteristics)
                        {
                            if (characteristic.Uuid.ToString() == targetStepCharUUID ||
                                characteristic.Uuid.ToString() == targetTurnCharUUID ||
                                characteristic.Uuid.ToString() == targetMapCharUUID ||
                                characteristic.Uuid.ToString() == targetPauseCharUUID ||
                                characteristic.Uuid.ToString() == targetScreenshotCharUUID ||
                                characteristic.Uuid.ToString() == targetToggleSensorCharUUID)
                            {
                                _ = SubscribeToNotifications(characteristic);
                            }
                        }
                        _ = UpdateSensorStateOnBLE("start");

                        return true;
                    }
                }
            }
        }
        return false;
#else
        return false;
#endif
    }

#if ENABLE_WINMD_SUPPORT
    private async Task SubscribeToNotifications(GattCharacteristic characteristic)
    {
        GattCommunicationStatus notifyStatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
            GattClientCharacteristicConfigurationDescriptorValue.Notify);

        if (notifyStatus == GattCommunicationStatus.Success)
        {
            TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> handler = HandleCharacteristicValueChanged;
            characteristic.ValueChanged += handler;
            subscribedCharacteristics.Add((characteristic, handler));
            Debug.Log($"Subscribed to notifications for characteristic: {characteristic.Uuid}");
        }
        else
        {
            Debug.LogError($"Failed to subscribe to notifications for characteristic: {characteristic.Uuid}");
        }
    }

    private void HandleCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        var reader = Windows.Storage.Streams.DataReader.FromBuffer(args.CharacteristicValue);
        byte[] input = new byte[reader.UnconsumedBufferLength];
        reader.ReadBytes(input);
        string receivedData = System.Text.Encoding.UTF8.GetString(input);
        Debug.Log($"Characteristic {sender.Uuid} value changed: {receivedData}");

        switch (sender.Uuid.ToString())
        {
            case string stepUuid when stepUuid == targetStepCharUUID:
                Debug.Log($"Received step data: {receivedData}");
                if (GameManager.Instance.CurrentState != GameManager.GameState.InGame)
                {
                    Debug.Log("BLEConnect: Ignoring step data since not InGame state");
                    return;
                }
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    OnGameStepDataUpdated?.Invoke(receivedData);
                }, false);
                break;

            case string turnUuid when turnUuid == targetTurnCharUUID:
                Debug.Log($"Received turn data: {receivedData}");
                if (GameManager.Instance.CurrentState != GameManager.GameState.InGame)
                {
                    Debug.Log("BLEConnect: Ignoring turn state data since not InGame state");
                    return;
                }
                if (int.TryParse(receivedData, out int turnState))
                {
                    OnTurnStateUpdated?.Invoke(turnState);
                }
                break;

            case string mapUuid when mapUuid == targetMapCharUUID:
                Debug.Log($"Received map coordinate data: {receivedData}");
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    OnMapCoordinateReceived?.Invoke(receivedData);
                }, false);
                break;

            case string pauseUuid when pauseUuid == targetPauseCharUUID:
                if (GameManager.Instance.CurrentState != GameManager.GameState.InGame)
                {
                    Debug.Log("BLEConnect: Ignoring pause request since not InGame state");
                    return;
                }
                if (receivedData == "pause")
                {
                    Debug.Log($"Received pause command: {receivedData}");
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        OnPauseRequested?.Invoke();
                    }, false);
                }
                break;

            case string screenshotUuid when screenshotUuid == targetScreenshotCharUUID:
                if (GameManager.Instance.CurrentState != GameManager.GameState.InGame)
                {
                    Debug.Log("BLEConnect: Ignoring screenshot request since not InGame state");
                    return;
                }
                if (receivedData == "take")
                {
                    Debug.Log("Screenshot requested via BLE");
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        OnScreenshotRequested?.Invoke();
                    }, false);
                }
                break;
        }
    }
#endif

    public async Task UpdateSensorStateOnBLE(string state)
    {
#if ENABLE_WINMD_SUPPORT
        if (connectedDevice == null)
        {
            Debug.Log("No device connected. Cannot update sensor state on BLE.");
            return;
        }

        byte[] stateBytes = System.Text.Encoding.UTF8.GetBytes(state);

        var service = connectedDevice.GetGattService(Guid.Parse(targetServiceUUID));
        if (service == null)
        {
            Debug.Log("BLE Service not found.");
            return;
        }

        try
        {
            var characteristicsResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);

            if (characteristicsResult.Status == GattCommunicationStatus.Success)
            {
                foreach (var characteristic in characteristicsResult.Characteristics)
                {
                    if (characteristic.Uuid == Guid.Parse(targetToggleSensorCharUUID))
                    {
                        var writer = new Windows.Storage.Streams.DataWriter();
                        writer.WriteBytes(stateBytes);

                        var writeStatus = await characteristic.WriteValueAsync(writer.DetachBuffer());

                        if (writeStatus == GattCommunicationStatus.Success)
                        {
                            Debug.Log($"Successfully updated sensor state: {state}");
                        }
                        else
                        {
                            Debug.LogError("Failed to update sensor state on BLE.");
                        }
                        return; // Stop checking other characteristics
                    }
                }
                Debug.LogError("Target characteristic not found.");
            }
            else
            {
                Debug.LogError("Failed to retrieve characteristics from service.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception in UpdateSensorStateOnBLE: {ex.Message}");
        }
#endif
    }
}