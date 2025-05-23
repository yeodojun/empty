using UnityEngine;

public class PlayerModeSwitcher : MonoBehaviour
{
    public GameObject normalPlayer;
    public GameObject glitchPlayer;

    private PlayerInputActions inputActions;
    private bool isGlitch = false;

    private float lastSwitchTime = -Mathf.Infinity;
    private float switchCooldown = 3f;

    void Awake()
    {
        inputActions = new PlayerInputActions();
        inputActions.Player.ModeSwitch.performed += ctx => TryToggleMode();
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

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
