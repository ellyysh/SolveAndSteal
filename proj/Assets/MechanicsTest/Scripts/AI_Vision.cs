using UnityEngine;

public class AI_Vision : MonoBehaviour
{
    [Header("Зрение")]
    public float viewRadius = 10f;
    [Range(0, 180)]
    public float viewAngle = 60f;
    public LayerMask obstacleMask;

    [Header("VR")]
    public Transform playerHead;

    [Header("Кость головы (для анимации)")]
    public Transform headBone; // <-- сюда перетащи HeadBone из модели

    private float currentViewRadius;
    private float currentViewAngle;
    private bool isChasing;

    void Awake()
    {
        currentViewRadius = viewRadius;
        currentViewAngle = viewAngle;
    }

    public void SetChaseMode(bool chasing)
    {
        isChasing = chasing;
        currentViewRadius = chasing ? viewRadius * 2 : viewRadius;
        currentViewAngle = chasing ? viewAngle * 2 : viewAngle;
    }

    public bool CanSeePlayer()
    {
        if (playerHead == null) return false;

        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
        Vector3 dirToPlayer = (playerHead.position - eyePosition).normalized;
        float distToPlayer = Vector3.Distance(eyePosition, playerHead.position);

        // Направление взгляда — из головы, если она есть
        Vector3 forward = headBone != null ? headBone.forward : transform.forward;

        float angle = Vector3.Angle(forward, dirToPlayer);

        if (distToPlayer < currentViewRadius && angle < currentViewAngle / 2f)
        {
            if (!Physics.Raycast(eyePosition, dirToPlayer, distToPlayer, obstacleMask))
                return true;
        }

        return false;
    }

    public Transform GetPlayer() => playerHead;

    private void OnDrawGizmos()
    {
        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
        Vector3 forward = headBone != null ? headBone.forward : transform.forward;

        Gizmos.color = isChasing ? Color.red : Color.green;
        Gizmos.DrawWireSphere(eyePosition, viewRadius);

        Vector3 leftDir = Quaternion.Euler(0, -currentViewAngle / 2f, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, currentViewAngle / 2f, 0) * forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(eyePosition, eyePosition + leftDir * currentViewRadius);
        Gizmos.DrawLine(eyePosition, eyePosition + rightDir * currentViewRadius);

        if (playerHead != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.yellow : Color.gray;
            Gizmos.DrawLine(eyePosition, playerHead.position);
        }
    }
}
