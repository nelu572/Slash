using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class SkeletonAnimation : MonoBehaviour
{
    [SerializeField] private AnimationClip attackClip; // 준비 동작부터 마무리까지 포함한 공격 모션

    private static readonly int movingParameter = Animator.StringToHash("IsMoving");
    private static readonly int attackingParameter = Animator.StringToHash("IsAttacking");

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    public float AttackDuration => attackClip.length;

    public void Initialize()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void UpdateAnimation(float horizontalSpeed, bool isMoving, bool isAttacking)
    {
        animator.SetBool(movingParameter, isMoving);
        animator.SetBool(attackingParameter, isAttacking);
        if (horizontalSpeed != 0f)
            Face(horizontalSpeed);
    }

    public void Face(float direction)
    {
        spriteRenderer.flipX = direction < 0f;
    }

    public void ResetAnimation()
    {
        if (animator == null)
            return;

        animator.SetBool(movingParameter, false);
        animator.SetBool(attackingParameter, false);
    }
}
