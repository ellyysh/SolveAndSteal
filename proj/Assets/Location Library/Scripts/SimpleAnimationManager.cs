using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class SimpleAnimationManager : MonoBehaviour
{
    [Header("Триггеры Animator")]
    public string walkTrigger = "walk";

    [Tooltip("Если idle-анимаций несколько — перечисли триггеры тут (например: idleleg, idlehand, idle2).")]
    public string[] idleTriggers;

    public enum IdleSelectMode { First, Random, Cycle }
    [Tooltip("Как выбирать idle, если их несколько.")]
    public IdleSelectMode idleSelectMode = IdleSelectMode.Random;

    [Header("Определение движения")]
    [Tooltip("Считаем, что персонаж идёт, если скорость > этого порога.")]
    public float moveSpeedThreshold = 0.05f;

    [Header("Пауза анимации")]
    [Tooltip("Если время в игре остановлено (Time.timeScale==0), аниматор будет остановлен.")]
    public bool freezeAnimatorWhenTimeStopped = true;

    [Header("Приоритетные анимации")]
    [Tooltip("Если true — во время обнаружения игрока (пока проигрывается 'shit') менеджер не будет переключать walk/idle.")]
    public bool lockWhileDetecting = true;

    private Animator _animator;
    private NavMeshAgent _agent;
    private Simple_AI_Behavior _ai;
    private Vector3 _lastPos;
    private bool _initialized;
    private bool _isWalking;
    private int _idleCycleIndex;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _ai = GetComponent<Simple_AI_Behavior>();
        _lastPos = transform.position;
        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;

        if (freezeAnimatorWhenTimeStopped && Time.timeScale == 0f)
        {
            _animator.speed = 0f;
            return;
        }
        else
        {
            _animator.speed = 1f;
        }

        if (lockWhileDetecting && _ai != null && _ai.IsInDetectSequence)
            return;

        bool walkingNow = IsMoving();
        if (walkingNow == _isWalking)
            return;

        _isWalking = walkingNow;
        PlayState(_isWalking ? "Walk" : "Idle");
    }

    private bool IsMoving()
    {
        float speed;

        // Если есть NavMeshAgent — берём скорость из него.
        if (_agent != null)
        {
            speed = _agent.velocity.magnitude;
        }
        else
        {
            // Иначе считаем скорость по изменению позиции (работает и без NavMesh).
            Vector3 pos = transform.position;
            float dt = Time.deltaTime;
            speed = dt > 0f ? (pos - _lastPos).magnitude / dt : 0f;
            _lastPos = pos;
        }

        return speed > moveSpeedThreshold;
    }

    private void PlayState(string state)
    {
        if (_animator == null) return;

        // Сбросим триггеры, чтобы не "залипали"
        if (!string.IsNullOrEmpty(walkTrigger)) _animator.ResetTrigger(walkTrigger);
        ResetIdleTriggers();

        if (state == "Walk")
        {
            if (!string.IsNullOrEmpty(walkTrigger)) _animator.SetTrigger(walkTrigger);
        }
        else
        {
            string t = PickIdleTrigger();
            if (!string.IsNullOrEmpty(t))
                _animator.SetTrigger(t);
        }
    }

    private void ResetIdleTriggers()
    {
        if (idleTriggers != null && idleTriggers.Length > 0)
        {
            for (int i = 0; i < idleTriggers.Length; i++)
            {
                string t = idleTriggers[i];
                if (!string.IsNullOrEmpty(t))
                    _animator.ResetTrigger(t);
            }
            return;
        }

    }

    private string PickIdleTrigger()
    {
        if (idleTriggers == null || idleTriggers.Length == 0)
            return null;

        int count = idleTriggers.Length;
        switch (idleSelectMode)
        {
            case IdleSelectMode.First:
                return idleTriggers[0];
            case IdleSelectMode.Cycle:
                if (_idleCycleIndex < 0) _idleCycleIndex = 0;
                string t = idleTriggers[_idleCycleIndex % count];
                _idleCycleIndex = (_idleCycleIndex + 1) % count;
                return t;
            case IdleSelectMode.Random:
            default:
                return idleTriggers[Random.Range(0, count)];
        }
    }

    // Можно вызвать извне, если нужно принудительно
    public void ForceWalk() { _isWalking = true; PlayState("Walk"); }
    public void ForceIdle() { _isWalking = false; PlayState("Idle"); }
}

