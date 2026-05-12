using UnityEngine;
using System;

public class PlayerRaycast : MonoBehaviour
{
    [SerializeField] private float hitDistance = 1f;
    public event Action<bool> OnInteractableNPCDetected;
    private bool wasNPCDetected = false;

    void Update()
    {
        GameObject npc = CheckNPC();
        bool isNPCDetected = npc != null;
        if (isNPCDetected != wasNPCDetected)
        {
            OnInteractableNPCDetected?.Invoke(isNPCDetected);
            wasNPCDetected = isNPCDetected;
        }
    }

    public GameObject CheckNPC()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, hitDistance))
        {
            if (hit.transform.gameObject.CompareTag("NPC"))
            {
                return hit.transform.gameObject;
            }
        }
        return null;
    }
}