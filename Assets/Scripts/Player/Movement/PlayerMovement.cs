using System;
using UnityEngine;

[Serializable]
public sealed class PlayerMovement
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField, Min(0f)] private float acceleration = 150f;
    [SerializeField, Min(0f)] private float deceleration = 120f;
    [SerializeField, Min(0f)] private float turnAcceleration = 240f;

    [Header("지면 감지")]
    [SerializeField] private LayerMask groundLayer;

    [Header("천장 모서리 보정")]
    [SerializeField, Range(0f, 0.3f)] private float ceilingCorrectionDistance = 0.15f;

    [Header("대시 모서리 보정")]
    [SerializeField, Range(0f, 0.3f)] private float dashCorrectionDistance = 0.15f;

    public float MoveSpeed => moveSpeed;

    private Rigidbody2D rigidbody2d;
    private Collider2D collider2d;
    private float dashCorrectionUsed;
    private readonly RaycastHit2D[] correctionHits = new RaycastHit2D[8];

    public void Initialize(Rigidbody2D rigidbody2d, Collider2D collider2d)
    {
        this.rigidbody2d = rigidbody2d;
        this.collider2d = collider2d;
    }

    public float GetHorizontalVelocity(float currentVelocity, float moveInput)
    {
        float rate = moveInput == 0f ? deceleration : acceleration;
        // 방향 전환 보정: 반대 방향 입력에는 출발 가속도와 별도의 반전 가속도를 적용한다.
        if (moveInput * currentVelocity < 0f)
            rate = turnAcceleration;

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

    /// <summary>
    /// 천장 모서리 보정 적용: 상승 중 머리가 모서리에 걸리면 빈 방향으로 수평 위치를 조정한다.
    /// 옆으로 옮기는 경로와 다음 물리 이동 경로가 모두 비어 있을 때만 적용한다.
    /// </summary>
    public void CorrectCeilingCorner(Vector2 velocity)
    {
        if (velocity.y <= 0f || ceilingCorrectionDistance <= 0f)
            return;

        const float skin = 0.01f;
        const float step = 0.025f;
        var bounds = collider2d.bounds;
        var center = (Vector2)bounds.center;
        var size = new Vector2(bounds.size.x, Mathf.Max(0.01f, bounds.size.y - skin * 2f));
        Vector2 movement = velocity * Time.fixedDeltaTime;
        movement += movement.normalized * skin;
        if (!HasBlockingHit(center, size, movement, true))
            return;

        int direction = velocity.x < 0f ? -1 : 1;
        int steps = Mathf.CeilToInt(ceilingCorrectionDistance / step);
        for (int i = 1; i <= steps; i++)
        {
            float distance = Mathf.Min(i * step, ceilingCorrectionDistance);
            for (int side = 0; side < 2; side++)
            {
                Vector2 offset = Vector2.right * (distance * direction * (side == 0 ? 1 : -1));
                // 옆으로 옮기는 경로와 다음 물리 이동 경로가 모두 비어 있어야 한다.
                if (HasBlockingHit(center, size, offset, false)
                    || HasBlockingHit(center + offset, size, movement, false))
                    continue;

                rigidbody2d.position += offset;
                return;
            }
        }
    }

    /// <summary>
    /// 대시 보정량 초기화: 누적 수직 보정량을 0으로 되돌려 새 대시에서 보정 한도를 다시 사용할 수 있게 한다.
    /// </summary>
    public void ResetDashCorrection()
    {
        dashCorrectionUsed = 0f;
    }

    /// <summary>
    /// 대시 모서리 보정 적용: 발끝이 낮은 윗모서리에 걸리면 이번 대시의 남은 보정 한도 안에서 위로 이동한다.
    /// 위쪽 경로와 보정 후 대시 경로가 모두 비어 있을 때만 적용한다.
    /// </summary>
    public void CorrectDashCorner(Vector2 velocity)
    {
        float remainingDistance = Mathf.Max(0f, dashCorrectionDistance - dashCorrectionUsed);
        if (velocity.x == 0f || remainingDistance <= 0f)
            return;

        const float skin = 0.01f;
        const float step = 0.025f;
        var bounds = collider2d.bounds;
        var center = (Vector2)bounds.center;
        var size = new Vector2(bounds.size.x, Mathf.Max(0.01f, bounds.size.y - skin * 2f));
        Vector2 movement = Vector2.right * (velocity.x * Time.fixedDeltaTime + Mathf.Sign(velocity.x) * skin);
        if (!HasBlockingHit(center, size, movement, false))
            return;

        int steps = Mathf.CeilToInt(remainingDistance / step);
        for (int i = 1; i <= steps; i++)
        {
            Vector2 offset = Vector2.up * Mathf.Min(i * step, remainingDistance);
            // 위로 옮기는 경로와 보정 후 대시 경로 모두 몸 전체가 통과해야 한다.
            if (HasBlockingHit(center, size, offset + Vector2.up * skin, false)
                || HasBlockingHit(center + offset, bounds.size, movement, false))
                continue;

            rigidbody2d.position += offset;
            dashCorrectionUsed += offset.y;
            return;
        }
    }

    /// <summary>
    /// 모서리 보정 경로 검사: 몸 크기의 박스캐스트로 지형 충돌을 확인하고 자기 자신과 트리거는 제외한다.
    /// ceilingOnly가 참이면 천장 방향의 충돌만 검사한다.
    /// </summary>
    private bool HasBlockingHit(Vector2 center, Vector2 size, Vector2 movement, bool ceilingOnly)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(groundLayer);
        int count = Physics2D.BoxCast(center, size, 0f, movement.normalized,
            filter, correctionHits, movement.magnitude);
        for (int i = 0; i < count; i++)
        {
            var hit = correctionHits[i];
            if (hit.collider.attachedRigidbody == rigidbody2d)
                continue;

            if (!ceilingOnly || hit.normal.y < -0.5f)
                return true;
        }

        return !ceilingOnly && count == correctionHits.Length;
    }
}
