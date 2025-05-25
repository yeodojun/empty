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

        to.position = from.position;

        Rigidbody2D fromRb = from.GetComponent<Rigidbody2D>();
        Rigidbody2D toRb = to.GetComponent<Rigidbody2D>();
        toRb.linearVelocity = fromRb.linearVelocity;

        Vector3 scale = to.localScale;
        scale.x = Mathf.Sign(from.localScale.x) * Mathf.Abs(scale.x);
        to.localScale = scale;

        normalPlayer.SetActive(!isGlitch);
        glitchPlayer.SetActive(isGlitch);
    }
}
