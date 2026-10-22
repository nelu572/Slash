using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PlayerJump
{
    private enum JumpType
    {
        Ground,
        Air,
        Wall
    }

    private enum JumpInput
    {
        Press,
        Release
    }

    [Header("점프")]
    [SerializeField] private float jumpSpeed = 18.5f;
    [SerializeField, Min(1f)] private float riseGravityMultiplier = 1f;
    [SerializeField, Range(0f, 1f)] private float jumpReleaseMultiplier = 0.75f;

    [Header("벽 점프")]
    [SerializeField, Min(0f)] private float wallJumpHorizontalSpeed = 14f;
    [SerializeField, Min(0f)] private float wallJumpForce = 20f;
    [SerializeField, Min(0.01f)] private float wallJumpAirResistance = 4f;

    [Header("정점 체류")]
    [SerializeField, Min(0.01f)] private float apexSpeedThreshold = 2f;
    [SerializeField, Range(0.01f, 1f)] private float apexGravityMultiplier = 0.7f;

    [Header("낙하")]
    [SerializeField, Min(1f)] private float fallGravityMultiplier = 5f;
    [SerializeField, Min(0.01f)] private float fallAccelerationDuration = 0.18f;

    public int WallJumpDirection { get; private set; }
    public int JumpSequence { get; private set; }
    public bool IsAirJump { get; private set; }

    private bool canAirJump = true;
    private bool canCutJump;
    private bool canHangAtApex;
    private readonly Queue<JumpInput> jumpInputs = new Queue<JumpInput>();
    private float fallTime;

    public void RequestJump(bool isReleased = false)
    {
        jumpInputs.Enqueue(isReleased ? JumpInput.Release : JumpInput.Press);
    }

    /// <summary>
    /// 예약된 누름/해제 입력을 순서대로 처리한다.
    /// 한 물리 갱신에서 점프는 한 번만 실행하고, 바로 이어진 버튼 해제는 함께 반영한다.
    /// </summary>
    public bool UpdateJump(bool isGrounded, int wallJumpDirection, ref Vector2 velocity)
    {
        bool appliedJump = false;
        while (jumpInputs.Count > 0)
        {
            // 해제는 이번에 점프했는지와 관계없이 처리한다.
            // "해제 → 누름"이면 continue로 다음 누름을 확인하고, "누름 → 해제"면 방금 한 점프의 해제를 반영한다.
            if (jumpInputs.Peek() == JumpInput.Release)
            {
                jumpInputs.Dequeue();
                ReleaseJump(ref velocity);
                continue;
            }
            else if (appliedJump)
            {
                // 해제가 아니라 누름이므로, 이번에 이미 점프했다면 큐에서 꺼내지 않고 남긴다.
                break;
            }

            jumpInputs.Dequeue();
            JumpType jumpType;
            if (wallJumpDirection != 0)
                jumpType = JumpType.Wall;
            else if (isGrounded)
                jumpType = JumpType.Ground;
            else if (canAirJump)
                jumpType = JumpType.Air;
            else
                continue;

            ApplyJump(jumpType, wallJumpDirection, ref velocity);
            appliedJump = true;
        }

        return appliedJump;
    }

    private void ReleaseJump(ref Vector2 velocity)
    {
        bool shouldCutJump = canCutJump;
        canCutJump = false;
        if (shouldCutJump && velocity.y > 0f)
            velocity.y *= jumpReleaseMultiplier;
    }

    public float GetHorizontalForce(float inputAcceleration, float horizontalVelocity, float mass)
    {
        return WallJumpDirection * wallJumpForce
            + inputAcceleration * mass
            - horizontalVelocity * wallJumpAirResistance * mass;
    }

    public float GetGravityMultiplier(float verticalVelocity)
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
        float progress = fallTime / fallAccelerationDuration;
        return Mathf.Lerp(riseGravityMultiplier, fallGravityMultiplier, progress * progress);
    }

    public void Land()
    {
        IsAirJump = false;
        canAirJump = true;
        WallJumpDirection = 0;
        canCutJump = false;
        canHangAtApex = false;
        fallTime = 0f;
    }

    public void Interrupt()
    {
        IsAirJump = false;
        jumpInputs.Clear();
        WallJumpDirection = 0;
        canCutJump = false;
        canHangAtApex = false;
        fallTime = 0f;
    }

    public void StopWallJump()
    {
        WallJumpDirection = 0;
    }

    private void ApplyJump(JumpType jumpType, int wallJumpDirection, ref Vector2 velocity)
    {
        switch (jumpType)
        {
            case JumpType.Ground:
                canAirJump = true;
                break;
            case JumpType.Air:
                canAirJump = false;
                break;
            case JumpType.Wall:
                WallJumpDirection = wallJumpDirection;
                velocity.x = wallJumpDirection * wallJumpHorizontalSpeed;
                break;
        }

        canCutJump = jumpType != JumpType.Air;
        canHangAtApex = true;
        fallTime = 0f;
        velocity.y = jumpSpeed;
        IsAirJump = jumpType == JumpType.Air;
        JumpSequence++;
    }
}
