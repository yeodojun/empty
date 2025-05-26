using System.Collections.Generic;
using UnityEngine;

public class BatteryPool : MonoBehaviour
{
    public GameObject batteryPrefab;
    private Queue<BatteryUI> pool = new Queue<BatteryUI>();

    public BatteryUI Get(Transform parent)
    {
        BatteryUI ui = pool.Count > 0 ? pool.Dequeue() : Instantiate(batteryPrefab).GetComponent<BatteryUI>();
        RectTransform rt = ui.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = Vector2.zero;
        ui.transform.localScale = Vector3.one;
        ui.gameObject.SetActive(true);
        return ui;
    }

    public void Return(BatteryUI ui)
    {
        ui.ResetBattery();
        ui.gameObject.SetActive(false);
        pool.Enqueue(ui);
    }
}
