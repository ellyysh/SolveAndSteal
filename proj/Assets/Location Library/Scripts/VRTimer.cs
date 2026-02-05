using TMPro;
using UnityEngine;

public class VRTimer : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public float startTime = 60f;
    public bool countDown = true;

    public Canvas youLoseCanvas;
    public GameOverManager gameOverManager;

    private float _currentTime;
    private bool _isRunning = true;
    private bool _gameOver;

    private void Start()
    {
        _currentTime = startTime;

        if (youLoseCanvas != null)
            youLoseCanvas.gameObject.SetActive(false);

        UpdateText();
    }

    private void Update()
    {
        if (!_isRunning || _gameOver)
            return;

        if (countDown)
        {
            _currentTime -= Time.deltaTime;
            if (_currentTime <= 0f)
            {
                _currentTime = 0f;
                OnGameOver();
            }
        }
        else
        {
            _currentTime += Time.deltaTime;
        }

        UpdateText();
    }

    private void OnGameOver()
    {
        if (_gameOver)
            return;

        _gameOver = true;
        _isRunning = false;

        // Требуем менеджер, который можно перетащить в инспекторе
        if (gameOverManager == null)
        {
            Debug.LogWarning($"[{nameof(VRTimer)}] Не назначен GameOverManager. Перетащи объект с GameOverManager в поле gameOverManager.", this);
            return;
        }

        gameOverManager.TriggerGameOver(youLoseCanvas);
    }

    private void UpdateText()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(_currentTime / 60f);
        int seconds = Mathf.FloorToInt(_currentTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // ��������� ������ ����������
    public void StartTimer() => _isRunning = true;
    public void StopTimer() => _isRunning = false;
    public void ResetTimer()
    {
        _currentTime = startTime;
        _gameOver = false;
        UpdateText();

        if (youLoseCanvas != null)
            youLoseCanvas.gameObject.SetActive(false);
    }
}