using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Simple_AI_Vision : MonoBehaviour
{
    [Header("Зрение")]
    public float viewRadius = 10f;
    [Range(0, 180)] public float viewAngle = 60f;
    public LayerMask obstacleMask;

    [Header("VR / Игрок")]
    [Tooltip("Обычно это Main Camera (голова игрока). Если не задано — найдётся автоматически.")]
    public Transform playerHead;

    [Tooltip("Откуда смотреть (по высоте).")]
    public float eyeHeight = 1.5f;

    [Tooltip("Если назначено — направление взгляда берётся из этой кости, иначе из transform.forward.")]
    public Transform headBone;

    [Header("Анимация при обнаружении")]
    [Tooltip("Trigger в Animator, который срабатывает при первом обнаружении игрока.")]
    public string detectTrigger = "shit";

    private Animator _animator;
    private bool _wasSeeingPlayer;

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (playerHead == null)
            TryFindPlayerHead();
    }

    private void Update()
    {
        bool seeing = CanSeePlayer();

        // Первый кадр обнаружения
        if (seeing && !_wasSeeingPlayer)
        {
            if (!string.IsNullOrEmpty(detectTrigger) && _animator != null)
            {
                _animator.ResetTrigger(detectTrigger);
                _animator.SetTrigger(detectTrigger);
            }
        }

        _wasSeeingPlayer = seeing;
    }

    public bool CanSeePlayer()
    {
        if (playerHead == null)
        {
            TryFindPlayerHead();
            if (playerHead == null) return false;
        }

        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 playerPos = playerHead.position;

        Vector3 toPlayer = playerPos - eyePos;
        float dist = toPlayer.magnitude;
        if (dist > viewRadius) return false;

        Vector3 dir = dist > 0.0001f ? (toPlayer / dist) : Vector3.zero;
        if (dir == Vector3.zero) return false;

        Vector3 forward = headBone != null ? headBone.forward : transform.forward;
        float angle = Vector3.Angle(forward, dir);
        if (angle > viewAngle * 0.5f) return false;

        // Нет препятствий между глазами и игроком
        if (Physics.Raycast(eyePos, dir, dist, obstacleMask))
            return false;

        return true;
    }

    private void TryFindPlayerHead()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            playerHead = cam.transform;
            return;
        }

        GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (camObj != null)
        {
            playerHead = camObj.transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 forward = headBone != null ? headBone.forward : transform.forward;

        // Радиус обзора
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(eyePos, viewRadius);

        // Границы угла обзора
        float halfAngle = viewAngle * 0.5f;
        Vector3 leftDir = Quaternion.Euler(0f, -halfAngle, 0f) * forward;
        Vector3 rightDir = Quaternion.Euler(0f, halfAngle, 0f) * forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(eyePos, eyePos + leftDir.normalized * viewRadius);
        Gizmos.DrawLine(eyePos, eyePos + rightDir.normalized * viewRadius);
        Gizmos.DrawLine(eyePos, eyePos + forward.normalized * viewRadius);

        // Линия до игрока (если найден)
        if (playerHead != null)
        {
            bool seeing = Application.isPlaying ? CanSeePlayer() : false;
            Gizmos.color = seeing ? Color.yellow : Color.gray;
            Gizmos.DrawLine(eyePos, playerHead.position);
        }
    }
}

