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

    public void SetIdle() => animator.Play("Life_Idle");
    public void SetNone() => animator.Play("Life_None");
    public void SetReduce() => animator.Play("Life_Reduce");
    public void SetFix()
    {
        animator.Play("Life_Fix");
        animator.Play("Life_Glow", 1); // 병렬 레이어에서 실행
    }
    public void SetTremble() => animator.Play("Life_Trembling");
}