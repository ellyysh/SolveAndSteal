using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    public Image fadeImage;       // Image на Canvas (черный, альфа=0)
    public float fadeDuration = 1f;

    public void StartGameOver()
    {
        StartCoroutine(FadeAndRestart());
    }

    private IEnumerator FadeAndRestart()
    {
        float timer = 0f;
        Color color = fadeImage.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Clamp01(timer / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        // Перезапуск сцены
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
