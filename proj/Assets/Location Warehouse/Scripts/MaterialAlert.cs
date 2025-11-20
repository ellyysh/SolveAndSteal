using UnityEngine;

public class MaterialAlert : MonoBehaviour
{
    public Renderer targetRenderer;
    public bool useSharedMaterial = true;

    private Material mat;
    private readonly string baseMap = "_BaseMap";

    private Vector2 normalOffset;
    [Header("Texture Offsets")]
    public Vector2 searchOffset = new Vector2(0.125f, 0f);
    public Vector2 alertOffset = new Vector2(0.25f, 0f);

    private AlertState currentState = AlertState.Normal;

    void Start()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        mat = useSharedMaterial ? targetRenderer.sharedMaterial : targetRenderer.material;

        // Сохраняем начальный offset
        normalOffset = mat.GetTextureOffset(baseMap);
    }

    public void SetState(AlertState state)
    {
        currentState = state;
        UpdateOffset();
    }

    // Backward compatibility
    public void SetAlert(bool active)
    {
        currentState = active ? AlertState.Alert : AlertState.Normal;
        UpdateOffset();
    }

    private void UpdateOffset()
    {
        if (mat == null) return;

        Vector2 targetOffset = currentState switch
        {
            AlertState.Normal => normalOffset,
            AlertState.Search => searchOffset,
            AlertState.Alert => alertOffset,
            _ => normalOffset
        };

        mat.SetTextureOffset(baseMap, targetOffset);
    }

    // Восстанавливаем при:
    // - отключении объекта
    // - уничтожении компонента
    // - изменении сцены
    void OnDisable()
    {
        if (mat != null)
        {
            mat.SetTextureOffset(baseMap, normalOffset);
        }
    }
}
