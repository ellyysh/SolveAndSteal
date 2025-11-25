using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BreakablePotion : MonoBehaviour
{
    [Header("Break Settings")]
    public AudioClip breakSound;
    public GameObject brokenVersion;
    public float destroyDelay = 0.1f;
    public float minBreakForce = 5f;

    [Header("Noise Settings")]
    public float noiseRadius = 10f;
    public float noiseVisualDuration = 1f;
    public float minImpactForce = 2f;

    [Header("Grab Settings")]
    public string activeLayerName = "DistractObject";

    [Header("Components")]
    private AudioSource audioSource;
    private MeshRenderer meshRenderer;
    private Collider objectCollider;
    private Rigidbody rigidbody;
    private XRGrabInteractable grabInteractable;

    private bool isBroken = false;
    private float noiseVisualTime = 0f;
    private bool isNoiseActive = false;

    // Время появления для защиты от ранних столкновений
    private float spawnTime;

    void Start()
    {
        spawnTime = Time.time;

        // Инициализация компонентов
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        meshRenderer = GetComponent<MeshRenderer>();
        objectCollider = GetComponent<Collider>();
        rigidbody = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Настройка GPU Instancing
        SetupGpuInstancing();

        // Подписка на события grab
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    void Update()
    {
        // Обновление визуализации шума
        if (isNoiseActive)
        {
            noiseVisualTime += Time.deltaTime;
            if (noiseVisualTime >= noiseVisualDuration)
                isNoiseActive = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // ❗ Игнорируем столкновения в первые 0.2 секунды
        if (Time.time - spawnTime < 0.2f)
            return;

        // Не ломаем если объект в руках
        if (grabInteractable != null && grabInteractable.isSelected)
            return;

        // Проверка на разбитие
        if (!isBroken && collision.relativeVelocity.magnitude > minBreakForce)
        {
            BreakPotion();
        }

        // Создание шума при ударе
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce >= minImpactForce)
        {
            MakeNoise();
        }
    }

    public void BreakPotion()
    {
        if (isBroken) return;

        isBroken = true;

        // Звук разбития
        if (breakSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(breakSound);
        }

        // Прячем оригинал
        if (meshRenderer != null)
            meshRenderer.enabled = false;

        if (objectCollider != null)
            objectCollider.enabled = false;

        if (rigidbody != null)
        {
            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = false;
        }

        // Отключаем взаимодействие
        if (grabInteractable != null)
            grabInteractable.enabled = false;

        // Создаем разбитую версию
        if (brokenVersion != null)
        {
            GameObject brokenPotion = Instantiate(brokenVersion, transform.position, transform.rotation);
            SetupBrokenPotion(brokenPotion);
        }

        // Уничтожаем оригинал
        Destroy(gameObject, destroyDelay);
    }

    private void SetupBrokenPotion(GameObject brokenPotion)
    {
        brokenPotion.transform.localScale = transform.localScale;

        Rigidbody[] shardRigidbodies = brokenPotion.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody shardRb in shardRigidbodies)
        {
            Vector3 randomForce = new Vector3(
                Random.Range(-2f, 2f),
                Random.Range(1f, 3f),
                Random.Range(-2f, 2f)
            );
            shardRb.AddForce(randomForce, ForceMode.Impulse);

            Vector3 randomTorque = new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f)
            );
            shardRb.AddTorque(randomTorque, ForceMode.Impulse);
        }

        brokenPotion.layer = LayerMask.NameToLayer("DistractObject");
    }

    private void SetupGpuInstancing()
    {
        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        if (meshRenderer != null)
        {
            meshRenderer.SetPropertyBlock(materialPropertyBlock);
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        gameObject.layer = LayerMask.NameToLayer(activeLayerName);
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

    public void ForceBreak()
    {
        BreakPotion();
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }
}
