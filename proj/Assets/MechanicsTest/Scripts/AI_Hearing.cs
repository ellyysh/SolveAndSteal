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
        if (distance > hearingRadius)
            return false;

        // Правильное направление — от AI к шуму!
        Vector3 dir = (noisePos - transform.position).normalized;

        if (Physics.Raycast(transform.position, dir, distance, obstacleMask))
            return false; // стена мешает

        return true;
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
    }
}
