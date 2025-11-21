using UnityEngine;

public class EscapeColorChanger : MonoBehaviour
{
    [Header("Объект, чей цвет нужно изменить")]
    public Renderer targetRenderer;

    [Header("Цвет при достижении точки побега")]
    public Color escapeColor = Color.black;

    [Header("Цвет эмиссии")]
    public Color emissionColor = Color.black;

    private bool changed = false;

    public void ChangeColor()
    {
        if (changed) return;
        changed = true;

        if (targetRenderer == null) return;

        // Создаём копию материала
        Material mat = targetRenderer.material;

        // Меняем обычный цвет
        mat.color = escapeColor;

        // Включаем эмиссию
        mat.EnableKeyword("_EMISSION");

        // Устанавливаем emission цвет
        mat.SetColor("_EmissionColor", emissionColor);
    }
}
