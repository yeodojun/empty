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
            if (i < currentHealth)
            {
                if (i == currentHealth - 1 && currentHealth == 1)
                {
                    if (breakIndexes.Contains(i))
                        activeHearts[i].SetBreakTremble();
                    else
                        activeHearts[i].SetTremble();
                }
                else if (currentHealth > lastHealth && i >= lastHealth)
                {
                    activeHearts[i].SetFix();
                }
                else
                {
                    activeHearts[i].SetIdle();
                }
            }
            else
            {
                if (currentHealth < lastHealth && i < lastHealth && i >= currentHealth)
                    activeHearts[i].SetReduce();
                else if (currentHealth > lastHealth && i < currentHealth)
                    activeHearts[i].SetFix();
                else if (currentHealth <= i)
                    activeHearts[i].SetNone();
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
                activeHearts[i].SetBreak();
                breakIndexes.Add(i);
                return;
            }
        }
    }

    public void OnHitWhileBreak()
    {
        if (breakIndexes.Count == 0) return;

        int targetIndex = breakIndexes[0];
        breakIndexes.RemoveAt(0);

        if (activeHearts[targetIndex] != null)
            activeHearts[targetIndex].SetBreakDestroy();
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