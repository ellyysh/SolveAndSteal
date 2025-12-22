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
    
    void Start()
    {
        currentTime = startTime;
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
