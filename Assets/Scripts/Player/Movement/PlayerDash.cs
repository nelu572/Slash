using System;
using UnityEngine;

[Serializable]
public sealed class PlayerDash
{
    [SerializeField, Min(0f)] private float dashSpeed = 30f;
    [SerializeField, Min(0.01f)] private float dashDuration = 0.2f; // 수평 대시 속도와 무중력 상태를 유지할 시간(초)
    [SerializeField, Min(0f)] private float dashCooldown = 0.6f; // 대시 종료 시점부터 계산할 재사용 대기시간(초)

    public bool IsDashing { get; private set; }
    public int Direction => (int)dashDirection;
    public float GravityBeforeDash { get; private set; } // 무중력 대시가 끝나면 복구할 gravityScale
    public Vector2 Velocity => new Vector2(dashDirection * dashSpeed, 0f);

    private float facingDirection = 1f; // 입력이 없을 때 대시 방향으로 사용할 마지막 이동 방향
    private float dashDirection; // 대시 시작 시 고정하며 도중에는 변경하지 않는 방향
    private float dashRemainingTime;
    private float cooldownRemainingTime;

    public bool TryStart(float moveInput, float gravityScale)
    {
        if (IsDashing || cooldownRemainingTime > 0f)
            return false;

        UpdateFacing(moveInput);
        dashDirection = facingDirection;
        dashRemainingTime = dashDuration;
        GravityBeforeDash = gravityScale;
        IsDashing = true;
        return true;
    }

    public bool UpdateDash(float moveInput, float moveSpeed, ref Vector2 velocity)
    {
        cooldownRemainingTime = Mathf.Max(0f, cooldownRemainingTime - Time.fixedDeltaTime);

        if (IsDashing)
        {
            dashRemainingTime -= Time.fixedDeltaTime;
            if (dashRemainingTime > 0f)
            {
                velocity = Velocity;
                return true;
            }

            IsDashing = false;
            cooldownRemainingTime = dashCooldown;
            velocity = new Vector2(Mathf.Clamp(velocity.x, -moveSpeed, moveSpeed), 0f);
        }

        UpdateFacing(moveInput);
        return false;
    }

    public void Cancel()
    {
        IsDashing = false;
        cooldownRemainingTime = dashCooldown;
    }

    private void UpdateFacing(float moveInput)
    {
        if (moveInput != 0f)
            facingDirection = Mathf.Sign(moveInput);
    }
}
