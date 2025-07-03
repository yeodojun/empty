using System.Collections.Generic;
using UnityEngine;

public class ManaCellPool : MonoBehaviour
{
    [SerializeField] private GameObject cellPrefab;
    private Queue<ManaCellUI> pool = new Queue<ManaCellUI>();

    public ManaCellUI Get()
    {
        if (pool.Count > 0) return pool.Dequeue();
        return Instantiate(cellPrefab).GetComponent<ManaCellUI>();
    }

    public void Return(ManaCellUI cell)
    {
        cell.gameObject.SetActive(false);
        pool.Enqueue(cell);
    }
}
