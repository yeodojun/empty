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

    public int maxHealth = 5;
    public int currentHealth = 5;
    public int maxMana = 100;
    public int currentMana = 100;
    public ManaUIManager manaUIManager;

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
        if (manaUIManager != null)
        {
            int initialSlots = Mathf.Clamp(maxMana / manaUIManager.cellsPerSlot, 1, manaUIManager.slotCount);
            manaUIManager.ResetSlots(initialSlots);
            manaUIManager.SetMana(currentMana);
        }
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
            inputActions.Disable();

            if (animator != null)
            {
                animator.ResetTrigger("Hit"); // 사망 전 Hit 무효화
                animator.SetTrigger("Death");
            }
            var playerScript = active.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.currentState = PlayerActionState.Death;
                playerScript.enabled = false;
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
        Debug.Log($"[Mana] After Spend: {currentMana}");
        UpdateManaUI();
        return true;
    }

    public bool GainMana(int amount)
    {
        if (currentMana >= maxMana) return false;
        currentMana = Mathf.Min(currentMana + amount, maxMana);
        UpdateManaUI();
        return true;
    }

    private void UpdateManaUI()
    {
        if (manaUIManager != null)
            manaUIManager.SetMana(currentMana);
    }

    public void SyncManaUI()
    {
        UpdateManaUI();
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
        to.position = from.position;
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
