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

    void Start()
    {
        FindTracker();
    }

    private void FindTracker()
    {
        tracker = FindAnyObjectByType<GPXMovementTracker>(); // Find the tracker dynamically

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
        Debug.Log("UWP Folder Selection initiated...");

        if (tracker == null)
        {
            tracker = FindAnyObjectByType<GPXMovementTracker>();
            if (tracker == null)
            {
                Debug.LogError("GPXFileSaver: No GPXMovementTracker found.");
                return;
            }
        }

        var charPoints = GameManager.Instance.SessionCharacterPoints;
        var realPoints = GameManager.Instance.SessionRealLifePoints;

        string characterGpx = GenerateGPX(charPoints, "Character Movement Session");
        string realLifeGpx = GenerateGPX(realPoints, "Real-Life Movement Session");

        UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
        {
            try
            {
                // Use FolderPicker instead of FileSavePicker
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                folderPicker.FileTypeFilter.Add("*"); // Required for FolderPicker

                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                if (folder != null)
                {
                    // Generate a base timestamp for the filenames
                    string timestamp = System.DateTime.Now.ToString("ddMMyy_HHmmss");
                    string baseFileName = $"fitmaze{timestamp}";

                    // Create the two files directly in the selected folder
                    StorageFile characterFile = await folder.CreateFileAsync(
                        baseFileName + "_character.gpx",
                        CreationCollisionOption.ReplaceExisting);

                    StorageFile realLifeFile = await folder.CreateFileAsync(
                        baseFileName + "_reallife.gpx",
                        CreationCollisionOption.ReplaceExisting);

                    // Write the data to the files
                    await FileIO.WriteTextAsync(characterFile, characterGpx);
                    await FileIO.WriteTextAsync(realLifeFile, realLifeGpx);

                    Debug.Log($"Successfully saved files to: {folder.Path}");
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

            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }, false);
        }, false);
    #else
        Debug.LogError("This file save method only works on UWP.");
    #endif
    }
}
