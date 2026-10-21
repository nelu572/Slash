using UnityEngine;

[DisallowMultipleComponent]
public class PlayerWallCling : MonoBehaviour
{
    [Header("벽 감지")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField, Min(0.01f)] private float wallCheckDistance = 0.05f;

    [Header("벽 이동")]
    [SerializeField, Min(0f)] private float climbSpeed = 9f;
    [SerializeField, Min(0f)] private float climbDownSpeed = 12f;
    [SerializeField, Min(0.01f)] private float wallSlideSpeed = 3f;
    [SerializeField, Min(0f)] private float wallSlideDelay = 0.12f;
    [SerializeField, Min(0.01f)] private float wallSlideRampDuration = 0.18f;

    public bool IsClinging { get; private set; }
    public int WallDirection { get; private set; }
    public bool CanWallJump => isActiveAndEnabled && WallDirection != 0 && HasWall(WallDirection);

    private Rigidbody2D rigidbody2d;
    private Collider2D collider2d;
    private PlayerInputHandler playerInputHandler;
    private PlayerDash playerDash;
    private readonly RaycastHit2D[] wallHits = new RaycastHit2D[8];
    private int releasedWallDirection;
    private float wallSlideTime;

    public void Initialize(PlayerComponents playerComponents)
    {
        rigidbody2d = playerComponents.rigidbody2d;
        collider2d = playerComponents.collider2d;
        playerInputHandler = playerComponents.playerInputHandler;
        playerDash = playerComponents.playerDash;
    }

    public bool TryCling(bool isGrounded)
    {
        bool wasClinging = IsClinging;
        int previousDirection = WallDirection;
        Release();

        if (!isActiveAndEnabled || isGrounded)
        {
            releasedWallDirection = 0;
            // 지면에서는 W를 눌렀을 때만 벽 등반을 시작한다.
            if (!isActiveAndEnabled || playerInputHandler.ClimbInput <= 0f)
                return false;
        }

        if (playerDash != null && playerDash.IsDashing)
            return false;

        int direction = previousDirection != 0 ? previousDirection : 1;
        if (!HasWall(direction))
        {
            direction = -direction;
            if (!HasWall(direction))
            {
                releasedWallDirection = 0;
                return false;
            }
        }

        if (releasedWallDirection != direction)
            releasedWallDirection = 0;

        if (releasedWallDirection == direction || playerInputHandler.MoveInput * direction < 0f)
            return false;

        WallDirection = direction;
        float climbInput = playerInputHandler.ClimbInput;
        float verticalSpeed = rigidbody2d.linearVelocity.y;
        float climbVelocity = climbInput * (climbInput > 0f ? climbSpeed : climbDownSpeed);

        if (!wasClinging || previousDirection != direction || climbInput != 0f)
            wallSlideTime = 0f;

        // S는 상승 중에도 바로 하강하고, 그 외에는 점프의 상승 관성을 유지한다.
        if (!wasClinging && climbInput >= 0f && verticalSpeed > Mathf.Max(0f, climbVelocity))
            return false;

        IsClinging = true;
        if (climbInput != 0f)
        {
            verticalSpeed = climbVelocity;
        }
        else
        {
            // 벽을 잡는 짧은 여유를 준 뒤 하강 속도를 부드럽게 높인다.
            float slideProgress = Mathf.Clamp01((wallSlideTime - wallSlideDelay) / wallSlideRampDuration);
            verticalSpeed = -wallSlideSpeed * Mathf.SmoothStep(0f, 1f, slideProgress);
            wallSlideTime += Time.fixedDeltaTime;
        }

        rigidbody2d.linearVelocity = new Vector2(0f, verticalSpeed);
        // 대쉬가 저장하는 gravityScale을 바꾸지 않고 이번 물리 스텝의 중력만 상쇄한다.
        rigidbody2d.AddForce(-Physics2D.gravity * rigidbody2d.gravityScale * rigidbody2d.mass);
        return true;
    }

    public void Release(bool untilSeparated = false)
    {
        if (untilSeparated && WallDirection != 0)
            releasedWallDirection = WallDirection;

        IsClinging = false;
        WallDirection = 0;
    }

    public bool IsTouchingWall(int direction)
    {
        return isActiveAndEnabled && HasWall(direction);
    }

    private bool HasWall(int direction)
    {
        var bounds = collider2d.bounds;
        // 발과 머리 부분을 제외해 바닥, 천장, 모서리를 벽으로 감지하지 않는다.
        var size = new Vector2(bounds.size.x * 0.9f, bounds.size.y * 0.8f);
        float distance = bounds.size.x * 0.05f + wallCheckDistance;
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(wallLayer);

        int count = Physics2D.BoxCast(bounds.center, size, 0f, Vector2.right * direction,
            filter, wallHits, distance);
        for (int i = 0; i < count; i++)
        {
            var hit = wallHits[i];
            if (hit.collider.attachedRigidbody == rigidbody2d)
                continue;

            if (hit.normal.x * direction < -0.9f)
                return true;
        }

        return false;
    }

    private void OnDisable()
    {
        Release();
        releasedWallDirection = 0;
        wallSlideTime = 0f;
    }
}
