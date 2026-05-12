using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public abstract class NPCBase : MonoBehaviour
{
    //[SerializeField] protected string npcId;
    //protected string currentState;
    //protected DialogueData dialogueData;
    //protected bool isTyping;

    //// Access UI via NPCInteraction instance
    //protected NPCInteraction npcInteraction => FindAnyObjectByType<NPCInteraction>();
    //protected GameObject dialoguePanel => npcInteraction?.DialoguePanel;
    //protected TMP_Text dialogueText => npcInteraction?.DialogueText;

    //protected virtual void Start()
    //{
    //    LoadDialogue();
    //    currentState = GetState(); // Initialize state
    //}

    //protected void LoadDialogue()
    //{
    //    TextAsset json = Resources.Load<TextAsset>($"NPCDialogue/{npcId}");
    //    if (json != null)
    //    {
    //        dialogueData = JsonUtility.FromJson<DialogueData>(json.text);
    //    }
    //    else
    //    {
    //        Debug.LogError($"Failed to load dialogue for {npcId}");
    //    }
    //}

    //public void Interact()
    //{
    //    if (npcInteraction == null)
    //    {
    //        Debug.LogError("NPCInteraction not found in scene");
    //        return;
    //    }

    //    if (dialoguePanel.activeSelf)
    //    {
    //        EndDialogue();
    //    }
    //    else
    //    {
    //        StartDialogue();
    //    }
    //}

    //protected virtual void StartDialogue()
    //{
    //    currentState = GetState(); // Update state (includes player inventory check)
    //    List<string> lines = dialogueData?.states.Find(s => s.stateId == currentState)?.lines;
    //    if (lines == null || lines.Count == 0)
    //    {
    //        Debug.LogWarning($"No lines for state {currentState} in {npcId}");
    //        return;
    //    }
    //    string line = lines[Random.Range(0, lines.Count)];
    //    dialoguePanel.SetActive(true);
    //    StartCoroutine(TypeDialogue(line));
    //}

    //protected IEnumerator TypeDialogue(string line)
    //{
    //    isTyping = true;
    //    dialogueText.text = "";
    //    foreach (char c in line)
    //    {
    //        dialogueText.text += c;
    //        yield return new WaitForSeconds(0.03f);
    //        if (!isTyping) break;
    //    }
    //    dialogueText.text = line;
    //    isTyping = false;
    //}

    //protected void EndDialogue()
    //{
    //    dialoguePanel.SetActive(false);
    //    isTyping = false;
    //}

    //public void FastForwardDialogue()
    //{
    //    if (isTyping)
    //    {
    //        isTyping = false; // Complete current line
    //    }
    //}

    //public abstract string GetState();
    //public virtual void SetState(string newState)
    //{
    //    currentState = newState;
    //    Debug.Log($"NPC {npcId} state set to {newState}");
    //}

    //[System.Serializable]
    //public class DialogueState
    //{
    //    public string stateId;
    //    public List<string> lines;
    //}

    //[System.Serializable]
    //public class DialogueData
    //{
    //    public string npcId;
    //    public List<DialogueState> states;
    //}
}