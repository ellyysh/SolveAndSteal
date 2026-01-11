using UnityEngine;
using System.Collections;

public class Simple_AI_Behavior : MonoBehaviour
{
    [Header("Патруль")]
    [Tooltip("Скорость патрулирования.")]
    public float patrolSpeed = 3f;

    [Tooltip("Сколько секунд стоять (idle) на точке перед переходом к следующей.")]
    public float waitAtPoint = 1.0f;

    [Header("Проигрыш")]
    public Canvas youLoseCanvas;
    public GameOverManager gameOverManager;

    [Header("Обнаружение -> GameOver")]
    [Tooltip("Сколько секунд дать анимации обнаружения (trigger 'shit') перед показом GameOver.")]
    public float detectToGameOverDelay = 0.8f;
    public string detectTrigger = "shit";


    private AI_Navigation _nav;
    private AI_Vision _vision;
    private Simple_AI_Vision _simpleVision;
    private Animator _animator;
    private bool _gameOver;
    private float _waitTimer;
    private bool _waiting;
    private Coroutine _detectRoutine;
    private bool _detected;

    public bool IsInDetectSequence => _detected && !_gameOver;

    private void Awake()
    {
        _nav = GetComponent<AI_Navigation>();
        _vision = GetComponent<AI_Vision>();
        _simpleVision = GetComponent<Simple_AI_Vision>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (youLoseCanvas != null)
            youLoseCanvas.gameObject.SetActive(false);

        if (_nav != null)
        {
            _nav.speed = patrolSpeed;
            _nav.GoToNextPoint();
        }
    }

    private void Update()
    {
        if (_gameOver)
            return;

        if (_detected)
            return;

        // Если увидел игрока — сразу GameOver
        bool canSee = _simpleVision != null ? _simpleVision.CanSeePlayer() : (_vision != null && _vision.CanSeePlayer());
        if (canSee)
        {
            OnDetectedPlayer();
            return;
        }

        // Патрулирование по точкам
        if (_nav != null)
        {
            if (_waiting)
            {
                // Стоим на месте, чтобы idle успел проиграться
                _waitTimer -= Time.deltaTime;
                _nav.Stop();
                if (_waitTimer <= 0f)
                {
                    _waiting = false;
                    _nav.Resume();
                    _nav.GoToNextPoint();
                }
                return;
            }

            _nav.Resume();
            _nav.speed = patrolSpeed;

            if (_nav.ReachedDestination())
            {
                if (waitAtPoint > 0f)
                {
                    _waiting = true;
                    _waitTimer = waitAtPoint;
                    _nav.Stop();
                }
                else
                {
                    _nav.GoToNextPoint();
                }
            }
        }
    }

    public void TriggerGameOver()
    {
        if (_gameOver)
            return;

        _gameOver = true;

        // Останавливаем ИИ сразу, чтобы он не продолжал идти по старой цели NavMeshAgent
        if (_nav != null)
            _nav.Stop();

        // Требуем менеджер, который можно перетащить в инспекторе
        if (gameOverManager == null)
        {
            Debug.LogWarning($"[{nameof(Simple_AI_Behavior)}] Не назначен GameOverManager. Перетащи объект с GameOverManager в поле gameOverManager.", this);
            return;
        }

        gameOverManager.TriggerGameOver(youLoseCanvas);
    }

    private void OnDetectedPlayer()
    {
        if (_detected)
            return;

        _detected = true;

        // Останавливаем ИИ сразу
        if (_nav != null)
            _nav.Stop();

        // На всякий случай дергаем анимацию обнаружения тут тоже
        if (_animator != null && !string.IsNullOrEmpty(detectTrigger))
        {
            _animator.ResetTrigger(detectTrigger);
            _animator.SetTrigger(detectTrigger);
        }

        if (_detectRoutine != null)
            StopCoroutine(_detectRoutine);
        _detectRoutine = StartCoroutine(DetectThenGameOver());
    }

    private IEnumerator DetectThenGameOver()
    {
        float delay = Mathf.Max(0f, detectToGameOverDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        TriggerGameOver();
        _detectRoutine = null;
    }
}

