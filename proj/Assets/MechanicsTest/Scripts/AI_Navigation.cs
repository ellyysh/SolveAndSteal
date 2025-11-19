using UnityEngine;
using UnityEngine.AI;

public class AI_Navigation : MonoBehaviour
{
    [Header("Патруль")]
    public Transform[] patrolPoints;

    private int currentPoint = 0;
    private NavMeshAgent agent;
    private Vector3 currentDestination;
    private bool destinationSet = false;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    void Start()
    {
        // Если есть хотя бы одна точка — идём к ней
        if (patrolPoints.Length > 0)
        {
            GoToNextPoint();
        }
    }

    // ---------------- Основные методы ----------------

    // Перейти к следующей точке патруля
    public void GoToNextPoint()
    {
        if (patrolPoints.Length == 0) return;

        // Если точка одна — остаёмся на месте
        currentDestination = patrolPoints[currentPoint].position;
        agent.SetDestination(currentDestination);
        destinationSet = true;

        // Если точек больше одной — переключаемся на следующую
        if (patrolPoints.Length > 1)
        {
            currentPoint = (currentPoint + 1) % patrolPoints.Length;
        }
    }

    // Перейти к произвольной позиции (шум, игрок, объект)
    public void MoveTo(Vector3 position)
    {
        currentDestination = position;
        agent.SetDestination(currentDestination);
        destinationSet = true;
    }

    // Вернуться к патрулю (к единственной точке или к следующей)
    public void ReturnToPatrol()
    {
        if (patrolPoints.Length == 0) return;

        // Если одна точка — просто возвращаемся к ней
        currentDestination = patrolPoints[currentPoint].position;
        agent.SetDestination(currentDestination);
        destinationSet = true;
    }

    // Установить цель
    public void SetDestination(Vector3 position)
    {
        currentDestination = position;
        agent.SetDestination(currentDestination);
        destinationSet = true;
    }

    // ---------------- Проверки ----------------
    public bool ReachedDestination(float threshold = 0.3f)
    {
        if (agent == null || agent.pathPending) return false;
        if (!destinationSet) return false;

        return agent.remainingDistance < threshold;
    }

    public float speed
    {
        get => agent.speed;
        set => agent.speed = value;
    }

    public bool HasDestination() => destinationSet;
    public Vector3 GetDestination() => currentDestination;

}
