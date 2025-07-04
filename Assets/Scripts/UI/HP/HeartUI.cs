using System.Collections;
using UnityEngine;

public class HeartUI : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
        gameObject.SetActive(false); // 풀링 기본 상태
    }

    public void Activate(Vector3 position, Transform parent)
    {
        transform.SetParent(parent);
        transform.localPosition = position;
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public bool IsBreakNoneState()
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName("BreakLife_None");
    }
    public bool IsNoneState()
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName("Life_None");
    }

    public bool IsTrembling()
        => animator.GetCurrentAnimatorStateInfo(0).IsName("Life_Trembling");


    public void SetIdle() => animator.Play("Life_Idle");
    public void SetNone() => animator.Play("Life_None");
    public void SetReduce()
    {
        animator.Play("Life_Reduce");
        StartCoroutine(DelayedNone());
    }

    private IEnumerator DelayedNone()
    {
        // 애니메이션 길이만큼 기다렸다가 None으로
        float len = 0f;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name == "Life_Reduce") { len = clip.length; break; }
        yield return new WaitForSeconds(len);
        animator.Play("Life_None");
    }

    public void SetFix()
    {
        animator.Play("Life_Fix");
    }
    public void SetTremble() => animator.Play("Life_Trembling");

    public void SetBreakIdle() => animator.Play("BreakLife_Idle");
    public void SetBreak() => animator.Play("BreakLife_Breaking");
    public void SetBreakDestroy() => animator.Play("BreakLife_Destroy");
    public void SetBreakFix() => animator.Play("BreakLife_Fix");
    public void SetBreakNoneFix() => animator.Play("BreakLife_NoneFix");
    public void SetBreakTremble() => animator.Play("BreakLife_Trembling");
}