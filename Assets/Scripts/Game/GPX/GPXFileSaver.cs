#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Pickers;
using System;
#endif
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GPXFileSaver : MonoBehaviour
{
    private GPXMovementTracker tracker;
    private bool isSaving = false; // Flag to prevent multiple concurrent save prompts

    void Start()
    {
        FindTracker();
    }

    private void FindTracker()
    {
        tracker = FindAnyObjectByType<GPXMovementTracker>();

        if (tracker == null)
        {
            Debug.LogError("GPXFileSaver: No GPXMovementTracker found in the scene.");
        }
    }

    private string GenerateGPX(List<(double latitude, double longitude, float elevation, string timestamp)> points, string trackName)
    {
        StringBuilder gpx = new StringBuilder();
        gpx.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        gpx.AppendLine("<gpx version=\"1.1\" creator=\"UnityGPXTracker\" xmlns=\"http://www.topografix.com/GPX/1/1\">");
        gpx.AppendLine("  <trk>");
        gpx.AppendLine($"    <name>{trackName}</name>");
        gpx.AppendLine("    <trkseg>");

        foreach (var point in points)
        {
            gpx.AppendLine($"      <trkpt lat=\"{point.latitude}\" lon=\"{point.longitude}\">");
            gpx.AppendLine($"        <ele>{point.elevation}</ele>");
            gpx.AppendLine($"        <time>{point.timestamp}</time>");
            gpx.AppendLine("      </trkpt>");
        }

        gpx.AppendLine("    </trkseg>");
        gpx.AppendLine("  </trk>");
        gpx.AppendLine("</gpx>");

        return gpx.ToString();
    }

    public void SaveGPXFileUWP()
    {
    #if ENABLE_WINMD_SUPPORT
        // Guard clause to prevent double-triggering if the player rapid-clicks
        if (isSaving)
        {
            Debug.LogWarning("Save process already in progress.");
            return;
        }

        if (tracker == null)
        {
            tracker = FindAnyObjectByType<GPXMovementTracker>();
            if (tracker == null)
            {
                Debug.LogError("GPXFileSaver: No GPXMovementTracker found.");
                return;
            }
        }

        // Check if GameManager has valid data before opening the picker
        if (GameManager.Instance == null)
        {
            Debug.LogError("GPXFileSaver: GameManager instance is missing.");
            return;
        }

        var charPoints = GameManager.Instance.SessionCharacterPoints;
        var realPoints = GameManager.Instance.SessionRealLifePoints;

        string characterGpx = GenerateGPX(charPoints, "Character Movement Session");
        string realLifeGpx = GenerateGPX(realPoints, "Real-Life Movement Session");

        isSaving = true;

        UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
        {
            try
            {
                // FolderPicker prompts the user ONCE for a directory
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                folderPicker.FileTypeFilter.Add("*"); 

                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                if (folder != null)
                {
                    string timestamp = System.DateTime.Now.ToString("ddMMyy_HHmmss");
                    string baseFileName = $"fitmaze_{timestamp}";

                    // Both files are generated seamlessly in the selected folder
                    StorageFile characterFile = await folder.CreateFileAsync(
                        baseFileName + "_character.gpx",
                        CreationCollisionOption.ReplaceExisting);

                    StorageFile realLifeFile = await folder.CreateFileAsync(
                        baseFileName + "_reallife.gpx",
                        CreationCollisionOption.ReplaceExisting);

                    await FileIO.WriteTextAsync(characterFile, characterGpx);
                    await FileIO.WriteTextAsync(realLifeFile, realLifeGpx);

                    Debug.Log($"Successfully saved both files to: {folder.Path}");
                }
                else
                {
                    Debug.LogWarning("Folder selection was canceled.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred during save: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // Ensure state is reset and UI thread unlocks the cursor back in Unity
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    isSaving = false;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }, false);
            }
        }, false);
    #else
        Debug.LogError("This file save method only works on UWP.");
    #endif
    }
}