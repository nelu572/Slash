using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class SkeletonAnimation : MonoBehaviour
{
    [SerializeField] private AnimationClip attackClip; // 준비 동작부터 마무리까지 포함한 공격 모션

    private static readonly int movingParameter = Animator.StringToHash("IsMoving"); // Idle·Move 전환에 사용할 파라미터
    private static readonly int attackingParameter = Animator.StringToHash("IsAttacking"); // 공격 모션 진입·종료에 사용할 파라미터

    private Animator animator; // 상태에 맞는 모션을 재생할 Animator
    private SpriteRenderer spriteRenderer; // 바라보는 방향에 따라 좌우를 뒤집을 렌더러

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
