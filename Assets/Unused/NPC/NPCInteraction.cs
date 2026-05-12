using UnityEngine;
using TMPro;

public class NPCInteraction : MonoBehaviour
{
    //[SerializeField] private PlayerRaycast playerRaycast;
    //[SerializeField] private GameObject dialoguePanel;
    //[SerializeField] private TMP_Text dialogueText;
    //[SerializeField] private GameObject interactableUI;

    //public GameObject DialoguePanel => dialoguePanel;
    //public TMP_Text DialogueText => dialogueText;

    //private NPCBase activeNPC;
    //private LevelManager levelManager;

    //void Awake()
    //{
    //    if (BLEManager.Instance != null && BLEManager.Instance.bleConnect != null)
    //    {
    //        BLEManager.Instance.bleConnect.OnInteractCommand += OnInteractCommand;
    //        BLEManager.Instance.bleConnect.OnDialogueCommand += OnDialogueCommand;
    //    }
    //    levelManager = FindAnyObjectByType<LevelManager>();
    //    if (levelManager == null)
    //    {
    //        Debug.LogError("NPCInteraction: LevelManager not found in scene");
    //    }
    //    if (playerRaycast != null)
    //    {
    //        playerRaycast.OnInteractableNPCDetected += HandleNPCDetection;
    //    }
    //    if (interactableUI != null)
    //    {
    //        interactableUI.SetActive(false); // Hide on start
    //    }
    //}

    //void OnDestroy()
    //{
    //    if (BLEManager.Instance != null && BLEManager.Instance.bleConnect != null)
    //    {
    //        BLEManager.Instance.bleConnect.OnInteractCommand -= OnInteractCommand;
    //        BLEManager.Instance.bleConnect.OnDialogueCommand -= OnDialogueCommand;
    //    }
    //    if (playerRaycast != null)
    //    {
    //        playerRaycast.OnInteractableNPCDetected -= HandleNPCDetection;
    //    }
    //}

    //private void HandleNPCDetection(bool isDetected)
    //{
    //    interactableUI.SetActive(isDetected && levelManager.CurrentLevelState != LevelManager.LevelState.Interacting);
    //}

    //public void OnInteractCommand()
    //{
    //    // Check GameManager state
    //    if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.InGame)
    //        return;

    //    // Check LevelManager state
    //    if (levelManager == null || (levelManager.CurrentLevelState != LevelManager.LevelState.Playing && levelManager.CurrentLevelState != LevelManager.LevelState.Interacting))
    //        return;

    //    // If dialogue is active, end interaction
    //    if (dialoguePanel != null && dialoguePanel.activeSelf && activeNPC != null)
    //    {
    //        activeNPC.Interact(); // Ends dialogue
    //        levelManager.EndInteraction();
    //        interactableUI.SetActive(playerRaycast.CheckNPC() != null); // Show if NPC in range
    //        activeNPC = null;
    //        return;
    //    }

    //    // Start new interaction
    //    GameObject npc = playerRaycast.CheckNPC();
    //    if (npc != null)
    //    {
    //        NPCBase npcScript = npc.GetComponent<NPCBase>();
    //        if (npcScript != null)
    //        {
    //            activeNPC = npcScript;
    //            interactableUI.SetActive(false); // Hide UI
    //            levelManager.StartInteraction();
    //            npcScript.Interact(); // Starts dialogue
    //        }
    //    }
    //}

    //public void OnDialogueCommand()
    //{
    //    if (dialoguePanel != null && dialoguePanel.activeSelf && activeNPC != null)
    //    {
    //        activeNPC.FastForwardDialogue(); // Fast-forward typing
    //    }
    //    // Else do nothing if no dialogue or line is complete
    //}
}