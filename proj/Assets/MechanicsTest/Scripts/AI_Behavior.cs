using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AI_Behavior : MonoBehaviour
{
    [Header("Настройки ожидания и осмотра")]
    public float investigateWait = 5f;
    public float lookAroundTime = 5f;
    public float lookSpeed = 90f;

    [Header("Скорости движения")]
    public float patrolSpeed = 3f;
    public float fleeSpeed = 8f;

    [Header("Убежище")]
    public Transform fleePoint;

    [Header("Побег")]
    public float freezeTime = 0.5f;          // замереть
    public float rotateToPlayerTime = 0.4f;  // плавно повернуться к игроку
    public float rotateToFleeTime = 0.4f;    // повернуться к точке побега

    private AI_Navigation nav;
    private AI_Vision vision;
    private AI_Hearing hearing;
    private Animator animator;

    private float stateTimer = 0f;
    private Transform player;
    private Vector3 noisePosition;

    private bool playerVisible = false;
    private bool isLookingAround = false;
    private float waitTimer = 0f;
    private float lookAroundTimer = 0f;

    private enum FleePhase { None, Freeze, RotateToPlayer, RotateToFlee, Run }
    private FleePhase fleePhase = FleePhase.None;

    private enum State { Patrol, Investigate, Wait, Flee }
    private State currentState = State.Patrol;

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
    }

    void Update()
    {
        playerVisible = vision.CanSeePlayer();

        switch (currentState)
        {
            case State.Patrol: PatrolUpdate(); break;
            case State.Investigate: InvestigateUpdate(); break;
            case State.Wait: WaitUpdate(); break;
            case State.Flee: FleeUpdate(); break;
        }
    }

    // ---------------- ПАТРУЛЬ ----------------
    private void PatrolUpdate()
    {
        nav.speed = patrolSpeed;

        if (playerVisible)
        {
            StartFlee();
            return;
        }

        if (nav.ReachedDestination())
            EnterWaitPhase();
    }

    // ---------------- ИССЛЕДОВАНИЕ ----------------
    private void InvestigateUpdate()
    {
        nav.speed = patrolSpeed;

        if (playerVisible)
        {
            StartFlee();
            return;
        }

        nav.MoveTo(noisePosition);

        if (nav.ReachedDestination(0.5f))
            EnterWaitPhase();
    }

    // ---------------- ОЖИДАНИЕ ----------------
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

    // ---------------- ПОБЕГ ----------------
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

    // ---------------- Реакция на шум ----------------
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

    // ---------- ФАЗА ОЦЕПЕНЕНИЯ ----------
    private void FreezePhase()
    {
        nav.Stop();
        animator.speed = 0f;

        // Поворот к игроку во время замерзания
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

    // ---------- ПОВОРОТ К ИГРОКУ ----------
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

    // ---------- ПОВОРОТ К ТОЧКЕ ПОБЕГА ----------
    private void RotateToFleePhase()
    {
        if (fleePoint != null)
            LookAtTarget(fleePoint.position);

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
            fleePhase = FleePhase.Run;
    }

    // ---------- ПОБЕГ ----------
    private void RunPhase()
    {
        nav.Resume();
        nav.speed = fleeSpeed;

        if (fleePoint != null)
            nav.MoveTo(fleePoint.position);

        if (nav.ReachedDestination(0.5f))
            EnterWaitPhase();
    }

    // ---------------- ПЛАВНЫЙ ПОВОРОТ ----------------
    private void LookAtTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0f;
        if (dir == Vector3.zero) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 7f);
    }

    // ---------------- ЗАПУСК ПОБЕГА ----------------
    private void StartFlee()
    {
        currentState = State.Flee;
        isLookingAround = false;
        vision.SetChaseMode(false);

        fleePhase = FleePhase.Freeze;
        stateTimer = freezeTime;
        animator.speed = 0f;
        nav.Stop();
    }

    // ---------------- ОСМОТР ----------------
    private void EnterWaitPhase()
    {
        currentState = State.Wait;
        waitTimer = 0f;
        StartLookAround();
        fleePhase = FleePhase.None;
    }

    private void StartLookAround()
    {
        isLookingAround = true;
        lookAroundTimer = 0f;
    }

    // ---------------- АНИМАЦИИ ----------------
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
