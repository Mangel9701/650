using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;

public class HUDManager : MonoBehaviour
{
    [Header("Script References")]
    public UIManager uiManager;
    public int totalObjectives = 0;
    public int currentObjectiveIndex = 0;

    private Animator animator;

    private int objectiveChangingHash = Animator.StringToHash("objectiveChanging");
    private int isClosingHash = Animator.StringToHash("isClosing");

    public bool isChanging;
    private float objectiveChangeDelay = 0.51f;

    [Header("Timing Settings")]
    public float objectiveVisibleTime = 5f;

    private Coroutine hideObjectiveCoroutine;


    private void Update()
    {
        CheckAnimationState();
    }

    private void CheckAnimationState()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (animator.GetBool(objectiveChangingHash) && stateInfo.IsTag("ObjectiveChange") && stateInfo.normalizedTime >= 1f)
        {
            animator.SetBool(objectiveChangingHash, false);
        }

        if (animator.GetBool(isClosingHash) && stateInfo.IsTag("Close") && stateInfo.normalizedTime >= 1f)
        {
            animator.SetBool(isClosingHash, false);
        }
    }

    private IEnumerator HideObjectiveAfterDelay()
    {
        yield return new WaitForSeconds(objectiveVisibleTime);

        if (animator != null)
        {
            animator.SetBool(isClosingHash, true);
        }
    }
}