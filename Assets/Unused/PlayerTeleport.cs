using UnityEngine;

public class PlayerTeleport : MonoBehaviour
{
    private GameObject targetPortal; // Target portal to teleport to
    private bool isTeleporting = false; // Prevent repeated teleporting

    private void OnTriggerEnter(Collider other)
    {
        if (isTeleporting) return; // Prevent re-entry during teleport

        if (other.CompareTag("Portals"))
        {
            // Get the target portal from the portal script
            Portal portalScript = other.GetComponent<Portal>();
            if (portalScript != null)
            {
                targetPortal = portalScript.TargetPortal;

                // Teleport the player
                if (targetPortal != null)
                {
                    Transform targetTransform = targetPortal.transform;
                    transform.position = targetTransform.position + targetTransform.forward * 1.0f; // Offset slightly
                    isTeleporting = true;

                    // Optionally handle rotation
                    transform.rotation = targetTransform.rotation;

                    // Reset teleporting state after a short delay
                    Invoke(nameof(ResetTeleport), 0.5f);
                }
            }
        }
    }

    private void ResetTeleport()
    {
        isTeleporting = false;
    }
}

