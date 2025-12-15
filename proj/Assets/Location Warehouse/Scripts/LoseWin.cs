using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
   
    // КАСАНИЕ ПРЕДМЕТА ДЛЯ ПОБЕДЫ
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WinObject"))
        {
            SceneManager.LoadScene("Lib");
        }
    }

}
