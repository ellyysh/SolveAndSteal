using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public GameObject winCanvas;
    public GameObject loseCanvas;

    // КАСАНИЕ ПРЕДМЕТА ДЛЯ ПОБЕДЫ
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WinObject"))
        {
            winCanvas.SetActive(true);
            Time.timeScale = 0f; // игра стопается
        }
    }

    // ЭТУ ФУНКЦИЮ БУДЕТ ВЫЗЫВАТЬ ГОБЛИН
    public void PlayerSpotted()
    {
        loseCanvas.SetActive(true);
        StartCoroutine(RestartIn5());
    }

    private System.Collections.IEnumerator RestartIn5()
    {
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
