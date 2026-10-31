using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PlayerAttack
{
    [SerializeField] private AnimationClip attackClip;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(0f)] private float hitDelay = 0.2f; // 준비 동작 후 주먹이 뻗는 시점까지의 시간(초)
    [SerializeField] private Vector2 hitOffset = new Vector2(0.95f, 0f); // 몸 중심에서 공격 판정 중심까지의 거리. X는 공격 방향으로 반전
    [SerializeField] private Vector2 hitSize = new Vector2(0.9f, 1f); // 주먹이 닿는 전방 영역의 너비와 높이
    [SerializeField] private LayerMask groundLayer; // 지형 너머의 적이 맞지 않도록 가림을 검사할 레이어

    public bool IsAttacking { get; private set; }

    private Collider2D bodyCollider;
    private int direction; // 공격 시작 시 고정한 방향. 왼쪽 -1, 오른쪽 1
    private float elapsedTime; // 현재 공격을 시작한 뒤 경과한 시간(초)
    private bool hitApplied; // 한 공격에서 피해 판정을 한 번만 실행하기 위한 상태

    public void Initialize(Collider2D bodyCollider)
    {
        this.bodyCollider = bodyCollider;
    }

    public void Start(int facingDirection)
    {
        direction = facingDirection;
        elapsedTime = 0f;
        hitApplied = false;
        IsAttacking = true;
    }

    public void UpdateAttack()
    {
        if (!IsAttacking)
            return;

        elapsedTime += Time.fixedDeltaTime;
        if (!hitApplied && elapsedTime >= hitDelay)
        {
            ApplyHit();
            hitApplied = true;
        }

        if (elapsedTime >= attackClip.length)
            Cancel();
    }

    public void Cancel()
    {
        IsAttacking = false;
    }

    private void ApplyHit()
    {
        Vector2 origin = bodyCollider.bounds.center;
        Vector2 center = origin + new Vector2(hitOffset.x * direction, hitOffset.y);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitSize, 0f);
        List<Skeleton> hitTargets = new List<Skeleton>();

        foreach (Collider2D hit in hits)
        {
            if (hit.isTrigger)
                continue;

            Skeleton skeleton = hit.GetComponentInParent<Skeleton>();
            if (skeleton == null || hitTargets.Contains(skeleton))
                continue;

            if (Physics2D.Linecast(origin, hit.bounds.center, groundLayer))
                continue;

            // 자식 충돌체가 여러 개여도 한 번만 맞도록 한다.
            hitTargets.Add(skeleton);
            skeleton.ReceiveHit(damage, direction);
        }
    }
}
