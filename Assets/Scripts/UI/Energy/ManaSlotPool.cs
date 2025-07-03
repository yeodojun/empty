using System.Collections.Generic;
using UnityEngine;

public class ManaSlotPool : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    private Queue<ManaSlotUI> pool = new Queue<ManaSlotUI>();

    public ManaSlotUI Get()
    {
        if (pool.Count > 0) return pool.Dequeue();
        return Instantiate(slotPrefab).GetComponent<ManaSlotUI>();
    }

    public void Return(ManaSlotUI slot)
    {
        slot.Cleanup();
        slot.gameObject.SetActive(false);
        pool.Enqueue(slot);
    }
}
