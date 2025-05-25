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

        if (currentHealth <= 0)
        {
            Debug.Log("플레이어 사망");
            // 죽음 처리 필요 시 여기서
        }
    }

    void TryToggleMode()
    {
        if (Time.time - lastSwitchTime < switchCooldown)
            return;

        ToggleMode();
        lastSwitchTime = Time.time;
    }

    void ToggleMode()
    {
        isGlitch = !isGlitch;

        Transform from = isGlitch ? normalPlayer.transform : glitchPlayer.transform;
        Transform to = isGlitch ? glitchPlayer.transform : normalPlayer.transform;

        // 위치 및 속도 동기화
        to.position = from.position;

        Rigidbody2D fromRb = from.GetComponent<Rigidbody2D>();
        Rigidbody2D toRb = to.GetComponent<Rigidbody2D>();
        toRb.linearVelocity = fromRb.linearVelocity;

        // 방향 동기화
        Vector3 scale = to.localScale;
        scale.x = Mathf.Sign(from.localScale.x) * Mathf.Abs(scale.x);
        to.localScale = scale;

        normalPlayer.SetActive(!isGlitch);
        glitchPlayer.SetActive(isGlitch);
    }
}
