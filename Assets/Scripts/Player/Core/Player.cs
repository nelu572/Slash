using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
[RequireComponent(typeof(PlayerInputHandler))]
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private CameraHitEffect cameraHitEffect;
    [SerializeField] private PlayerMovement playerMovement = new PlayerMovement();
    [SerializeField] private PlayerJump playerJump = new PlayerJump();
    [SerializeField] private PlayerDash playerDash = new PlayerDash();
    [SerializeField] private PlayerWallCling playerWallCling = new PlayerWallCling();
    [SerializeField] private PlayerAttack playerAttack = new PlayerAttack();
    [SerializeField, Min(1)] private int maxHealth = 5;
    [SerializeField] private float fallDeathY = -15f; // 이 월드 Y 좌표 이하로 내려가면 낙사
    [SerializeField, Min(0f)] private float knockbackSpeed = 6f; // 피격 시 밀려날 초기 수평 속도
    [SerializeField, Min(0f)] private float invincibleDuration = 0.8f; // 연속 피격을 막을 무적 시간(초)

    public PlayerState State { get; private set; } = PlayerState.Airborne;
    public int FacingDirection { get; private set; } = 1; // 바라보는 방향. 왼쪽 -1, 오른쪽 1
    public int JumpSequence => playerJump.JumpSequence;
    public bool IsAirJump => playerJump.IsAirJump;
    public bool IsAttacking => playerAttack.IsAttacking;
    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;
    public bool IsHit => hitRemainingTime > 0f;
    public bool IsInvincible => invincibleRemainingTime > 0f;

    private Rigidbody2D rigidbody2d;
    private PlayerInputHandler playerInputHandler;
    private float hitRemainingTime; // 피격 모션 동안 조작을 멈출 남은 시간
    private float invincibleRemainingTime;

    private void Awake()
    {
        CurrentHealth = MaxHealth;
        healthSlider.maxValue = MaxHealth;
        healthSlider.value = CurrentHealth;
        rigidbody2d = GetComponent<Rigidbody2D>();
        BoxCollider2D collider2d = GetComponent<BoxCollider2D>();
        playerInputHandler = GetComponent<PlayerInputHandler>();

        playerMovement.Initialize(rigidbody2d, collider2d);
        playerWallCling.Initialize(rigidbody2d, collider2d);
        playerAttack.Initialize(collider2d);
        playerInputHandler.Initialize(this);

        if (playerAnimation != null)
            playerAnimation.Initialize(this, rigidbody2d, playerInputHandler);
    }

    private void Update()
    {
        if (IsAttacking || IsHit || IsDead)
            return;

        switch (State)
        {
            case PlayerState.Wall:
                FacingDirection = playerWallCling.WallDirection;
                break;
            case PlayerState.WallJumping:
                FacingDirection = playerJump.WallJumpDirection;
                break;
            case PlayerState.Dashing:
                FacingDirection = playerDash.Direction;
                break;
            default:
                if (playerInputHandler.MoveInput != 0f)
                    FacingDirection = (int)Mathf.Sign(playerInputHandler.MoveInput);
                break;
        }
    }

    /// <summary>
    /// 각 이동 기능의 물리 갱신 순서를 관리하고, 계산된 결과와 플레이어 상태를 반영한다.
    /// </summary>
    private void FixedUpdate()
    {
        if (!IsDead && rigidbody2d.position.y <= fallDeathY)
            Die();

        invincibleRemainingTime = Mathf.Max(0f, invincibleRemainingTime - Time.fixedDeltaTime);
        if (IsDead)
        {
            rigidbody2d.linearVelocity = new Vector2(0f, rigidbody2d.linearVelocity.y);
            return;
        }

        if (IsHit)
        {
            hitRemainingTime = Mathf.Max(0f, hitRemainingTime - Time.fixedDeltaTime);
            Vector2 hitVelocity = rigidbody2d.linearVelocity;
            hitVelocity.x = Mathf.MoveTowards(hitVelocity.x, 0f,
                knockbackSpeed / playerAnimation.HitDuration * Time.fixedDeltaTime);
            rigidbody2d.linearVelocity = hitVelocity;
            return;
        }

        // -------- 접지 확인 및 착지 처리 -------- 
        bool isGrounded = playerMovement.IsGrounded();
        if (isGrounded)
            playerJump.Land();

        // -------- 대쉬 처리 --------  
        Vector2 velocity = rigidbody2d.linearVelocity;
        if (playerDash.UpdateDash(playerInputHandler.MoveInput, playerMovement.MoveSpeed, ref velocity))
        {
            playerJump.Interrupt();
            // 대시 보정 적용 순서: 수평 속도를 적용하기 전에 모서리에 걸린 위치부터 위로 보정한다.
            playerMovement.CorrectDashCorner(velocity);
            rigidbody2d.linearVelocity = velocity;
            State = PlayerState.Dashing;
            return;
        }

        if (State == PlayerState.Dashing)
            rigidbody2d.gravityScale = playerDash.GravityBeforeDash;

        // -------- 기본 이동 상태 갱신 -------- 
        // 공중에서는 벽 점프의 밀어내는 힘을 유지한다.
        if (isGrounded)
            State = PlayerState.Grounded;
        else if (State != PlayerState.WallJumping)
            State = PlayerState.Airborne;

        // -------- 벽 접촉 및 이동 가능 여부 확인 -------- 
        playerWallCling.UpdateWallContact(isGrounded, playerInputHandler.MoveInput,
            playerInputHandler.ClimbInput, velocity.y);

        // -------- 점프 및 벽 이동 처리 -------- 
        if (State == PlayerState.WallJumping && playerWallCling.IsTouchingWall(playerJump.WallJumpDirection))
        {
            playerJump.StopWallJump();
            State = PlayerState.Airborne;
        }

        int wallJumpDirection = playerWallCling.WallJumpDirection;
        if (playerJump.UpdateJump(isGrounded, wallJumpDirection, ref velocity))
        {
            playerWallCling.Release(-wallJumpDirection);

            // 공중 점프를 해도 기존 벽 점프의 밀어내는 힘은 유지한다.
            if (wallJumpDirection != 0)
                State = PlayerState.WallJumping;
            else if (State == PlayerState.Grounded)
                State = PlayerState.Airborne;
        }
        else if (playerWallCling.TryCling(playerInputHandler.ClimbInput, ref velocity))
        {
            // 점프하지 않은 경우에만 벽 이동 속도를 계산하고 적용한다.
            playerJump.Interrupt();
            playerAttack.Cancel();
            rigidbody2d.linearVelocity = velocity;
            rigidbody2d.AddForce(-Physics2D.gravity * rigidbody2d.gravityScale * rigidbody2d.mass);
            State = PlayerState.Wall;
            return;
        }

        // -------- 수평 이동 계산 -------- 
        playerAttack.UpdateAttack();
        float horizontalForce = 0f;
        if (State == PlayerState.WallJumping)
        {
            horizontalForce = playerJump.GetHorizontalForce(playerInputHandler.MoveInput,
                velocity.x, rigidbody2d.mass);
        }
        else
        {
            velocity.x = playerMovement.GetHorizontalVelocity(velocity.x, playerInputHandler.MoveInput);
        }

        // -------- 공중 중력 보정 -------- 
        if (State != PlayerState.Grounded)
        {
            velocity.y += Physics2D.gravity.y * rigidbody2d.gravityScale
                * (playerJump.GetGravityMultiplier(velocity.y) - 1f) * Time.fixedDeltaTime;
        }

        // -------- 물리 결과 적용 -------- 
        // 천장 보정 적용 순서: 최종 이동 속도를 적용하기 전에 머리가 걸린 모서리에서 벗어난다.
        playerMovement.CorrectCeilingCorner(velocity);
        rigidbody2d.linearVelocity = velocity;
        rigidbody2d.AddForce(Vector2.right * horizontalForce);
    }

    public void Jump()
    {
        if (!isActiveAndEnabled || IsHit || IsDead || State == PlayerState.Dashing)
            return;

        playerJump.RequestJump();
    }

    public void ReleaseJump()
    {
        if (!isActiveAndEnabled || IsHit || IsDead || State == PlayerState.Dashing)
            return;

        playerJump.RequestJump(isReleased: true);
    }

    public void Dash()
    {
        float direction = playerInputHandler.MoveInput != 0f ? playerInputHandler.MoveInput : FacingDirection;
        if (!isActiveAndEnabled || IsHit || IsDead || State == PlayerState.Dashing
            || !playerDash.TryStart(direction, rigidbody2d.gravityScale))
            return;

        playerWallCling.Release();
        playerJump.Interrupt();
        playerAttack.Cancel();
        // 대시 보정 시작 조건: 대시 시작에 성공한 경우에만 보정 한도를 새로 사용할 수 있게 한다.
        playerMovement.ResetDashCorrection();
        rigidbody2d.gravityScale = 0f;
        rigidbody2d.linearVelocity = playerDash.Velocity;
        State = PlayerState.Dashing;
    }

    public void Attack()
    {
        if (!isActiveAndEnabled || IsHit || IsDead || IsAttacking
            || State == PlayerState.Dashing || State == PlayerState.Wall)
            return;

        float playerScreenX = Camera.main.WorldToScreenPoint(transform.position).x;
        float aimOffset = playerInputHandler.AimInput.x - playerScreenX;
        if (Mathf.Abs(aimOffset) > 1f)
            FacingDirection = aimOffset > 0f ? 1 : -1;

        playerAttack.Start(FacingDirection);
        if (playerAnimation != null)
            playerAnimation.PlayAttack();
    }

    public void ReceiveHit(int damage, int direction)
    {
        if (!isActiveAndEnabled || IsDead || IsInvincible || damage <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        healthSlider.value = CurrentHealth;

        if (cameraHitEffect != null)
            cameraHitEffect.Play();

        FacingDirection = -direction;
        if (IsDead)
        {
            Die();
            return;
        }

        InterruptActions();
        State = PlayerState.Airborne;
        hitRemainingTime = playerAnimation.HitDuration;
        invincibleRemainingTime = invincibleDuration;
        rigidbody2d.linearVelocity = new Vector2(direction * knockbackSpeed,
            rigidbody2d.linearVelocity.y);
    }

    private void Die()
    {
        CurrentHealth = 0;
        healthSlider.value = 0;
        InterruptActions();
        State = PlayerState.Airborne;
        hitRemainingTime = 0f;
        invincibleRemainingTime = 0f;
        rigidbody2d.linearVelocity = new Vector2(0f, rigidbody2d.linearVelocity.y);
    }

    private void InterruptActions()
    {
        playerAttack.Cancel();
        playerJump.Interrupt();
        playerWallCling.Release();
        if (playerDash.IsDashing)
        {
            rigidbody2d.gravityScale = playerDash.GravityBeforeDash;
            playerDash.Cancel();
        }
    }

    private void OnDisable()
    {
        playerAttack.Cancel();
    }

}
