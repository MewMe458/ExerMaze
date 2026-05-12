using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    public float noStepTimeout = 0.6f; // Time in seconds to stop the character if no data is received
    public float turnSpeed = 40f; // Speed of turning
    private float baseSpeed = 0f; // speed from BLE
    private float speedMultiplier = 1f;
    public float CurrentSpeed => baseSpeed * speedMultiplier;
    private float lastStepUpdateTime = 0f; // Time when the last step data was received
    private int turnState = 0; // -1 = left, 0 = no turn, 1 = right

    private LevelManager levelManager; // Reference to LevelManager for state checks
    private CharacterController characterController; // Reference to CharacterController

    void Start()
    {
        // Find the LevelManager in the scene
        levelManager = FindAnyObjectByType<LevelManager>();
        if (levelManager == null)
        {
            Debug.LogError("LevelManager not found in the scene.");
        }

        // Get the CharacterController component
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("CharacterController component missing from the player.");
        }

        // Subscribe to events from BLEDataHandler
        if (BLEManager.Instance != null && BLEManager.Instance.bleDataHandler != null)
        {
            Debug.Log("Character subscribed to data handler");
            BLEManager.Instance.bleDataHandler.OnStepReceived += CharacterPlaySound;
            BLEManager.Instance.bleDataHandler.OnSpeedUpdated += CharacterUpdateSpeed;
            BLEManager.Instance.bleDataHandler.OnTurnStateUpdated += CharacterUpdateTurn;
        }
        else
        {
            Debug.LogError("BLEDataHandler instance is null. Ensure it is initialized.");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to avoid memory leaks
        if (BLEManager.Instance != null && BLEManager.Instance.bleDataHandler != null)
        {
            BLEManager.Instance.bleDataHandler.OnStepReceived -= CharacterPlaySound;
            BLEManager.Instance.bleDataHandler.OnSpeedUpdated -= CharacterUpdateSpeed;
            BLEManager.Instance.bleDataHandler.OnTurnStateUpdated -= CharacterUpdateTurn;
        }
    }

    void Update()
    {
        float currentTime = Time.time;

        // Stop when theres no step event anymore
        if (currentTime - lastStepUpdateTime > noStepTimeout)
        {
            baseSpeed = 0;
        }

        // Apply movement based on calculated speed
        if (characterController != null)
        {
            Vector3 forwardMovement = transform.forward * CurrentSpeed * Time.deltaTime;
            forwardMovement.y = 0; // Prevent vertical movement
            characterController.Move(forwardMovement);
        }

        // Handle turning
        if (turnState != 0) // Only rotate if there's a turn
        {
            float turnDirection = turnState * (turnSpeed * speedMultiplier) * Time.deltaTime;
            transform.Rotate(0, turnDirection, 0); // Rotate around the y-axis
        }
    }
    
    private void CharacterPlaySound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayFootstep();
        }
    }

    private void CharacterUpdateSpeed(float speed)
    {
        baseSpeed = speed;
        lastStepUpdateTime = Time.time; // Update the time when step data is received
        //Debug.Log($"Speed updated: {currentSpeed}");
    }

    private void CharacterUpdateTurn(int newTurnState)
    {
        //if (levelManager.CurrentLevelState == LevelManager.LevelState.Interacting) {
        //    turnState = 0;
        //    return;
        //}
        turnState = newTurnState; // Update the turn state
        //Debug.Log($"Turn state updated: {turnState}");
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    public void ResetSpeedMultiplier()
    {
        speedMultiplier = 1f;
    }
}