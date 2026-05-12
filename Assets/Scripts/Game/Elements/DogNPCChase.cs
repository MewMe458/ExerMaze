using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DogNPCChase : MonoBehaviour
{
    private bool allowMove = false;
    public bool AllowMove
    {
        get => allowMove;
        set => allowMove = value;
    }

    private GameObject player;
    private NavMeshAgent agent;

    [SerializeField] private float detectionSize = 5f; // Range to detect player
    public float DetectionSize
    {
        get => detectionSize;
        set => detectionSize = value;
    }
    private bool isTamed = false;
    [SerializeField] private float calmDuration = 15f; // Duration to remain calm before chasing again
    private bool isCalm = false;
    private bool isWandering = false; // Indicates if dog is wandering
    private float wanderTimer = 0f; // Tracks time since last wander destination
    private const float WANDER_INTERVAL = 5f; // Time between picking new destinations
    private const float WANDER_RADIUS = 10f; // Max distance for wander points
    private bool wasChasing = false;
    private bool playerFound = false;
    private Vector3 lastKnownPosition; // Last position where player was detected
    private float chaseTimer = 0f; // Tracks time since player left detection range
    private bool isChasingLastPosition = false; // Indicates pursuit of last known position
    private const float CHASE_DURATION = 5f; // 5-second countdown

    private Animator animator; // Reference to Animator component
    [SerializeField] private float animationDamping = 0.1f; // Damping time for smooth transitions
    private float currentVert = 0f; // Current smoothed Vert value
    private float currentState = 0f; // Current smoothed State value

    private InventoryManager inventoryManager;
    public delegate void DogBiteHandler();
    public event DogBiteHandler OnDogBite;
    public delegate void DogTamedHandler();
    public event DogTamedHandler OnDogTamed;
    public delegate void DogChaseStartedHandler();
    public event DogChaseStartedHandler OnDogChaseStarted;
    public delegate void DogChaseEndedHandler();
    public event DogChaseEndedHandler OnDogChaseEnded;

    #region Sprite colors for state visual indication
    private SpriteRenderer circleSprite; // Reference to Circle SpriteRenderer
    private readonly string defaultColorHex = "#00ffff"; // Cyan
    private readonly string calmColorHex = "#fff000"; // Yellow
    private readonly string chasingColorHex = "#ff1100"; // Red
    private readonly string tamedColorHex = "#01f01f"; // Green
    private Color defaultColor;
    private Color calmColor;
    private Color chasingColor;
    private Color tamedColor;
    #endregion

    void Start()
    {
        if (circleSprite == null)
        {
            circleSprite = transform.Find("Circle")?.GetComponent<SpriteRenderer>();
            if (circleSprite == null)
            {
                Debug.LogError("DogNPCChase: Circle SpriteRenderer not found!");
            }
        }

        ProcessColors();

        // Find the player by tag
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log("Dog initialized, player found");
        }
        else
        {
            Debug.LogWarning("Player not found! Make sure the player GameObject is tagged 'Player'.");
        }

        // Get NavMeshAgent and Animator components
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found on dog NPC!");
        }

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on dog NPC!");
        }

        inventoryManager = FindAnyObjectByType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogError("DogNPCChase: InventoryManager not found in scene!");
        }
    }

    void Update()
    {
        if (!allowMove || player == null || agent == null || animator == null || isTamed || isCalm)
        {
            if (isTamed || isCalm)
            {
                agent.destination = transform.position; // Stop moving
                isWandering = false; // Not wandering
                wanderTimer = 0f; // Reset timer
                UpdateAnimation(); // Update to idle
            }
            UpdateSpriteColour();
            return;
        }

        // Check for player detection
        float distance = Vector3.Distance(transform.position, player.transform.position);
        if (distance <= detectionSize)
        {
            if (!playerFound)
            {
                if (!isChasingLastPosition && !wasChasing) // First detection
                {
                    Debug.Log("Player found");
                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlayBarkSound();
                    }
                    OnDogChaseStarted?.Invoke();
                }
                playerFound = true;
                isChasingLastPosition = false; // Stop pursuing last position
                isWandering = false;
                chaseTimer = 0f;
                wanderTimer = 0f;
                wasChasing = true; // Mark as chasing
            }
            lastKnownPosition = player.transform.position;
            agent.destination = lastKnownPosition;
        }
        else if (playerFound || isChasingLastPosition)
        {
            if (playerFound)
            {
                Debug.Log("Player out of range");
                playerFound = false;
                isChasingLastPosition = true; // Start pursuing last known position
                isWandering = false; // Stop wandering
                chaseTimer = 0f; // Reset chase timer
                wanderTimer = 0f; // Reset wander timer
            }

            if (isChasingLastPosition)
            {
                chaseTimer += Time.deltaTime;
                // Check if dog has reached last known position (within a small threshold)
                if (Vector3.Distance(transform.position, lastKnownPosition) <= agent.stoppingDistance + 0.1f || chaseTimer >= CHASE_DURATION)
                {
                    isChasingLastPosition = false; // End chase
                    chaseTimer = 0f;
                    isWandering = true; // Start wandering
                    wanderTimer = 0f; // Reset for pause at current position
                    agent.destination = transform.position; // Pause
                    wasChasing = false;
                    OnDogChaseEnded?.Invoke();
                    Debug.Log("Reached last known position or chase timer expired, entering wander state");
                }
                else
                {
                    agent.destination = lastKnownPosition; // Continue to last position
                }
            }
        }
        else
        {
            // Non-chasing state: wander
            isWandering = true;
            wanderTimer += Time.deltaTime;
            if (wanderTimer >= WANDER_INTERVAL || Vector3.Distance(transform.position, agent.destination) <= agent.stoppingDistance + 0.1f)
            {
                if (Vector3.Distance(transform.position, agent.destination) <= agent.stoppingDistance + 0.1f)
                {
                    agent.destination = transform.position; // Ensure pause at position
                }
                if (wanderTimer >= WANDER_INTERVAL)
                {
                    agent.destination = GetRandomNavMeshPoint(); // Pick new destination
                    wanderTimer = 0f; // Reset timer
                    Debug.Log("Dog wandering to new random point");
                }
            }
        }

        UpdateAnimation();
        UpdateSpriteColour();
    }
    void ProcessColors()
    {
        if (!ColorUtility.TryParseHtmlString(defaultColorHex, out defaultColor))
        {
            Debug.LogError($"DogNPCChase: Failed to parse default color hex {defaultColorHex}, using fallback cyan");
            defaultColor = new Color(0f, 1f, 1f);
        }

        if (!ColorUtility.TryParseHtmlString(calmColorHex, out calmColor))
        {
            Debug.LogError($"DogNPCChase: Failed to parse default color hex {calmColorHex}, using fallback yellow");
            defaultColor = new Color(1f, 0.937f, 0f);
        }
        if (!ColorUtility.TryParseHtmlString(chasingColorHex, out chasingColor))
        {
            Debug.LogError($"DogNPCChase: Failed to parse chasing color hex {chasingColorHex}, using fallback red");
            chasingColor = new Color(1f, 0.067f, 0f);
        }
        if (!ColorUtility.TryParseHtmlString(tamedColorHex, out tamedColor))
        {
            Debug.LogError($"DogNPCChase: Failed to parse tamed color hex {tamedColorHex}, using fallback green");
            tamedColor = new Color(0.004f, 0.941f, 0.122f);
        }
    }

    void UpdateSpriteColour()
    {
        // Color update after state changes
        if (circleSprite != null)
        {
            if (isTamed)
            {
                circleSprite.color = tamedColor;
            }
            else if (isCalm)
            {
                circleSprite.color = calmColor;
            }
            else if (playerFound || isChasingLastPosition)
            {
                circleSprite.color = chasingColor;
            }
            else
            {
                circleSprite.color = defaultColor;
            }
        }
    }

    void UpdateAnimation()
    {
        if (animator == null) return;

        // Calculate target values
        float targetVert = (isTamed || isCalm) ? 0f : agent.velocity.magnitude / agent.speed;
        targetVert = Mathf.Clamp01(targetVert);
        float targetState = (isTamed || isCalm || agent.velocity.magnitude <= 0.1f) ? 0f : (isWandering ? 0f : 1f); // Walk when wandering, run when chasing

        // Smoothly interpolate Vert and State
        currentVert = Mathf.Lerp(currentVert, targetVert, Time.deltaTime / animationDamping);
        currentState = Mathf.Lerp(currentState, targetState, Time.deltaTime / animationDamping);

        // Apply to Animator
        animator.SetFloat("Vert", currentVert);
        animator.SetFloat("State", currentState);
    }

    void OnTriggerEnter(Collider collision)
    {
        if (!playerFound || isTamed || isCalm)
        {
            Debug.Log($"Dog collision ignored: either not found ({playerFound}), is tamed ({isTamed}) or is calm ({isCalm})");
            return; 
        }

        if (!collision.CompareTag("Player"))
        {
            Debug.Log("Dog collided with non-player object: " + collision.name);
            return; // Ignore collisions with non-player objects
        }

        Debug.Log("Dog collided with player");
        ShieldPowerUp shieldPowerUp = collision.GetComponent<ShieldPowerUp>();
        if (inventoryManager != null && inventoryManager.HasItem("Bones"))
        {
            inventoryManager.RemoveItem("Bones");
            isTamed = true;
            playerFound = false;
            OnDogChaseEnded?.Invoke();
            OnDogTamed?.Invoke();
            Debug.Log("Dog tamed with Bones");
        }
        else if (shieldPowerUp != null && shieldPowerUp.IsShieldActive())
        {
            isCalm = true;
            playerFound = false;
            OnDogChaseEnded?.Invoke();
            StartCoroutine(CalmPeriod());
            Debug.Log("Shield active, dog enters calm state without biting");
        }
        else
        {
            isCalm = true;
            playerFound = false;
            OnDogChaseEnded?.Invoke();
            OnDogBite?.Invoke();
            StartCoroutine(CalmPeriod());
            Debug.Log("Dog bit player, entering calm period");
        }
    }

    private Vector3 GetRandomNavMeshPoint()
    {
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * WANDER_RADIUS;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, WANDER_RADIUS, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return transform.position; // Fallback to current position if no valid point found
    }

    private IEnumerator CalmPeriod()
    {
        yield return new WaitForSecondsRealtime(calmDuration);
        isCalm = false;
        wasChasing = false;
        Debug.Log("Dog calm period ended, resuming chase");
    }
}