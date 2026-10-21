using System;
using UnityEngine;

[Serializable]
public sealed class PlayerWallCling
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
    public int WallJumpDirection { get; private set; }

    private Rigidbody2D rigidbody2d;
    private Collider2D collider2d;
    private readonly RaycastHit2D[] wallHits = new RaycastHit2D[8];
    private bool canCling;
    private int releasedWallDirection;
    private float wallSlideTime;

    public void Initialize(Rigidbody2D rigidbody2d, Collider2D collider2d)
    {
        this.rigidbody2d = rigidbody2d;
        this.collider2d = collider2d;
    }

    /// <summary>
    /// 벽 접촉과 벽 점프/등반 가능 여부를 갱신한다. 이동 속도는 변경하지 않는다.
    /// </summary>
    public void UpdateWallContact(bool isGrounded, float moveInput, float climbInput, float verticalVelocity)
    {
        bool wasClinging = IsClinging;
        int previousDirection = WallDirection;
        int wallJumpDirection = previousDirection != 0 && HasWall(previousDirection) ? -previousDirection : 0;
        Release();

        // 벽에서 떨어지는 방향을 눌렀더라도, 아직 닿아 있다면 이번 점프는 허용한다.
        WallJumpDirection = wallJumpDirection;
        if (isGrounded)
        {
            releasedWallDirection = 0;
            if (climbInput <= 0f)
                return;
        }

        int direction = previousDirection != 0 ? previousDirection : 1;
        if (!HasWall(direction))
        {
            direction = -direction;
            if (!HasWall(direction))
            {
                releasedWallDirection = 0;
                return;
            }
        }

        if (releasedWallDirection != direction)
            releasedWallDirection = 0;

        if (releasedWallDirection == direction || moveInput * direction < 0f)
            return;

        WallDirection = direction;
        if (WallJumpDirection == 0)
            WallJumpDirection = -direction;

        float climbVelocity = climbInput * (climbInput > 0f ? climbSpeed : climbDownSpeed);
        if (!wasClinging || previousDirection != direction || climbInput != 0f)
            wallSlideTime = 0f;

        // S는 즉시 하강하고, 벽을 스치는 상승 동작은 관성을 유지한다.
        if (!wasClinging && climbInput >= 0f && verticalVelocity > Mathf.Max(0f, climbVelocity))
            return;

        canCling = true;
    }

    public bool TryCling(float climbInput, ref Vector2 velocity)
    {
        if (!canCling)
            return false;

        IsClinging = true;
        if (climbInput != 0f)
        {
            float climbVelocity = climbInput * (climbInput > 0f ? climbSpeed : climbDownSpeed);
            velocity = new Vector2(0f, climbVelocity);
        }
        else
        {
            float progress = Mathf.Clamp01((wallSlideTime - wallSlideDelay) / wallSlideRampDuration);
            velocity = new Vector2(0f, -wallSlideSpeed * Mathf.SmoothStep(0f, 1f, progress));
            wallSlideTime += Time.fixedDeltaTime;
        }

        return true;
    }

    public void Release(int ignoredWallDirection = 0)
    {
        if (ignoredWallDirection != 0)
            releasedWallDirection = ignoredWallDirection;

        IsClinging = false;
        canCling = false;
        WallDirection = 0;
        WallJumpDirection = 0;
    }

    public bool IsTouchingWall(int direction)
    {
        return HasWall(direction);
    }

    private bool HasWall(int direction)
    {
        var bounds = collider2d.bounds;
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
}
