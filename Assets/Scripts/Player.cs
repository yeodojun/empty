using System.Collections;
using UnityEngine;
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Player : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
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
    private const float attackDelay = 0.3f;
    private const float comboResetTime = 0.5f;
    private bool canAttack = true;

    private bool isUsingSkill = false;

    private bool canDoubleJump = false;

    // 벽 관련
    public Transform wallCheck;
    public float wallCheckDistance = 0.5f;
    public LayerMask wallLayer;

    private bool isWallSliding = false;
    private float wallSlideSpeed = 1f;
    private bool isFacingRight = true;
    private bool isWallJumping = false;
    private float wallJumpForce = 18f;
    private float wallJumpDuration = 0.2f;

    // 대쉬 관련
    private bool isDashing = false;
    private bool canDash = true;
    private float dashCooldown = 0.5f;
    private float dashSpeed = 20f;
    private float dashDistance = 3f;
    private float dashTraveled = 0f;
    private Vector2 dashDir;

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

        inputActions.Player.Jump.started += ctx => OnJumpStart();
        inputActions.Player.Jump.canceled += ctx => OnJumpCancel();

        inputActions.Player.Attack.performed += ctx => Attack();
        inputActions.Player.Skill.performed += ctx => Skill();

        inputActions.Player.Dash.performed += ctx => TryDash();

    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void OnJumpStart()
    {
        if (isWallSliding)
        {
            PerformWallJump();
            return;
        }

        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isJumpHeld = true;
        }
        else if (canDoubleJump)
        {
            // 더블 점프는 기존 점프력의 2/3로
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 2f / 3f);
            isJumpHeld = true;
            canDoubleJump = false; // 한 번만 가능
            animator.SetTrigger("DoubleJump");
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

        if (animator.GetBool("isFalling") && isGrounded)
        {
            animator.SetBool("isFalling", false);  // Fall 애니메이션 종료
            animator.SetTrigger("land");           // land 트리거도 실행
        }
        // 점프 키를 떼면 상승 멈춤 (가변 점프 구현)
        if (!isWallJumping && !isJumpHeld && rb.linearVelocity.y > 0f && !isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }
        // 애니메이션 설정
        if (isGrounded)
        {
            animator.SetBool("isRunning", Mathf.Abs(moveInput.x) > 0.1f);
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);
            animator.ResetTrigger("DoubleJump");
        }
        else if (isWallSliding)
        {
            animator.SetBool("isRunning", false);
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);
            animator.ResetTrigger("wallSlide");
            animator.ResetTrigger("WallJump");
            animator.SetTrigger("wallSlide"); // 벽 슬라이드 애니메이션 유지
        }
        else
        {
            animator.SetBool("isRunning", false);

            if (isWallJumping)
            {
                animator.SetBool("isJumping", false);
                animator.SetBool("isFalling", false);
            }
            else
            {
                animator.SetBool("isJumping", rb.linearVelocity.y > 0.1f);
                animator.SetBool("isFalling", rb.linearVelocity.y < -0.1f);
            }
        }


        if (!isWallJumping)  // 벽점프 중에는 방향 고정
        {
            if (moveInput.x > 0.01f)
            {
                transform.localScale = new Vector3(1, 1, 1);
                isFacingRight = true;
            }
            else if (moveInput.x < -0.01f)
            {
                transform.localScale = new Vector3(-1, 1, 1);
                isFacingRight = false;
            }
        }


        bool touchingWall = Physics2D.Raycast(
            wallCheck.position,
            isFacingRight ? Vector2.right : Vector2.left,
            wallCheckDistance,
            wallLayer
        );

        float inputX = moveInput.x;
        bool sameDirAsWall = (isFacingRight && inputX > 0) || (!isFacingRight && inputX < 0);

        if (isWallJumping)
            return;
        if (!isGrounded && rb.linearVelocity.y < -0.5f && touchingWall && sameDirAsWall)
        {
            if (!isWallSliding)
            {
                isWallSliding = true;
                animator.ResetTrigger("wallSlide");
                animator.SetTrigger("wallSlide");
            }
            animator.SetBool("isFalling", false);
            animator.SetBool("isJumping", false);

            // 중력에 맡기되 제한 없음
        }
        else
        {
            if (isWallSliding)
            {
                isWallSliding = false;
                animator.ResetTrigger("wallSlide");
                animator.SetBool("isFalling", true);
            }
        }

        if (isGrounded)
        {
            canDoubleJump = true; // 착지하면 초기화
        }

    }

    void FixedUpdate()
    {
        if (isWallJumping)
            return;
        if (isDashing)
        {
            float moveStep = dashSpeed * Time.fixedDeltaTime;
            rb.linearVelocity = dashDir * dashSpeed;
            dashTraveled += moveStep;

            if (dashTraveled >= dashDistance)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);  // 이동 중지
                isDashing = false;

                // 애니메이션 전환
                animator.SetBool("isRunning", Mathf.Abs(moveInput.x) > 0.1f);
                animator.SetBool("isJumping", !isGrounded && rb.linearVelocity.y > 0.1f);
                animator.SetBool("isFalling", !isGrounded && rb.linearVelocity.y < -0.1f);
                animator.SetBool("wallSlide", isWallSliding);

                // 쿨타임 시작
                Invoke(nameof(ResetDash), dashCooldown);
            }

            return; // 대쉬 중이면 일반 이동 무시
        }

        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -wallSlideSpeed, float.MaxValue));
        }
    }


    void Attack()
    {
        float currentTime = Time.time;

        if (!canAttack || isWallSliding) return;

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

    public void landEnd()
    {
        animator.ResetTrigger("land");
    }

    void TryDash()
    {
        if (!canDash || isDashing || isWallSliding) return;

        isDashing = true;
        canDash = false;
        dashTraveled = 0f;
        animator.SetTrigger("Dash");

        // 입력 방향 또는 바라보는 방향
        dashDir = moveInput.x != 0 ? new Vector2(Mathf.Sign(moveInput.x), 0) : (isFacingRight ? Vector2.right : Vector2.left);
    }

    void ResetDash()
    {
        canDash = true;
    }

    void PerformWallJump()
    {
        isWallJumping = true;
        isWallSliding = false;

        animator.ResetTrigger("wallSlide");
        animator.SetTrigger("WallJump");

        // 벽 방향 기억
        int wallDir = isFacingRight ? 1 : -1;

        // x 방향 힘을 줄이고 y 방향을 강조
        Vector2 jumpDir = new Vector2(-wallDir * 0.3f * wallJumpForce, 0.8f * wallJumpForce);
        rb.linearVelocity = jumpDir;

        Invoke(nameof(EndWallJump), wallJumpDuration);
    }



    void EndWallJump()
    {
        isWallJumping = false;
        rb.gravityScale = 3f; // 원래 중력 복구
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
        if (wallCheck != null)
        {
            Gizmos.color = Color.green;
            Vector3 dir = Vector3.right * (isFacingRight ? 1 : -1);
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + dir * wallCheckDistance);
        }
    }
}
