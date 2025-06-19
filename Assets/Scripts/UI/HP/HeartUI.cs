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

    public void SetIdle() => animator.Play("Life_Idle");
    public void SetNone() => animator.Play("Life_None");
    public void SetReduce() => animator.Play("Life_Reduce");
    public void SetFix()
    {
        animator.Play("Life_Fix");
    }
    public void SetTremble() => animator.Play("Life_Trembling");
    public void SetBreak() => animator.Play("BreakLife_Breaking");
    public void SetBreakDestroy() => animator.Play("BreakLife_Destroy");
    public void SetBreakFix() => animator.Play("BreakLife_Fix");
    public void SetBreakNoneFix() => animator.Play("BreakLife_NoneFix");
    public void SetBreakTremble() => animator.Play("BreakLife_Trembling");
}