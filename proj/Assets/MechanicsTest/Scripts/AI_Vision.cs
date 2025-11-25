using UnityEngine;

public class AI_Vision : MonoBehaviour
{
    [Header("Зрение")]
    public float viewRadius = 10f;
    [Range(0, 180)]
    public float viewAngle = 60f;
    public LayerMask obstacleMask;

    [Header("Обнаружение объектов интереса")]
    public float interestRadius = 15f;
    [Range(0, 180)]
    public float interestViewAngle = 120f;
    public LayerMask interestLayer;

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

    public bool TryGetInterestTarget(out Transform interest)
    {
        interest = null;

        if (interestRadius <= 0f || interestLayer == 0)
            return false;

        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
        Vector3 forward = headBone != null ? headBone.forward : transform.forward;
        Collider[] hits = Physics.OverlapSphere(eyePosition, interestRadius, interestLayer);

        float closestDistance = Mathf.Infinity;
        foreach (var hit in hits)
        {
            if (hit == null) continue;

            float distance = Vector3.Distance(eyePosition, hit.transform.position);
            Vector3 dirToTarget = (hit.transform.position - eyePosition).normalized;
            float angle = Vector3.Angle(forward, dirToTarget);

            if (angle > interestViewAngle / 2f)
                continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                interest = hit.transform;
            }
        }

        return interest != null;
    }

    private void OnDrawGizmos()
    {
        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
        Vector3 forward = headBone != null ? headBone.forward : transform.forward;
        float baseViewRadius = currentViewRadius > 0f ? currentViewRadius : viewRadius;
        float baseViewAngle = currentViewAngle > 0f ? currentViewAngle : viewAngle;
        float interestAngle = interestViewAngle > 0f ? interestViewAngle : baseViewAngle;

        Gizmos.color = isChasing ? Color.red : Color.green;
        Gizmos.DrawWireSphere(eyePosition, baseViewRadius);

        Vector3 leftDir = Quaternion.Euler(0, -baseViewAngle / 2f, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, baseViewAngle / 2f, 0) * forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(eyePosition, eyePosition + leftDir * baseViewRadius);
        Gizmos.DrawLine(eyePosition, eyePosition + rightDir * baseViewRadius);

        if (interestRadius > 0f)
        {
            Vector3 interestLeftDir = Quaternion.Euler(0, -interestAngle / 2f, 0) * forward;
            Vector3 interestRightDir = Quaternion.Euler(0, interestAngle / 2f, 0) * forward;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(eyePosition, interestRadius);
            Gizmos.DrawLine(eyePosition, eyePosition + interestLeftDir * interestRadius);
            Gizmos.DrawLine(eyePosition, eyePosition + interestRightDir * interestRadius);
        }

        if (playerHead != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.yellow : Color.gray;
            Gizmos.DrawLine(eyePosition, playerHead.position);
        }
    }
}
