using UnityEngine;

public class AI_Vision : MonoBehaviour
{
    [Header("Зрение")]
    public float viewRadius = 10f;
    [Range(0, 180)]
    public float viewAngle = 60f;
    public LayerMask obstacleMask;

    [Header("VR")]
    public Transform playerHead; // <- сюда перетащи Main Camera из XR Origin

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

        // Используем направление взгляда AI
        Vector3 forward = transform.forward;

        var behavior = GetComponent<AI_Behavior>();
        if (behavior != null && behavior.lookTarget != null)
            forward = behavior.lookTarget.forward; // берем поворот глаза

        float angle = Vector3.Angle(forward, dirToPlayer);

        if (distToPlayer < viewRadius && angle < viewAngle / 2)
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

        Vector3 forward = transform.forward;
        var behavior = GetComponent<AI_Behavior>();
        if (behavior != null && behavior.lookTarget != null)
            forward = behavior.lookTarget.forward;

        Gizmos.color = isChasing ? Color.red : Color.green;

        Gizmos.DrawWireSphere(eyePosition, currentViewRadius);

        Vector3 leftDir = Quaternion.Euler(0, -currentViewAngle / 2f, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, currentViewAngle / 2f, 0) * forward;

        Gizmos.color = isChasing ? new Color(1f, 0.5f, 0f) : Color.cyan;
        Gizmos.DrawLine(eyePosition, eyePosition + leftDir * currentViewRadius);
        Gizmos.DrawLine(eyePosition, eyePosition + rightDir * currentViewRadius);

        if (playerHead != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.yellow : Color.gray;
            Gizmos.DrawLine(eyePosition, playerHead.position);
        }
    }
}
