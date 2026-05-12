using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoalLocationMarker : MonoBehaviour
{
    [SerializeField] private Image markerUI;
    private GameObject endGoalObject;
    private Camera mainCamera;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float revealTime = 5f; // Time in seconds to reveal the marker UI
    private float remainingTime = 0f;
    private bool isHintActive = false;
    private Coroutine flashCoroutine;

    public event Action OnHintActivated;
    public event Action<float> OnHintTick;
    public event Action OnHintDeactivated;

    private void Start()
    {
        if(markerUI == null)
        {
            Debug.LogError("Marker UI is not assigned! Please assign it in the inspector.");
            return;
        }
        markerUI.enabled = false; // Initially hide the marker UI

        endGoalObject = GameObject.FindGameObjectWithTag("MazeGoal");
        if (endGoalObject == null)
        {
            Debug.LogError("End goal object not found! Please ensure it has the tag 'MazeGoal'.");
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found! Please ensure there is a camera tagged as 'MainCamera'.");
            return;
        }

        if (offset == null)
        {
            offset = Vector3.zero; // Default offset if not set
        }
    }

    private void Update()
    {
        float minX = markerUI.GetPixelAdjustedRect().width / 2;
        float maxX = Screen.width - minX;

        float minY = markerUI.GetPixelAdjustedRect().height / 2;
        float maxY = Screen.height - minY;
        
        // Vector2 pos = Camera.main.WorldToScreenPoint(endGoalObject.transform.position + offset);
        Vector2 pos = Vector2.zero;

        // if(Vector3.Dot((endGoalObject.transform.position - mainCamera.transform.position), mainCamera.transform.forward) < 0)
        // {
        //     if(pos.x < Screen.width / 2)
        //     {
        //         pos.x = maxX;
        //     }
        //     else
        //     {
        //         pos.x = minX;
        //     }
        // }

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        markerUI.transform.position = pos;
    }

    public void ActivateHint()
    {
        if (!isHintActive)
        {
            isHintActive = true;
            remainingTime = revealTime;
            markerUI.enabled = true;
            OnHintActivated?.Invoke();
            StartCoroutine(HintCountdown());
            Debug.Log("Hint activated");
        }
        else
        {
            remainingTime += revealTime;
            Debug.Log($"Hint time extended, remaining: {remainingTime}s");
        }
    }

    private IEnumerator HintCountdown()
    {
        while (remainingTime > 0)
        {
            OnHintTick?.Invoke(remainingTime);
            remainingTime -= Time.deltaTime;
            if (remainingTime <= 2f && flashCoroutine == null)
            {
                flashCoroutine = StartCoroutine(FlashHint());
            }
            yield return null;
        }
        isHintActive = false;
        markerUI.enabled = false;
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        OnHintDeactivated?.Invoke();
        Debug.Log("Hint deactivated");
    }

    private IEnumerator FlashHint()
    {
        if (markerUI == null) yield break;
        Color originalColor = markerUI.color;
        while (remainingTime > 0)
        {
            markerUI.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            yield return new WaitForSeconds(0.25f);
            markerUI.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
            yield return new WaitForSeconds(0.25f);
        }
        markerUI.color = originalColor; // Restore original color
    }
}
