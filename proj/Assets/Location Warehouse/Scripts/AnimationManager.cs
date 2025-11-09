using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationManager : MonoBehaviour
{
    private Animator animator;
    private AI_Behavior ai;
    private string currentState = "";

    void Awake()
    {
        animator = GetComponent<Animator>();
        ai = GetComponent<AI_Behavior>();
    }

    void Update()
    {
        if (ai == null) return;

        string newState = ai.GetCurrentStateName();

        if (newState != currentState)
        {
            SetAnimationTrigger(newState);
            currentState = newState;
        }
    }

    private void SetAnimationTrigger(string stateName)
    {
        // —брасываем все старые триггеры, чтобы не зависнуть в переходах
        animator.ResetTrigger("Patrol");
        animator.ResetTrigger("Investigate");
        animator.ResetTrigger("Chase");
        animator.ResetTrigger("Wait");

        switch (stateName)
        {
            case "Patrol":
                animator.SetTrigger("Patrol");
                break;

            case "Investigate":
                animator.SetTrigger("Investigate");
                break;

            case "Chase":
                animator.SetTrigger("Chase");
                break;

            case "Wait":
            default:
                animator.SetTrigger("Wait");
                break;
        }
    }
}
