using UnityEngine;

/// <summary>
/// Визуализирует текущее состояние ИИ для игрока без Canvas (использует 3D TextMesh)
/// </summary>
public class AI_StateVisualizer3D : MonoBehaviour
{
    [Header("3D Текст")]
    [Tooltip("TextMesh для отображения состояния (создается автоматически, если не установлен)")]
    public TextMesh stateTextMesh;

    [Header("Настройки позиционирования")]
    [Tooltip("Смещение текста относительно позиции ИИ")]
    public Vector3 textOffset = new Vector3(0, 2.5f, 0);
    
    [Tooltip("Поворачивать ли текст к камере игрока")]
    public bool lookAtCamera = true;

    [Header("Настройки отображения")]
    [Tooltip("Показывать ли состояние всегда или только при определенных условиях")]
    public bool alwaysShow = true;
    
    [Tooltip("Расстояние, на котором текст виден (0 = всегда виден)")]
    public float maxViewDistance = 20f;

    [Header("Цвета состояний")]
    public Color patrolColor = Color.green;
    public Color investigateColor = Color.yellow;
    public Color waitColor = Color.cyan;
    public Color fleeColor = Color.red;
    public Color freezeColor = Color.magenta;

    [Header("Тексты состояний")]
    public string patrolText = "Патруль";
    public string investigateText = "Исследую";
    public string waitText = "Ожидание";
    public string fleeText = "Побег!";
    public string freezeText = "Замер";

    [Header("Настройки текста")]
    [Tooltip("Размер шрифта")]
    public int fontSize = 20;
    
    [Tooltip("Стиль текста")]
    public FontStyle fontStyle = FontStyle.Bold;
    
    [Tooltip("Выравнивание текста")]
    public TextAlignment alignment = TextAlignment.Center;
    
    [Tooltip("Якорь текста")]
    public TextAnchor anchor = TextAnchor.MiddleCenter;

    private AI_Behavior aiBehavior;
    private Camera playerCamera;
    private string lastState = "";
    private GameObject textObject;

    void Awake()
    {
        aiBehavior = GetComponent<AI_Behavior>();
        
        // Автоматически находим камеру игрока
        FindPlayerCamera();
    }

    void Start()
    {
        // Создаем 3D текст, если его нет
        if (stateTextMesh == null)
        {
            Create3DText();
        }

        // Инициализируем текст
        UpdateText();
    }

    void Update()
    {
        if (stateTextMesh == null) return;

        // Обновляем позицию текста
        UpdateTextPosition();

        // Обновляем текст состояния
        UpdateText();

        // Показываем/скрываем текст в зависимости от расстояния
        UpdateTextVisibility();
    }

    /// <summary>
    /// Находит камеру игрока
    /// </summary>
    private void FindPlayerCamera()
    {
        playerCamera = Camera.main;
        
        if (playerCamera == null)
        {
            GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (cameraObj != null)
            {
                playerCamera = cameraObj.GetComponent<Camera>();
            }
        }

        if (playerCamera == null)
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in cameras)
            {
                if (cam.enabled && cam.gameObject.activeInHierarchy)
                {
                    playerCamera = cam;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Создает 3D TextMesh автоматически
    /// </summary>
    private void Create3DText()
    {
        // Создаем объект для текста
        textObject = new GameObject("AI_StateText");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = textOffset;

        // Добавляем TextMesh компонент
        stateTextMesh = textObject.AddComponent<TextMesh>();
        stateTextMesh.text = "Патруль";
        stateTextMesh.fontSize = fontSize;
        stateTextMesh.fontStyle = fontStyle;
        stateTextMesh.alignment = alignment;
        stateTextMesh.anchor = anchor;
        stateTextMesh.color = Color.white;
        
        // Используем встроенный шрифт Unity
        stateTextMesh.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        
        // Настраиваем размер символов
        stateTextMesh.characterSize = 0.1f;
        stateTextMesh.lineSpacing = 1f;
    }

    /// <summary>
    /// Обновляет позицию текста
    /// </summary>
    private void UpdateTextPosition()
    {
        if (stateTextMesh == null || textObject == null) return;

        // Устанавливаем позицию относительно ИИ
        textObject.transform.position = transform.position + textOffset;

        // Поворачиваем к камере
        if (lookAtCamera && playerCamera != null)
        {
            Vector3 directionToCamera = playerCamera.transform.position - textObject.transform.position;
            directionToCamera.y = 0; // Только горизонтальный поворот
            if (directionToCamera != Vector3.zero)
            {
                textObject.transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }

    /// <summary>
    /// Обновляет текст и цвет состояния
    /// </summary>
    private void UpdateText()
    {
        if (aiBehavior == null || stateTextMesh == null) return;

        string currentState = aiBehavior.GetCurrentStateName();
        
        // Обновляем только если состояние изменилось
        if (currentState != lastState)
        {
            lastState = currentState;
            
            // Устанавливаем текст и цвет в зависимости от состояния
            switch (currentState)
            {
                case "Patrol":
                    stateTextMesh.text = patrolText;
                    stateTextMesh.color = patrolColor;
                    break;
                case "Investigate":
                    stateTextMesh.text = investigateText;
                    stateTextMesh.color = investigateColor;
                    break;
                case "Wait":
                case "LookAround":
                    stateTextMesh.text = waitText;
                    stateTextMesh.color = waitColor;
                    break;
                case "Flee":
                case "Run":
                case "Turn": // Поворот во время побега
                    stateTextMesh.text = fleeText;
                    stateTextMesh.color = fleeColor;
                    break;
                case "Freeze":
                    stateTextMesh.text = freezeText;
                    stateTextMesh.color = freezeColor;
                    break;
                default:
                    stateTextMesh.text = currentState;
                    stateTextMesh.color = Color.white;
                    break;
            }
        }
    }

    /// <summary>
    /// Обновляет видимость текста в зависимости от расстояния
    /// </summary>
    private void UpdateTextVisibility()
    {
        if (textObject == null || !alwaysShow) return;

        bool shouldShow = true;

        // Проверяем расстояние до камеры
        if (maxViewDistance > 0 && playerCamera != null)
        {
            float distance = Vector3.Distance(transform.position, playerCamera.transform.position);
            shouldShow = distance <= maxViewDistance;
        }

        // Показываем/скрываем текст
        if (textObject.activeSelf != shouldShow)
        {
            textObject.SetActive(shouldShow);
        }
    }

    /// <summary>
    /// Показать текст принудительно
    /// </summary>
    public void ShowText()
    {
        if (textObject != null)
        {
            textObject.SetActive(true);
        }
    }

    /// <summary>
    /// Скрыть текст принудительно
    /// </summary>
    public void HideText()
    {
        if (textObject != null)
        {
            textObject.SetActive(false);
        }
    }

    /// <summary>
    /// Установить кастомный текст состояния
    /// </summary>
    public void SetCustomState(string text, Color color)
    {
        if (stateTextMesh != null)
        {
            stateTextMesh.text = text;
            stateTextMesh.color = color;
        }
    }

    void OnDestroy()
    {
        // Удаляем созданный объект при уничтожении компонента
        if (textObject != null)
        {
            Destroy(textObject);
        }
    }
}

