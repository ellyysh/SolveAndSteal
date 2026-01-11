using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("UI")]
    public Canvas defaultLoseCanvas;

    [Header("Респавн игрока")]
    public Transform playerRoot;
    public Transform spawnPoint;
    public bool resetPlayerRotation = true;

    [Header("Перезапуск")]
    [Tooltip("Задержка перед респавном/перезапуском (в реальном времени).")]
    public float gameOverDelay = 0.5f;
    public bool restartSceneOnGameOver = true;

    private bool _running;
    private Coroutine _routine;

    public void TriggerGameOver()
    {
        TriggerGameOver(defaultLoseCanvas);
    }

    public void TriggerGameOver(Canvas canvasToShow)
    {
        if (_running)
            return;

        _running = true;

        if (canvasToShow != null)
            canvasToShow.gameObject.SetActive(true);

        if (_routine != null)
            StopCoroutine(_routine);
        _routine = StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        float delay = Mathf.Max(0f, gameOverDelay);
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        ResetPlayerToSpawn();

        if (restartSceneOnGameOver)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        _routine = null;
        _running = false;
    }

    private void ResetPlayerToSpawn()
    {
        if (playerRoot == null || spawnPoint == null)
            return;

        var cc = playerRoot.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerRoot.position = spawnPoint.position;
        if (resetPlayerRotation)
            playerRoot.rotation = spawnPoint.rotation;

        if (cc != null) cc.enabled = true;

        var rb = playerRoot.GetComponent<Rigidbody>();
        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }
    }
}

