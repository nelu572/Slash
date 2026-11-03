using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class Skeleton : MonoBehaviour
{
    [SerializeField] private Player target;
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private ParticleSystem hitSparks;
    [SerializeField] private ParticleSystem hitFlash;
    [SerializeField] private LayerMask groundLayer; // 접지·장애물·착지 바닥 검사에 사용할 지형 레이어
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField, Min(0f)] private float detectionRange = 10f; // 플레이어 추적을 시작할 거리
    [SerializeField, Min(0f)] private float moveSpeed = 2f; // 지상과 공중에서 유지할 수평 이동 속도
    [SerializeField, Min(0f)] private float stopDistance = 1.5f; // 추적을 멈추고 공격을 시도하는 수평 거리
    [SerializeField, Min(0f)] private float jumpHeight = 2.5f; // 점프 초기 속도와 경로를 계산할 때 사용할 목표 높이
    [SerializeField, Min(0f)] private float attackHeight = 0.75f; // 공격 시작을 허용할 플레이어와의 높이 차이
    [SerializeField, Min(0f)] private float attackDelay = 0.7f; // 공격 모션 종료 후 다음 행동까지 기다릴 시간(초)
    [SerializeField, Min(1)] private int attackDamage = 1;
    [SerializeField, Min(0f)] private float attackHitDelay = 0.25f; // 준비 동작 후 검을 휘두르는 시점(초)
    [SerializeField] private Vector2 hitOffset = new Vector2(0.95f, 0f); // 공격 방향으로 반전할 판정 중심
    [SerializeField] private Vector2 hitSize = new Vector2(1.1f, 1.2f); // 검이 닿는 전방 영역의 크기
    [SerializeField, Min(0f)] private float knockbackSpeed = 5f; // 피격 시 공격 방향으로 밀려날 초기 수평 속도

    private Rigidbody2D rigidbody2d;
    private BoxCollider2D bodyCollider;
    private float attackRemainingTime; // 현재 공격 모션이 끝날 때까지 남은 시간
    private float delayRemainingTime; // 공격 후 대기를 끝낼 때까지 남은 시간
    private float jumpDirection; // 점프·낙하 중 유지할 수평 방향. 왼쪽 -1, 오른쪽 1, 미사용 0
    private float hitRemainingTime; // 피격 모션과 넉백을 유지할 남은 시간(초)
    private float deathRemainingTime; // 사망 모션이 끝나면 적을 비활성화할 시간
    private int attackDirection; // 준비 동작이 시작될 때 고정한 공격 방향
    private bool hitApplied; // 한 번의 공격에서 피해가 중복되지 않도록 관리

    public bool IsHit => hitRemainingTime > 0f;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = CurrentHealth;
        rigidbody2d = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        skeletonAnimation.Initialize();
    }

    private void FixedUpdate()
    {
        if (IsDead)
        {
            rigidbody2d.linearVelocity = new Vector2(0f, rigidbody2d.linearVelocity.y);
            deathRemainingTime -= Time.fixedDeltaTime;
            if (deathRemainingTime <= 0f)
                gameObject.SetActive(false);
            return;
        }

        if (IsHit)
        {
            hitRemainingTime = Mathf.Max(0f, hitRemainingTime - Time.fixedDeltaTime);
            Vector2 velocity = rigidbody2d.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, 0f,
                knockbackSpeed / skeletonAnimation.HitDuration * Time.fixedDeltaTime);
            rigidbody2d.linearVelocity = velocity;
            skeletonAnimation.UpdateAnimation(0f, false, false, IsHit);
            return;
        }

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
            if (!hitApplied && skeletonAnimation.AttackDuration - attackRemainingTime >= attackHitDelay)
            {
                ApplyHit();
                hitApplied = true;
            }

            if (attackRemainingTime == 0f)
            {
                delayRemainingTime = attackDelay;
            }
        }
        else if (delayRemainingTime > 0f)
        {
            delayRemainingTime = Mathf.Max(0f, delayRemainingTime - Time.fixedDeltaTime);
        }
        else if (target != null && target.isActiveAndEnabled && !target.IsDead
            && Vector2.Distance(transform.position, target.transform.position) <= detectionRange)
        {
            Vector2 offset = target.transform.position - transform.position;
            float direction = Mathf.Sign(offset.x);
            if (Mathf.Abs(offset.x) <= stopDistance)
            {
                if (Mathf.Abs(offset.y) <= attackHeight && isGrounded
                    && !Physics2D.Linecast(bodyCollider.bounds.center, target.transform.position, groundLayer))
                {
                    if (Mathf.Abs(offset.x) > 0.01f)
                        skeletonAnimation.Face(direction);

                    // 클립 앞부분의 준비 동작부터 공격 종료까지 이동과 방향을 고정한다.
                    attackDirection = (int)direction;
                    hitApplied = false;
                    attackRemainingTime = skeletonAnimation.AttackDuration;
                }
            }
            else if (TryJump(direction) || CanMove(direction))
                horizontalSpeed = direction * moveSpeed;
        }

        rigidbody2d.linearVelocity = new Vector2(horizontalSpeed, rigidbody2d.linearVelocity.y);
        skeletonAnimation.UpdateAnimation(horizontalSpeed,
            horizontalSpeed != 0f && jumpDirection == 0f && isGrounded, attackRemainingTime > 0f, false);
    }

    public void ReceiveHit(int damage, int direction)
    {
        if (!isActiveAndEnabled || IsDead || damage <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        healthSlider.value = CurrentHealth;
        if (hitSparks != null)
        {
            Bounds bounds = bodyCollider.bounds;
            hitSparks.transform.position = bounds.center + Vector3.left * (direction * bounds.extents.x);
            if (hitFlash != null)
            {
                hitFlash.transform.position = hitSparks.transform.position;
                hitFlash.Emit(1);
            }
            // 맞은 면의 바깥쪽으로 파편을 부채꼴로 펼치고, 각도에 작은 차이를 준다.
            for (int i = 0; i < 5; i++)
            {
                float angle = (direction > 0 ? 180f : 0f) + (i - 2) * 32f + Random.Range(-6f, 6f);
                Vector2 velocity = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                var particle = new ParticleSystem.EmitParams();
                particle.velocity = velocity * Random.Range(2f, 3.5f);
                particle.rotation = -angle;
                hitSparks.Emit(particle, 1);
            }
        }
        attackRemainingTime = 0f;
        delayRemainingTime = 0f;
        jumpDirection = 0f;
        hitRemainingTime = IsDead ? 0f : skeletonAnimation.HitDuration;
        deathRemainingTime = IsDead ? skeletonAnimation.DeathDuration : 0f;
        rigidbody2d.linearVelocity = new Vector2(IsDead ? 0f : direction * knockbackSpeed,
            rigidbody2d.linearVelocity.y);
        skeletonAnimation.UpdateAnimation(0f, false, false, IsHit, IsDead);
    }

    private void ApplyHit()
    {
        if (target == null || !target.isActiveAndEnabled || target.IsDead)
            return;

        Vector2 origin = bodyCollider.bounds.center;
        Vector2 center = origin + new Vector2(hitOffset.x * attackDirection, hitOffset.y);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitSize, 0f);
        foreach (Collider2D hit in hits)
        {
            if (hit.isTrigger || hit.GetComponentInParent<Player>() != target)
                continue;
            if (Physics2D.Linecast(origin, hit.bounds.center, groundLayer))
                continue;

            target.ReceiveHit(attackDamage, attackDirection);
            break;
        }
    }

    private void OnDisable()
    {
        attackRemainingTime = 0f;
        delayRemainingTime = 0f;
        jumpDirection = 0f;
        hitRemainingTime = 0f;
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
