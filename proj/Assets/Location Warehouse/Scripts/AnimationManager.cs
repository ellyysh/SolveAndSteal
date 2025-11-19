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
            SetTriggerForState(newState);
            currentState = newState;
        }
    }

    private void SetTriggerForState(string state)
    {
        animator.ResetTrigger("Walk");
        animator.ResetTrigger("Investigate");
        animator.ResetTrigger("LookAround");
        animator.ResetTrigger("Wait");
        animator.ResetTrigger("Flee");

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

            case "Flee":
                animator.SetTrigger("Run");
                break;

            case "Wait":
            default:
                animator.SetTrigger("Wait");
                break;
        }
    }
}
