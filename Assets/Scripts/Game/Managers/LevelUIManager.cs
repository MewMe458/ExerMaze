using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelUIManager : MonoBehaviour
{
    [SerializeField] private GameObject bluetoothCheckUI;
    [SerializeField] private TMP_Text countdownText;

    [SerializeField] private TMP_Text stepCounterText;
    [SerializeField] private TMP_Text timerText;

    #region Challenge UI
    [SerializeField] private GameObject challengeUI; 
    [SerializeField] private GameObject iconHolder; 
    [SerializeField] private GameObject dogChaseIconPrefab; 
    [SerializeField] private GameObject dogBiteIconPrefab; 
    [SerializeField] private GameObject dogTamedIconPrefab; 
    [SerializeField] private GameObject bonesIconPrefab; 
    [SerializeField] private GameObject shieldIconPrefab; 
    [SerializeField] private GameObject hintIconPrefab; 
    private GameObject dogChaseIconInstance;
    private GameObject dogBiteIconInstance;
    private GameObject dogTamedIconInstance;
    private GameObject bonesIconInstance;
    private GameObject shieldIconInstance;
    private GameObject hintIconInstance;
    private bool isDogChaseIconInstantiated;
    private bool isDogBiteIconInstantiated;
    private bool isDogTamedIconInstantiated;
    private bool isBonesIconInstantiated;
    private bool isShieldIconInstantiated;
    private bool isHintIconInstantiated;
    [SerializeField] private GameObject levelMessageText; 
    #endregion

    private CanvasGroup messageCanvasGroup;
    private Coroutine messageFadeCoroutine;
    private LevelManager levelManager;
    private InventoryManager inventoryManager;
    private ShieldPowerUp shieldPowerUp;
    private GoalLocationMarker goalMarker;
    private Timer timer;

    private void Awake()
    {
        if (bluetoothCheckUI == null) Debug.LogError("LevelUIManager: bluetoothNotConnectedPanel not assigned");
        if (countdownText == null) Debug.LogError("LevelUIManager: countdownText not assigned");
        if (stepCounterText == null) Debug.LogError("LevelUIManager: stepCounterText not assigned");
        if (timerText == null) Debug.LogError("LevelUIManager: timerText not assigned");
        if (challengeUI == null) Debug.LogError("LevelUIManager: challengeUI not assigned");
        if (iconHolder == null) Debug.LogError("LevelUIManager: iconHolder not assigned");
        if (dogChaseIconPrefab == null) Debug.LogError("LevelUIManager: dogChaseIconPrefab not assigned");
        if (dogBiteIconPrefab == null) Debug.LogError("LevelUIManager: dogBiteIconPrefab not assigned");
        if (dogTamedIconPrefab == null) Debug.LogError("LevelUIManager: dogTamedIconPrefab not assigned");
        if (bonesIconPrefab == null) Debug.LogError("LevelUIManager: bonesIconPrefab not assigned");
        if (shieldIconPrefab == null) Debug.LogError("LevelUIManager: shieldIconPrefab not assigned");
        if (hintIconPrefab == null) Debug.LogError("LevelUIManager: hintIconPrefab not assigned");
        if (levelMessageText == null) Debug.LogError("LevelUIManager: levelMessageText GameObject not assigned");
        messageCanvasGroup = levelMessageText?.GetComponent<CanvasGroup>();
        if (messageCanvasGroup == null) Debug.LogError("LevelUIManager: CanvasGroup not found on levelMessageText GameObject");

        levelManager = FindAnyObjectByType<LevelManager>();
        if (levelManager == null) Debug.LogError("LevelUIManager: LevelManager not found in scene");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        inventoryManager = player?.GetComponent<InventoryManager>();
        if (inventoryManager == null) Debug.LogError("LevelUIManager: InventoryManager not found on player");

        UpdateBonesCount(inventoryManager?.GetItemCount("Bones") ?? 0);

        shieldPowerUp = player?.GetComponent<ShieldPowerUp>();
        if (shieldPowerUp == null) Debug.LogError("LevelUIManager: ShieldPowerUp not found on player");

        goalMarker = FindAnyObjectByType<GoalLocationMarker>();
        if (goalMarker == null) Debug.LogError("LevelUIManager: GoalLocationMarker not found in scene");

        timer = FindAnyObjectByType<Timer>();
        if (timer == null) Debug.LogError("LevelUIManager: Timer not found in scene");
    }

    private void OnDestroy()
    {
        if (shieldPowerUp != null)
        {
            shieldPowerUp.OnShieldActivated -= HandleShieldActivated;
            shieldPowerUp.OnShieldTick -= UpdateShieldStatus;
            shieldPowerUp.OnShieldDeactivated -= HandleShieldDeactivated;
        }

        if (inventoryManager != null)
        {
            inventoryManager.OnItemAdded -= HandleItemAdded;
            inventoryManager.OnItemRemoved -= HandleItemRemoved;
        }

        if (goalMarker != null)
        {
            goalMarker.OnHintActivated -= HandleHintRegistration;
            goalMarker.OnHintTick -= UpdateHintStatus;
            goalMarker.OnHintDeactivated -= HandleHintDeactivation;
        }
    }

    private void Update()
    {
        if (levelManager != null && levelManager.CurrentLevelState == LevelManager.LevelState.Playing)
        {
            UpdateTimer();
        }
        if (bluetoothCheckUI.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            bluetoothCheckUI.SetActive(false);
            GameManager.Instance.ClearCurrentLevelName();
            GameManager.Instance.ClearCurrentCustomLevelPath();
            GameManager.Instance.SetGameState(GameManager.GameState.Menu);
            BLEManager.Instance?.bleConnect?.UpdateSensorStateOnBLE("stop");
            SceneManager.LoadScene("Menu");
        }
    }

    public void InitializeUI()
    {
        UpdateStepCount(0);
        UpdateTimer();
        UpdateDogChaseCount(levelManager?.GetDogChaseCount() ?? 0);
        UpdateDogBiteCount(0);
        UpdateDogTamedCount(0);
        UpdateBonesCount(inventoryManager?.GetItemCount("Bones") ?? 0);
        UpdateShieldStatus(0f);

        if (inventoryManager != null)
        {
            inventoryManager.OnItemAdded += HandleItemAdded;
            inventoryManager.OnItemRemoved += HandleItemRemoved;
        }

        if (shieldPowerUp != null)
        {
            shieldPowerUp.OnShieldActivated += () =>
            {
                if (!isShieldIconInstantiated)
                {
                    shieldIconInstance = Instantiate(shieldIconPrefab, iconHolder.transform);
                    isShieldIconInstantiated = true;
                    Debug.Log("Instantiated ShieldIcon");
                }
                HandleShieldActivated();
                ShowLevelMessage("Shield Activated");
            };
            shieldPowerUp.OnShieldTick += UpdateShieldStatus;
            shieldPowerUp.OnShieldDeactivated += () =>
            {
                HandleShieldDeactivated();
                ShowLevelMessage("Shield Expired");
            };
        }

        if (goalMarker != null)
        {
            goalMarker.OnHintActivated += HandleHintRegistration;
            goalMarker.OnHintTick += UpdateHintStatus;
            goalMarker.OnHintDeactivated += HandleHintDeactivation;
        }

        // REMOVED: Broken SpecialItem lookups and event attachments are safely handled directly from the item now.
        levelMessageText?.SetActive(false);
    }

    private void HandleHintRegistration()
    {
        if (!isHintIconInstantiated)
        {
            hintIconInstance = Instantiate(hintIconPrefab, iconHolder.transform);
            isHintIconInstantiated = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(iconHolder.GetComponent<RectTransform>());
            Debug.Log("Instantiated HintIcon");
        }
        UpdateHintStatus(goalMarker != null ? 5f : 0f);
    }

    private void HandleHintDeactivation()
    {
        if (hintIconInstance != null)
        {
            Destroy(hintIconInstance);
            hintIconInstance = null;
            isHintIconInstantiated = false;
            Debug.Log("Hint UI destroyed");
        }
    }

    public void ShowBluetoothConnectCheckUI()
    {
        if (bluetoothCheckUI != null)
        {
            bluetoothCheckUI.SetActive(true);
        }
    }

    public void HideBluetoothConnectCheckUI()
    {
        if (bluetoothCheckUI != null)
        {
            bluetoothCheckUI.SetActive(false);
        }
    }

    public IEnumerator StartCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "3";
            yield return new WaitForSecondsRealtime(1f);
            countdownText.text = "2";
            yield return new WaitForSecondsRealtime(1f);
            countdownText.text = "1";
            yield return new WaitForSecondsRealtime(1f);
            countdownText.text = "Go!";
            yield return new WaitForSecondsRealtime(0.5f);
            countdownText.gameObject.SetActive(false);
        }
    }

    public void UpdateStepCount(int steps)
    {
        if (stepCounterText != null)
        {
            stepCounterText.text = steps.ToString();
        }
    }

    public void UpdateDogChaseCount(int count)
    {
        if (count > 0 && !isDogChaseIconInstantiated)
        {
            dogChaseIconInstance = Instantiate(dogChaseIconPrefab, iconHolder.transform);
            isDogChaseIconInstantiated = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(iconHolder.GetComponent<RectTransform>());
        }
        if (dogChaseIconInstance != null)
        {
            TMP_Text textComponent = dogChaseIconInstance.GetComponentInChildren<TMP_Text>();
            if (textComponent != null) textComponent.text = count.ToString();
        }
    }

    public void UpdateDogBiteCount(int count)
    {
        if (count > 0 && !isDogBiteIconInstantiated)
        {
            dogBiteIconInstance = Instantiate(dogBiteIconPrefab, iconHolder.transform);
            isDogBiteIconInstantiated = true;
        }
        if (dogBiteIconInstance != null)
        {
            TMP_Text textComponent = dogBiteIconInstance.GetComponentInChildren<TMP_Text>();
            if (textComponent != null) textComponent.text = count.ToString();
        }
    }

    public void UpdateDogTamedCount(int count)
    {
        if (count > 0 && !isDogTamedIconInstantiated)
        {
            dogTamedIconInstance = Instantiate(dogTamedIconPrefab, iconHolder.transform);
            isDogTamedIconInstantiated = true;
        }
        if (dogTamedIconInstance != null)
        {
            TMP_Text textComponent = dogTamedIconInstance.GetComponentInChildren<TMP_Text>();
            if (textComponent != null) textComponent.text = count.ToString();
        }
    }

    public void UpdateBonesCount(int count)
    {
        if (bonesIconInstance != null)
        {
            TMP_Text textComponent = bonesIconInstance.GetComponentInChildren<TMP_Text>();
            if (textComponent != null) textComponent.text = count.ToString();
        }
    }

    private void UpdateShieldStatus(float remainingTime)
    {
        if (shieldIconInstance != null)
        {
            TMP_Text textComponent = shieldIconInstance.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                textComponent.text = $"{minutes:00}:{seconds:00}";
            }
        }
    }

    private void HandleShieldActivated()
    {
        UpdateShieldStatus(shieldPowerUp != null ? 10f : 0f);
    }

    private void HandleShieldDeactivated()
    {
        if (shieldIconInstance != null)
        {
            Destroy(shieldIconInstance);
            shieldIconInstance = null;
            isShieldIconInstantiated = false;
        }
    }

    private void UpdateHintStatus(float remainingTime)
    {
        if (hintIconInstance != null)
        {
            TMP_Text textComponent = hintIconInstance.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                textComponent.text = $"{minutes:00}:{seconds:00}";
            }
        }
    }

    private void HandleItemAdded(string itemType, int count)
    {
        if (itemType == "Bones")
        {
            ShowLevelMessage("Bones Collected");
            if (!isBonesIconInstantiated)
            {
                bonesIconInstance = Instantiate(bonesIconPrefab, iconHolder.transform);
                isBonesIconInstantiated = true;
            }
            UpdateBonesCount(count);
        }
    }

    private void HandleItemRemoved(string itemType, int newCount)
    {
        if (itemType == "Bones")
        {
            UpdateBonesCount(newCount);
        }
    }

    public void UpdateTimer()
    {
        if (timerText != null && timer != null)
        {
            float elapsedTime = timer.GetElapsedTime();
            int hours = Mathf.FloorToInt(elapsedTime / 3600f);
            int minutes = Mathf.FloorToInt((elapsedTime % 3600f) / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            if (hours >= 1)
            {
                timerText.text = string.Format("{0}:{1:00}:{2:00}", hours, minutes, seconds);
            }
            else
            {
                timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
            }
        }
    }

    public void ShowLevelMessage(string message)
    {
        if (levelMessageText != null)
        {
            if (messageFadeCoroutine != null)
            {
                StopCoroutine(messageFadeCoroutine);
            }
            TMP_Text textComponent = levelMessageText.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = message;
            }
            messageCanvasGroup.alpha = 1f;
            levelMessageText.SetActive(true);
            messageFadeCoroutine = StartCoroutine(FadeOutMessage());
            Debug.Log($"Level message displayed: {message}");
        }
    }

    private IEnumerator FadeOutMessage()
    {
        yield return new WaitForSeconds(1.5f);
        float fadeDuration = 0.5f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            messageCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        levelMessageText.SetActive(false);
    }

    public void SetChallengeIconPanelVisibility(bool isVisible)
    {
        if (challengeUI != null)
        {
            challengeUI.SetActive(isVisible);
        }
    }
}