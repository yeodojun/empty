using UnityEngine;
using UnityEngine.InputSystem; // 새 Input System 사용을 위한 네임스페이스

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Player : MonoBehaviour
{
    private PlayerInputActions inputActions;     // 자동 생성된 입력 클래스
    private Rigidbody2D rb;                      // 물리 제어용
    private Animator animator;                   // 애니메이션 제어용
    private Vector2 moveInput;                   // 이동 입력 값 저장용
    private bool isJumpPressed;                  // 점프 키 눌림 여부
    private bool isJumpHeld = false;             // 점프 버튼 누르고 있는 중인지

    public float moveSpeed = 5f;                 // 이동 속도
    public float jumpHeight = 2f;
    public float fallSpeed = 7f;
    private float fallTimer = 0f;
    private const float fallThreshold = 2f;
    private const float normalFallSpeed = 7f;
    private const float fastFallSpeed = 10f;
    public Transform headCheck;            // 머리 위 위치 (빈 오브젝트)
    public float headCheckRadius = 0.1f;   // 충돌 감지 반경
    public LayerMask ceilingLayer;         // 충돌할 벽/천장 레이어

    public Transform groundCheck;                // 땅 체크 위치 기준
    public float groundCheckRadius = 0.2f;       // 바닥 판정 범위
    public LayerMask groundLayer;                // 바닥 레이어 설정

    private bool isGrounded;                     // 현재 땅에 닿았는지 여부
    private bool wasGroundedLastFrame = true; // 이전 프레임에 땅에 있었는지 저장

    private bool isJumping = false;
    private float targetY = 0f;

    private float lastAttackTime = -1f;
    private int nextAttackIndex = 1;
    private const float attackDelay = 0.2f;
    private const float comboResetTime = 0.5f;
    private bool canAttack = true;

    private bool isUsingSkill = false;



    void Awake()
    {
        // 컴포넌트 참조 가져오기
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // InputAction 클래스 인스턴스 생성
        inputActions = new PlayerInputActions();

        // Move 입력값 처리 (좌우 방향 입력)
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // Jump 키 눌림 처리
        inputActions.Player.Jump.performed += ctx => isJumpPressed = true;

        // 공격 키 처리
        inputActions.Player.Attack.performed += ctx => Attack();

        // 공격 키 처리
        inputActions.Player.Skill.performed += ctx => Skill();

        inputActions.Player.Jump.started += ctx => OnJumpStart();    // 눌렀을 때
        inputActions.Player.Jump.canceled += ctx => OnJumpCancel();  // 뗐을 때
    }

    void OnEnable()
    {
        // 입력 활성화
        inputActions.Enable();
    }

    void OnDisable()
    {
        // 입력 비활성화
        inputActions.Disable();
    }

    void OnJumpStart()
    {
        if (isGrounded && !isJumping)
        {
            isJumping = true;
            isJumpHeld = true;
            targetY = transform.position.y + jumpHeight;
        }
    }

    void OnJumpCancel()
    {
        isJumpHeld = false; // 점프 중간에 멈춤
    }

    void Update()
    {
        // 바닥 판정
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // 착지 감지: 이전엔 공중, 지금은 땅 → 착지
        if (!wasGroundedLastFrame && isGrounded)
        {
            animator.SetTrigger("land");
        }

        // 다음 프레임을 위한 저장
        wasGroundedLastFrame = isGrounded;

        // 바닥 감지 (원형 오버랩으로 체크)
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // 애니메이션 설정: 달리기
        animator.SetBool("isRunning", Mathf.Abs(moveInput.x) > 0.1f);

        // 애니메이션 설정: 점프
        animator.SetBool("isJumping", !isGrounded && isJumping);

        // 애니메이션 설정: 하강
        animator.SetBool("isFalling", !isGrounded && !isJumping);

        // 이동 방향에 따라 좌우 반전
        if (moveInput.x > 0.01f)
        {
            transform.localScale = new Vector3(10, 10, 10); // 오른쪽 바라봄
        }
        else if (moveInput.x < -0.01f)
        {
            transform.localScale = new Vector3(-10, 10, 10); // 왼쪽 바라봄
        }

        // 점프 입력 처리
        if (isJumpPressed && isGrounded && !isJumping)
        {
            isJumping = true;
            targetY = transform.position.y + jumpHeight;
        }
        isJumpPressed = false;

        if (isJumping)
        {
            if (!isJumpHeld || transform.position.y >= targetY)
            {
                isJumping = false;
                fallTimer = 0f;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position,
                                                         new Vector3(transform.position.x, targetY, transform.position.z),
                                                         fallSpeed * Time.deltaTime);
            }
        }
        else if (!isGrounded)
        {
            // 낙하 중 시간 누적
            fallTimer += Time.deltaTime;

            // 낙하 2초 이상이면 빠르게 떨어지게 설정
            fallSpeed = (fallTimer >= fallThreshold) ? fastFallSpeed : normalFallSpeed;

            // 낙하 적용
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        }
        else
        {
            // 착지 시 초기화
            fallSpeed = normalFallSpeed;
            fallTimer = 0f;
        }
        // 머리 위에 벽이 있으면 점프 강제 중단
        if (isJumping && Physics2D.OverlapCircle(headCheck.position, headCheckRadius, ceilingLayer))
        {
            isJumping = false;
        }


    }

    void FixedUpdate()
    {
        // 이동 처리
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        // 점프 처리
        if (isJumpPressed && isGrounded)
        {
            // 수평 이동
            transform.position += new Vector3(moveInput.x * moveSpeed * Time.fixedDeltaTime, 0, 0);
        }

        // 점프 키 플래그 초기화
        isJumpPressed = false;
    }

    void Attack()
    {
        float currentTime = Time.time;

        // 딜레이 중이면 무시
        if (!canAttack) return;

        // 콤보 유지 시간 넘었으면 초기화
        if (currentTime - lastAttackTime > comboResetTime)
        {
            nextAttackIndex = 1;
        }

        // 트리거 실행
        string triggerName = $"Attack{nextAttackIndex}";
        animator.ResetTrigger("Attack1");
        animator.ResetTrigger("Attack2");
        animator.SetTrigger(triggerName);

        // 다음 공격 번호 설정
        nextAttackIndex = (nextAttackIndex == 1) ? 2 : 1;
        lastAttackTime = currentTime;

        // 딜레이 설정
        canAttack = false;
        Invoke(nameof(ResetAttackDelay), attackDelay);
    }
    void ResetAttackDelay()
    {
        canAttack = true;
    }
    public void OnAttackEnd()
    {
        if (!isGrounded)
        {
            // 공중일 경우 상태에 따라 점프 또는 낙하
            animator.SetBool("isJumping", isJumping);
            animator.SetBool("isFalling", !isJumping);
        }
        else
        {
            // 지상일 경우 점프/낙하 해제
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);
        }
    }

    void Skill()
    {
        // 스킬 사용 중이면 무시
        if (isUsingSkill) return;

        isUsingSkill = true;
        animator.SetTrigger("Skill");
    }

    public void EndSkill()
    {
        isUsingSkill = false;
    }

    // 디버그용: 땅 체크 영역 표시
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
