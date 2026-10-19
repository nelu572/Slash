using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 80f;

    [Header("점프")]
    [SerializeField] private float jumpSpeed = 18.5f;
    [SerializeField, Min(1f)] private float riseGravityMultiplier = 1f;
    [SerializeField, Range(0f, 1f)] private float jumpReleaseMultiplier = 0.75f;

    [Header("정점 체류")]
    [SerializeField, Min(0.01f)] private float apexSpeedThreshold = 2f;
    [SerializeField, Range(0.01f, 1f)] private float apexGravityMultiplier = 0.7f;

    [Header("낙하")]
    [SerializeField, Min(1f)] private float fallGravityMultiplier = 5f;
    [SerializeField, Min(0.01f)] private float fallAccelerationDuration = 0.18f;

    [Header("지면 감지")]
    [SerializeField] private LayerMask groundLayer;

    public float MoveSpeed => moveSpeed;

    private Rigidbody2D rigidbody2d;
    private Collider2D collider2d;
    private PlayerInputHandler playerInputHandler;
    private PlayerDash playerDash;
    private bool canAirJump = true;
    private bool canCutJump;
    private bool canHangAtApex;
    private bool jumpNeedsPhysicsStep;
    private bool hasPendingAirJump;
    private float fallTime;

    public void Initialize(PlayerComponents playerComponents)
    {
        rigidbody2d = playerComponents.rigidbody2d;
        collider2d = playerComponents.collider2d;
        playerInputHandler = playerComponents.playerInputHandler;
        playerDash = playerComponents.playerDash;
    }

    private void FixedUpdate()
    {
        bool isGrounded = OnGround();
        if (isGrounded)
        {
            canAirJump = true;
            canCutJump = false;
            canHangAtApex = false;
            jumpNeedsPhysicsStep = false;
            hasPendingAirJump = false;
            fallTime = 0f;
        }

        if (playerDash != null && playerDash.IsDashing)
        {
            if (hasPendingAirJump)
                canAirJump = true;

            canCutJump = false;
            canHangAtApex = false;
            jumpNeedsPhysicsStep = false;
            hasPendingAirJump = false;
            fallTime = 0f;
            return;
        }

        // 같은 물리 프레임에 들어온 두 점프가 하나의 도약으로 합쳐지지 않게 한다.
        if (hasPendingAirJump && !jumpNeedsPhysicsStep)
        {
            hasPendingAirJump = false;
            ApplyJump(false);
        }
        jumpNeedsPhysicsStep = false;

        float moveInput = playerInputHandler.MoveInput;
        Vector2 velocity = rigidbody2d.linearVelocity;

        float rate = moveInput == 0f ? deceleration : acceleration;
        velocity.x = Mathf.MoveTowards(velocity.x, moveInput * moveSpeed, rate * Time.fixedDeltaTime);

        if (!isGrounded)
        {
            float gravityMultiplier = GetGravityMultiplier(velocity.y);
            velocity.y += Physics2D.gravity.y * rigidbody2d.gravityScale
                * (gravityMultiplier - 1f) * Time.fixedDeltaTime;
        }

        rigidbody2d.linearVelocity = velocity;
    }

    private float GetGravityMultiplier(float verticalVelocity)
    {
        if (canHangAtApex && Mathf.Abs(verticalVelocity) <= apexSpeedThreshold)
        {
            fallTime = 0f;
            return apexGravityMultiplier;
        }

        if (verticalVelocity >= 0f)
        {
            fallTime = 0f;
            return riseGravityMultiplier;
        }

        canHangAtApex = false;
        fallTime = Mathf.Min(fallTime + Time.fixedDeltaTime, fallAccelerationDuration);
        float fallProgress = fallTime / fallAccelerationDuration;
        // 낙하 초반은 완만하게, 시간이 지날수록 가속도를 더 빠르게 높인다.
        return Mathf.Lerp(riseGravityMultiplier, fallGravityMultiplier, fallProgress * fallProgress);
    }

    public void Jump()
    {
        if (playerDash != null && playerDash.IsDashing)
            return;

        bool isGrounded = OnGround();
        if (!isGrounded && !canAirJump)
            return;

        if (!isGrounded && jumpNeedsPhysicsStep)
        {
            canAirJump = false;
            canCutJump = false;
            hasPendingAirJump = true;
            return;
        }

        ApplyJump(isGrounded);
    }

    private void ApplyJump(bool isGrounded)
    {
        canAirJump = isGrounded;
        canCutJump = isGrounded;
        canHangAtApex = true;
        jumpNeedsPhysicsStep = true;
        fallTime = 0f;
        rigidbody2d.linearVelocity = new Vector2(rigidbody2d.linearVelocity.x, jumpSpeed);
    }

    public void ReleaseJump()
    {
        bool shouldCutJump = canCutJump;
        canCutJump = false;
        if (!shouldCutJump || (playerDash != null && playerDash.IsDashing))
            return;

        Vector2 velocity = rigidbody2d.linearVelocity;
        if (velocity.y > 0f)
        {
            velocity.y *= jumpReleaseMultiplier;
            rigidbody2d.linearVelocity = velocity;
        }
    }

    private bool OnGround()
    {
        if (rigidbody2d.linearVelocity.y > 0.1f)
            return false;

        var bounds = collider2d.bounds;
        var position = new Vector2(bounds.center.x, bounds.min.y);
        var size = new Vector2(bounds.size.x * 0.9f, 0.1f);
        
        return Physics2D.OverlapBox(position, size, 0f, groundLayer) != null;
    }
}
