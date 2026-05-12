using UnityEngine;
using System.Collections.Generic;
using System;

public static class GPXCoordinate
{
    public static double InitialLatitude { get; private set; } = 2.923122;
    public static double InitialLongitude { get; private set; } = 101.641938;

    [Serializable]
    public struct Coordinate
    {
        public double latitude;
        public double longitude;
        public string name;

        public Coordinate(double latitude, double longitude, string name)
        {
            this.latitude = latitude;
            this.longitude = longitude;
            this.name = name;
        }
    }

    private static List<Coordinate> savedCoordinates = new List<Coordinate>();
    private const string COORDINATES_KEY = "SavedCoordinates";
    public static event Action OnCoordinateSaved;

    public enum TrackingMode
    {
        CharacterTracking,
        RealLifeTracking
    }
    public static TrackingMode CurrentTrackingMode { get; private set; } = TrackingMode.CharacterTracking;
    public static float StepLength { get; private set; } = 0.7f; // Default step length in meters
    public static event Action OnSettingsChanged;
    private const string TRACKING_MODE_KEY = "TrackingMode";
    private const string STEP_LENGTH_KEY = "StepLength";

    static GPXCoordinate()
    {
        LoadSavedCoordinates();
        CurrentTrackingMode = (TrackingMode)PlayerPrefs.GetInt(TRACKING_MODE_KEY, (int)TrackingMode.CharacterTracking);
        StepLength = PlayerPrefs.GetFloat(STEP_LENGTH_KEY, 70f) / 100f; // Default to 70 (0.70m)
        StepLength = Mathf.Clamp(StepLength, 0.65f, 0.80f);
        Debug.Log($"GPXCoordinate: Loaded tracking mode: {CurrentTrackingMode}, step length: {StepLength}");
    }

    public static void UpdateInitialCoordinates(double latitude, double longitude) // Remove
    {
        InitialLatitude = latitude;
        InitialLongitude = longitude;
        Debug.Log($"GPXCoordinate: Updated initial coordinates -> Lat: {InitialLatitude}, Lon: {InitialLongitude}");
    }

    public static void SaveCoordinate(double latitude, double longitude, string name = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            name = $"Coord{savedCoordinates.Count + 1}";
        }

        if (!savedCoordinates.Exists(c => c.latitude == latitude && c.longitude == longitude))
        {
            savedCoordinates.Add(new Coordinate(latitude, longitude, name));
            SaveToPlayerPrefs();
            Debug.Log($"GPXCoordinate: Saved coordinate -> Name: {name}, Lat: {latitude}, Lon: {longitude}");
            OnCoordinateSaved?.Invoke();
        }
        else
        {
            Debug.Log($"GPXCoordinate: Coordinate Lat: {latitude}, Lon: {longitude} already exists.");
        }
    }

    public static List<Coordinate> GetSavedCoordinates()
    {
        return new List<Coordinate>(savedCoordinates);
    }

    public static void SetInitialFromSaved(int index)
    {
        if (index >= 0 && index < savedCoordinates.Count)
        {
            var coord = savedCoordinates[index];
            InitialLatitude = coord.latitude;
            InitialLongitude = coord.longitude;
            Debug.Log($"GPXCoordinate: Set initial coordinates from saved -> Name: {coord.name}, Lat: {InitialLatitude}, Lon: {InitialLongitude}");
        }

        else
        {
            Debug.LogError($"GPXCoordinate: Invalid index {index} for SetInitialFromSaved. Falling back to default coordinate (index 0).");
            var defaultCoord = savedCoordinates[0];
            InitialLatitude = defaultCoord.latitude;
            InitialLongitude = defaultCoord.longitude;
        }
    }

    public static void UpdateCoordinateName(int index, string newName)
    {
        if (index >= 0 && index < savedCoordinates.Count)
        {
            var coord = savedCoordinates[index];
            savedCoordinates[index] = new Coordinate(coord.latitude, coord.longitude, newName);
            SaveToPlayerPrefs();
            Debug.Log($"GPXCoordinate: Updated name at index {index} to {newName}");
            OnCoordinateSaved?.Invoke();
        }

        else
        {
            Debug.LogError($"GPXCoordinate: Invalid index {index} for operation.");
        }
    }

    public static void DeleteCoordinate(int index)
    {
        if (index >= 0 && index < savedCoordinates.Count)
        {
            savedCoordinates.RemoveAt(index);
            SaveToPlayerPrefs();
            Debug.Log($"GPXCoordinate: Deleted coordinate at index {index}");
            OnCoordinateSaved?.Invoke();
        }

        else
        {
            Debug.LogError($"GPXCoordinate: Invalid index {index} for operation.");
        }
    }

    private static void LoadSavedCoordinates()
    {
        if (PlayerPrefs.HasKey(COORDINATES_KEY))
        {
            string json = PlayerPrefs.GetString(COORDINATES_KEY);
            var coords = JsonUtility.FromJson<SerializableCoordinates>(json);
            if (coords != null && coords.coordinates != null)
            {
                savedCoordinates = new List<Coordinate>(coords.coordinates);
                Debug.Log($"GPXCoordinate: Loaded {savedCoordinates.Count} saved coordinates.");
            }
        }

        if (savedCoordinates.Count == 0)
        {
            savedCoordinates.Add(new Coordinate(InitialLatitude, InitialLongitude, "Default"));
            SaveToPlayerPrefs();
        }
    }

    private static void SaveToPlayerPrefs()
    {
        var serializable = new SerializableCoordinates { coordinates = savedCoordinates.ToArray() };
        string json = JsonUtility.ToJson(serializable);
        PlayerPrefs.SetString(COORDINATES_KEY, json);
        PlayerPrefs.Save();
        Debug.Log("GPXCoordinate: Saved coordinates to PlayerPrefs: " + json);
    }

    [Serializable]
    private class SerializableCoordinates
    {
        public Coordinate[] coordinates;
    }

    public static void SetTrackingMode(TrackingMode mode)
    {
        CurrentTrackingMode = mode;
        PlayerPrefs.SetInt(TRACKING_MODE_KEY, (int)mode);
        PlayerPrefs.Save();
        Debug.Log($"GPXCoordinate: Set tracking mode to {mode}");
        OnSettingsChanged?.Invoke();
    }

    public static void SetStepLength(float sliderValue)
    {
        StepLength = Mathf.Clamp(sliderValue / 100f, 0.65f, 0.80f);
        PlayerPrefs.SetFloat(STEP_LENGTH_KEY, sliderValue); // Store slider value (65-80)
        PlayerPrefs.Save();
        Debug.Log($"GPXCoordinate: Set step length to {StepLength}");
        OnSettingsChanged?.Invoke();
    }
}