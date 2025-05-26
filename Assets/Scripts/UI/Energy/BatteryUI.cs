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
        float totalCellWidth = spacing * desiredCells;
        float startX = cellParent.rect.width - totalCellWidth;

        while (cells.Count > desiredCells)
        {
            var last = cells[^1];
            cellPool.Return(last);
            cells.RemoveAt(cells.Count - 1);
        }

        while (cells.Count < desiredCells)
        {
            var cell = cellPool.Get();
            float x = startX + cells.Count * spacing;
            Vector2 pos = new Vector2(x, 0);
            cell.Activate(pos, cellParent, true);
            cells.Add(cell);
        }
    }


    public void ResetBattery()
    {
        foreach (var cell in cells)
            cellPool.Return(cell);
        cells.Clear();
    }
}
