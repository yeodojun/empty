using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public enum BuffType { DamageUp, RangeUp, SkillManaDown, SkillRangeUp }

[Serializable]
public class BuffData
{
    public BuffType type;
    public Transform buffObject;   // 자식 오브젝트(DamageUp, RangeUp…)
    public Image iconImage;    // 아이콘 Image
    [HideInInspector] private Sprite defaultSprite;
    public Image timerImage;   // 라디얼 타이머 Image
    public Sprite[] levelSprites; // 단계별 스프라이트 (1~4단계)
    public float duration;     // 초기 지속시간
    public float[] multipliers;  // 단계별 배수 (예: {1.1f,1.2f,1.3f,1.4f})
    private Color originalColor; // 이미지 색깔

    [HideInInspector] public int level;
    [HideInInspector] public float timer;

    private Player player;
    private int baseValue; // DamageUp이면 baseDamage, RangeUp이면 baseRange 등

    public void Init(Player p, int baseVal)
    {
        player = p;
        baseValue = baseVal;
        level = 0;
        defaultSprite = iconImage.sprite;
        originalColor = iconImage.color;
        iconImage.color = Color.gray;
        buffObject.gameObject.SetActive(true);
        timerImage.gameObject.SetActive(false);
    }

    public void Activate()
    {
        if (level < levelSprites.Length)
        {
            level++;
            iconImage.sprite = levelSprites[level - 1];
            ApplyEffect();
        }
        // 이미 max 레벨이면 효과 유지
        timer = duration;
        buffObject.gameObject.SetActive(true);
        timerImage.gameObject.SetActive(true);
        UpdateTimerUI();
    }

    public void Update(float delta)
    {
        if (level == 0) return;
        timer -= delta;
        UpdateTimerUI();
        if (timer <= 0f)
            End();
    }

    private void UpdateTimerUI()
    {
        timerImage.fillAmount = Mathf.Clamp01(timer / duration);
    }

    private void ApplyEffect()
    {
        switch (type)
        {
            case BuffType.DamageUp:
                player.attackDamage = Mathf.RoundToInt(baseValue * multipliers[level - 1]);
                break;
            case BuffType.RangeUp:
                //player.attackRange = baseValue * multipliers[level - 1];
                break;
                // SkillManaDown, SkillRangeUp 등은 p. 스킬 매니저나 프로퍼티에 맞춰 추가
        }
    }

    private void End()
    {
        // 원상 복귀
        switch (type)
        {
            case BuffType.DamageUp:
                player.attackDamage = baseValue;
                break;
            case BuffType.RangeUp:
                //player.attackRange = baseValue;
                break;
                // 기타
        }
        level = 0;
        iconImage.sprite = defaultSprite;
        timerImage.gameObject.SetActive(false);
    }
}

public class BuffManager : MonoBehaviour
{
    [Header("버프 설정 리스트")]
    [SerializeField] List<BuffData> buffs;

    Player player;
    int baseDamage;
    int baseRange;
    public static BuffManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        player = UnityEngine.Object.FindFirstObjectByType<Player>();
        baseDamage = player.attackDamage;
        //baseRange = player.attackRange;

        // 각 BuffData 초기화
        foreach (var b in buffs)
        {
            b.Init(player, (b.type == BuffType.DamageUp) ? baseDamage : baseRange);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        foreach (var b in buffs)
            b.Update(dt);
    }

    // 외부에서 호출: 파링 시 BuffType.DamageUp, 아이템 먹을 땐 다른 타입 등
    public void ActivateBuff(BuffType type)
    {
        var buff = buffs.Find(x => x.type == type);
        if (buff != null)
            buff.Activate();
    }
}
