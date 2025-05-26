using UnityEngine;

public class BatteryPoolManager : MonoBehaviour
{
    public BatteryPool batteryPool;
    public BatteryCellPool cellPool;
    public int defaultMana = 100;

    void Start()
    {
        GameObject heartsContainer = GameObject.Find("HeartsContainer");
        if (heartsContainer == null)
            return;

        GameObject batteryParent = new GameObject("BatteryUIParent", typeof(RectTransform));
        batteryParent.transform.SetParent(heartsContainer.transform.parent, false);

        RectTransform rt = batteryParent.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(20f, -100f);

        BatteryUI battery = batteryPool.Get(rt);
        battery.Init(defaultMana, cellPool);

        var switcher = FindAnyObjectByType<PlayerModeSwitcher>();
        if (switcher != null)
        {
            switcher.batteryUI = battery;
            switcher.currentMana = defaultMana;
        }
    }
}
