using UnityEngine;

/// <summary>
/// Визуализирует текущее состояние ИИ через изменение цвета сферы
/// </summary>
public class AI_StateColorVisualizer : MonoBehaviour
{
    [Header("Визуализация")]
    [Tooltip("Сфера для отображения состояния. Перетащите готовую сферу из сцены")]
    public GameObject stateSphere;
    
    [Tooltip("Renderer для изменения цвета (находится автоматически из stateSphere, если не установлен)")]
    public Renderer stateRenderer;

    [Header("Привязка к кости головы")]
    [Tooltip("Кость головы ИИ (из AI_Vision). Если установлена, сфера будет следовать за ней")]
    public Transform headBone;
    
    [Tooltip("Смещение сферы относительно кости головы (в локальных координатах кости)")]
    public Vector3 headBoneOffset = new Vector3(0, 0.2f, 0);
    
    [Tooltip("Плавное следование за костью головы")]
    public bool smoothFollow = false;
    
    [Tooltip("Скорость плавного следования")]
    [Range(0.1f, 20f)]
    public float followSpeed = 5f;

    [Header("Цвета состояний")]
    [Tooltip("Цвет для патруля")]
    public Color patrolColor = Color.green;
    
    [Tooltip("Цвет для исследования")]
    public Color investigateColor = Color.yellow;
    
    [Tooltip("Цвет для ожидания")]
    public Color waitColor = Color.cyan;
    
    [Tooltip("Цвет для побега")]
    public Color fleeColor = Color.red;
    
    [Tooltip("Цвет для замерзания")]
    public Color freezeColor = Color.magenta;


    private AI_Behavior aiBehavior;
    private string lastState = "";
    private Material instanceMaterial;

    void Awake()
    {
        aiBehavior = GetComponent<AI_Behavior>();
        
        // Пытаемся найти headBone из AI_Vision, если не установлен
        if (headBone == null)
        {
            AI_Vision vision = GetComponent<AI_Vision>();
            if (vision != null)
            {
                // Используем рефлексию для доступа к приватному полю headBone
                var headBoneField = typeof(AI_Vision).GetField("headBone", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (headBoneField != null)
                {
                    headBone = headBoneField.GetValue(vision) as Transform;
                }
            }
        }
    }

    void Start()
    {
        // Инициализируем визуализацию
        InitializeVisualization();
        
        // Устанавливаем начальный цвет
        UpdateColor();
    }

    void Update()
    {
        if (stateRenderer == null || stateSphere == null) return;

        // Обновляем позицию сферы (следуем за костью головы)
        UpdateSpherePosition();

        // Обновляем цвет состояния
        UpdateColor();
    }

    /// <summary>
    /// Инициализирует визуализацию
    /// </summary>
    private void InitializeVisualization()
    {
        // Проверяем, что сфера установлена
        if (stateSphere == null)
        {
            Debug.LogWarning($"[AI_StateColorVisualizer] State Sphere не установлена на {gameObject.name}. Перетащите сферу из сцены в поле State Sphere.");
            return;
        }

        // Находим Renderer в сфере
        if (stateRenderer == null)
        {
            stateRenderer = stateSphere.GetComponent<Renderer>();
        }

        // Если Renderer найден, настраиваем материал
        if (stateRenderer != null)
        {
            // Создаем экземпляр материала, чтобы не менять оригинальный
            if (stateRenderer.material != null)
            {
                instanceMaterial = new Material(stateRenderer.material);
                stateRenderer.material = instanceMaterial;
            }
        }
        else
        {
            Debug.LogWarning($"[AI_StateColorVisualizer] Renderer не найден на сфере '{stateSphere.name}' на {gameObject.name}.");
        }
    }

    /// <summary>
    /// Обновляет позицию сферы (следует за костью головы)
    /// </summary>
    private void UpdateSpherePosition()
    {
        if (stateSphere == null) return;

        Vector3 targetPosition;

        // Если есть кость головы, следуем за ней
        if (headBone != null)
        {
            // Используем мировое пространство для смещения относительно кости головы
            targetPosition = headBone.position + headBone.TransformDirection(headBoneOffset);
        }
        else
        {
            // Если кость головы не установлена, сфера остается на своей позиции
            // (позиция должна быть установлена вручную в сцене)
            return;
        }

        // Плавное или мгновенное перемещение
        if (smoothFollow)
        {
            stateSphere.transform.position = Vector3.Lerp(
                stateSphere.transform.position, 
                targetPosition, 
                Time.deltaTime * followSpeed
            );
        }
        else
        {
            stateSphere.transform.position = targetPosition;
        }
    }


    /// <summary>
    /// Обновляет цвет в зависимости от состояния
    /// </summary>
    private void UpdateColor()
    {
        if (aiBehavior == null || instanceMaterial == null) return;

        string currentState = aiBehavior.GetCurrentStateName();
        
        // Обновляем цвет только если состояние изменилось
        if (currentState != lastState)
        {
            lastState = currentState;
            Color stateColor = GetColorForState(currentState);
            ApplyColor(stateColor);
        }
    }

    /// <summary>
    /// Получает цвет для состояния
    /// </summary>
    private Color GetColorForState(string state)
    {
        switch (state)
        {
            case "Patrol":
                return patrolColor;
            case "Investigate":
                return investigateColor;
            case "Wait":
            case "LookAround":
                return waitColor;
            case "Flee":
            case "Run":
            case "Turn":
                return fleeColor;
            case "Freeze":
                return freezeColor;
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// Применяет цвет к материалу
    /// </summary>
    private void ApplyColor(Color color)
    {
        if (instanceMaterial == null) return;

        instanceMaterial.SetColor("_Color", color);
        
        // Для Unlit шейдера эмиссия не нужна, но для Standard - нужна
        if (instanceMaterial.HasProperty("_EmissionColor"))
        {
            instanceMaterial.SetColor("_EmissionColor", color);
        }
    }


    /// <summary>
    /// Показать индикатор принудительно
    /// </summary>
    public void ShowIndicator()
    {
        if (stateSphere != null)
        {
            stateSphere.SetActive(true);
        }
        else if (stateRenderer != null)
        {
            stateRenderer.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Скрыть индикатор принудительно
    /// </summary>
    public void HideIndicator()
    {
        if (stateSphere != null)
        {
            stateSphere.SetActive(false);
        }
        else if (stateRenderer != null)
        {
            stateRenderer.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Установить кастомный цвет
    /// </summary>
    public void SetCustomColor(Color color)
    {
        ApplyColor(color);
    }

    void OnDestroy()
    {
        // Удаляем созданный экземпляр материала
        if (instanceMaterial != null)
        {
            Destroy(instanceMaterial);
        }
    }
}

