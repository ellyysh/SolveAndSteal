using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

/// <summary>
/// Наведение + Trigger без взятия в руки.
/// При наведении показывает TextMeshPro (подсказку) с фейдом.
/// При нажатии Trigger — открывает/закрывает свиток.
/// </summary>
public class ScrollXRInteraction : MonoBehaviour
{
    public enum ActivateMode
    {
        Toggle,
        Hold
    }

    [Header("References")]
    [Tooltip("Animator that contains a bool parameter to open/close the scroll.")]
    public Animator animator;

    [Tooltip("XRSimpleInteractable для наведения и Activate (не Grab).")]
    public XRSimpleInteractable simpleInteractable;

    [Header("Hover Hint")]
    [Tooltip("TextMeshPro, который показывается при наведении.")]
    public TMP_Text hoverHintText;

    [Tooltip("Длительность фейда подсказки.")]
    public float hintFadeDuration = 0.25f;

    [Tooltip("Скрыть подсказку при старте.")]
    public bool hideHintOnAwake = true;

    [Header("Animator")]
    [Tooltip("Bool parameter name in Animator Controller (default: open).")]
    public string openBoolParameter = "open";

    [Header("Behavior")]
    [Tooltip("Toggle: press Activate once to open/close. Hold: while Activate is held => open.")]
    public ActivateMode activateMode = ActivateMode.Toggle;

    [Tooltip("Закрывать свиток когда луч убран.")]
    public bool closeOnHoverExit = false;

    private Coroutine _hintFadeRoutine;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>(true);
        simpleInteractable = GetComponent<XRSimpleInteractable>();
        hoverHintText = GetComponentInChildren<TMP_Text>(true);
    }

    private void Awake()
    {
        if (simpleInteractable == null)
            simpleInteractable = GetComponent<XRSimpleInteractable>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (hoverHintText == null)
            hoverHintText = GetComponentInChildren<TMP_Text>(true);

        if (hideHintOnAwake && hoverHintText != null)
            SetHintAlpha(0f);
    }

    private void OnEnable()
    {
        if (simpleInteractable == null)
            return;

        simpleInteractable.hoverEntered.AddListener(OnHoverEntered);
        simpleInteractable.hoverExited.AddListener(OnHoverExited);
        simpleInteractable.activated.AddListener(OnActivated);
        simpleInteractable.deactivated.AddListener(OnDeactivated);
    }

    private void OnDisable()
    {
        if (simpleInteractable == null)
            return;

        simpleInteractable.hoverEntered.RemoveListener(OnHoverEntered);
        simpleInteractable.hoverExited.RemoveListener(OnHoverExited);
        simpleInteractable.activated.RemoveListener(OnActivated);
        simpleInteractable.deactivated.RemoveListener(OnDeactivated);
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        FadeHint(1f);
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        FadeHint(0f);

        if (closeOnHoverExit)
            Close();
    }

    private void OnActivated(ActivateEventArgs args)
    {
        if (activateMode == ActivateMode.Toggle)
            Toggle();
        else
            Open();
    }

    private void OnDeactivated(DeactivateEventArgs args)
    {
        if (activateMode == ActivateMode.Hold)
            Close();
    }

    public void Open()
    {
        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
    }

    public void Toggle()
    {
        if (animator == null) return;
        SetOpen(!animator.GetBool(openBoolParameter));
    }

    private void SetOpen(bool value)
    {
        if (animator == null) return;
        if (string.IsNullOrWhiteSpace(openBoolParameter)) return;
        animator.SetBool(openBoolParameter, value);
    }

    private void FadeHint(float targetAlpha)
    {
        if (hoverHintText == null) return;

        if (_hintFadeRoutine != null)
            StopCoroutine(_hintFadeRoutine);

        _hintFadeRoutine = StartCoroutine(FadeHintRoutine(targetAlpha));
    }

    private IEnumerator FadeHintRoutine(float targetAlpha)
    {
        float startAlpha = hoverHintText.alpha;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / hintFadeDuration;
            hoverHintText.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        hoverHintText.alpha = targetAlpha;
        _hintFadeRoutine = null;
    }

    private void SetHintAlpha(float alpha)
    {
        if (hoverHintText != null)
            hoverHintText.alpha = alpha;
    }
}

