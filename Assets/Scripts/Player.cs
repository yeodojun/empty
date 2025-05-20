using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Player : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private bool isJumpPressed;
    private bool isJumpHeld = false;

    public float moveSpeed = 5f;
    public float jumpForce = 15f;

    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    public Transform headCheck;
    public float headCheckRadius = 0.1f;
    public LayerMask ceilingLayer;

    private bool isGrounded;
    private bool wasGroundedLastFrame = true;

    private float lastAttackTime = -1f;
    private int nextAttackIndex = 1;
    private const float attackDelay = 0.2f;
    private const float comboResetTime = 0.5f;
    private bool canAttack = true;

    private bool isUsingSkill = false;

    void Start()
    {
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        inputActions = new PlayerInputActions();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Jump.performed += ctx => isJumpPressed = true;
        inputActions.Player.Jump.started += ctx => OnJumpStart();
        inputActions.Player.Jump.canceled += ctx => OnJumpCancel();

        inputActions.Player.Attack.performed += ctx => Attack();
        inputActions.Player.Skill.performed += ctx => Skill();
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void OnJumpStart()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void OnJumpCancel()
    {
        isJumpHeld = false;
    }

    void Update()
    {
        // Ground & ceiling check
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        bool hitCeiling = Physics2D.OverlapCircle(headCheck.position, headCheckRadius, ceilingLayer);

        // 머리 부딪히면 상승 강제 중단
        if (hitCeiling && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }

        // 착지 시
        if (!wasGroundedLastFrame && isGrounded)
        {
            animator.SetTrigger("land");
        }
        wasGroundedLastFrame = isGrounded;

        // 애니메이션 설정
        animator.SetBool("isRunning", Mathf.Abs(moveInput.x) > 0.1f);
        animator.SetBool("isJumping", !isGrounded && rb.linearVelocity.y > 0.1f);
        animator.SetBool("isFalling", !isGrounded && rb.linearVelocity.y < -0.1f);

        // 스프라이트 반전
        if (moveInput.x > 0.01f)
            transform.localScale = new Vector3(10, 10, 10);
        else if (moveInput.x < -0.01f)
            transform.localScale = new Vector3(-10, 10, 10);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        isJumpPressed = false;
    }

    void Attack()
    {
        float currentTime = Time.time;
        if (!canAttack) return;

        if (currentTime - lastAttackTime > comboResetTime)
        {
            nextAttackIndex = 1;
        }

        string triggerName = $"Attack{nextAttackIndex}";
        animator.ResetTrigger("Attack1");
        animator.ResetTrigger("Attack2");
        animator.SetTrigger(triggerName);

        nextAttackIndex = (nextAttackIndex == 1) ? 2 : 1;
        lastAttackTime = currentTime;

        canAttack = false;
        Invoke(nameof(ResetAttackDelay), attackDelay);
    }

    void ResetAttackDelay()
    {
        canAttack = true;
    }

    public void OnAttackEnd()
    {
        animator.SetBool("isJumping", !isGrounded && rb.linearVelocity.y > 0.1f);
        animator.SetBool("isFalling", !isGrounded && rb.linearVelocity.y < -0.1f);
    }

    void Skill()
    {
        if (isUsingSkill) return;

        isUsingSkill = true;
        animator.SetTrigger("Skill");
    }

    public void EndSkill()
    {
        isUsingSkill = false;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (headCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(headCheck.position, headCheckRadius);
        }
    }
}
