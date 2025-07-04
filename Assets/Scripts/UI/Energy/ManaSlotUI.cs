using System.Collections.Generic;
using UnityEngine;

public class ManaSlotUI : MonoBehaviour
{
    [SerializeField] private RectTransform cellContainer;
    private List<ManaCellUI> cells = new List<ManaCellUI>();
    private ManaCellPool cellPool;
    private int maxCells;
    private float cellSpacing;
    public int MaxCells => maxCells;
    public int CurrentCellCount => cells.Count;

    // 한 슬롯에 최대 cellCount(=10)개 셀 세팅
    public void Init(ManaCellPool pool, int maxCells, float cellSpacing)
    {
        this.cellPool = pool;
        this.maxCells = maxCells;
        this.cellSpacing = cellSpacing;
    }

    // 오로지 제거
    public void RemoveCells(int removeCount)
    {
        for (int i = 0; i < removeCount && cells.Count > 0; i++)
        {
            // 리스트의 마지막 요소가 왼쪽 위치이므로 마지막부터 제거
            var c = cells[cells.Count - 1];
            cells.RemoveAt(cells.Count - 1);
            cellPool.Return(c);
        }
        RepositionCells();
    }

    // 오로지 추가
    public void AddCells(int addCount)
    {
        int current = cells.Count;
        int target = Mathf.Min(maxCells, current + addCount);

        for (int i = current; i < target; i++)
        {
            var cell = cellPool.Get();
            cell.transform.SetParent(cellContainer, false);
            cell.gameObject.SetActive(true);  // 풀에서 꺼낸 셀 활성화
            cell.SetFilled();                 // 채움 상태 적용
            cells.Add(cell);
        }
        RepositionCells();
    }

    private void RepositionCells()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            // index 0 -> 오른쪽, index 증가할수록 왼쪽으로
            float x = ((maxCells - 1) / 2f - i) * cellSpacing;
            cells[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(x, 0);
        }
    }

    public void GlowAllCells()
    {
        foreach (var cell in cells)
            cell.Glow();
    }

    public void ResetGlowAllCells()
    {
        foreach (var cell in cells)
            cell.ResetState();
    }

    // 반환할 때
    public void Cleanup()
    {
        foreach (var c in cells)
            cellPool.Return(c);
        cells.Clear();
    }
}
