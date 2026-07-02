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

    private const string DIRECTORY_KEY = "ScreenshotDirectory"; 
    private const string FOLDER_TOKEN_KEY = "ScreenshotFolderToken"; 
    private string defaultDirectory; 
    private string screenshotFolder = "Screenshots";
    private string baseScreenshotFolder = "Screenshots";
    private bool isSessionFolderActive = false; 
    private string activeSessionFolderPath = ""; // Tracks the current active subfolder path across scenes

#if ENABLE_WINMD_SUPPORT
    private StorageFolder screenshotStorageFolder; 
    private StorageFolder baseStorageFolder;
    private StorageFolder activeSessionStorageFolder; // Tracks the current UWP subfolder folder object
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); 
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
#if ENABLE_WINMD_SUPPORT
        InitializeScreenshotFolder(); 
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
                        }, false);
                    }
                    else
                    {
                        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                        {
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
        }
#endif
    }

    public void SetScreenshotFolder(string newFolder, object folderObj)
    {
        screenshotFolder = newFolder;
        baseScreenshotFolder = newFolder;

#if ENABLE_WINMD_SUPPORT
        screenshotStorageFolder = folderObj as StorageFolder;
        baseStorageFolder = folderObj as StorageFolder;
#endif
    }

    public string GetScreenshotFolder()
    {
        return screenshotFolder;
    }

    /// <summary>
    /// Explicitly clears session states so a fresh subfolder can be created next loop.
    /// </summary>
    public void ResetSessionTracking()
    {
        isSessionFolderActive = false;
        activeSessionFolderPath = "";
        screenshotFolder = baseScreenshotFolder;
#if ENABLE_WINMD_SUPPORT
        activeSessionStorageFolder = null;
        screenshotStorageFolder = baseStorageFolder;
#endif
    }

    /// <summary>
    /// Creates a single subfolder once per continuous gameplay sequence loop.
    /// </summary>
    public void CreateSessionSubfolder()
    {
        // If a continuous session is active or a folder has already been initialized, preserve it.
        bool isContinuing = (GameManager.Instance != null && GameManager.Instance.IsContinuingSession);
        if ((isSessionFolderActive || isContinuing) && !string.IsNullOrEmpty(activeSessionFolderPath))
        {
            screenshotFolder = activeSessionFolderPath;
#if ENABLE_WINMD_SUPPORT
            if (activeSessionStorageFolder != null)
            {
                screenshotStorageFolder = activeSessionStorageFolder;
            }
#endif
            return;
        }

        string subfolderName = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        isSessionFolderActive = true;

#if ENABLE_WINMD_SUPPORT
        if (baseStorageFolder != null)
        {
            UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
            {
                try
                {
                    StorageFolder subFolder = await baseStorageFolder.CreateFolderAsync(subfolderName, CreationCollisionOption.OpenIfExists);
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        activeSessionStorageFolder = subFolder;
                        screenshotStorageFolder = subFolder;
                        
                        activeSessionFolderPath = subFolder.Path;
                        screenshotFolder = subFolder.Path;
                    }, false);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create UWP subfolder: {ex.Message}");
                }
            }, false);
        }
        else
        {
            CreateFallbackSubfolder(subfolderName);
        }
#else
        CreateFallbackSubfolder(subfolderName);
#endif
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        bool isContinuing = (GameManager.Instance != null && GameManager.Instance.IsContinuingSession);

        if (sceneName == "DefaultLevel")
        {
            CreateSessionSubfolder();
        }
        else if (sceneName == "CustomLevel" || sceneName == "RandomLevel")
        {
            // If continuing a sequence session, preserve the existing subfolder paths explicitly
            if (isContinuing && !string.IsNullOrEmpty(activeSessionFolderPath))
            {
                screenshotFolder = activeSessionFolderPath;
                isSessionFolderActive = true;
#if ENABLE_WINMD_SUPPORT
                if (activeSessionStorageFolder != null)
                {
                    screenshotStorageFolder = activeSessionStorageFolder;
                }
#endif
            }
            else
            {
                // Fresh run entry from a menu selection
                CreateSessionSubfolder();
            }
        }
        else if (sceneName != "ContinueChoiceScene") // Ignore UI overlay transitions
        {
            // If the user fully exits to the main menu systems and is not continuing, clear tracking
            if (!isContinuing)
            {
                ResetSessionTracking();
            }
        }
    }

    private void CreateFallbackSubfolder(string subfolderName)
    {
        string targetPath = Path.Combine(baseScreenshotFolder, subfolderName);
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }
        activeSessionFolderPath = targetPath;
        screenshotFolder = targetPath;
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
        string fileTimestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string exifTimestamp = DateTime.UtcNow.ToString("yyyy:MM:dd HH:mm:ss");

        string charFilename = $"FitMaze_Character_{fileTimestamp}.jpg";
        string realFilename = $"FitMaze_RealLife_{fileTimestamp}.jpg";

        GPXMovementTracker tracker = FindAnyObjectByType<GPXMovementTracker>();
        
        double charLat = tracker != null ? tracker.GetCurrentLatitude() : 0.0;
        double charLon = tracker != null ? tracker.GetCurrentLongitude() : 0.0;
        
        double realLat = tracker != null ? tracker.GetRealLifeLatitude() : 0.0;
        double realLon = tracker != null ? tracker.GetRealLifeLongitude() : 0.0;

        byte[] jpgBytes = screenshot.EncodeToJPG();

#if ENABLE_WINMD_SUPPORT
        string tempPathChar = Path.Combine(Application.persistentDataPath, "temp_char.jpg"); 
        string tempPathReal = Path.Combine(Application.persistentDataPath, "temp_real.jpg"); 

        if (screenshotStorageFolder == null)
        {
            string fallbackPath = screenshotFolder;
            if (!Directory.Exists(fallbackPath)) Directory.CreateDirectory(fallbackPath);
            
            string filePathChar = Path.Combine(fallbackPath, charFilename);
            File.WriteAllBytes(filePathChar, jpgBytes);
            AddExifData(filePathChar, exifTimestamp, charLat, charLon);

            string filePathReal = Path.Combine(fallbackPath, realFilename);
            File.WriteAllBytes(filePathReal, jpgBytes);
            AddExifData(filePathReal, exifTimestamp, realLat, realLon);
        }
        else
        {
            UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
            {
                try
                {
                    StorageFile fileChar = await screenshotStorageFolder.CreateFileAsync(charFilename, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteBytesAsync(fileChar, jpgBytes);
                    File.WriteAllBytes(tempPathChar, jpgBytes);
                    try
                    {
                        AddExifData(tempPathChar, exifTimestamp, charLat, charLon);
                        byte[] updatedBytesChar = File.ReadAllBytes(tempPathChar);
                        await FileIO.WriteBytesAsync(fileChar, updatedBytesChar);
                    }
                    finally
                    {
                        if (File.Exists(tempPathChar)) File.Delete(tempPathChar);
                    }

                    StorageFile fileReal = await screenshotStorageFolder.CreateFileAsync(realFilename, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteBytesAsync(fileReal, jpgBytes);
                    File.WriteAllBytes(tempPathReal, jpgBytes);
                    try
                    {
                        AddExifData(tempPathReal, exifTimestamp, realLat, realLon);
                        byte[] updatedBytesReal = File.ReadAllBytes(tempPathReal);
                        await FileIO.WriteBytesAsync(fileReal, updatedBytesReal);
                    }
                    finally
                    {
                        if (File.Exists(tempPathReal)) File.Delete(tempPathReal);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to save screenshots in UWP: {ex.Message}");
                }
            }, false);
        }
#else
        if (!Directory.Exists(screenshotFolder)) Directory.CreateDirectory(screenshotFolder);
        
        string filePathChar = Path.Combine(screenshotFolder, charFilename);
        File.WriteAllBytes(filePathChar, jpgBytes);
        AddExifData(filePathChar, exifTimestamp, charLat, charLon);

        string filePathReal = Path.Combine(screenshotFolder, realFilename);
        File.WriteAllBytes(filePathReal, jpgBytes);
        AddExifData(filePathReal, exifTimestamp, realLat, realLon);
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
            
            GeoCoordinate latCoord = GeoCoordinate.FromDecimal((decimal)Math.Abs(latitude), true);
            GeoCoordinate lonCoord = GeoCoordinate.FromDecimal((decimal)Math.Abs(longitude), false);
            
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
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.InGame)
            return false;

        LevelManager levelManager = FindAnyObjectByType<LevelManager>();
        if (levelManager == null || levelManager.CurrentLevelState != LevelManager.LevelState.Playing)
            return false;

        return true;
    }
}