using System.Collections.Generic;
using UnityEngine;

public class BatteryUI : MonoBehaviour
{
    public RectTransform cellParent;
    private BatteryCellPool cellPool;

    private List<BatteryCellUI> cells = new List<BatteryCellUI>();
    public int currentMana { get; private set; } = 100;
    private const int maxMana = 100;

    public void Init(int startingMana, BatteryCellPool pool)
    {
        if (cellParent == null)
        {
            var found = transform.Find("CellParent");
            if (found != null)
                cellParent = found.GetComponent<RectTransform>();
        }

        cellPool = pool;
        currentMana = Mathf.Clamp(startingMana, 0, maxMana);
        UpdateUI();
    }

    public bool SpendMana(int amount)
    {
        if (currentMana < amount) return false;
        currentMana -= amount;
        UpdateUI();
        return true;
    }

    public void GainMana(int amount)
    {
        currentMana = Mathf.Min(maxMana, currentMana + amount);
        UpdateUI();
    }

    private void UpdateUI()
    {
        int desiredCells = currentMana / 10;
        float spacing = 20f;

        // 셀 없으면 초기 생성
        if (cells.Count == 0)
        {
            for (int i = 0; i < maxMana / 10; i++)
            {
                var cell = cellPool.Get();

                // 중심 정렬 계산
                float offsetX = 2f;
                float startX = -((maxMana / 10 - 1) * spacing) / 2f + offsetX;
                float x = startX + i * spacing;

                Vector2 pos = new Vector2(x, 0);
                cell.Activate(pos, cellParent, false);
                cells.Add(cell);
            }
        }
        // 현재 상태 업데이트
        int totalCells = cells.Count;
        for (int i = 0; i < totalCells; i++)
        {
            var cell = cells[i]; // ← 순서대로 접근

            if (i < desiredCells)
            {
                if (!cell.gameObject.activeSelf)
                    cell.Activate(cell.GetComponent<RectTransform>().anchoredPosition, cellParent, true);
                else
                    cell.Activate(cell.GetComponent<RectTransform>().anchoredPosition, cellParent, false);
            }
            else
            {
                cell.Deactivate();
            }
        }
    }

    public void ResetBattery()
    {
        foreach (var cell in cells)
            cellPool.Return(cell);
        cells.Clear();
    }
}
