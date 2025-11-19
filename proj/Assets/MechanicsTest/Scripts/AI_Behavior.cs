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

    private AI_Navigation nav;
    private AI_Vision vision;
    private AI_Hearing hearing;
    private Animator animator;

    private enum State { Patrol, Investigate, Wait, Flee }
    private State currentState = State.Patrol;

    private float waitTimer;
    private bool playerVisible = false;
    private Vector3 noisePosition;

    private bool isLookingAround = false;
    private float lookTimer = 0f;
    private float lookAroundTimer = 0f;

    void Awake()
    {
        nav = GetComponent<AI_Navigation>();
        vision = GetComponent<AI_Vision>();
        hearing = GetComponent<AI_Hearing>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        nav.GoToNextPoint();
        nav.speed = patrolSpeed;
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

    // ---------------- Патруль ----------------
    private void PatrolUpdate()
    {
        nav.speed = patrolSpeed;

        if (playerVisible)
        {
            StartFlee();
            return;
        }

        if (nav.ReachedDestination())
        {
            EnterWaitPhase();
        }
    }

    // ---------------- Исследование ----------------
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
        {
            EnterWaitPhase();
        }
    }

    // ---------------- Ожидание / Осмотр ----------------
    private void WaitUpdate()
    {
        waitTimer += Time.deltaTime;

        if (isLookingAround)
        {
            lookTimer += Time.deltaTime * lookSpeed;
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
            nav.speed = patrolSpeed;
        }
    }

    // ---------------- Убегание ----------------
    private void FleeUpdate()
    {
        if (fleePoint == null)
        {
            currentState = State.Patrol;
            nav.GoToNextPoint();
            nav.speed = patrolSpeed;
            return;
        }

        nav.speed = fleeSpeed;
        nav.MoveTo(fleePoint.position);
        isLookingAround = false;

        if (nav.ReachedDestination(0.5f))
        {
            EnterWaitPhase();
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

    // ---------------- Переходы ----------------
    private void StartFlee()
    {
        currentState = State.Flee;
        vision.SetChaseMode(false);
        isLookingAround = false;
    }

    private void EnterWaitPhase()
    {
        currentState = State.Wait;
        waitTimer = 0f;
        StartLookAround();
    }

    private void StartLookAround()
    {
        isLookingAround = true;
        lookTimer = 0f;
        lookAroundTimer = 0f;
    }

    public bool IsLookingAround() => isLookingAround;

    public string GetCurrentStateName()
    {
        if (currentState == State.Wait && isLookingAround)
            return "LookAround";
        return currentState.ToString();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = currentState switch
        {
            State.Patrol => Color.blue,
            State.Investigate => Color.yellow,
            State.Wait => Color.white,
            State.Flee => Color.green,
            _ => Color.gray
        };
        Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.2f);

        if (nav != null && nav.HasDestination())
        {
            Vector3 dest = nav.GetDestination();
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, dest);
            Gizmos.DrawSphere(dest, 0.3f);
        }

        if (currentState == State.Investigate)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, noisePosition);
            Gizmos.DrawSphere(noisePosition, 0.2f);
        }
    }
}
