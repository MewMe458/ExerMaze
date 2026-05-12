using System.Collections;
using UnityEngine;

public class MazeTeleporter : MonoBehaviour
{
    [SerializeField] private float yOffset = 0.1f;
    [SerializeField] private bool avoidCenterRoom = true;
    [SerializeField] private GameObject teleportEffect;
    [SerializeField] private AudioSource teleportSound;
    [SerializeField] private float teleportDelay = 0.9f;

    private AutoMG3D_1010 maze;

    private void Awake()
    {
        maze = FindObjectOfType<AutoMG3D_1010>();
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

    private IEnumerator TeleportRoutine(Transform player, CharacterController controller)
    {
        // Stop movement
        controller.enabled = false;

        // Play effect
        teleportEffect.SetActive(true);

        teleportSound.Play();

        // Wait for effect
        yield return new WaitForSeconds(teleportDelay);

        // Teleport
        Vector3 targetPos = maze.GetRandomCellWorldPosition(avoidCenterRoom);
        targetPos.y += yOffset;
        player.position = targetPos;

        // Stop effect
        teleportEffect.SetActive(false);

        // Re-enable movement
        controller.enabled = true;
    }
}
