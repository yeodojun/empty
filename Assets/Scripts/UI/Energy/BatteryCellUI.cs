using UnityEngine;

public class BatteryCellUI : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
        gameObject.SetActive(false);
    }

    public void Activate(Vector2 anchoredPos, Transform parent, bool isLight)
    {
        transform.SetParent(parent);
        RectTransform rt = GetComponent<RectTransform>();

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        rt.anchoredPosition = anchoredPos;

        rt.localScale = Vector3.one;
        gameObject.SetActive(true);

        animator.Play(isLight ? "Light_Energy" : "Idle");
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }
}
