using UnityEngine;

public class AI_Vision : MonoBehaviour
{
    [Header("Зрение")]
    public float viewRadius = 10f;      // Радиус в обычном состоянии
    [Range(0, 180)]
    public float viewAngle = 60f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;

    [Header("Отладка")]
    public bool showDebug = true;             // Можно выключить в инспекторе

    private Transform player;
    private float currentViewRadius;
    private float currentViewAngle;
    private bool isChasing;

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        currentViewRadius = viewRadius;
        currentViewAngle = viewAngle;
    }

    // Установить режим погони
    public void SetChaseMode(bool chasing)
    {
        isChasing = chasing;
        currentViewRadius = chasing ? viewRadius * 2 : viewRadius;
        currentViewAngle = chasing ? viewAngle * 2 : viewAngle;
    }

    public bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer < currentViewRadius)
        {
            float angle = Vector3.Angle(transform.forward, dirToPlayer);
            if (angle < viewAngle / 2)
            {
                if (!Physics.Raycast(transform.position + Vector3.up * 1.5f, dirToPlayer, distToPlayer, obstacleMask))
                    return true;
            }
        }
        return false;
    }

    public Transform GetPlayer() => player;

    // --- Визуализация зрения ---
    private void OnDrawGizmos()
    {
        if (!showDebug) return;

        // Цвет в зависимости от состояния
        Gizmos.color = isChasing ? Color.red : Color.green;

        // Радиус текущего обзора
        Gizmos.DrawWireSphere(transform.position, currentViewRadius);

        // Углы зрения
        Vector3 leftDir = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;

        Gizmos.color = isChasing ? new Color(1f, 0.5f, 0f) : Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + leftDir * currentViewRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * currentViewRadius);

        // Если есть игрок — нарисуем луч к нему
        if (Application.isPlaying && player != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.yellow : Color.gray;
            Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, player.position + Vector3.up * 1.5f);
        }
    }
}
