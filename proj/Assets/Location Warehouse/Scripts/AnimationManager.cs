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

        // -------------------------
        //  Freeze анимаций
        // -------------------------
        if (ai.IsFrozen)
        {
            animator.speed = 0;
            return;
        }
        else
        {
            animator.speed = 1;
        }

        string newState = ai.GetCurrentStateName();

        if (newState != currentState)
        {
            SetTriggerForState(newState);
            currentState = newState;
        }
    }

    private void SetTriggerForState(string state)
    {
        // Сбрасываем все триггеры
        animator.ResetTrigger("Walk");
        animator.ResetTrigger("LookAround");
        animator.ResetTrigger("Wait");
        animator.ResetTrigger("Run");

        switch (state)
        {
            case "Patrol":
                animator.SetTrigger("Walk");
                break;

            case "Investigate":
                animator.SetTrigger("Walk");
                break;

            case "LookAround":
                animator.SetTrigger("LookAround");
                break;
            case "Interest":
                animator.SetTrigger("Walk");
                break;
            case "Run":          //  ПОБЕГ
                animator.SetTrigger("Run");
                break;

            case "Wait":
            default:
                animator.SetTrigger("Wait");
                break;
        }
    }
}
