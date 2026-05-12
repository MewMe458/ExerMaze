using UnityEngine;
using System.Collections;
using static LevelManager;

public class SpeedBoostEffect : MonoBehaviour
{ 
//    [SerializeField] private float multiplier = 1.5f; // Speed multiplier
//    [SerializeField] private float duration = 3f; // Duration in seconds
//    private CharacterMovement characterMovement; // Reference to CharacterMovement
//    private LevelManager levelManager; // For state checks
//    private bool isBoostActive; // Prevent stacking boosts

//    private void Start()
//    {
//        // Subscribe to item collection event
//        ItemCollision.OnItemCollected += HandleItemCollected;
//        // Find CharacterMovement (assumes this script is on the player)
//        characterMovement = GetComponent<CharacterMovement>();
//        if (characterMovement == null)
//        {
//            Debug.LogError("SpeedBoostEffect: CharacterMovement not found on GameObject", gameObject);
//        }
//        // Find LevelManager for state checks
//        levelManager = FindAnyObjectByType<LevelManager>();
//        if (levelManager == null)
//        {
//            Debug.LogError("SpeedBoostEffect: LevelManager not found in scene", gameObject);
//        }
//    }

//    private void OnDestroy()
//    {
//        // Unsubscribe to prevent memory leaks
//        ItemCollision.OnItemCollected -= HandleItemCollected;
//    }

//    private void HandleItemCollected(string itemType)
//    {
//        if (itemType == "SpeedBoost" && !isBoostActive && levelManager != null &&
//            levelManager.CurrentLevelState == LevelState.Playing)
//        {
//            StartCoroutine(ApplySpeedBoost());
//        }
//    }

//    private IEnumerator ApplySpeedBoost()
//    {
//        if (characterMovement == null) yield break;

//        isBoostActive = true;
//        characterMovement.BoostMultiplier = multiplier; // Apply boost
//        Debug.Log($"Speed boost applied: Multiplier {multiplier}");

//        float elapsed = 0f;
//        while (elapsed < duration)
//        {
//            if (levelManager.CurrentLevelState != LevelState.Playing)
//            {
//                // Pause boost if game is paused
//                yield return new WaitUntil(() => levelManager.CurrentLevelState == LevelState.Playing);
//            }
//            elapsed += Time.deltaTime;
//            yield return null;
//        }

//        characterMovement.BoostMultiplier = 1f; // Revert to no boost
//        isBoostActive = false;
//        Debug.Log("Speed boost ended");
//    }
}