using System;
using UnityEngine;

[Serializable]
public sealed class PlayerDash
{
    [SerializeField, Min(0f)] private float dashSpeed = 30f;
    [SerializeField, Min(0.01f)] private float dashDuration = 0.2f;
    [SerializeField, Min(0f)] private float dashCooldown = 0.6f;

    public bool IsDashing { get; private set; }
    public int Direction => (int)dashDirection;
    public float GravityBeforeDash { get; private set; }
    public Vector2 Velocity => new Vector2(dashDirection * dashSpeed, 0f);

    private float facingDirection = 1f;
    private float dashDirection;
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

    private void UpdateFacing(float moveInput)
    {
        if (moveInput != 0f)
            facingDirection = Mathf.Sign(moveInput);
    }
}
