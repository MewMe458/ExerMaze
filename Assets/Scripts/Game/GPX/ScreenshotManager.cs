#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using CompactExifLib;

public class ScreenshotManager : MonoBehaviour
{
    public static ScreenshotManager Instance { get; private set; }

    private const string DIRECTORY_KEY = "ScreenshotDirectory"; // Match SettingsManager
    private const string FOLDER_TOKEN_KEY = "ScreenshotFolderToken"; // Match SettingsManager
    private string defaultDirectory; // Default folder path
    private string screenshotFolder = "Screenshots";
#if ENABLE_WINMD_SUPPORT
    private StorageFolder screenshotStorageFolder; // Store the StorageFolder for UWP
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }

    private void Start()
    {
#if ENABLE_WINMD_SUPPORT
        InitializeScreenshotFolder(); // Initialize screenshot folder
#endif
    }

    public void InitializeScreenshotFolder()
    {
#if ENABLE_WINMD_SUPPORT
        defaultDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FitMazeScreenshots");
        if (!Directory.Exists(defaultDirectory))
        {
            Directory.CreateDirectory(defaultDirectory);
        }
        string savedDirectory = PlayerPrefs.GetString(DIRECTORY_KEY, defaultDirectory);
        string token = PlayerPrefs.GetString(FOLDER_TOKEN_KEY, "");
        if (!string.IsNullOrEmpty(token))
        {
            UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
            {
                try
                {
                    StorageFolder folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
                    if (folder != null)
                    {
                        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                        {
                            SetScreenshotFolder(savedDirectory, folder);
                            Debug.Log($"Initialized screenshot folder from PlayerPrefs: {savedDirectory}");
                        }, false);
                    }
                    else
                    {
                        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                        {
                            Debug.LogWarning("Invalid folder token. Using default directory.");
                            SetScreenshotFolder(defaultDirectory, null);
                            PlayerPrefs.SetString(DIRECTORY_KEY, defaultDirectory);
                            PlayerPrefs.DeleteKey(FOLDER_TOKEN_KEY);
                            PlayerPrefs.Save();
                        }, false);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        Debug.LogError($"Failed to retrieve folder: {ex.Message}");
                        SetScreenshotFolder(defaultDirectory, null);
                        PlayerPrefs.SetString(DIRECTORY_KEY, defaultDirectory);
                        PlayerPrefs.DeleteKey(FOLDER_TOKEN_KEY);
                        PlayerPrefs.Save();
                    }, false);
                }
            }, true);
        }
        else
        {
            SetScreenshotFolder(savedDirectory, null);
            Debug.Log($"Initialized screenshot folder from PlayerPrefs (no token): {savedDirectory}");
        }
#endif
    }

#if ENABLE_WINMD_SUPPORT
    public void SetScreenshotFolder(string newFolder, StorageFolder folder)
    {
        screenshotFolder = newFolder;
        screenshotStorageFolder = folder;
    }
#endif

    public string GetScreenshotFolder()
    {
        return screenshotFolder;
    }

    public System.Collections.IEnumerator TakeScreenshotWithExif()
    {
        if (!CanTakeScreenshot())
        {
            Debug.Log("Cannot take screenshot: Game must be in active gameplay.");
            yield break;
        }

        yield return new WaitForEndOfFrame();

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmssZ");
        string filename = $"FitMaze_{timestamp}.jpg";

        GPXMovementTracker tracker = FindAnyObjectByType<GPXMovementTracker>();
        double latitude = tracker != null ? tracker.GetCurrentLatitude() : 0.0;
        double longitude = tracker != null ? tracker.GetCurrentLongitude() : 0.0;
        byte[] jpgBytes = screenshot.EncodeToJPG();

#if ENABLE_WINMD_SUPPORT
    string tempPath = Path.Combine(Application.persistentDataPath, "temp.jpg"); // Compute on main thread

    if (screenshotStorageFolder == null)
    {
        Debug.LogWarning("StorageFolder is null. Using screenshotFolder path.");
        string fallbackPath = screenshotFolder;
        if (!Directory.Exists(fallbackPath)) Directory.CreateDirectory(fallbackPath);
        string filePath = Path.Combine(fallbackPath, filename);
        File.WriteAllBytes(filePath, jpgBytes);
        AddExifData(filePath, timestamp, latitude, longitude);
        Debug.Log($"Screenshot saved (fallback): {filePath}");
    }
    else
    {
        UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
        {
            try
            {
                StorageFile file = await screenshotStorageFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBytesAsync(file, jpgBytes);
                File.WriteAllBytes(tempPath, jpgBytes);

                try
                {
                    AddExifData(tempPath, timestamp, latitude, longitude);
                    byte[] updatedBytes = File.ReadAllBytes(tempPath);
                    await FileIO.WriteBytesAsync(file, updatedBytes);
                    Debug.Log($"Screenshot saved: {file.Path}");
                }
                finally
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save screenshot: {ex.Message}");
            }
        }, false);
    }
#endif

        Destroy(screenshot);
    }

    private void AddExifData(string path, string timestamp, double latitude, double longitude)
    {
        try
        {
            ExifData exif = new ExifData(path);
            exif.SetTagValue(ExifTag.DateTimeOriginal, timestamp, StrCoding.Utf8);
            exif.SetTagValue(ExifTag.GpsLatitudeRef, latitude >= 0 ? "N" : "S", StrCoding.UsAscii);
            exif.SetTagValue(ExifTag.GpsLongitudeRef, longitude >= 0 ? "E" : "W", StrCoding.UsAscii);
            GeoCoordinate latCoord = GeoCoordinate.FromDecimal((decimal)latitude, true);
            GeoCoordinate lonCoord = GeoCoordinate.FromDecimal((decimal)longitude, false);
            exif.SetGpsLatitude(latCoord);
            exif.SetGpsLongitude(lonCoord);
            exif.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to add EXIF data: {e.Message}");
        }
    }

    private bool CanTakeScreenshot()
    {
        // Check GameManager state
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.InGame)
            return false;

        // Check LevelManager state
        LevelManager levelManager = FindAnyObjectByType<LevelManager>();
        if (levelManager == null || levelManager.CurrentLevelState != LevelManager.LevelState.Playing)
            return false;

        return true;
    }

    internal void SetScreenshotFolder(string currentPath, object value)
    {
        throw new NotImplementedException();
    }

}