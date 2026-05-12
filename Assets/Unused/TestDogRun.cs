using UnityEngine;
using Controller; // For CreatureMover

public class TestDogRun : MonoBehaviour
{
    private GameObject player;
    private CreatureMover mover;
    private bool playerFound = false;
    private float detectionRange = 10f; // meters

    private bool isPlayerInRange = false; // Tracks if player is currently in range

    void Start()
    {
        // Find the player by tag
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log("Dog found player");
            playerFound = true;
        }
        else
        {
            Debug.LogWarning("Player not found! Make sure the player GameObject is tagged 'Player'.");
        }

        // Get the CreatureMover component on this NPC
        mover = GetComponent<CreatureMover>();
        if (mover == null)
        {
            Debug.LogError("CreatureMover component not found on dog NPC!");
        }
    }

    void Update()
    {
        if (!playerFound || mover == null)
            return;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        bool playerNowInRange = distance <= detectionRange;

        // Log only when the detection state changes
        if (playerNowInRange && !isPlayerInRange)
        {
            Debug.Log("Player detected");
            isPlayerInRange = true;
        }
        else if (!playerNowInRange && isPlayerInRange)
        {
            Debug.Log("Player gone");
            isPlayerInRange = false;
        }

        if (isPlayerInRange)
        {
            // Calculate the direction to the player, ignoring Y to keep the dog upright
            Vector3 direction = player.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * 5f // Adjust 5f for rotation speed
                );
            }

            // Make the dog look at the player
            //transform.LookAt(new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z));

            // Press R to make the dog run toward the player
            if (Input.GetKey(KeyCode.R))
            {
                Vector3 toPlayer = (player.transform.position - transform.position).normalized;
                Vector2 axis = new Vector2(toPlayer.x, toPlayer.z).normalized; // XZ plane
                Vector3 target = player.transform.position;

                mover.SetInput(axis, target, true, false); // isRun = true, isJump = false
            }
            else
            {
                // Stop moving when R is not pressed
                mover.SetInput(Vector2.zero, transform.position, false, false);
            }
        }
        else
        {
            // Player not in range, ensure dog is stopped
            mover.SetInput(Vector2.zero, transform.position, false, false);
        }
    }
}
