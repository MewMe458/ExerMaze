using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeTeleporter : MonoBehaviour
{
    [SerializeField] private float yOffset = 0.1f;
    [SerializeField] private bool avoidCenterRoom = true;
    [SerializeField] private GameObject teleportEffect;
    [SerializeField] private AudioSource teleportSound;
    [SerializeField] private float teleportDelay = 0.9f;

    private AutoMG3D_1010 randomMazeManager;
    private LevelLoader customLevelLoader;

    private void Awake()
    {
        // Scan dynamically for whichever level generation system is running in the current scene
        randomMazeManager = FindFirstObjectByType<AutoMG3D_1010>();
        customLevelLoader = FindFirstObjectByType<LevelLoader>();
    }

    private void Start()
    {
        // 🛠️ FIX: If loading into a Custom Level scene, lower this specific prefab's anchor height position by 1
        if (customLevelLoader != null)
        {
            Vector3 pos = transform.position;
            pos.y -= 1.0f;
            transform.position = pos;
            Debug.Log($"MazeTeleporter: Custom Level detected. Lowered prefab visual tracking Y-position to: {transform.position.y}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        CharacterController controller = other.GetComponent<CharacterController>();
        if (controller == null)
            return;

        StartCoroutine(TeleportRoutine(other.transform, controller));
    }

    private IEnumerator TeleportRoutine(Transform playerTransform, CharacterController controller)
    {
        // Stop movement parameters cleanly to avoid clipping layout bugs during teleport frame
        controller.enabled = false;

        // Play visual and audio effects
        if (teleportEffect != null) teleportEffect.SetActive(true);
        if (teleportSound != null) teleportSound.Play();

        // Wait for effect buildup
        yield return new WaitForSeconds(teleportDelay);

        Vector3 targetPos = transform.position; // Fallback to current spot if everything fails

        // Choose destination calculation path depending on the scene type running
        if (randomMazeManager != null)
        {
            // Random Level Scene Mode
            targetPos = randomMazeManager.GetRandomCellWorldPosition(avoidCenterRoom);
        }
        else if (customLevelLoader != null && customLevelLoader.CurrentMazeData != null)
        {
            // Custom Level Scene Mode
            targetPos = GetRandomCustomCellWorldPosition(customLevelLoader.CurrentMazeData);
            
            // 🛠️ FIX: Ensure that the player is teleported to the correct ground height (compensating for the lowered teleporter position)
            targetPos.y = playerTransform.position.y; 
        }
        else
        {
            Debug.LogWarning("MazeTeleporter: No level manager found in the scene to calculate destination.");
        }

        targetPos.y += yOffset;
        playerTransform.position = targetPos;

        // Clear visual effect tracking reference
        if (teleportEffect != null) teleportEffect.SetActive(false);

        // Re-enable player tracking controller loops cleanly
        controller.enabled = true;
    }

    // Safely calculates an open tile grid location from custom saved levels
    private Vector3 GetRandomCustomCellWorldPosition(MazeData mazeData)
    {
        List<Vector2Int> availableCells = new List<Vector2Int>();

        // Collect all tiles that aren't blocked or aren't visited
        for (int x = 0; x < mazeData.rows; x++)
        {
            for (int y = 0; y < mazeData.columns; y++)
            {
                if (mazeData.cells[x, y] != null && mazeData.cells[x, y].IsVisited)
                {
                    availableCells.Add(new Vector2Int(x, y));
                }
            }
        }

        if (availableCells.Count > 0)
        {
            // Pick a safe grid cell position coordinate uniformly at random
            Vector2Int randomGridCell = availableCells[Random.Range(0, availableCells.Count)];
            
            // Mirror the exact 3D positioning calculation algorithm run inside CustomLevelLoader
            float cellSize = 5.0f; // Uses matching cell spatial layout factor from LevelLoader base class
            float posX = randomGridCell.y * cellSize;
            float posZ = (mazeData.rows - 1 - randomGridCell.x) * cellSize;

            return new Vector3(posX, transform.position.y, posZ);
        }

        Debug.LogWarning("GetRandomCustomCellWorldPosition: Could not isolate an open valid tile. Defaulting fallback.");
        return transform.position;
    }
}