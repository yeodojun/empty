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
    public float fallSpeed = 8f;

    public Transform groundCheck;                // 땅 체크 위치 기준
    public float groundCheckRadius = 0.2f;       // 바닥 판정 범위
    public LayerMask groundLayer;                // 바닥 레이어 설정

    private bool isGrounded;                     // 현재 땅에 닿았는지 여부
    private bool wasGroundedLastFrame = true; // 이전 프레임에 땅에 있었는지 저장

    private bool isJumping = false;
    private float targetY = 0f;


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

        // 점프 중: 위로 이동
        if (isJumping)
        {
            // 목표 지점 도달 OR 버튼을 뗐으면 종료
            if (!isJumpHeld || transform.position.y >= targetY)
            {
                isJumping = false;
            }
            else
            {
                // 계속 위로 이동
                transform.position = Vector3.MoveTowards(transform.position,
                                                         new Vector3(transform.position.x, targetY, transform.position.z),
                                                         fallSpeed * Time.deltaTime);
            }
        }
        else if (!isGrounded)
        {
            // 수동 낙하
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
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
        // 공격 애니메이션 실행
        animator.SetTrigger("attack");

        // 추후에 공격 로직 (히트박스, 데미지 등) 추가 가능
    }

    // 디버그용: 땅 체크 영역 표시
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
