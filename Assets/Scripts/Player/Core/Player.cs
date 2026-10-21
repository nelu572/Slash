using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
[RequireComponent(typeof(PlayerInputHandler))]
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement = new PlayerMovement();
    [SerializeField] private PlayerJump playerJump = new PlayerJump();
    [SerializeField] private PlayerDash playerDash = new PlayerDash();
    [SerializeField] private PlayerWallCling playerWallCling = new PlayerWallCling();

    public PlayerState State { get; private set; } = PlayerState.Airborne;

    private Rigidbody2D rigidbody2d;
    private PlayerInputHandler playerInputHandler;

    private void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        var collider2d = GetComponent<Collider2D>();
        playerInputHandler = GetComponent<PlayerInputHandler>();

        playerMovement.Initialize(rigidbody2d, collider2d);
        playerWallCling.Initialize(rigidbody2d, collider2d);
        playerInputHandler.Initialize(this);
    }

    /// <summary>
    /// 각 이동 기능의 물리 갱신 순서를 관리하고, 계산된 결과와 플레이어 상태를 반영한다.
    /// </summary>
    private void FixedUpdate()
    {
        // -------- 접지 확인 및 착지 처리 -------- 
        bool isGrounded = playerMovement.IsGrounded();
        if (isGrounded)
            playerJump.Land();

        // -------- 대쉬 처리 --------  
        Vector2 velocity = rigidbody2d.linearVelocity;
        if (playerDash.UpdateDash(playerInputHandler.MoveInput, playerMovement.MoveSpeed, ref velocity))
        {
            playerJump.Interrupt();
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
            rigidbody2d.linearVelocity = velocity;
            rigidbody2d.AddForce(-Physics2D.gravity * rigidbody2d.gravityScale * rigidbody2d.mass);
            State = PlayerState.Wall;
            return;
        }

        // -------- 수평 이동 계산 -------- 
        float horizontalForce = 0f;
        if (State == PlayerState.WallJumping)
        {
            horizontalForce = playerJump.GetHorizontalForce(playerInputHandler.MoveInput * playerMovement.Acceleration,
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
        rigidbody2d.linearVelocity = velocity;
        rigidbody2d.AddForce(Vector2.right * horizontalForce);
    }

    public void Jump()
    {
        if (!isActiveAndEnabled || State == PlayerState.Dashing)
            return;

        playerJump.RequestJump();
    }

    public void ReleaseJump()
    {
        if (!isActiveAndEnabled || State == PlayerState.Dashing)
            return;

        playerJump.RequestJump(isReleased: true);
    }

    public void Dash()
    {
        if (!isActiveAndEnabled || State == PlayerState.Dashing
            || !playerDash.TryStart(playerInputHandler.MoveInput, rigidbody2d.gravityScale))
            return;

        playerWallCling.Release();
        playerJump.Interrupt();
        rigidbody2d.gravityScale = 0f;
        rigidbody2d.linearVelocity = playerDash.Velocity;
        State = PlayerState.Dashing;
    }

}
