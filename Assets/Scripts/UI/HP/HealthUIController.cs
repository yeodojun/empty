using UnityEngine;
using System.Collections.Generic;

public class HealthUIController : MonoBehaviour
{
    public HeartPool heartPool;
    public Transform heartParent;
    public int maxHealth = 4;
    public const int absoluteMaxHealth = 15;

    private List<HeartUI> activeHearts = new List<HeartUI>();
    public IReadOnlyList<HeartUI> ActiveHearts => activeHearts;
    private int lastHealth = -1;

    private List<int> breakIndexes = new();
    private int parrySuccessCount = 0;

    public void InitHearts()
    {
        ClearHearts();
        int clampedMax = Mathf.Clamp(maxHealth, 1, absoluteMaxHealth);
        for (int i = 0; i < clampedMax; i++)
        {
            HeartUI heart = heartPool.Get();

            RectTransform rt = heart.GetComponent<RectTransform>();
            rt.SetParent(heartParent);
            rt.localScale = Vector3.one;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(i * 90f, 0);

            heart.gameObject.SetActive(true);
            heart.SetIdle();
            activeHearts.Add(heart);
        }

        lastHealth = clampedMax;
    }

    public void UpdateHealthUI(int currentHealth)
    {
        for (int i = 0; i < activeHearts.Count; i++)
        {
            HeartUI heart = activeHearts[i];
            bool isBreak = breakIndexes.Contains(i);
            bool isDestroyedBreak = heart.IsBreakNoneState();

            if (i < currentHealth)
            {
                if (i == currentHealth - 1 && currentHealth == 1)
                {
                    if (isBreak) heart.SetBreakTremble();
                    else heart.SetTremble();
                }
                else if (currentHealth > lastHealth && i >= lastHealth)
                {
                    if (isBreak) heart.SetBreakFix();
                    else heart.SetFix();
                }
                else
                {
                    if (isBreak) heart.SetBreakIdle();
                    else heart.SetIdle();
                }
            }
            else
            {
                if (currentHealth < lastHealth && i < lastHealth && i >= currentHealth)
                {
                    // 일반 Life만 Reduce, BreakLife는 Destroy만 별도로 실행됨
                    if (!isBreak && !isDestroyedBreak)
                        heart.SetReduce();
                }
                else if (currentHealth > lastHealth && i < currentHealth)
                {
                    if (isBreak && heart.IsBreakNoneState()) heart.SetBreakNoneFix();
                    else if (isBreak) heart.SetBreakFix();
                    else heart.SetFix();
                }
            }
        }

        lastHealth = currentHealth;
    }

    public void AddBreak()
    {
        for (int i = activeHearts.Count - 1; i >= 0; i--)
        {
            if (!breakIndexes.Contains(i))
            {
                breakIndexes.Add(i);
                activeHearts[i].SetBreakIdle();
                break;
            }
        }
    }

    public void OnHitWhileBreak()
    {
        for (int i = activeHearts.Count - 1; i >= 0; i--)
        {
            HeartUI heart = activeHearts[i];

            if (breakIndexes.Contains(i) && !heart.IsBreakNoneState())
            {
                heart.SetBreakDestroy();
                breakIndexes.Remove(i);
                return;
            }
            else if (!breakIndexes.Contains(i) && !heart.IsNoneState())
            {
                heart.SetReduce();
                return;
            }
        }
    }

    public void OnParrySuccess()
    {
        if (breakIndexes.Count == 0) return;

        parrySuccessCount++;
        if (parrySuccessCount >= 2)
        {
            parrySuccessCount = 0;

            int lastIdx = breakIndexes[breakIndexes.Count - 1];
            breakIndexes.RemoveAt(breakIndexes.Count - 1);

            if (activeHearts[lastIdx] != null)
            {
                if (activeHearts[lastIdx].IsBreakNoneState())
                    activeHearts[lastIdx].SetBreakNoneFix();
                else
                    activeHearts[lastIdx].SetBreakFix();
            }
        }
    }

    public bool IsBreakFull() => breakIndexes.Count >= 3;
    public bool HasBreak() => breakIndexes.Count > 0;
    public bool IsLastHeartBreak() => breakIndexes.Contains(activeHearts.Count - 1);

    public void ClearHearts()
    {
        foreach (var heart in activeHearts)
            heartPool.Return(heart);

        activeHearts.Clear();
        breakIndexes.Clear();
        parrySuccessCount = 0;
    }

    public void SetMaxHealth(int newMax)
    {
        maxHealth = Mathf.Clamp(newMax, 1, absoluteMaxHealth);
        InitHearts();
    }
}