using UnityEngine;

public class EyeCamera : MonoBehaviour
{
    [Header("Vision Settings")]
    public float viewDistance = 12f;
    public float viewAngle = 45f;
    public LayerMask targetLayer;
    public LayerMask obstacleMask;

    [Header("Scan Settings")]
    public Transform[] scanPoints;
    public float scanPointStayTime = 1.5f;
    public float scanSpeed = 2f;
    public float microShake = 1f;
    public float microShakeSpeed = 4f;

    [Header("Focus Settings")]
    public float focusSpeed = 4f;
    public float relaxSpeed = 1.8f;

    [Header("Pupil Controller")]
    public PupilController pupilController; // <-- ссылка на PupilController

    private int currentScanIndex = 0;
    private float stayTimer = 0;

    private Transform target;
    private float shakeTimer = 0f;
    private Quaternion baseRotation;

    private enum State { Scan, Focus, Relax }
    private State currentState = State.Scan;

    void Start()
    {
        baseRotation = transform.rotation;

        if (pupilController == null)
            Debug.LogWarning("PupilController not assigned!");
    }

    void Update()
    {
        DetectTarget();
        UpdatePupilState(); // <-- теперь управляем через PupilController

        switch (currentState)
        {
            case State.Scan: ScanUpdate(); break;
            case State.Focus: FocusUpdate(); break;
            case State.Relax: RelaxUpdate(); break;
        }
    }

    void UpdatePupilState()
    {
        if (pupilController == null) return;

        switch (currentState)
        {
            case State.Scan:
                pupilController.SetNormal();
                break;
            case State.Focus:
                pupilController.SetNarrow();
                break;
            case State.Relax:
                pupilController.SetWide(); // слегка расширен в режиме relax
                break;
        }
    }

    void DetectTarget()
    {
        target = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, viewDistance, targetLayer);

        foreach (var h in hits)
        {
            Vector3 dir = (h.transform.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, dir) < viewAngle / 2)
            {
                if (!Physics.Raycast(transform.position, dir, viewDistance, obstacleMask))
                {
                    target = h.transform;
                    currentState = State.Focus;
                    return;
                }
            }
        }

        if (currentState == State.Focus)
            currentState = State.Relax;
    }

    void ScanUpdate()
    {
        if (scanPoints.Length == 0) return;

        Transform point = scanPoints[currentScanIndex];
        Vector3 dir = (point.position - transform.position).normalized;

        shakeTimer += Time.deltaTime * microShakeSpeed;
        float shake = Mathf.Sin(shakeTimer) * microShake;
        dir = Quaternion.Euler(0, shake, 0) * dir;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, scanSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRot) < 3f)
        {
            stayTimer += Time.deltaTime;
            if (stayTimer >= scanPointStayTime)
            {
                currentScanIndex = (currentScanIndex + 1) % scanPoints.Length;
                stayTimer = 0;
            }
        }
    }

    void FocusUpdate()
    {
        if (target == null) return;

        Vector3 dir = (target.position - transform.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, focusSpeed * Time.deltaTime);
    }

    void RelaxUpdate()
    {
        Quaternion desired = baseRotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, desired, relaxSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, desired) < 0.5f)
            currentState = State.Scan;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * viewDistance);

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.25f);
        float halfAngle = viewAngle * 0.5f;

        Quaternion leftRot = Quaternion.AngleAxis(-halfAngle, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(halfAngle, Vector3.up);

        Vector3 leftDir = leftRot * transform.forward;
        Vector3 rightDir = rightRot * transform.forward;

        Gizmos.DrawRay(transform.position, leftDir * viewDistance);
        Gizmos.DrawRay(transform.position, rightDir * viewDistance);

        int segments = 32;
        Vector3 prevPoint = transform.position + leftDir * viewDistance;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 dir = rot * transform.forward;
            Vector3 nextPoint = transform.position + dir * viewDistance;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        if (Application.isPlaying && scanPoints != null && scanPoints.Length > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, scanPoints[currentScanIndex].position);
            Gizmos.DrawSphere(scanPoints[currentScanIndex].position, 0.1f);
        }
    }
}
