using UnityEngine;

public enum AlertState { Normal, Search, Alert }

public class SpotLightAlert : MonoBehaviour
{
    [Header("Light")]
    public Light spotLight;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color searchColor = Color.yellow;
    public Color alertColor = Color.red;

    [Header("Settings")]
    public float colorLerpSpeed = 5f;

    private AlertState currentState = AlertState.Normal;

    public void SetState(AlertState state)
    {
        currentState = state;
    }

    // Backward compatibility
    public void SetAlert(bool active)
    {
        currentState = active ? AlertState.Alert : AlertState.Normal;
    }

    void Update()
    {
        if (spotLight == null) return;

        Color targetColor = currentState switch
        {
            AlertState.Normal => normalColor,
            AlertState.Search => searchColor,
            AlertState.Alert => alertColor,
            _ => normalColor
        };

        spotLight.color = Color.Lerp(spotLight.color, targetColor, Time.deltaTime * colorLerpSpeed);
    }
}
