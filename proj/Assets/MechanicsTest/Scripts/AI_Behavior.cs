using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AI_Behavior : MonoBehaviour
{
    [Header("Время ожидания")]
    public float investigateWait = 5f;

    [Header("Осмотр вокруг")]
    public float lookAroundTime = 5f;
    public float lookSpeed = 90f;
    public float lookAngle = 90f;

    private AI_Navigation nav;
    private AI_Vision vision;
    private AI_Hearing hearing;
    private Animator animator;

    private enum State { Patrol, Investigate, Wait, Chase }
    private State currentState = State.Patrol;

    private float waitTimer;
    private bool playerVisible = false;
    private Vector3 noisePosition;

    private bool isLookingAround = false;
    private float lookTimer = 0f;
    private float baseYRotation;
    private float lookAroundTimer = 0f;

    private Vector3 lastSeenPlayerPosition;

    public Transform lookTarget; // Укажи в инспекторе или создавай в коде
    public OverrideTransform overrideTransform; // ← сюда укажешь компонент

    void Awake()
    {
        nav = GetComponent<AI_Navigation>();
        vision = GetComponent<AI_Vision>();
        hearing = GetComponent<AI_Hearing>();
        animator = GetComponent<Animator>();
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
            StartLookAround();
        }
        ResetLook();
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
                    animator.SetTrigger("Wait");
                    lookAroundTimer += Time.deltaTime;

                    if (vision.CanSeePlayer())
                    {
                        StartChase();
                        return;
                    }

                    if (lookAroundTimer >= lookAroundTime)
                    {
                        isLookingAround = false;
                        ResetLook();
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
        ResetLook();
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
        lookTarget.rotation = Quaternion.Euler(0, transform.eulerAngles.y + angle, 0);

        if (lookAroundTimer >= lookAroundTime)
        {
            isLookingAround = false;
            ResetLook(); // 👈 вызываем сброс здесь
        }
    }


    public bool IsLookingAround() => isLookingAround;

    public string GetCurrentStateName()
    {
        if (currentState == State.Wait && isLookingAround)
            return "LookAround"; // <-- новое состояние для AnimationManager
        return currentState.ToString();
    }
    private void ResetLook()
    {
        if (lookTarget == null || overrideTransform == null)
            return;

        // Отключаем временно влияние Rig
        overrideTransform.weight = 0f;

        // Через 0.1 секунды возвращаем вес обратно
        StartCoroutine(RestoreRigWeight());
        // Сбрасываем локальный поворот
        lookTarget.localRotation = Quaternion.identity;
    }

    private System.Collections.IEnumerator RestoreRigWeight()
    {
        yield return new WaitForSeconds(0.1f);
        overrideTransform.weight = 1f;
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
