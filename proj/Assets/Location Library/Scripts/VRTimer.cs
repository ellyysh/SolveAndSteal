using UnityEngine;
using TMPro;

public class VRTimer : MonoBehaviour
{
    [Header("Настройки")]
    public TextMeshProUGUI timerText; // Текст для отображения времени
    public float startTime = 60f; // Начальное время в секундах
    public bool countDown = true; // true = обратный отсчет, false = прямой
    
    private float currentTime;
    private bool isRunning = true;

    [Header("Проигрыш")]
    public Canvas youLoseCanvas; // ИЗМЕНИЛ: теперь Canvas вместо GameObject

    void Start()
    {
        currentTime = startTime;

        // Скрываем Canvas при старте
        if (youLoseCanvas != null)
            youLoseCanvas.gameObject.SetActive(false); // ИЗМЕНИЛ: для Canvas
    }
    
    void Update()
    {
        if (!isRunning) return;
        
        if (countDown)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                currentTime = 0;
                isRunning = false;

                Debug.Log($"Canvas назначен: {youLoseCanvas != null}");

                if (youLoseCanvas != null)
                {
                    Debug.Log($"Canvas активен до: {youLoseCanvas.gameObject.activeSelf}");
                    youLoseCanvas.gameObject.SetActive(true);
                    Debug.Log($"Canvas активен после: {youLoseCanvas.gameObject.activeSelf}");

                    // Сразу проверяем - виден ли Canvas на сцене?
                    if (youLoseCanvas.gameObject.activeInHierarchy)
                        Debug.Log("Canvas ВИДЕН В ИЕРАРХИИ!");
                    else
                        Debug.Log("Canvas НЕ ВИДЕН в иерархии!");
                }
            }
        }
        else
        {
            currentTime += Time.deltaTime;
        }
        
        UpdateText();
    }
    
    void UpdateText()
    {
        if (timerText == null) return;
        
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    // Публичные методы для управления
    public void StartTimer() => isRunning = true;
    public void StopTimer() => isRunning = false;
    public void ResetTimer() { currentTime = startTime; UpdateText(); }
}
