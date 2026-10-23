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

    [Header("점프 입력 보정")]
    [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

    [Header("벽 점프")]
    [SerializeField, Min(0f)] private float wallJumpHorizontalSpeed = 14f;
    [SerializeField, Min(0f)] private float wallJumpForce = 20f;
    [SerializeField, Min(0f)] private float wallJumpInputAcceleration = 60f;
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
    private bool hasBufferedJump;
    private bool bufferedJumpReleased;
    private float coyoteExpiresAt = float.NegativeInfinity;
    private float jumpBufferExpiresAt = float.NegativeInfinity;
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
        // 점프 버퍼 만료: 보관 시간이 지난 입력을 버린다.
        if (Time.time > jumpBufferExpiresAt)
            hasBufferedJump = false;

        bool appliedJump = false;
        while (jumpInputs.Count > 0)
        {
            // 해제는 이번에 점프했는지와 관계없이 처리한다.
            // "해제 → 누름"이면 continue로 다음 누름을 확인하고, "누름 → 해제"면 방금 한 점프의 해제를 반영한다.
            if (jumpInputs.Peek() == JumpInput.Release)
            {
                jumpInputs.Dequeue();
                // 버튼 해제 기록: 보관 중인 입력으로 나중에 점프해도 짧게 누른 높이를 유지한다.
                if (hasBufferedJump)
                    bufferedJumpReleased = true;
                ReleaseJump(ref velocity);
                continue;
            }
            else if (appliedJump)
            {
                // 해제가 아니라 누름이므로, 이번에 이미 점프했다면 큐에서 꺼내지 않고 남긴다.
                break;
            }

            jumpInputs.Dequeue();
            // 점프 버퍼 저장: 새 누름으로 보관 시간을 갱신하고 가능한 점프가 있으면 즉시 실행한다.
            hasBufferedJump = true;
            bufferedJumpReleased = false;
            jumpBufferExpiresAt = Time.time + jumpBufferTime;
            appliedJump = TryBufferedJump(isGrounded, wallJumpDirection, ref velocity);
        }

        // 점프 버퍼 재시도: 이번에 새 입력이 없어도 착지나 벽 접촉으로 실행 가능해졌는지 확인한다.
        if (!appliedJump)
            appliedJump = TryBufferedJump(isGrounded, wallJumpDirection, ref velocity);

        return appliedJump;
    }

    /// <summary>
    /// 점프 버퍼 실행: 보관한 입력을 한 번 소모하며 벽 점프, 지상·코요테 점프, 공중 점프 순으로 선택한다.
    /// </summary>
    private bool TryBufferedJump(bool isGrounded, int wallJumpDirection, ref Vector2 velocity)
    {
        if (!hasBufferedJump)
            return false;

        JumpType jumpType;
        if (wallJumpDirection != 0)
            jumpType = JumpType.Wall;
        else if (isGrounded || Time.time < coyoteExpiresAt)
            jumpType = JumpType.Ground;
        else if (canAirJump)
            jumpType = JumpType.Air;
        else
            return false;

        hasBufferedJump = false;
        ApplyJump(jumpType, wallJumpDirection, ref velocity);

        // 예약된 해제 반영: 버퍼 대기 중 버튼을 놓았다면 방금 실행한 점프의 높이를 줄인다.
        if (bufferedJumpReleased)
            ReleaseJump(ref velocity);

        return true;
    }

    private void ReleaseJump(ref Vector2 velocity)
    {
        bool shouldCutJump = canCutJump;
        canCutJump = false;
        if (shouldCutJump && velocity.y > 0f)
            velocity.y *= jumpReleaseMultiplier;
    }

    public float GetHorizontalForce(float moveInput, float horizontalVelocity, float mass)
    {
        // 벽 점프 입력 가속은 기본 이동의 출발·반전 가속도와 별도로 적용한다.
        return WallJumpDirection * wallJumpForce
            + moveInput * wallJumpInputAcceleration * mass
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
        // 코요테 타임 갱신: 접지 중 지상 점프 유예의 만료 시각을 계속 연장한다.
        coyoteExpiresAt = Time.time + coyoteTime;
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
        // 점프 보정 취소: 대시·벽 붙기로 전환하면 남은 점프 버퍼와 코요테 타임을 버린다.
        hasBufferedJump = false;
        bufferedJumpReleased = false;
        coyoteExpiresAt = float.NegativeInfinity;
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
        // 코요테 타임 소모: 점프가 실행되면 지상 점프 유예를 끝내 중복 사용을 막는다.
        coyoteExpiresAt = float.NegativeInfinity;
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
