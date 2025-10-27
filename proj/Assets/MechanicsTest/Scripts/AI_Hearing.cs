using UnityEngine;

public class AI_Hearing : MonoBehaviour
{
    [Header("Слух")]
    public float hearingRadius = 10f;
    public LayerMask obstacleMask; // Маска стен/преград

    // Проверка, слышит ли ИИ шум
    public bool CanHear(Vector3 noisePos)
    {
        float distance = Vector3.Distance(transform.position, noisePos);
        if (distance > hearingRadius) return false;

        // Проверяем, нет ли препятствий между источником и ИИ
        Vector3 dir = (transform.position - noisePos).normalized;
        if (Physics.Raycast(noisePos, dir, distance, obstacleMask))
            return false; // стена мешает

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
    }
}
