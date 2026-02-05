using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AI_Behavior : MonoBehaviour
{
    [Header("Настройки ожидания и осмотра")]
    public float investigateWait = 5f;
    public float lookAroundTime = 5f;
    public float lookSpeed = 90f;

    [Header("Скорости движения")]
    [Tooltip("Скорость патрулирования и исследования")]
    public float patrolSpeed = 3f;
    [Tooltip("Скорость побега")]
    public float fleeSpeed = 8f;

    [Header("Убежище")]
    public Transform fleePoint;

    [Header("Побег")]
    public float freezeTime = 0.5f;          
    public float rotateToPlayerTime = 0.4f;  
    public float rotateToFleeTime = 0.4f;    

    [Header("Проигрыш")]
    public Canvas youLoseCanvas; // ИЗМЕНИЛ: теперь Canvas вместо GameObject

    private AI_Navigation nav;
    private AI_Vision vision;
    private AI_Hearing hearing;
    private Animator animator;

    private float stateTimer = 0f;
    private Transform player;
    private Vector3 noisePosition;
    private Transform interestTarget;
    private bool isInvestigatingPlayer = false;
    private bool playerVisible = false;
    private bool playerInInterestArea = false;
    private bool isLookingAround = false;
    private float waitTimer = 0f;
    private float lookAroundTimer = 0f;

    private enum FleePhase { None, Freeze, RotateToPlayer, RotateToFlee, Run }
    private FleePhase fleePhase = FleePhase.None;

    private enum State { Patrol, Investigate, Wait, Flee }
    private State currentState = State.Patrol;

    [Header("Менеджер изменения цвета при побеге")]
    public EscapeColorChanger escapeManager;

    void Awake()
    {
        nav = GetComponent<AI_Navigation>();
        vision = GetComponent<AI_Vision>();
        hearing = GetComponent<AI_Hearing>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        nav.speed = patrolSpeed;
        nav.GoToNextPoint();
        
        // Скрываем Canvas при старте
        if (youLoseCanvas != null)
            youLoseCanvas.gameObject.SetActive(false); // ИЗМЕНИЛ: для Canvas
    }

    void Update()
    {
        playerVisible = vision.CanSeePlayer();
        playerInInterestArea = vision.IsPlayerInInterestArea();

        if (currentState != State.Flee)
        {
            if (playerInInterestArea)
            {
                player = vision.GetPlayer();
                if (player != null)
                {
                    Vector3 playerWorldPos = player.position;
                    
                    if (!isInvestigatingPlayer)
                    {
                        isInvestigatingPlayer = true;
                        interestTarget = null;
                        noisePosition = playerWorldPos;
                        currentState = State.Investigate;
                        nav.Resume();
                        nav.MoveTo(noisePosition);
                        waitTimer = 0f;
                    }
                }
            }
            else
            {
                // Оставляем как есть
            }

            if (playerVisible && !isInvestigatingPlayer)
            {
                StartFlee();
                return;
            }

            if (!isInvestigatingPlayer)
            {
                if (vision.TryGetInterestTarget(out Transform interest))
                {
                    bool shouldSwitch = false;
                    
                    if (interestTarget == null || interestTarget != interest || currentState != State.Investigate)
                    {
                        shouldSwitch = true;
                    }
                    
                    if (shouldSwitch)
                    {
                        interestTarget = interest;
                        noisePosition = interest.position;
                        currentState = State.Investigate;
                        nav.Resume();
                        nav.MoveTo(noisePosition);
                        waitTimer = 0f;
                    }
                }
                else
                {
                    if (interestTarget != null)
                    {
                        interestTarget = null;
                        if (currentState == State.Investigate)
                        {
                            EnterWaitPhase();
                        }
                    }
                }
            }
        }

        switch (currentState)
        {
            case State.Patrol: PatrolUpdate(); break;
            case State.Investigate: InvestigateUpdate(); break;
            case State.Wait: WaitUpdate(); break;
            case State.Flee: FleeUpdate(); break;
        }
    }

    private void PatrolUpdate()
    {
        nav.Resume();
        nav.speed = patrolSpeed;

        if (playerVisible && !isInvestigatingPlayer)
        {
            StartFlee();
            return;
        }

        if (nav.ReachedDestination())
            EnterWaitPhase();
    }

    private void InvestigateUpdate()
    {
        nav.speed = patrolSpeed;
        nav.Resume();

        if (isInvestigatingPlayer)
        {
            bool playerStillInInterest = vision.IsPlayerInInterestArea();
            bool playerNowVisible = vision.CanSeePlayer();
            player = vision.GetPlayer();
            
            if (playerNowVisible)
            {
                isInvestigatingPlayer = false;
                StartFlee();
                return;
            }
            
            if (player != null)
            {
                Vector3 playerWorldPos = player.position;
                noisePosition = playerWorldPos;
                
                nav.Resume();
                nav.MoveTo(noisePosition);
                
                float distance = Vector3.Distance(transform.position, playerWorldPos);
                
                if (!playerStillInInterest)
                {
                    if (nav.ReachedDestination(1.0f))
                    {
                        isInvestigatingPlayer = false;
                        EnterWaitPhase();
                    }
                }
            }
            else
            {
                isInvestigatingPlayer = false;
                EnterWaitPhase();
            }
        }
        else if (interestTarget != null)
        {
            if (vision.TryGetInterestTarget(out Transform interest) && interest == interestTarget)
            {
                noisePosition = interestTarget.position;
                nav.MoveTo(noisePosition);
                
                if (nav.ReachedDestination(0.5f))
                {
                    EnterWaitPhase();
                }
            }
            else
            {
                interestTarget = null;
                EnterWaitPhase();
            }
        }
        else
        {
            nav.MoveTo(noisePosition);
            if (nav.ReachedDestination(0.5f))
                EnterWaitPhase();
        }
    }

    private void WaitUpdate()
    {
        waitTimer += Time.deltaTime;

        if (isLookingAround)
        {
            lookAroundTimer += Time.deltaTime;

            if (playerVisible)
            {
                StartFlee();
                return;
            }

            if (lookAroundTimer >= lookAroundTime)
                isLookingAround = false;
        }

        if (!isLookingAround && waitTimer >= investigateWait)
        {
            currentState = State.Patrol;
            nav.GoToNextPoint();
        }
    }

    public bool IsFrozen => currentState == State.Flee && fleePhase == FleePhase.Freeze;

    private void FleeUpdate()
    {
        switch (fleePhase)
        {
            case FleePhase.Freeze: FreezePhase(); break;
            case FleePhase.RotateToPlayer: RotateToPlayerPhase(); break;
            case FleePhase.RotateToFlee: RotateToFleePhase(); break;
            case FleePhase.Run: RunPhase(); break;
        }
    }

    public void HearNoise(Vector3 position)
    {
        if (currentState != State.Flee && hearing.CanHear(position))
        {
            noisePosition = position;
            currentState = State.Investigate;
            nav.MoveTo(noisePosition);
            waitTimer = 0f;
        }
    }

    private void FreezePhase()
    {
        nav.Stop();
        animator.speed = 0f;

        player = vision.GetPlayer();
        if (player != null)
            LookAtTarget(player.position);

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            fleePhase = FleePhase.RotateToPlayer;
            stateTimer = rotateToPlayerTime;
            animator.speed = 1f;
        }
    }

    private void RotateToPlayerPhase()
    {
        player = vision.GetPlayer();
        if (player != null)
            LookAtTarget(player.position);

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            fleePhase = FleePhase.RotateToFlee;
            stateTimer = rotateToFleeTime;
        }
    }

    private void RotateToFleePhase()
    {
        if (fleePoint != null)
            LookAtTarget(fleePoint.position);

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
            fleePhase = FleePhase.Run;
    }

   private void RunPhase()
{
    nav.Resume();
    nav.speed = fleeSpeed;

    if (fleePoint != null)
    {
        nav.MoveTo(fleePoint.position);
        
    }

    if (nav.ReachedDestination(0.5f))
    {
        Debug.Log("=== ДОБЕЖАЛ ДО ТОЧКИ! ===");
        Debug.Log($"Canvas назначен: {youLoseCanvas != null}");
        
        if (youLoseCanvas != null)
        {
            Debug.Log($"Canvas активен до: {youLoseCanvas.gameObject.activeSelf}");
            youLoseCanvas.gameObject.SetActive(true);
            Debug.Log($"Canvas активен после: {youLoseCanvas.gameObject.activeSelf}");
            
            // Сразу проверяем - виден ли Canvas на сцене?
            if (youLoseCanvas.gameObject.activeInHierarchy)
                Debug.Log("Canvas ВИДЕН В ИЕРАРХИИ!");
            else
                Debug.Log("Canvas НЕ ВИДЕН в иерархии!");
        }
        
        if (escapeManager != null)
            escapeManager.ChangeColor();
            
        // ОСТАНАВЛИВАЕМ ИГРУ чтобы увидеть результат
        Time.timeScale = 0f;
        Debug.Log("ИГРА ОСТАНОВЛЕНА!");
        
        EnterWaitPhase();
    }       
}

    private void LookAtTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0f;
        if (dir == Vector3.zero) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 7f);
    }

    private void StartFlee()
    {
        currentState = State.Flee;
        isLookingAround = false;
        isInvestigatingPlayer = false;
        interestTarget = null;

        fleePhase = FleePhase.Freeze;
        stateTimer = freezeTime;
        animator.speed = 0f;
        nav.Stop();
    }

    private void EnterWaitPhase()
    {
        currentState = State.Wait;
        waitTimer = 0f;
        StartLookAround();
        fleePhase = FleePhase.None;
        isInvestigatingPlayer = false;
        interestTarget = null;
    }

    private void StartLookAround()
    {
        isLookingAround = true;
        lookAroundTimer = 0f;
    }

    public string GetCurrentStateName()
    {
        if (currentState == State.Flee)
        {
            if (fleePhase == FleePhase.Freeze) return "Freeze";
            if (fleePhase == FleePhase.RotateToPlayer) return "Turn";
            if (fleePhase == FleePhase.RotateToFlee) return "Turn";
            if (fleePhase == FleePhase.Run) return "Run";
        }

        if (currentState == State.Wait && isLookingAround)
            return "LookAround";

        return currentState.ToString();
    }
}