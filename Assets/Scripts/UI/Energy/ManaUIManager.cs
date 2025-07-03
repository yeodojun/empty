using System.Collections.Generic;
using UnityEngine;

public class ManaUIManager : MonoBehaviour
{
    [SerializeField] private ManaSlotPool slotPool;
    [SerializeField] private ManaCellPool cellPool;
    [SerializeField] private RectTransform uiParent;

    [Header("설정")]
    [Range(1, 3)] public int slotCount = 1;       // 기본 1, 최대 3
    public int cellsPerSlot = 10;                // 슬롯당 10
    public int manaPerCell = 10;                // 마나 셀 당 10
    public float slotSpacing = 120f;             // 슬롯끼리 간격
    public float cellSpacing = 20f;              // 셀 간격

    private List<ManaSlotUI> slots = new List<ManaSlotUI>();
    private int maxMana => slotCount * cellsPerSlot * manaPerCell;
    private int currentMana;

    private void CreateSlots()
    {
        for (int i = 0; i < slotCount; i++)
        {
            var slot = slotPool.Get();
            slot.transform.SetParent(uiParent, false);
            float x = -i * slotSpacing;
            slot.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, 0);
            slot.Init(cellPool, cellsPerSlot, cellSpacing);
            slots.Add(slot);
        }
    }

    public bool Spend(int amount)
    {
        if (currentMana < amount) return false;

        // 얼마나 셀을 제거할지 계산
        int removeCells = amount / manaPerCell;
        currentMana -= removeCells * manaPerCell;
        // 슬롯별로 차례대로 제거
        int remain = removeCells;
        foreach (var slot in slots)
        {
            int cur = slot.CurrentCellCount;
            int toRemove = Mathf.Min(cur, remain);
            slot.RemoveCells(toRemove);
            remain -= toRemove;
            if (remain <= 0) break;
        }
        return true;
    }

    public void Gain(int amount)
    {
        int addCells = amount / manaPerCell;
        currentMana = Mathf.Min(maxMana, currentMana + addCells * manaPerCell);

        int remain = addCells;
        foreach (var slot in slots)
        {
            int cur = slot.CurrentCellCount;
            int canAdd = Mathf.Min(slot.MaxCells - cur, remain);
            slot.AddCells(canAdd);
            remain -= canAdd;
            if (remain <= 0) break;
        }
    }

    public void SetMana(int value)
    {
        currentMana = Mathf.Clamp(value, 0, maxMana);
        int remain = currentMana;

        foreach (var slot in slots)
        {
            // 이 슬롯에 있어야 할 셀 개수
            int desiredCells = Mathf.Min(remain / manaPerCell, slot.MaxCells);
            // 현재 슬롯에 있는 셀 개수
            int currentCells = slot.CurrentCellCount;
            // 차이 계산
            int diff = desiredCells - currentCells;

            if (diff > 0)
            {
                // 부족한 만큼 추가
                slot.AddCells(diff);
            }
            else if (diff < 0)
            {
                // 초과된 만큼 제거
                slot.RemoveCells(-diff);
            }

            // 이 슬롯에서 사용한 마나만큼 남은값 차감
            remain -= desiredCells * manaPerCell;
        }
    }


    // 슬롯 수 변경 시 호출
    public void ResetSlots(int newCount)
    {
        // 기존 슬롯 반환
        foreach (var s in slots) slotPool.Return(s);
        slots.Clear();

        slotCount = Mathf.Clamp(newCount, 1, 3);
        CreateSlots();
        // 현재 마나 값으로 다시 채움/회수
        SetMana(currentMana);
    }

}
