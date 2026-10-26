using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class Skeleton : MonoBehaviour
{
    [SerializeField] private Player target; // 거리 제한 없이 추적할 플레이어
    [SerializeField] private SkeletonAnimation skeletonAnimation; // 모션 재생과 스프라이트 방향 전환 담당
    [SerializeField] private LayerMask groundLayer; // 접지·장애물·착지 바닥 검사에 사용할 지형 레이어
    [SerializeField, Min(0f)] private float moveSpeed = 2f; // 지상과 공중에서 유지할 수평 이동 속도
    [SerializeField, Min(0f)] private float stopDistance = 1.5f; // 추적을 멈추고 공격을 시도하는 수평 거리
    [SerializeField, Min(0f)] private float jumpHeight = 2.5f; // 점프 초기 속도와 경로를 계산할 때 사용할 목표 높이
    [SerializeField, Min(0f)] private float attackHeight = 0.75f; // 공격 시작을 허용할 플레이어와의 높이 차이
    [SerializeField, Min(0f)] private float attackDelay = 0.7f; // 공격 모션 종료 후 다음 행동까지 기다릴 시간(초)

    private Rigidbody2D rigidbody2d; // 이동 속도와 중력을 적용할 물리 본체
    private BoxCollider2D bodyCollider; // 접지와 점프 경로 검사에 사용할 몸통 영역
    private float attackRemainingTime; // 현재 공격 모션이 끝날 때까지 남은 시간
    private float delayRemainingTime; // 공격 후 대기를 끝낼 때까지 남은 시간
    private float jumpDirection; // 점프·낙하 중 유지할 수평 방향. 왼쪽 -1, 오른쪽 1, 미사용 0

    private void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        skeletonAnimation.Initialize();
    }

    private void FixedUpdate()
    {
        float horizontalSpeed = 0f;
        bool isGrounded = IsGrounded();
        if (!isGrounded && attackRemainingTime > 0f)
        {
            attackRemainingTime = 0f;
        }

        if (jumpDirection != 0f && isGrounded)
            jumpDirection = 0f;
        else if (!isGrounded && jumpDirection == 0f && Mathf.Abs(rigidbody2d.linearVelocity.x) > 0.1f)
            jumpDirection = Mathf.Sign(rigidbody2d.linearVelocity.x);

        if (jumpDirection != 0f)
        {
            horizontalSpeed = jumpDirection * moveSpeed;
        }
        else if (attackRemainingTime > 0f)
        {
            attackRemainingTime = Mathf.Max(0f, attackRemainingTime - Time.fixedDeltaTime);
            if (attackRemainingTime == 0f)
            {
                delayRemainingTime = attackDelay;
            }
        }
        else if (delayRemainingTime > 0f)
        {
            delayRemainingTime = Mathf.Max(0f, delayRemainingTime - Time.fixedDeltaTime);
        }
        else if (target != null && target.isActiveAndEnabled)
        {
            Vector2 offset = target.transform.position - transform.position;
            float direction = Mathf.Sign(offset.x);
            if (Mathf.Abs(offset.x) <= stopDistance)
            {
                if (Mathf.Abs(offset.y) <= attackHeight && IsGrounded()
                    && !Physics2D.Linecast(bodyCollider.bounds.center, target.transform.position, groundLayer))
                {
                    if (Mathf.Abs(offset.x) > 0.01f)
                        skeletonAnimation.Face(direction);

                    // 클립 앞부분의 준비 동작부터 공격 종료까지 이동과 방향을 고정한다.
                    attackRemainingTime = skeletonAnimation.AttackDuration;
                }
            }
            else if (TryJump(direction) || CanMove(direction))
                horizontalSpeed = direction * moveSpeed;
        }

        rigidbody2d.linearVelocity = new Vector2(horizontalSpeed, rigidbody2d.linearVelocity.y);
        skeletonAnimation.UpdateAnimation(horizontalSpeed,
            horizontalSpeed != 0f && jumpDirection == 0f && isGrounded, attackRemainingTime > 0f);
    }

    private void OnDisable()
    {
        attackRemainingTime = 0f;
        delayRemainingTime = 0f;
        jumpDirection = 0f;
        if (rigidbody2d != null)
            rigidbody2d.linearVelocity = new Vector2(0f, rigidbody2d.linearVelocity.y);
        if (skeletonAnimation != null)
            skeletonAnimation.ResetAnimation();
    }

    private bool IsGrounded()
    {
        if (rigidbody2d.linearVelocity.y > 0.1f)
            return false;

        Bounds bounds = bodyCollider.bounds;
        RaycastHit2D hit = Physics2D.BoxCast(bounds.center, (Vector2)bounds.size - Vector2.one * 0.04f,
            0f, Vector2.down, 0.1f, groundLayer);
        return hit && hit.normal.y > 0.5f;
    }

    private bool TryJump(float direction)
    {
        float gravity = -Physics2D.gravity.y * rigidbody2d.gravityScale;
        if (!IsGrounded() || jumpHeight <= 0f || moveSpeed <= 0f || gravity <= 0f)
            return false;

        Bounds bounds = bodyCollider.bounds;
        float jumpSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);
        float lookAhead = moveSpeed * jumpSpeed / gravity + 0.05f;
        RaycastHit2D wall = Physics2D.BoxCast(bounds.center,
            new Vector2(bounds.size.x, bounds.size.y - 0.1f), 0f,
            Vector2.right * direction, lookAhead, groundLayer);
        if (!wall || wall.normal.x * direction > -0.5f)
            return false;

        // 실제 물리 갱신 간격으로 몸통의 궤적을 검사하고, 내려앉을 바닥까지 확인한다.
        Vector2 position = bounds.center;
        Vector2 size = (Vector2)bounds.size - Vector2.one * 0.04f;
        Vector2 velocity = new Vector2(direction * moveSpeed, jumpSpeed);
        int steps = Mathf.CeilToInt(2f * jumpSpeed / (gravity * Time.fixedDeltaTime)) + 2;
        for (int i = 0; i < steps; i++)
        {
            velocity.y -= gravity * Time.fixedDeltaTime;
            Vector2 movement = velocity * Time.fixedDeltaTime;
            RaycastHit2D hit = Physics2D.BoxCast(position, size, 0f,
                movement.normalized, movement.magnitude, groundLayer);
            if (hit)
            {
                if (velocity.y >= 0f || hit.normal.y < 0.5f)
                    return false;

                Vector2 footAhead = new Vector2(
                    hit.centroid.x + direction * bounds.extents.x,
                    hit.centroid.y - bounds.extents.y + 0.05f);
                RaycastHit2D landing = Physics2D.Raycast(footAhead, Vector2.down, 0.15f, groundLayer);
                if (!landing || landing.normal.y < 0.5f)
                    return false;

                jumpDirection = direction;
                rigidbody2d.linearVelocity = new Vector2(direction * moveSpeed, jumpSpeed);
                return true;
            }

            position += movement;
        }

        return false;
    }

    private bool CanMove(float direction)
    {
        Bounds bounds = bodyCollider.bounds;
        float distance = moveSpeed * Time.fixedDeltaTime + 0.05f;
        Vector2 wallCheckSize = new Vector2(bounds.size.x, bounds.size.y - 0.1f);
        if (Physics2D.BoxCast(bounds.center, wallCheckSize, 0f,
            Vector2.right * direction, distance, groundLayer))
            return false;

        return true;
    }
}
