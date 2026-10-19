using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 80f;
    [SerializeField] private float jumpSpeed = 14.5f;
    [SerializeField] private LayerMask groundLayer;

    public float MoveSpeed => moveSpeed;

    private Rigidbody2D rigidbody2d;
    private Collider2D collider2d;
    private PlayerInputHandler playerInputHandler;
    private PlayerDash playerDash;

    private void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        collider2d = GetComponent<Collider2D>();
        playerInputHandler = GetComponent<PlayerInputHandler>();
        playerDash = GetComponent<PlayerDash>();
    }

    private void FixedUpdate()
    {
        if (playerDash != null && playerDash.IsDashing)
            return;

        float moveInput = playerInputHandler.MoveInput;
        Vector2 velocity = rigidbody2d.linearVelocity;

        float rate = moveInput == 0f ? deceleration : acceleration;
        velocity.x = Mathf.MoveTowards(velocity.x, moveInput * moveSpeed, rate * Time.fixedDeltaTime);
        rigidbody2d.linearVelocity = velocity;
    }

    public void Jump()
    {
        if ((playerDash != null && playerDash.IsDashing) || !OnGround())
            return;

        rigidbody2d.linearVelocity = new Vector2(rigidbody2d.linearVelocity.x, jumpSpeed);
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
