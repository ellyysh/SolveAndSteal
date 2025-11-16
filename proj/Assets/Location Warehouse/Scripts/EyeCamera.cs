using UnityEngine;

public class EyeCamera : MonoBehaviour
{
    [Header("Vision Settings")]
    public float viewDistance = 12f;
    public float viewAngle = 45f;
    public LayerMask targetLayer;
    public LayerMask obstacleMask;

    [Header("Distraction Settings")]
    public LayerMask distractionLayer;
    public float distractionStayTime = 1.2f;
    public float distractionRotateSpeed = 3f;
    [Tooltip("Layer name to set on a detected distraction object after validation. Leave empty to skip.")]
    public string distractionConsumedLayerName = "";
    [Tooltip("If true, applies the new layer to the entire hierarchy of the distraction object.")]
    public bool distractionChangeLayerRecursively = true;
    [Tooltip("Change layer only on the specific hit collider's GameObject (recommended).")]
    public bool distractionChangeOnlyHitCollider = true;

    [Header("Scan Settings")]
    public Transform[] scanPoints;
    public float scanMoveSpeed = 2f;
    public float scanStayTime = 1.4f;

    [Header("Search Settings (after losing target)")]
    public Transform[] searchPoints;
    public float searchMoveSpeed = 4f;
    public float searchStayTime = 0.4f;

    [Header("Search Strobe")]
    public float strobeShake = 3f;
    public float strobeSpeed = 15f;

    [Header("Micro Shake (Scan Only)")]
    public float microShake = 0.5f;
    public float microShakeSpeed = 4f;

    [Header("Focus Settings")]
    public float focusSpeed = 5f;
    public float focusBoostAngle = 10f;
    public float focusBoostMultiplier = 2f;

    [Header("Relax Settings")]
    public float relaxSpeed = 2f;

    [Header("Pupil Controller")]
    public PupilController pupilController;

    [Header("Receivers")]
    [Tooltip("Явный список ИИ, которым камера будет передавать позицию цели.")]
    public AI_Behavior[] listeners;

    [Header("Alarm")]
    [Tooltip("If true, when a target is detected, report its position to all AI to investigate.")]
    public bool alarmOnDetect = true;
    [Tooltip("Seconds between reports to avoid spamming.")]
    public float alarmCooldown = 2f;

    [Header("Detection Tuning")]
    [Tooltip("0 = detect every frame; >0 = seconds between visibility checks")]
    public float detectInterval = 0f;

    private int currentIndex = 0;
    private float stayTimer = 0f;
    private float shakeTimer = 0f;

    private int searchIndex = 0;
    private float searchTimer = 0f;
    private float strobeTimer = 0f;

    private float detectTimer = 0f;
    private float lastAlarmTime = -999f;

    private Transform target;
    private Transform distractionTarget;
    private float distractionTimer = 0f;

    private enum State { Scan, Focus, Search, Relax, Distract }
    private State state = State.Scan;

    // -----------------------------
    // HELPERS
    // -----------------------------
    private void RotateTowards(Vector3 direction, float speed)
    {
        if (direction.sqrMagnitude < 0.000001f) return;
        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, speed * Time.deltaTime);
    }

    private bool TryFindVisible(LayerMask layer, float halfAngleDeg, out Transform found, out Collider foundCollider)
    {
        found = null;
        foundCollider = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, viewDistance, layer);
        for (int i = 0; i < hits.Length; i++)
        {
            Vector3 dir = (hits[i].transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dir) > halfAngleDeg) continue;
            if (Physics.Raycast(transform.position, dir, viewDistance, obstacleMask)) continue;
            found = hits[i].transform;
            foundCollider = hits[i];
            return true;
        }
        return false;
    }

    private void SetLayerOnTransform(Transform t, int layer, bool recursively)
    {
        if (t == null) return;
        if (!recursively)
        {
            t.gameObject.layer = layer;
            return;
        }
        foreach (var tr in t.GetComponentsInChildren<Transform>(true))
        {
            tr.gameObject.layer = layer;
        }
    }

    private void RaisePlayerSighting(Vector3 targetPosition)
    {
        if (!alarmOnDetect) return;
        if (Time.time - lastAlarmTime < alarmCooldown) return;
        lastAlarmTime = Time.time;

        // Сообщаем явным слушателям о позиции цели, чтобы они пошли проверять (Investigate)
        if (listeners == null || listeners.Length == 0) return;
        for (int i = 0; i < listeners.Length; i++)
        {
            var ai = listeners[i];
            if (ai == null) continue;
            ai.HearNoise(targetPosition);
        }
    }

    // Unified patrol routine for Scan/Search
    private bool PatrolUpdate(
        Transform[] points,
        float moveSpeed,
        float pointStayTime,
        float jitterAmplitude,
        float jitterSpeed,
        float reachedAngleDeg,
        bool loop,
        ref int index,
        ref float localStayTimer,
        ref float localJitterTimer
    )
    {
        if (points == null || points.Length == 0) return true;

        Transform p = points[Mathf.Clamp(index, 0, points.Length - 1)];
        Vector3 dir = (p.position - transform.position).normalized;

        if (jitterAmplitude > 0f)
        {
            localJitterTimer += Time.deltaTime * jitterSpeed;
            float jitter = Mathf.Sin(localJitterTimer) * jitterAmplitude;
            dir = Quaternion.Euler(0f, jitter, 0f) * dir;
        }

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, moveSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRot) < reachedAngleDeg)
        {
            localStayTimer += Time.deltaTime;
            if (localStayTimer >= pointStayTime)
            {
                localStayTimer = 0f;
                index++;
                if (loop)
                {
                    index %= points.Length;
                }
                else if (index >= points.Length)
                {
                    return true; // finished linear patrol
                }
            }
        }

        return false;
    }

    void Update()
    {
        // Throttle detection if requested
        if (detectInterval <= 0f)
        {
            DetectTarget();
            DetectDistraction();
        }
        else
        {
            detectTimer += Time.deltaTime;
            if (detectTimer >= detectInterval)
            {
                detectTimer = 0f;
                DetectTarget();
                DetectDistraction();
            }
        }
        UpdatePupilState();

        switch (state)
        {
            case State.Scan: ScanUpdate(); break;
            case State.Focus: FocusUpdate(); break;
            case State.Search: SearchUpdate(); break;
            case State.Relax: RelaxUpdate(); break;
            case State.Distract: DistractUpdate(); break;
        }
    }

    // -----------------------------
    // DETECT DISTRACTION
    // -----------------------------
    void DetectDistraction()
    {
        if (state == State.Focus || state == State.Search) return;

        if (distractionTarget != null) return;

        if (TryFindVisible(distractionLayer, viewAngle, out Transform found, out Collider hitCol))
        {
            if (!string.IsNullOrEmpty(distractionConsumedLayerName))
            {
                int newLayer = LayerMask.NameToLayer(distractionConsumedLayerName);
                if (newLayer != -1)
                {
                    // Always prefer changing only the encountered collider's GameObject
                    if (hitCol != null)
                    {
                        hitCol.gameObject.layer = newLayer;
                    }
                    // If hitCol is somehow null, fall back to changing just the found transform (non-recursive)
                    else
                    {
                        SetLayerOnTransform(found, newLayer, false);
                    }
                }
            }
            distractionTarget = found;
            distractionTimer = 0f;
            state = State.Distract;
            return;
        }
    }


    // -----------------------------
    // DISTRACTION BEHAVIOUR
    // -----------------------------
    void DistractUpdate()
    {
        if (distractionTarget == null)
        {
            state = State.Scan;
            return;
        }

        Vector3 dir = (distractionTarget.position - transform.position).normalized;
        RotateTowards(dir, distractionRotateSpeed);

        distractionTimer += Time.deltaTime;

        if (distractionTimer >= distractionStayTime)
        {
            distractionTarget = null;
            state = State.Scan;
        }
    }


    // -----------------------------
    // VISION DETECTION (MAIN TARGET)
    // -----------------------------
    void DetectTarget()
    {
        target = null;

        if (TryFindVisible(targetLayer, viewAngle * 0.5f, out Transform found, out Collider _))
        {
            target = found;
            distractionTarget = null;
            state = State.Focus;
            // Передаём позицию игрока всем ИИ для начала поиска
            RaisePlayerSighting(target.position);
            return;
        }

        if (state == State.Focus)
        {
            if (searchPoints != null && searchPoints.Length > 0)
            {
                state = State.Search;
                searchIndex = 0;
                searchTimer = 0f;
                strobeTimer = 0f;
            }
            else
            {
                state = State.Relax;
            }
        }
    }


    // -----------------------------
    // SCAN
    // -----------------------------
    void ScanUpdate()
    {
        if (scanPoints == null || scanPoints.Length == 0) return;
        PatrolUpdate(
            scanPoints,
            scanMoveSpeed,
            scanStayTime,
            microShake,
            microShakeSpeed,
            4f,
            true,
            ref currentIndex,
            ref stayTimer,
            ref shakeTimer
        );
    }

    // -----------------------------
    // SEARCH
    // -----------------------------
    void SearchUpdate()
    {
        if (searchPoints.Length == 0)
        {
            state = State.Relax;
            return;
        }

        bool finished = PatrolUpdate(
            searchPoints,
            searchMoveSpeed,
            searchStayTime,
            strobeShake,
            strobeSpeed,
            5f,
            false,
            ref searchIndex,
            ref searchTimer,
            ref strobeTimer
        );
        if (finished)
        {
            state = State.Relax;
            return;
        }
    }

    // Noise-based distraction removed by design (only visual distraction allowed once)

    // -----------------------------
    // FOCUS
    // -----------------------------
    void FocusUpdate()
    {
        if (target == null) return;

        Vector3 dir = (target.position - transform.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dir);

        float angle = Quaternion.Angle(transform.rotation, targetRot);
        float speed = (angle > focusBoostAngle)
            ? focusSpeed * focusBoostMultiplier
            : focusSpeed;

        RotateTowards(dir, speed);
    }

    // -----------------------------
    // RELAX
    // -----------------------------
    void RelaxUpdate()
    {
        if (scanPoints.Length == 0)
        {
            state = State.Scan;
            return;
        }

        Transform closest = scanPoints[currentIndex];
        Vector3 dir = (closest.position - transform.position).normalized;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, relaxSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRot) < 3f)
            state = State.Scan;
    }


    // -----------------------------
    // PUPIL STATE
    // -----------------------------
    void UpdatePupilState()
    {
        if (pupilController == null) return;

        switch (state)
        {
            case State.Scan: pupilController.SetNormal(); break;
            case State.Focus: pupilController.SetNarrow(); break;
            case State.Search: pupilController.SetNarrow(); break;
            case State.Distract: pupilController.SetWide(); break;
            case State.Relax: pupilController.SetWide(); break;
        }
    }

    // -----------------------------
    // GIZMOS
    // -----------------------------
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.yellow;
        if (scanPoints != null && scanPoints.Length > 0)
        {
            Gizmos.DrawLine(transform.position, scanPoints[currentIndex].position);
            Gizmos.DrawSphere(scanPoints[currentIndex].position, 0.1f);
        }

        Gizmos.color = Color.red;
        if (searchPoints != null && searchPoints.Length > 0)
        {
            Gizmos.DrawSphere(searchPoints[Mathf.Clamp(searchIndex, 0, searchPoints.Length - 1)].position, 0.07f);
        }

        Gizmos.color = Color.green;
        if (distractionTarget != null)
            Gizmos.DrawSphere(distractionTarget.position, 0.1f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * viewDistance);
    }
}
