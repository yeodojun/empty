using System;
using System.Collections;
using UnityEngine;

// 상태 우선 순위 높을 수록 우선 순위
public enum PlayerActionState
{
    None = 0,
    Idle = 1,
    Run = 2,
    Heal = 3,
    Land = 4,
    Jump = 5,
    Fall = 6,
    WallSlide = 7,
    DoubleJump = 8,
    WallJump = 9,
    Attack = 9,
    Charge = 9,
    Skill = 9,
    Guard = 9,
    Dash = 10,
    Hit = 11,
    Death = 12
}
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Player : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private bool isJumpHeld = false;

    public float moveSpeed;
    public float jumpForce;

    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    public Transform headCheck;
    public float headCheckRadius = 0.1f;
    public LayerMask ceilingLayer;

    private bool isGrounded;
    private bool wasGroundedLastFrame = true;

    public PlayerActionState currentState = PlayerActionState.Idle;

    // 공격 관련
#if true
    private float lastAttackTime = -1f;
    private int nextAttackIndex = 1;
    private const float attackDelay = 0.5f;
    private const float comboResetTime = 0.8f;
    private bool canAttack = true;
    public Transform attackPoint;     // 검 끝 위치
    public Vector2 attackBoxsize = new Vector2(1.2f, 0.6f);  // 공격 범위
    public LayerMask enemyLayer;      // 공격 대상 레이어
    public int attackDamage = 10;
    // 윗공
    public Transform upAttackBoxCenter;
    public Vector2 upAttackBoxSize = new Vector2(1.2f, 0.6f);
    private enum AttackDirection { Forward, Upward, Downward, Wall }
    private AttackDirection currentAttackDir = AttackDirection.Forward;
    // 아랫공
    public Transform downAttackBoxCenter;
    public Vector2 downAttackBoxSize = new Vector2(1.2f, 0.6f);
    // 벽공
    public Transform wallAttackBoxCenter;
    public Vector2 wallAttackBoxSize = new Vector2(1.2f, 0.6f);

#endif

    // 넉백 관련
#if true
    private bool isKnockbacked = false;
    private float knockbackTimer = 0f;
    private float knockbackDuration = 0.2f;
#endif

    private bool isUsingSkill = false;
    private bool canDoubleJump = false;

    // 벽 관련
#if true
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
#endif


#if true
    // 대쉬 관련
    private bool isDashing = false;
    private bool canDash = true;
    private float dashCooldown = 0.5f;
    private float dashSpeed = 20f;
    private Vector2 dashDir;
    private float dashDuration = 0.2f;
    private float dashTimer = 0f;
#endif

    // 생존 관련
#if true
    public PlayerModeSwitcher switcher;
    private bool isInvincible = false;
    public bool IsInvincible => isInvincible;
    private float invincibleDuration = 0.5f;
#endif

    // 힐 관련
#if true
    private Coroutine healCoroutine;
    private bool isHealing = false;
    private float healCooldown = 0.5f;
    private float lastHealTime = -Mathf.Infinity;

#endif

    // 패링 관련
#if true
    [SerializeField] private Collider2D parryCollider;
    private bool isParrying;
    private bool isPerfectParryWindow;
    private bool isGuardSuccessWindow;
    private bool isParryBlocked;
    private bool isControlLocked;
    private bool isGuardHeld;
    private Coroutine parryCoroutine;
    public bool WasParryBlocked() => isParryBlocked;
#endif

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
        inputActions.Player.Guard.started += _ => StartParry();
        inputActions.Player.Guard.canceled += _ => StopParry();
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    // 대쉬
    void TryDash()
    {
        if (!canDash || isDashing || isWallSliding || isParrying || (int)currentState > (int)PlayerActionState.Dash) return;

        TrySetState(PlayerActionState.Dash);
        isDashing = true;
        canDash = false;
        dashTimer = dashDuration;

        animator.SetTrigger("Dash");

        dashDir = moveInput.x != 0 ? new Vector2(Mathf.Sign(moveInput.x), 0) : (isFacingRight ? Vector2.right : Vector2.left);
    }

    void ResetDash() => canDash = true;

    // 점프
    void OnJumpStart()
    {
        bool recentlyWallSliding = (Time.time - lastWallSlideTime) <= wallJumpBufferTime;

        if (isWallSliding || recentlyWallSliding)
        {
            PerformWallJump();
            return;
        }

        if (isParrying || (int)currentState > (int)PlayerActionState.Jump) return;

        if (isGrounded)
        {
            TrySetState(PlayerActionState.Jump);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isJumpHeld = true;
        }
        else if (canDoubleJump)
        {
            TrySetState(PlayerActionState.DoubleJump);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 2.7f / 3f);
            isJumpHeld = true;
            canDoubleJump = false;
            animator.SetTrigger("DoubleJump");
        }
    }

    void OnJumpCancel() => isJumpHeld = false;

    // 벽 점프
    void PerformWallJump()
    {
        TrySetState(PlayerActionState.WallJump);
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
        transform.localScale = new Vector3(isFacingRight ? 1 : -1, 1, 1);

        Invoke(nameof(EndWallJump), wallJumpDuration);
    }

    void EndWallJump()
    {
        isWallJumping = false;
        rb.gravityScale = 3f;
        isExitingWallSlide = true;
        wallSlideExitTimer = wallSlideExitDelay;
        currentState = PlayerActionState.Idle;
    }

    // 공격
    void Attack()
    {
        float currentTime = Time.time;
        if (!canAttack || isParrying || (int)currentState > (int)PlayerActionState.Attack) return;

        // 벽공
        if (isWallSliding)
        {
            currentAttackDir = AttackDirection.Wall;
            animator.SetTrigger("AttackWall");
            TrySetState(PlayerActionState.Attack);
            canAttack = false;
            Invoke(nameof(ResetAttackDelay), attackDelay);
            return;
        }

        // 윗공
        if (moveInput.y > 0.5f)
        {
            currentAttackDir = AttackDirection.Upward;
            animator.SetTrigger("AttackUp");
            canAttack = false;
            Invoke(nameof(ResetAttackDelay), attackDelay);
            return;
        }
        // 아랫공
        else if (moveInput.y < -0.5f && !isGrounded)
        {
            currentAttackDir = AttackDirection.Downward;
            animator.SetTrigger("AttackDown");
            canAttack = false;
            Invoke(nameof(ResetAttackDelay), attackDelay);
            return;
        }

        currentAttackDir = AttackDirection.Forward;

        TrySetState(PlayerActionState.Attack);

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

    public void OnAttackEnd()
    {
        currentState = PlayerActionState.Idle;
        isControlLocked = false;
        animator.SetBool("isJumping", !isGrounded && rb.linearVelocity.y > 0.1f);
        animator.SetBool("isFalling", !isGrounded && rb.linearVelocity.y < -0.1f);
    }

    void ResetAttackDelay()
    {
        canAttack = true;
    }

    // 스킬 1
    void Skill()
    {
        if (isUsingSkill || isParrying || (int)currentState > (int)PlayerActionState.Skill) return;

        TrySetState(PlayerActionState.Skill);
        isUsingSkill = true;
        animator.SetTrigger("Skill");
    }

    public void EndSkill()
    {
        isUsingSkill = false;
        currentState = PlayerActionState.Idle;
    }

    // 힐 관련
    void StartHeal()
    {
        // 기본 조건
        if (isHealing || !isGrounded || Time.time - lastHealTime < healCooldown || isParrying || (int)currentState > (int)PlayerActionState.Heal)
            return;

        if (moveInput.sqrMagnitude > 0.01f) // 이동 중 체크
            return;

        // 마나 부족 시 힐 불가
        if (switcher == null)
            return;
        if (switcher.currentMana < 30)
        {
            // 마나 UI에 글로우 효과 트리거
            if (switcher.manaUIManager != null)
                switcher.manaUIManager.Spend(30);
            return;
        }

        TrySetState(PlayerActionState.Heal);
        animator.ResetTrigger("HealStop");
        healCoroutine = StartCoroutine(HealRoutine());
    }
    void CancelHeal()
    {
        if (isHealing)
            StopHealing();
        currentState = PlayerActionState.Idle;
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
            if (switcher.SpendMana(30))
            {
                switcher.SyncManaUI();
                // 가장 최근 일반 하트 회복 시도
                bool healed = switcher.healthUI.HealLatestNormal();
                if (healed)
                {
                    // 일반 하트 회복에 성공했으면 현재 체력 +1
                    switcher.currentHealth++;
                    animator.SetTrigger("HealStop");
                }
                else
                {
                    StopHealing();
                }
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

    // 패링 시작
    void StartParry()
    {
        if (!isGrounded || isParrying || isHealing || isDashing || isUsingSkill || isInvincible || (int)currentState > (int)PlayerActionState.Guard) return;

        TrySetState(PlayerActionState.Guard);
        animator.ResetTrigger("GuardEnd");
        animator.ResetTrigger("Parrying");
        animator.ResetTrigger("Guarding");

        isGuardHeld = true;

        parryCoroutine = StartCoroutine(ParryRoutine());
    }

    // 패링 종료
    void StopParry() => isGuardHeld = false;

    // 패링 루틴
    IEnumerator ParryRoutine()
    {
        isControlLocked = isParrying = true;
        isPerfectParryWindow = isGuardSuccessWindow = true;
        parryCollider.enabled = true;
        animator.SetTrigger("Guard");

        yield return new WaitForSecondsRealtime(0.2f);
        isPerfectParryWindow = false;         // 0.2초 후 패링 창 종료

        while (isGuardHeld) yield return null; // 누르는 동안 가드 지속

        isGuardSuccessWindow = false;
        parryCollider.enabled = false;
        animator.SetTrigger("GuardEnd");
        ResetParryFlags();
    }

    void ResetParryFlags()
    {
        isParrying = isControlLocked = false;
        isParryBlocked = false;
        currentState = PlayerActionState.Idle;
    }

    // 강제 종료
    void ForceEndParry()
    {
        if (parryCoroutine != null)
        {
            StopCoroutine(parryCoroutine);
            parryCoroutine = null;
        }
        isGuardHeld = false;
        parryCollider.enabled = false;
        animator.SetTrigger("GuardEnd");
        isParrying = false;
        isControlLocked = false;
        isParryBlocked = false;
        currentState = PlayerActionState.Idle;
    }

    // dEATH
    public void HandleDeath()
    {
        currentState = PlayerActionState.Death;
    }

    private void TrySetState(PlayerActionState newState)
    {
        if ((int)newState >= (int)currentState)
        {
            currentState = newState;
        }
    }

    void Update()
    {
        if (currentState == PlayerActionState.Death)
            return;  // 사망 상태면 애니메이션 제어 중단
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
        if ((int)currentState >= (int)PlayerActionState.Attack && (int)currentState < (int)PlayerActionState.Hit)
            return;

        if (!isWallJumping && !isExitingWallSlide && !isControlLocked)
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

        if (!isGrounded && rb.linearVelocity.y < -0.5f && touchingWall)
        {
            lastWallSlideTime = Time.time;
            bool isDownInput = moveInput.y < -0.1f;

            if (!isWallJumping && !isExitingWallSlide && sameDirAsWall && !isDownInput)
            {
                if (!isWallSliding)
                {
                    isWallSliding = true;
                    lastWallSlideTime = Time.time;
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
                currentState = PlayerActionState.Idle;

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
        if (isControlLocked)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

    }

    public void AttackHitbox()
    {
        Collider2D[] hitEnemies = null;

        if (currentAttackDir == AttackDirection.Forward)
        {
            hitEnemies = Physics2D.OverlapBoxAll(attackPoint.position, attackBoxsize, 0f, enemyLayer);
        }
        else if (currentAttackDir == AttackDirection.Upward)
        {
            hitEnemies = Physics2D.OverlapBoxAll(upAttackBoxCenter.position, upAttackBoxSize, 0f, enemyLayer);
        }
        else if (currentAttackDir == AttackDirection.Downward)
        {
            hitEnemies = Physics2D.OverlapBoxAll(downAttackBoxCenter.position, downAttackBoxSize, 0f, enemyLayer);
        }
        else if (currentAttackDir == AttackDirection.Wall)
        {
            hitEnemies = Physics2D.OverlapBoxAll(wallAttackBoxCenter.position, wallAttackBoxSize, 0f, enemyLayer);
        }

        if (hitEnemies != null)
        {
            foreach (var enemy in hitEnemies)
            {
                var enemyScript = enemy.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    enemyScript.TakeDamage(attackDamage, this);
                    ApplyKnockback(enemy.transform.position, 3f);
                }
            }
        }
    }

    public void AttackHit()
    {
        if (switcher.GainMana(10))
        { switcher.SyncManaUI(); }
    }

    public void ApplyKnockback(Vector2 sourcePosition, float knockbackForce = 3f)
    {
        float direction = transform.position.x > sourcePosition.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * knockbackForce, rb.linearVelocity.y);

        isKnockbacked = true;
        knockbackTimer = knockbackDuration;
    }

    public void landEnd() => animator.ResetTrigger("land");

    // 데미지 처리
    public void TakeDamage(int amount)
    {
        if (isInvincible) return;

        if (isHealing)
            StopHealing();

        StartCoroutine(SlowTime(0.2f, 0.05f));
        isInvincible = true;
        StartCoroutine(InvincibilityTimer());

        TrySetState(PlayerActionState.Hit);
        switcher.ApplyDamage(amount);
        if (switcher.currentHealth <= 0)
            return;
        animator.SetTrigger("Hit");
    }

    private IEnumerator InvincibilityTimer()
    {
        yield return new WaitForSeconds(invincibleDuration);
        isInvincible = false;
    }

    // 공격 감지
    public void OnIncomingAttack(Vector2 attackerPos)
    {
        if (!isParrying) { isParryBlocked = false; return; }

        if (isPerfectParryWindow)
        {
            animator.SetTrigger("Parrying");
            isInvincible = true;
            switcher?.GainMana(50);
            switcher?.healthUI.OnParrySuccess();
            StartCoroutine(SlowTime(0.5f, 0.5f));
            StartCoroutine(InvincibilityTimer());
            isParryBlocked = true;
            ForceEndParry();
        }
        else if (isGuardSuccessWindow)
        {
            animator.SetTrigger("Guarding");
            isInvincible = true;
            ApplyKnockback(attackerPos, 4f);
            StartCoroutine(InvincibilityTimer());
            isParryBlocked = true;

            if (switcher.healthUI.IsBreakFull())
            {
                if (switcher.currentHealth == 1 && switcher.healthUI.IsLastHeartBreak())
                    switcher.healthUI.ActiveHearts[^1].SetBreakTremble();
                else
                    switcher.ApplyDamage(1);
            }
            else
            {
                switcher.healthUI.AddBreak();
            }
            ForceEndParry();
        }
        else
        {
            isParryBlocked = false;
        }
    }

    IEnumerator SlowTime(float duration, float slowScale)
    {
        Time.timeScale = slowScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
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
            Gizmos.DrawWireCube(attackPoint.position, attackBoxsize);
        }
        if (upAttackBoxCenter != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(upAttackBoxCenter.position, upAttackBoxSize);
        }
        if (downAttackBoxCenter != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(downAttackBoxCenter.position, downAttackBoxSize);
        }
        if (wallAttackBoxCenter != null)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireCube(wallAttackBoxCenter.position, wallAttackBoxSize);
        }

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!parryCollider.enabled || isParryBlocked) return;

        if (!other.CompareTag("EnemyAttack")) return;

        if (!other.TryGetComponent(out Enemy enemy)) return;

        Vector2 attackerPos = enemy.transform.position;

        if (isPerfectParryWindow)
        {
            animator.SetTrigger("Parrying");
            if (switcher.GainMana(50))
                switcher.SyncManaUI();
            StartCoroutine(SlowTime(0.5f, 0.5f));
        }
        else if (isGuardSuccessWindow)
        {
            animator.SetTrigger("Guarding");
            ApplyKnockback(attackerPos, 4f);
        }

        isParryBlocked = true;
        ForceEndParry();
    }
}
