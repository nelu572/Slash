using System;
using UnityEngine;

[Serializable]
public sealed class PlayerMovement
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 80f;

    [Header("지면 감지")]
    [SerializeField] private LayerMask groundLayer;

    public float MoveSpeed => moveSpeed;
    public float Acceleration => acceleration;

    private Rigidbody2D rigidbody2d;
    private Collider2D collider2d;

    public void Initialize(Rigidbody2D rigidbody2d, Collider2D collider2d)
    {
        this.rigidbody2d = rigidbody2d;
        this.collider2d = collider2d;
    }

    public float GetHorizontalVelocity(float currentVelocity, float moveInput)
    {
        float rate = moveInput == 0f ? deceleration : acceleration;
        return Mathf.MoveTowards(currentVelocity, moveInput * moveSpeed, rate * Time.fixedDeltaTime);
    }

    public bool IsGrounded()
    {
        if (rigidbody2d.linearVelocity.y > 0.1f)
            return false;

        var bounds = collider2d.bounds;
        var position = new Vector2(bounds.center.x, bounds.min.y);
        var size = new Vector2(bounds.size.x * 0.9f, 0.1f);
        return Physics2D.OverlapBox(position, size, 0f, groundLayer) != null;
    }
}
