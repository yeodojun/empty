using UnityEngine;
using System.Collections.Generic;

public class HealthUIController : MonoBehaviour
{
    public HeartPool heartPool;
    public Transform heartParent;
    public int maxHealth = 4;
    public const int absoluteMaxHealth = 15;

    private List<HeartUI> activeHearts = new List<HeartUI>();
    private int lastHealth = -1;

    public void InitHearts()
    {
        ClearHearts();
        int clampedMax = Mathf.Clamp(maxHealth, 1, absoluteMaxHealth);
        for (int i = 0; i < clampedMax; i++)
        {
            HeartUI heart = heartPool.Get();

            // UI 좌표 기반 배치
            RectTransform rt = heart.GetComponent<RectTransform>();
            rt.SetParent(heartParent);
            rt.localScale = Vector3.one;
            rt.anchorMin = new Vector2(0, 1); // 좌상단
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1); // 좌상단 기준
            rt.anchoredPosition = new Vector2(i * 90f, 0); // 왼→오 정렬, 하트 간격 100px

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
                    activeHearts[i].SetTremble();
                else
                    activeHearts[i].SetIdle();
            }
            else
            {
                if (currentHealth < lastHealth && i < lastHealth && i >= currentHealth)
                    activeHearts[i].SetReduce(); // 새로 닳은 하트만 애니메이션 재생
                else if (currentHealth > lastHealth && i < currentHealth)
                    activeHearts[i].SetFix();    // 새로 찬 하트만 애니메이션 재생
                else if (currentHealth <= i)
                    activeHearts[i].SetNone();    // 닳아있는 하트는 None 상태로 유지
            }
        }
        lastHealth = currentHealth;
    }

    public void ClearHearts()
    {
        foreach (var heart in activeHearts)
            heartPool.Return(heart);

        activeHearts.Clear();
    }

    public void SetMaxHealth(int newMax)
    {
        maxHealth = Mathf.Clamp(newMax, 1, absoluteMaxHealth);
        InitHearts();
    }
}