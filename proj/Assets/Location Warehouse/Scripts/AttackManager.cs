using UnityEngine;
using UnityEngine.SceneManagement;

public class AttackManager : MonoBehaviour
{
    [Header("Ссылки")]
    public Animator aiAnimator;          // Animator ИИ
    public string attackTrigger = "Attack"; // Триггер анимации удара
    public GameOverManager gameOverManager; // Ссылка на GameOverManager
    public float attackDelay = 0.5f;    // Задержка перед вызовом GameOver

    private bool isAttacking = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isAttacking) return;

        // Проверяем столкновение с игроком
        if (other.CompareTag("Player"))
        {
            isAttacking = true;
            StartCoroutine(AttackRoutine());
        }
    }

    private System.Collections.IEnumerator AttackRoutine()
    {
        // Воспроизводим анимацию удара
        if (aiAnimator != null)
            aiAnimator.SetTrigger(attackTrigger);

        // Ждём небольшую задержку перед GameOver
        yield return new WaitForSeconds(attackDelay);

        if (gameOverManager != null)
            gameOverManager.StartGameOver();
    }
}
