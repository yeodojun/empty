using System.Collections.Generic;
using UnityEngine;

public class HeartPool : MonoBehaviour
{
    public GameObject heartPrefab;
    private Queue<HeartUI> pool = new Queue<HeartUI>();

    public HeartUI Get()
    {
        if (pool.Count > 0)
            return pool.Dequeue();

        GameObject go = Instantiate(heartPrefab);
        return go.GetComponent<HeartUI>();
    }

    public void Return(HeartUI heart)
    {
        heart.Deactivate();
        pool.Enqueue(heart);
    }
}