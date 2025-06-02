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

    // 공격 관련
    private float lastAttackTime = -1f;
    private int nextAttackIndex = 1;
    private const float attackDelay = 0.3f;
    private const float comboResetTime = 0.5f;
    private bool canAttack = true;
    public Transform attackPoint;     // 검 끝 위치
    public float attackRange = 0.5f;  // 공격 범위
    public LayerMask enemyLayer;      // 공격 대상 레이어
    public int attackDamage = 10;

    private bool isKnockbacked = false;
    private float knockbackTimer = 0f;
    private float knockbackDuration = 0.2f;

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
    private float wallJumpDuration = 0.3f;
    private bool isExitingWallSlide = false;
    private float wallSlideExitTimer = 0f;
    private const float wallSlideExitDelay = 0.05f;
    private float wallJumpBufferTime = 0.15f;
    private float lastWallSlideTime = -999f;
    private int lastWallSlideDir = 0; // 1 = 오른쪽 벽, -1 = 왼쪽 벽


    // 대쉬 관련
    private bool isDashing = false;
    private bool canDash = true;
    private float dashCooldown = 0.5f;
    private float dashSpeed = 20f;
    private float dashDistance = 3f;
    private float dashTraveled = 0f;
    private Vector2 dashDir;
    private float dashDuration = 0.2f;
    private float dashTimer = 0f;

    // 생존 관련
    public PlayerModeSwitcher switcher;
    private bool isInvincible = false;
    public bool IsInvincible => isInvincible;
    private float invincibleDuration = 0.5f;

    // 힐 관련
    private Coroutine healCoroutine;
    private bool isHealing = false;
    private float healCooldown = 0.5f;
    private float lastHealTime = -Mathf.Infinity;

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
        switcher = GetComponentInParent<PlayerModeSwitcher>();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Jump.started += ctx => OnJumpStart();
        inputActions.Player.Jump.canceled += ctx => OnJumpCancel();
        inputActions.Player.Attack.performed += ctx => Attack();
        inputActions.Player.Skill.performed += ctx => Skill();
        inputActions.Player.Dash.performed += ctx => TryDash();
        inputActions.Player.Heal.started += ctx => StartHeal();
        inputActions.Player.Heal.canceled += ctx => CancelHeal();
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void OnJumpStart()
    {
        bool recentlyWallSliding = (Time.time - lastWallSlideTime) <= wallJumpBufferTime;

        if (isWallSliding || recentlyWallSliding)
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
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 2f / 3f);
            isJumpHeld = true;
            canDoubleJump = false;
            animator.SetTrigger("DoubleJump");
        }
    }

    void OnJumpCancel() => isJumpHeld = false;

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        bool hitCeiling = Physics2D.OverlapCircle(headCheck.position, headCheckRadius, ceilingLayer);

        if (hitCeiling && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        if (!wasGroundedLastFrame && isGrounded)
            animator.SetTrigger("land");
        wasGroundedLastFrame = isGrounded;

        if (animator.GetBool("isFalling") && isGrounded)
        {
            animator.SetBool("isFalling", false);
            animator.SetTrigger("land");
        }

        if (!isWallJumping && !isJumpHeld && rb.linearVelocity.y > 0f && !isGrounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        animator.SetBool("isRunning", isGrounded && Mathf.Abs(moveInput.x) > 0.1f);
        animator.SetBool("isJumping", !isGrounded && rb.linearVelocity.y > 0.1f);
        animator.SetBool("isFalling", !isGrounded && rb.linearVelocity.y < -0.1f);

        // 애니메이션 설정
        if (isGrounded)
        {
            animator.SetBool("isRunning", Mathf.Abs(moveInput.x) > 0.1f);
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);
            animator.ResetTrigger("DoubleJump");
            if (isWallSliding)
            {
                isWallSliding = false;
                animator.ResetTrigger("wallSlide");
            }
        }
        else if (isWallSliding)
        {
            animator.SetBool("isRunning", false);
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);
            animator.ResetTrigger("WallJump");
            animator.SetTrigger("wallSlide");
        }
        else if (isWallJumping && !isExitingWallSlide)
        {
            isExitingWallSlide = true;
            wallSlideExitTimer = wallSlideExitDelay;
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);
        }
        else
        {
            animator.SetBool("isRunning", false);
            animator.SetBool("isJumping", rb.linearVelocity.y > 0.1f);
            animator.SetBool("isFalling", rb.linearVelocity.y < -0.1f);
        }

        if (!isWallJumping && !isExitingWallSlide)
        {
            if (moveInput.x > 0.01f)
            {
                transform.localScale = new Vector3(4, 4, 4);
                isFacingRight = true;
            }
            else if (moveInput.x < -0.01f)
            {
                transform.localScale = new Vector3(-4, 4, 4);
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

        if (!isGrounded && rb.linearVelocity.y < -0.5f && touchingWall)
        {
            lastWallSlideTime = Time.time;
            bool isDownInput = moveInput.y < -0.1f;

            if (!isWallJumping && !isExitingWallSlide && sameDirAsWall && !isDownInput)
            {
                if (!isWallSliding)
                {
                    isWallSliding = true;
                    animator.ResetTrigger("wallSlide");
                    animator.SetTrigger("wallSlide");
                    canDoubleJump = true;
                }

                animator.SetBool("isFalling", false);
                animator.SetBool("isJumping", false);
                lastWallSlideDir = isFacingRight ? 1 : -1;
            }
            else
            {
                if (isWallSliding)
                {
                    isWallSliding = false;
                    animator.ResetTrigger("wallSlide");
                }
            }
        }
        else
        {
            if (isWallSliding)
            {
                isWallSliding = false;
                animator.ResetTrigger("wallSlide");
            }
        }

        if (isGrounded)
            canDoubleJump = true;

        if (isExitingWallSlide)
        {
            wallSlideExitTimer -= Time.deltaTime;
            if (wallSlideExitTimer <= 0f)
            {
                isWallSliding = false;
                isExitingWallSlide = false;
                animator.ResetTrigger("wallSlide");
            }
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            rb.linearVelocity = dashDir * dashSpeed;
            dashTimer -= Time.fixedDeltaTime;

            if (dashTimer <= 0f)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                isDashing = false;

                animator.ResetTrigger("Dash");
                animator.SetBool("isRunning", Mathf.Abs(moveInput.x) > 0.1f);
                animator.SetBool("isJumping", !isGrounded && rb.linearVelocity.y > 0.1f);
                animator.SetBool("isFalling", !isGrounded && rb.linearVelocity.y < -0.1f);
                animator.SetBool("wallSlide", isWallSliding);

                Invoke(nameof(ResetDash), dashCooldown);
            }

            return;
        }

        if (isKnockbacked)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            if (knockbackTimer <= 0f)
                isKnockbacked = false;
            return;
        }

        if (isWallJumping)
        {
            // 벽 점프 중엔 기존 벡터 유지 (X를 덮어쓰지 않음!)
            Vector2 velocity = rb.linearVelocity;

            // 중력에 의한 낙하속도만 제한
            const float maxFallSpeed = -12f;
            if (velocity.y < maxFallSpeed)
                velocity.y = maxFallSpeed;

            rb.linearVelocity = new Vector2(velocity.x, velocity.y);
        }
        else
        {
            Vector2 velocity = rb.linearVelocity;
            float targetX = moveInput.x * moveSpeed;

            const float maxFallSpeed = -12f;
            if (!isWallSliding && velocity.y < maxFallSpeed)
                velocity.y = maxFallSpeed;

            if (isWallSliding)
                velocity.y = Mathf.Max(velocity.y, -wallSlideSpeed);

            rb.linearVelocity = new Vector2(targetX, velocity.y);
        }
    }

    public void AttackHitbox()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            var enemyScript = enemy.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(attackDamage, this);

                // 넉백
                ApplyKnockback(enemy.transform.position, 3f);
            }
        }
    }

    void Attack()
    {
        float currentTime = Time.time;
        if (!canAttack || (isWallSliding && !isWallJumping)) return;

        if (currentTime - lastAttackTime > comboResetTime)
            nextAttackIndex = 1;

        string triggerName = $"Attack{nextAttackIndex}";
        animator.ResetTrigger("Attack1");
        animator.ResetTrigger("Attack2");
        animator.SetTrigger(triggerName);

        nextAttackIndex = (nextAttackIndex == 1) ? 2 : 1;
        lastAttackTime = currentTime;
        canAttack = false;
        Invoke(nameof(ResetAttackDelay), attackDelay);
    }

    void ResetAttackDelay() => canAttack = true;

    public void OnAttackEnd()
    {
        animator.SetBool("isJumping", !isGrounded && rb.linearVelocity.y > 0.1f);
        animator.SetBool("isFalling", !isGrounded && rb.linearVelocity.y < -0.1f);
    }

    public void AttackHit()
    {
        if (switcher.GainMana(10))
        { }
    }

    public void ApplyKnockback(Vector2 sourcePosition, float knockbackForce = 3f)
    {
        float direction = transform.position.x > sourcePosition.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * knockbackForce, rb.linearVelocity.y);

        isKnockbacked = true;
        knockbackTimer = knockbackDuration;
    }


    void Skill()
    {
        if (isUsingSkill) return;
        isUsingSkill = true;
        animator.SetTrigger("Skill");
    }

    public void EndSkill() => isUsingSkill = false;

    public void landEnd() => animator.ResetTrigger("land");

    void TryDash()
    {
        if (!canDash || isDashing || isWallSliding) return;

        isDashing = true;
        canDash = false;
        dashTraveled = 0f;
        dashTimer = dashDuration;

        animator.SetTrigger("Dash");

        dashDir = moveInput.x != 0 ? new Vector2(Mathf.Sign(moveInput.x), 0) : (isFacingRight ? Vector2.right : Vector2.left);
    }

    void ResetDash() => canDash = true;

    void PerformWallJump()
    {
        isWallJumping = true;
        isWallSliding = false;

        animator.ResetTrigger("wallSlide");
        animator.SetTrigger("WallJump");

        // 벽 방향: 마지막 슬라이딩 방향 기준 (입력 무시)
        int wallDir = lastWallSlideDir != 0 ? lastWallSlideDir : (isFacingRight ? 1 : -1);

        Vector2 jumpDir = new Vector2(-wallDir * 0.3f * wallJumpForce, 0.8f * wallJumpForce);
        rb.linearVelocity = jumpDir;

        // 시선 전환
        isFacingRight = wallDir == -1;
        transform.localScale = new Vector3(isFacingRight ? 4 : -4, 4, 4);

        Invoke(nameof(EndWallJump), wallJumpDuration);
    }

    void EndWallJump()
    {
        isWallJumping = false;
        rb.gravityScale = 3f;
        isExitingWallSlide = true;
        wallSlideExitTimer = wallSlideExitDelay;
    }


    public void TakeDamage(int amount)
    {
        if (isInvincible) return;

        if (isHealing)
            StopHealing();

        isInvincible = true;
        StartCoroutine(InvincibilityTimer());

        switcher.ApplyDamage(amount);
    }

    private IEnumerator InvincibilityTimer()
    {
        yield return new WaitForSeconds(invincibleDuration);
        isInvincible = false;
    }

    void StartHeal()
    {
        // 기본 조건
        if (isHealing || !isGrounded || Time.time - lastHealTime < healCooldown)
            return;

        if (moveInput.sqrMagnitude > 0.01f) // 이동 중 체크
            return;

        // 마나 부족 시 힐 불가
        if (switcher == null || switcher.currentMana < 30)
            return;

        animator.ResetTrigger("HealStop");
        healCoroutine = StartCoroutine(HealRoutine());
    }


    void CancelHeal()
    {
        if (isHealing)
            StopHealing();
    }

    IEnumerator HealRoutine()
    {
        isHealing = true;
        animator.SetTrigger("Heal");

        float healDuration = 2f;
        float timer = 0f;

        while (timer < healDuration)
        {
            if (!isGrounded || moveInput.sqrMagnitude > 0.01f)
            {
                StopHealing();
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (switcher != null)
        {
            if (switcher.currentHealth < switcher.maxHealth && switcher.SpendMana(30))
            {
                switcher.currentHealth++;
                switcher.healthUI.UpdateHealthUI(switcher.currentHealth);
                animator.SetTrigger("HealStop");
            }
            else if (switcher.SpendMana(30))
            {
                StopHealing();
            }
        }

        lastHealTime = Time.time;
        isHealing = false;
        healCoroutine = null;
    }

    void StopHealing()
    {
        if (healCoroutine != null)
        {
            StopCoroutine(healCoroutine);
            animator.SetTrigger("HealStop");
            healCoroutine = null;
        }
        isHealing = false;
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
        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}
