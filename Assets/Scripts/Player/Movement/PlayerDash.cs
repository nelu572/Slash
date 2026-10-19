using UnityEngine;

public class PlayerDash : MonoBehaviour
{
    [SerializeField, Min(0f)] private float dashSpeed = 30f;
    [SerializeField, Min(0.01f)] private float dashDuration = 0.2f;
    [SerializeField, Min(0f)] private float dashCooldown = 0.6f;

    public bool IsDashing { get; private set; }

    private Rigidbody2D rigidbody2d;
    private PlayerInputHandler playerInputHandler;
    private PlayerMovement playerMovement;

    private float facingDirection = 1f;
    private float dashDirection;
    private float dashEndTime;
    private float nextDashTime;
    private float gravityBeforeDash;

    public void Initialize(PlayerComponents playerComponents)
    {
        rigidbody2d = playerComponents.rigidbody2d;
        playerInputHandler = playerComponents.playerInputHandler;
        playerMovement = playerComponents.playerMovement;
    }

    private void FixedUpdate()
    {
        if (IsDashing)
        {
            if (Time.fixedTime < dashEndTime)
            {
                rigidbody2d.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
                return;
            }

            EndDash();
        }

        float moveInput = playerInputHandler.MoveInput;
        if (moveInput != 0f)
            facingDirection = Mathf.Sign(moveInput);
    }

    public void Dash()
    {
        if (!isActiveAndEnabled || IsDashing || Time.time < nextDashTime)
            return;

        float moveInput = playerInputHandler.MoveInput;
        if (moveInput != 0f)
            facingDirection = Mathf.Sign(moveInput);

        dashDirection = facingDirection;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = dashEndTime + dashCooldown;
        gravityBeforeDash = rigidbody2d.gravityScale;
        IsDashing = true;

        rigidbody2d.gravityScale = 0f;
        rigidbody2d.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
    }

    private void EndDash()
    {
        IsDashing = false;
        rigidbody2d.gravityScale = gravityBeforeDash;
        float moveSpeed = playerMovement.MoveSpeed;
        rigidbody2d.linearVelocity = new Vector2(
            Mathf.Clamp(rigidbody2d.linearVelocity.x, -moveSpeed, moveSpeed), 0f);
    }
}
