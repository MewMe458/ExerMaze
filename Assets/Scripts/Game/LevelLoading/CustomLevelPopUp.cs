using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CustomLevelPopUp : MonoBehaviour
{
    [SerializeField] private GameObject popUpPanel; // Parent panel to block interaction
    [SerializeField] private GameObject deleteConfirmPopUp;
    [SerializeField] private TMP_Text deleteConfirmText;
    [SerializeField] private Button deleteYesButton;
    [SerializeField] private Button deleteNoButton;
    [SerializeField] private GameObject errorPopUp;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private Button errorOkButton;

    private Action<bool> deleteCallback;

    void Awake()
    {
        if (popUpPanel == null) Debug.LogError("PopUp Panel not assigned!");
        if (deleteConfirmPopUp == null) Debug.LogError("Delete Confirm PopUp not assigned!");
        if (deleteConfirmText == null) Debug.LogError("Delete Confirm Text not assigned!");
        if (deleteYesButton == null) Debug.LogError("Delete Yes Button not assigned!");
        if (deleteNoButton == null) Debug.LogError("Delete No Button not assigned!");
        if (errorPopUp == null) Debug.LogError("Error PopUp not assigned!");
        if (errorText == null) Debug.LogError("Error Text not assigned!");
        if (errorOkButton == null) Debug.LogError("Error OK Button not assigned!");

        deleteYesButton.onClick.AddListener(() => OnDeleteConfirm(true));
        deleteNoButton.onClick.AddListener(() => OnDeleteConfirm(false));
        errorOkButton.onClick.AddListener(OnErrorDismiss);

        deleteConfirmPopUp.SetActive(false);
        errorPopUp.SetActive(false);
        popUpPanel.SetActive(false); // Ensure panel is initially inactive
    }

    public void ShowDeleteConfirmation(string levelName, Action<bool> callback)
    {
        deleteCallback = callback;
        deleteConfirmText.text = $"Are you sure you want to delete {levelName}?";
        deleteConfirmPopUp.SetActive(true);
        errorPopUp.SetActive(false);
        popUpPanel.SetActive(true); // Activate panel to block interaction
    }

    public void ShowErrorPopUp(string message)
    {
        errorText.text = $"Error: {message}";
        errorPopUp.SetActive(true);
        deleteConfirmPopUp.SetActive(false);
        popUpPanel.SetActive(true); // Activate panel to block interaction
    }

    private void OnDeleteConfirm(bool confirmed)
    {
        deleteConfirmPopUp.SetActive(false);
        deleteCallback?.Invoke(confirmed);
        deleteCallback = null;
        // Deactivate panel if both pop-ups are inactive
        if (!deleteConfirmPopUp.activeSelf && !errorPopUp.activeSelf)
        {
            popUpPanel.SetActive(false);
        }
    }

    private void OnErrorDismiss()
    {
        errorPopUp.SetActive(false);
        // Deactivate panel if both pop-ups are inactive
        if (!deleteConfirmPopUp.activeSelf && !errorPopUp.activeSelf)
        {
            popUpPanel.SetActive(false);
        }
    }
}