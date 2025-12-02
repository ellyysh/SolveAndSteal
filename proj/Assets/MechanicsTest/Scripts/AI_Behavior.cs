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
    }

    void Update()
    {
        playerVisible = vision.CanSeePlayer();
        playerInInterestArea = vision.IsPlayerInInterestArea();

        // Приоритет: игрок в области интереса (подойти) > обычное зрение (побег) > обычные объекты интереса
        if (currentState != State.Flee)
        {
            // ЛОГИКА ДЛЯ ИГРОКА В ОБЛАСТИ ИНТЕРЕСА (отдельно от обычных объектов)
            // ПРИОРИТЕТ: сначала подойти к игроку в большой области, чтобы проверить его в маленькой
            if (playerInInterestArea)
            {
                // Если игрок в области интереса - исследуем его (даже если исследуем обычный объект)
                player = vision.GetPlayer();
                if (player != null)
                {
                    // Получаем мировую позицию игрока
                    Vector3 playerWorldPos = player.position;
                    
                    // Переключаемся на игрока, если еще не исследуем его
                    if (!isInvestigatingPlayer)
                    {
                        isInvestigatingPlayer = true;
                        interestTarget = null; // Сбрасываем обычный объект интереса
                        noisePosition = playerWorldPos;
                        currentState = State.Investigate;
                        nav.Resume(); // Убеждаемся, что навигация активна
                        nav.MoveTo(noisePosition);
                        waitTimer = 0f;
                        
                        Debug.Log($"[AI_Behavior] Начинаю исследование игрока. Позиция: {playerWorldPos}, Расстояние: {Vector3.Distance(transform.position, playerWorldPos):F2}, Состояние: {currentState}");
                    }
                    // Если уже исследуем игрока, просто обновляем позицию (в InvestigateUpdate)
                }
            }
            else
            {
                // Если игрок больше не в области интереса - НЕ сбрасываем сразу, даем время дойти
                // Сброс произойдет в InvestigateUpdate, если игрок действительно ушел
            }

            // Если игрок в обычной области видимости И мы НЕ исследуем его (не подходим) - побег
            // Если мы исследуем игрока, проверка обычного зрения будет в InvestigateUpdate
            if (playerVisible && !isInvestigatingPlayer)
            {
                StartFlee();
                return;
            }

            // ЛОГИКА ДЛЯ ОБЫЧНЫХ ОБЪЕКТОВ ИНТЕРЕСА (отдельно от игрока)
            // Проверяем только если НЕ исследуем игрока
            if (!isInvestigatingPlayer)
            {
                if (vision.TryGetInterestTarget(out Transform interest))
                {
                    // Если нашли объект интереса
                    bool shouldSwitch = false;
                    
                    // Переключаемся если:
                    // 1. У нас нет текущего объекта интереса
                    // 2. Найденный объект отличается от текущего
                    // 3. Мы не в состоянии Investigate
                    if (interestTarget == null || interestTarget != interest || currentState != State.Investigate)
                    {
                        shouldSwitch = true;
                    }
                    
                    if (shouldSwitch)
                    {
                        interestTarget = interest;
                        noisePosition = interest.position;
                        currentState = State.Investigate;
                        nav.Resume(); // Убеждаемся, что навигация активна
                        nav.MoveTo(noisePosition);
                        waitTimer = 0f;
                    }
                }
                else
                {
                    // Если объект интереса не найден, но мы его исследовали - сбрасываем
                    if (interestTarget != null)
                    {
                        interestTarget = null;
                        // Если мы в состоянии Investigate из-за объекта, переходим в Wait
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

    // ---------------- ПАТРУЛЬ ----------------
    private void PatrolUpdate()
    {
        nav.Resume(); // Убеждаемся, что навигация активна
        nav.speed = patrolSpeed;

        // Если игрок в обычном зрении и мы не исследуем его - побег
        if (playerVisible && !isInvestigatingPlayer)
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
        nav.Resume(); // Убеждаемся, что навигация активна

        // ЛОГИКА ДЛЯ ИГРОКА (отдельно от обычных объектов)
        if (isInvestigatingPlayer)
        {
            // Обновляем проверку области интереса заново
            bool playerStillInInterest = vision.IsPlayerInInterestArea();
            bool playerNowVisible = vision.CanSeePlayer(); // Проверяем обычное зрение
            player = vision.GetPlayer();
            
            // Если подошли близко и видим игрока в обычном зрении - побег!
            if (playerNowVisible)
            {
                isInvestigatingPlayer = false;
                StartFlee();
                return;
            }
            
            if (player != null)
            {
                // Обновляем позицию игрока в реальном времени (используем мировую позицию)
                Vector3 playerWorldPos = player.position;
                noisePosition = playerWorldPos;
                
                // ВАЖНО: всегда обновляем навигацию, даже если игрок на границе области
                nav.Resume(); // Убеждаемся, что навигация активна
                nav.MoveTo(noisePosition);
                
                float distance = Vector3.Distance(transform.position, playerWorldPos);
                
                // Отладка каждую секунду
                if (Time.frameCount % 60 == 0)
                {
                    UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
                    bool hasPath = agent != null && agent.hasPath;
                    bool pathPending = agent != null && agent.pathPending;
                    float remainingDistance = agent != null ? agent.remainingDistance : 0f;
                    bool isStopped = agent != null && agent.isStopped;
                }
                
                // Если игрок ушел из области интереса - переходим в Wait только после небольшой задержки
                if (!playerStillInInterest)
                {
                    // Даем время дойти до последней известной позиции
                    if (nav.ReachedDestination(1.0f)) // Увеличиваем порог для проверки
                    {
                        isInvestigatingPlayer = false;
                        EnterWaitPhase();
                    }
                }
                // НЕ переходим в Wait при достижении цели - продолжаем следовать за игроком
                // ИИ должен продолжать следовать за игроком, пока он в области интереса
                // Когда подойдем близко и увидим в обычном зрении - убежим
            }
            else
            {
                // Игрок пропал - переходим в Wait
                isInvestigatingPlayer = false;
                EnterWaitPhase();
            }
        }
        // ЛОГИКА ДЛЯ ОБЫЧНЫХ ОБЪЕКТОВ ИНТЕРЕСА (отдельно от игрока)
        else if (interestTarget != null)
        {
            // Проверяем, виден ли объект еще
            if (vision.TryGetInterestTarget(out Transform interest) && interest == interestTarget)
            {
                // Обновляем позицию объекта
                noisePosition = interestTarget.position;
                nav.MoveTo(noisePosition);
                
                // Проверяем достижение цели
                if (nav.ReachedDestination(0.5f))
                {
                    EnterWaitPhase();
                }
            }
            else
            {
                // Объект пропал - переходим в Wait
                interestTarget = null;
                EnterWaitPhase();
            }
        }
        else
        {
            // Если ни игрок, ни объект интереса не найдены - переходим в Wait
            nav.MoveTo(noisePosition);
            if (nav.ReachedDestination(0.5f))
                EnterWaitPhase();
        }
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
        {
            if (escapeManager != null)
                escapeManager.ChangeColor();
            EnterWaitPhase();
        }       
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
        isInvestigatingPlayer = false;
        interestTarget = null;

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
        isInvestigatingPlayer = false;
        interestTarget = null;
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