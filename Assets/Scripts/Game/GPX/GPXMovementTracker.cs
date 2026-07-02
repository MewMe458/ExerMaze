using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GPXMovementTracker : MonoBehaviour
{
    private double initialLatitude;
    private double initialLongitude;
    private float movementScale = 0.000009f; 
    private List<(double latitude, double longitude, float elevation, string timestamp)> characterTrackPoints 
        = new List<(double, double, float, string)>();

    private List<(double latitude, double longitude, float elevation, string timestamp)> realLifeTrackPoints 
        = new List<(double, double, float, string)>();
    private double characterLatitude;
    private double characterLongitude;

    private double realLifeLatitude;
    private double realLifeLongitude;
    private Vector3 lastPosition; 

    private int stepCount = 0; 

    // Public Getters for both tracking systems
    public double GetCurrentLatitude() => characterLatitude;
    public double GetCurrentLongitude() => characterLongitude;
    public double GetRealLifeLatitude() => realLifeLatitude;
    public double GetRealLifeLongitude() => realLifeLongitude;

    void Start()
    {
        if (BLEManager.Instance != null && BLEManager.Instance.bleDataHandler != null)
        {
            BLEManager.Instance.bleDataHandler.OnStepReceived += HandleStepReceived;
        }

        int lastIndex = PlayerPrefs.GetInt("SelectedCoordinateIndex", 0);
        lastIndex = Mathf.Clamp(lastIndex, 0, GPXCoordinate.GetSavedCoordinates().Count - 1);
        GPXCoordinate.SetInitialFromSaved(lastIndex);
        initialLatitude = GPXCoordinate.InitialLatitude;
        initialLongitude = GPXCoordinate.InitialLongitude;

        ResetTracking(); 
    }

    private void OnDestroy()
    {
        if (BLEManager.Instance != null && BLEManager.Instance.bleDataHandler != null)
        {
            BLEManager.Instance.bleDataHandler.OnStepReceived -= HandleStepReceived;
        }
    }

    public void SaveToPersistence()
    {
        GPXDataPersistence.SavedCharacterPoints = new List<(double, double, float, string)>(characterTrackPoints);
        GPXDataPersistence.SavedRealLifePoints = new List<(double, double, float, string)>(realLifeTrackPoints);
        GPXDataPersistence.LastCharLat = characterLatitude;
        GPXDataPersistence.LastCharLon = characterLongitude;
        GPXDataPersistence.LastRealLat = realLifeLatitude;
        GPXDataPersistence.LastRealLon = realLifeLongitude;
    }

    public void LoadFromPersistence()
    {
        characterTrackPoints = new List<(double, double, float, string)>(GPXDataPersistence.SavedCharacterPoints);
        realLifeTrackPoints = new List<(double, double, float, string)>(GPXDataPersistence.SavedRealLifePoints);
        characterLatitude = GPXDataPersistence.LastCharLat;
        characterLongitude = GPXDataPersistence.LastCharLon;
        realLifeLatitude = GPXDataPersistence.LastRealLat;
        realLifeLongitude = GPXDataPersistence.LastRealLon;
    }

    public void ResetTracking()
    {
        if (!GameManager.Instance.IsContinuingSession)
        {
            characterTrackPoints.Clear();
            realLifeTrackPoints.Clear();
            stepCount = 0;
            GameManager.Instance.ClearSessionData(); 

            initialLatitude = GPXCoordinate.InitialLatitude;
            initialLongitude = GPXCoordinate.InitialLongitude;
            
            characterLatitude = initialLatitude;
            characterLongitude = initialLongitude;

            realLifeLatitude = initialLatitude;
            realLifeLongitude = initialLongitude;
        }
        else
        {
            characterTrackPoints = new List<(double, double, float, string)>(GameManager.Instance.SessionCharacterPoints);
            realLifeTrackPoints = new List<(double, double, float, string)>(GameManager.Instance.SessionRealLifePoints);
            stepCount = GameManager.Instance.AccumulatedSteps;

            if (characterTrackPoints.Count > 0)
            {
                var lastChar = characterTrackPoints[characterTrackPoints.Count - 1];
                characterLatitude = lastChar.latitude;
                characterLongitude = lastChar.longitude;
            }
            if (realLifeTrackPoints.Count > 0)
            {
                var lastReal = realLifeTrackPoints[realLifeTrackPoints.Count - 1];
                realLifeLatitude = lastReal.latitude;
                realLifeLongitude = lastReal.longitude;
            }
        }

        lastPosition = transform.position; 

        AddTrackPoint(characterTrackPoints, characterLatitude, characterLongitude);
        AddTrackPoint(realLifeTrackPoints, realLifeLatitude, realLifeLongitude);
    }

    private void HandleStepReceived()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.InGame &&
            FindAnyObjectByType<LevelManager>().CurrentLevelState == LevelManager.LevelState.Playing)
        {
            TrackCharacterMovement();
            TrackRealLifeMovement();
        }
    }

    private void TrackCharacterMovement()
    {
        Vector3 movement = transform.position - lastPosition;

        double deltaLatitude = movement.z * movementScale;
        double deltaLongitude = movement.x * (movementScale / Math.Cos(characterLatitude * (Math.PI / 180)));

        characterLatitude += deltaLatitude;
        characterLongitude += deltaLongitude;

        lastPosition = transform.position;

        AddTrackPoint(characterTrackPoints, characterLatitude, characterLongitude);
    }

    private void TrackRealLifeMovement()
    {
        float distance = GPXCoordinate.StepLength;
        float distanceInDegrees = distance / 111139f;

        float randomAngle = UnityEngine.Random.Range(0f, 360f);

        double deltaLatitude = distanceInDegrees * Math.Cos(randomAngle * (Math.PI / 180));
        double deltaLongitude = distanceInDegrees * Math.Sin(randomAngle * (Math.PI / 180)) 
                            / Math.Cos(realLifeLatitude * (Math.PI / 180));

        realLifeLatitude += deltaLatitude;
        realLifeLongitude += deltaLongitude;

        AddTrackPoint(realLifeTrackPoints, realLifeLatitude, realLifeLongitude);
    }

    private void AddTrackPoint(List<(double, double, float, string)> list, double lat, double lon)
    {
        string timestamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var point = (lat, lon, 0f, timestamp);
        
        list.Add(point);

        if (list == characterTrackPoints)
        {
            if (!GameManager.Instance.SessionCharacterPoints.Contains(point))
                GameManager.Instance.SessionCharacterPoints.Add(point);
        }
        else
        {
            if (!GameManager.Instance.SessionRealLifePoints.Contains(point))
                GameManager.Instance.SessionRealLifePoints.Add(point);
        }
    }

    public string GenerateCharacterGPXData()
    {
        return GenerateGPX(characterTrackPoints, "Character Movement");
    }

    public string GenerateRealLifeGPXData()
    {
        return GenerateGPX(realLifeTrackPoints, "Real-Life Movement");
    }

    private string GenerateGPX(
        List<(double latitude, double longitude, float elevation, string timestamp)> points,
        string trackName)
    {
        StringBuilder gpxData = new StringBuilder();

        gpxData.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        gpxData.AppendLine("<gpx version=\"1.1\" creator=\"GPXMovementTracker\" xmlns=\"http://www.topografix.com/GPX/1/1\">");
        gpxData.AppendLine("<trk>");
        gpxData.AppendLine($"<name>{trackName}</name>");
        gpxData.AppendLine("<trkseg>");

        foreach (var point in points)
        {
            gpxData.AppendLine($"<trkpt lat=\"{point.latitude}\" lon=\"{point.longitude}\">");
            gpxData.AppendLine($"  <ele>{point.elevation}</ele>");
            gpxData.AppendLine($"  <time>{point.timestamp}</time>");
            gpxData.AppendLine($"</trkpt>");
        }

        gpxData.AppendLine("</trkseg>");
        gpxData.AppendLine("</trk>");
        gpxData.AppendLine("</gpx>");

        return gpxData.ToString();
    }
}