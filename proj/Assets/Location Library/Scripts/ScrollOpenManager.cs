using System.Collections;
using UnityEngine;
using TMPro;

public class ScrollOpenManager : MonoBehaviour
{
    [Header("Ссылки")]
    public Animator animator;

    [Tooltip("TextMeshPro, который появляется/исчезает при открытии/закрытии свитка.")]
    public TMP_Text textToShow;

    [Header("Логика")]
    [Tooltip("Имя bool-параметра в Animator Controller.")]
    public string openBoolParameter = "open";

    [Tooltip("Через сколько секунд после open=true показать текст.")]
    public float showDelay = 1.0f;

    [Header("Затухание (Fade)")]
    [Tooltip("Если true — текст будет появляться плавно.")]
    public bool useFade = true;

    [Tooltip("Длительность затухания/проявления (сек).")]
    public float fadeDuration = 0.35f;

    [Tooltip("Если true — текст будет скрыт при старте сцены.")]
    public bool hideOnAwake = true;

    [Tooltip("Если true — когда open станет false, текст снова скрывается.")]
    public bool hideWhenClosed = true;

    [Tooltip("Если true — при закрытии свитка текст будет плавно исчезать.")]
    public bool fadeOutWhenClosed = true;

    private Coroutine _routine;
    private bool _wasOpen;

    private void Awake()
    {
        if (hideOnAwake && textToShow != null)
            textToShow.alpha = 0f;
    }

    private void Update()
    {
        if (animator == null)
            return;

        bool isOpen = animator.GetBool(openBoolParameter);

        // rising edge: false -> true
        if (isOpen && !_wasOpen)
        {
            RestartRoutine(ShowTextAfterDelay());
        }

        // falling edge: true -> false
        if (!isOpen && _wasOpen)
        {
            if (hideWhenClosed && textToShow != null)
            {
                if (useFade && fadeOutWhenClosed)
                    RestartRoutine(FadeOutText());
                else
                    textToShow.alpha = 0f;
            }
        }

        _wasOpen = isOpen;
    }

    private IEnumerator ShowTextAfterDelay()
    {
        if (showDelay > 0f)
            yield return new WaitForSeconds(showDelay);

        if (textToShow == null)
            yield break;

        if (useFade)
        {
            yield return FadeTo(1f);
        }
        else
        {
            textToShow.alpha = 1f;
        }

        _routine = null;
    }

    private IEnumerator FadeOutText()
    {
        if (textToShow == null)
            yield break;

        yield return FadeTo(0f);
        _routine = null;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (textToShow == null)
            yield break;

        if (fadeDuration <= 0f)
        {
            textToShow.alpha = targetAlpha;
            yield break;
        }

        float startAlpha = textToShow.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            textToShow.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        textToShow.alpha = targetAlpha;
    }

    private void RestartRoutine(IEnumerator routine)
    {
        if (_routine != null)
            StopCoroutine(_routine);
        _routine = StartCoroutine(routine);
    }
}
