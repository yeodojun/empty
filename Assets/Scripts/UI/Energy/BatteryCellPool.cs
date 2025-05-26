using System.Collections.Generic;
using UnityEngine;

public class BatteryCellPool : MonoBehaviour
{
    public GameObject cellPrefab;
    private Queue<BatteryCellUI> pool = new Queue<BatteryCellUI>();

    public BatteryCellUI Get()
    {
        if (pool.Count > 0) return pool.Dequeue();
        return Instantiate(cellPrefab).GetComponent<BatteryCellUI>();
    }

    public void Return(BatteryCellUI cell)
    {
        cell.Deactivate();
        pool.Enqueue(cell);
    }
}
