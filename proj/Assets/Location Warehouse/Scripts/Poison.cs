using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PotionController : MonoBehaviour
{
    [Header("Настройки зелья")]
    public GameObject brokenPotionEffect;  // Эффект когда разбивается
    public AudioClip grabSound;           // Звук когда берем
    public AudioClip breakSound;          // Звук когда разбивается
    
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private AudioSource audioSource;
    private bool isBroken = false;

    void Start()
    {
        // Находим компоненты
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        audioSource = GetComponent<AudioSource>();
        
        // Подписываемся на события
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    // Когда взяли зелье
    private void OnGrab(SelectEnterEventArgs args)
    {
        if (!isBroken && grabSound != null)
        {
            audioSource.PlayOneShot(grabSound);
        }
    }

    // Когда отпустили зелье
    private void OnRelease(SelectExitEventArgs args)
    {
        // Проверяем скорость - если бросили сильно, то разбивается
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb.linearVelocity.magnitude > 2f)
        {
            BreakPotion();
        }
    }

    // Когда зелье сталкивается с чем-то
    private void OnCollisionEnter(Collision collision)
    {
        // Если ударилось сильно - разбивается
        if (collision.relativeVelocity.magnitude > 3f && !isBroken)
        {
            BreakPotion();
        }
    }

    // Функция разбивания зелья
    public void BreakPotion()
    {
        if (isBroken) return;
        
        isBroken = true;
        
        // Проигрываем звук разбивания
        if (breakSound != null)
            audioSource.PlayOneShot(breakSound);
        
        // Создаем эффект разбитого зелья (если он есть)
        if (brokenPotionEffect != null)
        {
            Instantiate(brokenPotionEffect, transform.position, transform.rotation);
        }
        
        // Прячем целое зелье
        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
        
        // Уничтожаем объект через 2 секунды
        Destroy(gameObject, 2f);
    }
}