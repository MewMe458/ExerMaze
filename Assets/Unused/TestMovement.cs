using UnityEngine;

public class TestMovement : MonoBehaviour
{
    public float stepLength = 0.7f; // Average step length in meters
    private float currentSpeed = 0f; // Calculated speed

    private float decelerationRate = 2f; // Rate at which the character slows down
    private float lastStepUpdateTime = 0f; // Time when the last step data was received
    private float noStepTimeout = 0.6f; // Time in seconds to stop the character if no data is received

    private CharacterController characterController; // Reference to CharacterController

    public float turnSpeed = 40f; // Speed of turning
    private int turnState = 0; // -1 = left, 0 = no turn, 1 = right

    void Start()
    {
        // Get the CharacterController component
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("CharacterController component missing from the player.");
        }

        // Subscribe to the events
        if (BluetoothLEClient.Instance != null)
        {
            BluetoothLEClient.Instance.OnGameStepDataUpdated += OnGameStepDataReceived;
            BluetoothLEClient.Instance.OnTurnStateUpdated += OnTurnStateReceived;
        }
        else
        {
            Debug.LogError("BluetoothLEClient instance is null. Ensure it is initialized.");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to avoid memory leaks
        if (BluetoothLEClient.Instance != null)
        {
            BluetoothLEClient.Instance.OnGameStepDataUpdated -= OnGameStepDataReceived;
            BluetoothLEClient.Instance.OnTurnStateUpdated -= OnTurnStateReceived;
        }
    }

    void Update()
    {
        float currentTime = Time.time;

        // Decelerate to stop if no step data has been received recently
        if (currentTime - lastStepUpdateTime > noStepTimeout)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, decelerationRate * Time.deltaTime);
        }

        // Apply movement based on calculated speed
        if (characterController != null)
        {
            Vector3 forwardMovement = transform.forward * currentSpeed * Time.deltaTime;
            forwardMovement.y = 0; // Prevent vertical movement
            characterController.Move(forwardMovement);
            Debug.Log($"Charcacter moved at speed {currentSpeed}");
        }

        // Handle turning
        if (turnState != 0) // Only rotate if there's a turn
        {
            float turnDirection = turnState * turnSpeed * Time.deltaTime;
            transform.Rotate(0, turnDirection, 0); // Rotate around the y-axis
        }
    }

    private void OnGameStepDataReceived(string stepData)
    {
        if (!string.IsNullOrEmpty(stepData))
        {
            lastStepUpdateTime = Time.time; // Update the time when step data is received
            ProcessStepData(stepData);
        }
    }

    private void OnTurnStateReceived(int newTurnState)
    {
        turnState = newTurnState; // Update the turn state
        Debug.Log($"Turn state updated: {turnState}");
    }

    private void ProcessStepData(string stepData)
    {
        // Step data format: "steps,time" (e.g., "24,500")
        string[] parts = stepData.Split(',');

        if (parts.Length == 2 && int.TryParse(parts[0], out int steps) && int.TryParse(parts[1], out int timeMs))
        {
            // Calculate step frequency (steps per second)
            float stepFrequency = steps / (timeMs / 1000f); // Convert ms to seconds

            // Calculate speed using step frequency and step length
            currentSpeed = stepFrequency * stepLength;

            Debug.Log($"Steps: {steps}, Time (ms): {timeMs}, Step Frequency: {stepFrequency} Hz, Speed: {currentSpeed} m/s");
        }
        else
        {
            Debug.LogWarning("Invalid GameStepData format.");
        }
    }
}
