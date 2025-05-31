using UnityEngine;

public class PlayerModeSwitcher : MonoBehaviour
{
    public GameObject normalPlayer;
    public GameObject glitchPlayer;
    public HealthUIController healthUI;

    private PlayerInputActions inputActions;

    private bool isGlitch = false;

    private float lastSwitchTime = -Mathf.Infinity;
    private float switchCooldown = 3f;

    public int maxHealth = 4;
    public int currentHealth = 4;

    public BatteryUI batteryUI; // BatteryUI 연결
    public int maxMana = 100;
    public int currentMana = 100;

    void Awake()
    {
        inputActions = new PlayerInputActions();
        inputActions.Player.ModeSwitch.performed += ctx => TryToggleMode();
    }

    void OnEnable()
    {
        inputActions.Enable();
        healthUI.SetMaxHealth(maxHealth);
        healthUI.UpdateHealthUI(currentHealth);
    }

    void OnDisable() => inputActions.Disable();

    public void ApplyDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        healthUI.UpdateHealthUI(currentHealth);

        GameObject active = isGlitch ? glitchPlayer : normalPlayer;
        Animator animator = active.GetComponent<Animator>();

        if (currentHealth <= 0)
        {
            Debug.Log("플레이어 사망");
            if (animator != null)
            {
                animator.ResetTrigger("Hit"); // 사망 전 Hit 무효화
                animator.SetTrigger("Death");
            }
            return;
        }

        if (animator != null)
            animator.SetTrigger("Hit");
    }

    public bool SpendMana(int amount)
    {
        if (currentMana < amount) return false;
        currentMana -= amount;
        batteryUI?.SpendMana(amount);
        return true;
    }

    public bool GainMana(int amount)
    {
        if (currentMana == maxMana) return false;
        currentMana += amount;
        batteryUI?.GainMana(amount);
        return true;
    }

    public void SyncManaUI()
    {
        batteryUI?.GainMana(0); // 강제 업데이트
    }

    void TryToggleMode()
    {
        GameObject active = isGlitch ? glitchPlayer : normalPlayer;
        Player player = active.GetComponent<Player>();

        if (player != null && player.IsInvincible)
            return;

        if (Time.time - lastSwitchTime < switchCooldown)
            return;

        ToggleMode();
        lastSwitchTime = Time.time;
    }


    void ToggleMode()
    {
        isGlitch = !isGlitch;

        GameObject fromObj = isGlitch ? normalPlayer : glitchPlayer;
        GameObject toObj = isGlitch ? glitchPlayer : normalPlayer;

        Transform from = fromObj.transform;
        Transform to = toObj.transform;

        to.position = from.position;

        // 먼저 켜주고 물리 정보 복사
        toObj.SetActive(true);

        // 컴포넌트 캐싱 방식 (최적화)
        var fromRb = from.GetComponent<Rigidbody2D>();
        var toRb = to.GetComponent<Rigidbody2D>();

        toRb.linearVelocity = fromRb.linearVelocity;

        Vector3 scale = to.localScale;
        scale.x = Mathf.Sign(from.localScale.x) * Mathf.Abs(scale.x);
        to.localScale = scale;

        fromObj.SetActive(false);

        SyncManaUI();
    }

}
