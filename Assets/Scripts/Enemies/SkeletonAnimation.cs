using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class SkeletonAnimation : MonoBehaviour
{
    [SerializeField] private AnimationClip attackClip; // 준비 동작부터 마무리까지 포함한 공격 모션
    [SerializeField] private AnimationClip hitClip;
    [SerializeField] private AnimationClip deathClip;

    private static readonly int movingParameter = Animator.StringToHash("IsMoving");
    private static readonly int attackingParameter = Animator.StringToHash("IsAttacking");
    private static readonly int hitParameter = Animator.StringToHash("IsHit");
    private static readonly int deadParameter = Animator.StringToHash("IsDead");

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    public float AttackDuration => attackClip.length;
    public float HitDuration => hitClip.length;
    public float DeathDuration => deathClip.length;

    public void Initialize()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void UpdateAnimation(float horizontalSpeed, bool isMoving, bool isAttacking, bool isHit, bool isDead = false)
    {
        animator.SetBool(movingParameter, isMoving);
        animator.SetBool(attackingParameter, isAttacking);
        animator.SetBool(hitParameter, isHit);
        animator.SetBool(deadParameter, isDead);
        spriteRenderer.color = isHit ? new Color(1f, 0.5f, 0.5f) : Color.white;
        if (horizontalSpeed != 0f)
            Face(horizontalSpeed);
    }

    public void Face(float direction)
    {
        spriteRenderer.flipX = direction < 0f;
    }

    public void ResetAnimation()
    {
        if (animator == null || !animator.isActiveAndEnabled || !animator.isInitialized)
            return;

        animator.SetBool(movingParameter, false);
        animator.SetBool(attackingParameter, false);
        animator.SetBool(hitParameter, false);
        animator.SetBool(deadParameter, false);
        spriteRenderer.color = Color.white;
    }
}
