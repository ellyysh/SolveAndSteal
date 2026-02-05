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
        
        // Автоматический поиск камеры, если playerHead не установлен
        if (playerHead == null)
        {
            FindPlayerHead();
        }
    }

    void Start()
    {
        // Повторная попытка найти камеру в Start (на случай, если она появилась позже)
        if (playerHead == null)
        {
            FindPlayerHead();
        }
    }

    /// <summary>
    /// Автоматически находит камеру игрока (Main Camera или камеру в XR Origin)
    /// </summary>
    private void FindPlayerHead()
    {
        // Сначала пробуем найти Main Camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            playerHead = mainCamera.transform;
            Debug.Log($"[AI_Vision] Найден Main Camera: {mainCamera.name} на {gameObject.name}");
            return;
        }

        // Если Main Camera не найден, ищем любую камеру с тегом "MainCamera"
        GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (cameraObj != null)
        {
            playerHead = cameraObj.transform;
            Debug.Log($"[AI_Vision] Найден объект с тегом MainCamera: {cameraObj.name} на {gameObject.name}");
            return;
        }

        // Если не нашли, ищем камеру в XR Origin (через рефлексию, чтобы не требовать зависимости)
        try
        {
            System.Type xrOriginType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XROrigin, Unity.XR.Interaction.Toolkit");
            if (xrOriginType != null)
            {
                UnityEngine.Object xrOrigin = FindFirstObjectByType(xrOriginType);
                if (xrOrigin != null)
                {
                    var cameraProperty = xrOriginType.GetProperty("Camera");
                    if (cameraProperty != null)
                    {
                        Camera xrCamera = cameraProperty.GetValue(xrOrigin) as Camera;
                        if (xrCamera != null)
                        {
                            playerHead = xrCamera.transform;
                            Debug.Log($"[AI_Vision] Найден XR Origin Camera: {xrCamera.name} на {gameObject.name}");
                            return;
                        }
                    }
                }
            }
        }
        catch
        {
            // XR Toolkit не установлен или недоступен - пропускаем
        }

        // Последняя попытка - найти любую активную камеру
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera cam in cameras)
        {
            if (cam.enabled && cam.gameObject.activeInHierarchy)
            {
                playerHead = cam.transform;
                Debug.Log($"[AI_Vision] Автоматически найден playerHead: {cam.name} на {gameObject.name}");
                return;
            }
        }

        Debug.LogWarning($"[AI_Vision] Не удалось найти камеру игрока на {gameObject.name}. Установите playerHead вручную в инспекторе.");
    }

    /// <summary>
    /// Получает мировую позицию головы игрока
    /// transform.position всегда возвращает мировую позицию, даже для дочерних объектов
    /// </summary>
    private Vector3 GetPlayerWorldPosition()
    {
        if (playerHead == null) return Vector3.zero;
        
        // transform.position всегда возвращает мировую позицию в Unity
        // Даже если объект является дочерним, position возвращает мировые координаты
        Vector3 worldPos = playerHead.position;
        
        // Отладка: проверяем, что позиция валидна
        if (float.IsNaN(worldPos.x) || float.IsNaN(worldPos.y) || float.IsNaN(worldPos.z))
        {
            Debug.LogError($"[AI_Vision] Некорректная позиция игрока на {gameObject.name}: {worldPos}");
            return Vector3.zero;
        }
        
        return worldPos;
    }

    public bool CanSeePlayer()
    {
        if (playerHead == null) return false;

        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
        Vector3 playerWorldPos = GetPlayerWorldPosition();
        Vector3 dirToPlayer = (playerWorldPos - eyePosition).normalized;
        float distToPlayer = Vector3.Distance(eyePosition, playerWorldPos);

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

    public Transform GetPlayer()
    {
        // Если playerHead потерян, пытаемся найти снова
        if (playerHead == null)
        {
            FindPlayerHead();
        }
        return playerHead;
    }

    /// <summary>
    /// Проверяет, находится ли игрок в области интереса (interestRadius и interestViewAngle)
    /// </summary>
    public bool IsPlayerInInterestArea()
    {
        if (playerHead == null || interestRadius <= 0f)
        {
            if (playerHead == null && Time.frameCount % 300 == 0) // Каждые 5 секунд
            {
                Debug.LogWarning($"[AI_Vision] playerHead == null на {gameObject.name}. Попытка найти камеру...");
                FindPlayerHead();
            }
            return false;
        }

        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
        Vector3 playerWorldPos = GetPlayerWorldPosition();
        
        if (playerWorldPos == Vector3.zero)
            return false;
            
        Vector3 dirToPlayer = (playerWorldPos - eyePosition).normalized;
        float distToPlayer = Vector3.Distance(eyePosition, playerWorldPos);

        // Проверяем расстояние
        if (distToPlayer > interestRadius)
            return false;

        // Проверяем угол обзора интереса
        Vector3 forward = headBone != null ? headBone.forward : transform.forward;
        float angle = Vector3.Angle(forward, dirToPlayer);

        if (angle > interestViewAngle / 2f)
            return false;

        // Проверяем препятствия
        if (Physics.Raycast(eyePosition, dirToPlayer, distToPlayer, obstacleMask))
            return false;

        return true;
    }

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
            Vector3 playerWorldPos = GetPlayerWorldPosition();
            
            if (CanSeePlayer())
            {
                Gizmos.color = Color.yellow;
            }
            else if (IsPlayerInInterestArea())
            {
                Gizmos.color = Color.cyan; // Игрок в области интереса
            }
            else
            {
                Gizmos.color = Color.gray;
            }
            Gizmos.DrawLine(eyePosition, playerWorldPos);
        }
    }
}
