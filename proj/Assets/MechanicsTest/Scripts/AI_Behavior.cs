using UnityEngine;

public class AI_Behavior : MonoBehaviour
{
    [Header("Время ожидания")]
    public float investigateWait = 3f;

    [Header("Осмотр вокруг")]
    public float lookAroundTime = 3f;
    public float lookSpeed = 60f;
    public float lookAngle = 60f;

    private AI_Navigation nav;
    private AI_Vision vision;
    private AI_Hearing hearing;

    private enum State { Patrol, Investigate, Wait, Chase }
    private State currentState = State.Patrol;

    private float waitTimer;
    private bool isInvestigating = false;
    private bool playerVisible = false;
    private Vector3 noisePosition;

    private bool isLookingAround = false;
    private float lookTimer = 0f;
    private float baseYRotation;
    private float lookAroundTimer = 0f;

    private Vector3 lastSeenPlayerPosition;

    void Awake()
    {
        nav = GetComponent<AI_Navigation>();
        vision = GetComponent<AI_Vision>();
        hearing = GetComponent<AI_Hearing>();
    }

    void Start()
    {
        nav.GoToNextPoint(); // сразу к первой точке патруля
    }

    void Update()
    {
        playerVisible = vision.CanSeePlayer();

        switch (currentState)
        {
            case State.Patrol: PatrolUpdate(); break;
            case State.Investigate: InvestigateUpdate(); break;
            case State.Wait: WaitUpdate(); break;
            case State.Chase: ChaseUpdate(); break;
        }
    }

    // ---------------- Патруль ----------------
    private void PatrolUpdate()
    {
        if (playerVisible)
        {
            StartChase();
            return;
        }

        if (nav.ReachedDestination())
        {
            currentState = State.Wait;
            waitTimer = 0f;
            isInvestigating = false;
            StartLookAround();
        }
    }

    // ---------------- Исследование (шум/объект) ----------------
    private void InvestigateUpdate()
    {
        if (playerVisible)
        {
            StartChase();
            return;
        }

        if (nav.ReachedDestination(0.5f))
        {
            currentState = State.Wait;
            isInvestigating = true;
            waitTimer = 0f;
            StartLookAround();
        }
    }

    // ---------------- Ожидание / осмотр ----------------
    private void WaitUpdate()
    {
        if (playerVisible)
        {
            StartChase();
            return;
        }

        waitTimer += Time.deltaTime;

        if (isLookingAround)
        {
            LookAround();
            lookAroundTimer += Time.deltaTime;

            if (vision.CanSeePlayer())
            {
                StartChase();
                return;
            }

            if (lookAroundTimer >= lookAroundTime)
                isLookingAround = false;
        }

        // После ожидания возвращаемся к патрулю
        if (!isLookingAround && waitTimer >= investigateWait)
        {
            isInvestigating = false;
            currentState = State.Patrol;
            nav.GoToNextPoint();
        }
    }

    // ---------------- Погоня ----------------
    private void ChaseUpdate()
    {
        var player = vision.GetPlayer();

        if (playerVisible && player != null)
        {
            nav.MoveTo(player.position);
            isLookingAround = false;
            lastSeenPlayerPosition = player.position;
        }
        else
        {
            if (!nav.ReachedDestination(0.5f))
            {
                nav.MoveTo(lastSeenPlayerPosition);
            }
            else
            {
                if (!isLookingAround)
                    StartLookAround();

                if (isLookingAround)
                {
                    LookAround();
                    lookAroundTimer += Time.deltaTime;

                    if (vision.CanSeePlayer())
                    {
                        StartChase();
                        return;
                    }

                    if (lookAroundTimer >= lookAroundTime)
                    {
                        isLookingAround = false;
                        StopChase();
                        return;
                    }
                }
            }
        }
    }

    // ---------------- Реакция на шум ----------------
    public void HearNoise(Vector3 position)
    {
        if (currentState != State.Chase && hearing.CanHear(position))
        {
            noisePosition = position;
            currentState = State.Investigate;
            nav.MoveTo(noisePosition);
            isInvestigating = true;
            waitTimer = 0f;
        }
    }

    // ---------------- Переходы ----------------
    private void StartChase()
    {
        currentState = State.Chase;
        vision.SetChaseMode(true);
        isLookingAround = false;

        var player = vision.GetPlayer();
        if (player != null)
            lastSeenPlayerPosition = player.position;
    }

    private void StopChase()
    {
        vision.SetChaseMode(false);
        currentState = State.Patrol;
        nav.GoToNextPoint();

        isLookingAround = false;
        lookAroundTimer = 0f;
    }

    // ---------------- Осмотр ----------------
    private void StartLookAround()
    {
        if (isLookingAround) return;

        isLookingAround = true;
        lookTimer = 0f;
        lookAroundTimer = 0f;
        baseYRotation = transform.eulerAngles.y;
    }

    private void LookAround()
    {
        lookTimer += Time.deltaTime * lookSpeed;
        float angle = Mathf.Sin(lookTimer * Mathf.Deg2Rad) * lookAngle;
        transform.rotation = Quaternion.Euler(0, baseYRotation + angle, 0);
    }

    public string GetCurrentStateName()
    {
        return currentState.ToString();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) 
            return; 
        Gizmos.color = currentState switch 
        { 
            State.Patrol => Color.blue, 
            State.Investigate => Color.yellow, 
            State.Wait => Color.white, 
            State.Chase => Color.red, 
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
