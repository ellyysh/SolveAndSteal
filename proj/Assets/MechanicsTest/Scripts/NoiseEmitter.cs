using UnityEngine;

public class NoiseEmitter : MonoBehaviour
{
    [Header("Настройки шума")]
    public float noiseRadius = 10f;
    public float noiseVisualDuration = 1f;
    public float minImpactForce = 2f; // минимальная сила удара, чтобы создать шум

    private float noiseVisualTime = 0f;
    private bool isNoiseActive = false;

    void OnCollisionEnter(Collision collision)
    {
        // Проверяем силу удара
        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce >= minImpactForce)
        {
            MakeNoise();
        }
    }

    public void MakeNoise()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, noiseRadius);
        foreach (var col in colliders)
        {
            var ai = col.GetComponent<AI_Behavior>();
            if (ai != null)
                ai.HearNoise(transform.position);
        }

        isNoiseActive = true;
        noiseVisualTime = 0f;
    }

    void Update()
    {
        if (isNoiseActive)
        {
            noiseVisualTime += Time.deltaTime;
            if (noiseVisualTime >= noiseVisualDuration)
                isNoiseActive = false;
        }
    }
}
