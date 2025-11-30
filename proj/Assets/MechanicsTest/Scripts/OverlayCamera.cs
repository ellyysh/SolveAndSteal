using UnityEngine;

/// <summary>
/// Камера для рендеринга объектов поверх всего (видно через стены)
/// Добавьте этот компонент на отдельную камеру для видимости индикаторов через стены
/// Поддерживает как обычную камеру, так и VR камеру из XR Origin
/// </summary>
[RequireComponent(typeof(Camera))]
public class OverlayCamera : MonoBehaviour
{
    [Header("Настройки Overlay камеры")]
    [Tooltip("Слой, который будет рендериться поверх всего")]
    public int overlayLayer = 31;
    
    [Tooltip("Цвет фона (обычно прозрачный)")]
    public Color backgroundColor = new Color(0, 0, 0, 0);
    
    [Tooltip("Очищать ли буфер глубины")]
    public bool clearDepth = false;

    [Header("VR Настройки")]
    [Tooltip("Камера VR игрока (из XR Origin). Оставьте пустым для автоматического поиска")]
    public Camera vrCamera;

    private Camera overlayCam;
    private Camera targetCamera;

    void Awake()
    {
        overlayCam = GetComponent<Camera>();
        SetupOverlayCamera();
        FindTargetCamera();
    }

    void Start()
    {
        SetupOverlayCamera();
        FindTargetCamera();
    }

    /// <summary>
    /// Настраивает камеру для рендеринга поверх всего
    /// </summary>
    private void SetupOverlayCamera()
    {
        if (overlayCam == null) return;

        // Настраиваем камеру для рендеринга только указанного слоя
        overlayCam.cullingMask = 1 << overlayLayer;
        
        // Прозрачный фон (полностью прозрачный черный)
        overlayCam.backgroundColor = new Color(0, 0, 0, 0);
        
        // Используем SolidColor с прозрачным фоном для правильного смешивания
        overlayCam.clearFlags = CameraClearFlags.SolidColor;
        
        // Включаем альфа-канал для прозрачности
        overlayCam.allowHDR = false;
        overlayCam.allowMSAA = false;
        
        // Устанавливаем глубину камеры выше основной (рендерится последней)
        overlayCam.depth = 100;
        
        // Отключаем тест глубины, чтобы рендерилось поверх всего
        overlayCam.depthTextureMode = DepthTextureMode.None;
        
        // Копируем настройки из целевой камеры (будет обновлено в LateUpdate)
        if (targetCamera != null)
        {
            overlayCam.fieldOfView = targetCamera.fieldOfView;
            overlayCam.nearClipPlane = targetCamera.nearClipPlane;
            overlayCam.farClipPlane = targetCamera.farClipPlane;
        }
        else
        {
            // Временные настройки до нахождения целевой камеры
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                overlayCam.fieldOfView = mainCam.fieldOfView;
                overlayCam.nearClipPlane = mainCam.nearClipPlane;
                overlayCam.farClipPlane = mainCam.farClipPlane;
            }
        }
        
        // Проверяем, есть ли объекты на нужном слое
        GameObject[] objectsOnLayer = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int objectsFound = 0;
        foreach (GameObject obj in objectsOnLayer)
        {
            if (obj.layer == overlayLayer)
            {
                objectsFound++;
            }
        }
        
        Debug.Log($"[OverlayCamera] Настроена камера. Слой: {overlayLayer} ({LayerMask.LayerToName(overlayLayer)}), " +
                 $"CullingMask: {overlayCam.cullingMask}, Найдено объектов на слое: {objectsFound}");
    }

    /// <summary>
    /// Находит целевую камеру (VR или обычную)
    /// </summary>
    private void FindTargetCamera()
    {
        // Если VR камера установлена вручную, используем её
        if (vrCamera != null)
        {
            targetCamera = vrCamera;
            return;
        }

        // Пытаемся найти VR камеру из XR Origin
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
                        Camera xrCam = cameraProperty.GetValue(xrOrigin) as Camera;
                        if (xrCam != null)
                        {
                            targetCamera = xrCam;
                            Debug.Log($"[OverlayCamera] Найдена VR камера: {xrCam.name}");
                            return;
                        }
                    }
                }
            }
        }
        catch
        {
            // XR Toolkit не установлен или недоступен
        }

        // Если VR камера не найдена, используем основную камеру
        targetCamera = Camera.main;
        if (targetCamera == null)
        {
            // Последняя попытка - найти любую активную камеру
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in cameras)
            {
                if (cam.enabled && cam.gameObject.activeInHierarchy && cam != overlayCam)
                {
                    targetCamera = cam;
                    break;
                }
            }
        }
    }

    void LateUpdate()
    {
        // Обновляем поиск камеры, если она была потеряна
        if (targetCamera == null || !targetCamera.gameObject.activeInHierarchy)
        {
            FindTargetCamera();
        }

        // Синхронизируем позицию и поворот с целевой камерой
        if (targetCamera != null)
        {
            overlayCam.transform.position = targetCamera.transform.position;
            overlayCam.transform.rotation = targetCamera.transform.rotation;
            overlayCam.fieldOfView = targetCamera.fieldOfView;
            overlayCam.nearClipPlane = targetCamera.nearClipPlane;
            overlayCam.farClipPlane = targetCamera.farClipPlane;
        }
    }

    /// <summary>
    /// Установить VR камеру вручную
    /// </summary>
    public void SetVRCamera(Camera camera)
    {
        vrCamera = camera;
        targetCamera = camera;
    }
}

