using System.Collections.Generic;
using UnityEngine;

public static class GPXDataPersistence
{
    // Flag to check if we are continuing a session
    public static bool IsContinuingSession = false;

    // Data from GPXMovementTracker
    public static List<(double lat, double lon, float ele, string time)> SavedCharacterPoints = new();
    public static List<(double lat, double lon, float ele, string time)> SavedRealLifePoints = new();
    public static double LastCharLat;
    public static double LastCharLon;
    public static double LastRealLat;
    public static double LastRealLon;
    
    // Data from LevelManager
    public static int TotalStepsAccumulated = 0;
    public static int TotalScoreAccumulated = 0;
    public static float TotalTimeAccumulated = 0f;

    public static void Clear()
    {
        IsContinuingSession = false;
        SavedCharacterPoints.Clear();
        SavedRealLifePoints.Clear();
        TotalStepsAccumulated = 0;
        TotalScoreAccumulated = 0;
        TotalTimeAccumulated = 0f;
    }
}