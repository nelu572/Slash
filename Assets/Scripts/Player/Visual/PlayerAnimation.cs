using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private AnimationClip hitClip;

    private static readonly int stateParameter = Animator.StringToHash("State");
    private static readonly int movingParameter = Animator.StringToHash("IsMoving");
    private static readonly int risingParameter = Animator.StringToHash("IsRising");
    private static readonly int climbParameter = Animator.StringToHash("ClimbDirection");
    private static readonly int airJumpParameter = Animator.StringToHash("IsAirJump");
    private static readonly int jumpParameter = Animator.StringToHash("Jump");
    private static readonly int attackingParameter = Animator.StringToHash("IsAttacking");
    private static readonly int attackParameter = Animator.StringToHash("Attack");
    private static readonly int hitParameter = Animator.StringToHash("IsHit");
    private static readonly int deadParameter = Animator.StringToHash("IsDead");

    private Player player;
    private PlayerInputHandler playerInputHandler;
    private Rigidbody2D rigidbody2d;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private int currentJumpSequence; // 새 점프만 트리거로 전달하기 위해 마지막으로 반영한 실행 번호
    private bool attackRequested; // 다음 애니메이션 갱신에서 새 공격을 한 번 재생할 요청

    public float HitDuration => hitClip.length;

    public void Initialize(Player player, Rigidbody2D rigidbody2d, PlayerInputHandler playerInputHandler)
    {
        this.player = player;
        this.rigidbody2d = rigidbody2d;
        this.playerInputHandler = playerInputHandler;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void PlayAttack()
    {
        attackRequested = true;
    }

    private void LateUpdate()
    {
        if (player == null || !player.isActiveAndEnabled)
            return;

        spriteRenderer.flipX = player.FacingDirection < 0;
        // 무적 시간이 남아 있는 동안 깜빡여서 다시 맞지 않는 상태를 보여 준다.
        float alpha = player.IsInvincible && !player.IsDead && Mathf.Repeat(Time.time, 0.12f) < 0.06f
            ? 0.35f : 1f;
        spriteRenderer.color = new Color(1f, 1f, 1f, alpha);

        if (!animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            return;

        Vector2 velocity = rigidbody2d.linearVelocity;
        animator.SetInteger(stateParameter, (int)player.State);
        animator.SetBool(movingParameter, Mathf.Abs(velocity.x) > 0.1f);
        animator.SetBool(risingParameter, velocity.y > 0f);
        animator.SetInteger(climbParameter, System.Math.Sign(playerInputHandler.ClimbInput));
        animator.SetBool(airJumpParameter, player.IsAirJump);
        animator.SetBool(attackingParameter, player.IsAttacking);
        animator.SetBool(hitParameter, player.IsHit);
        animator.SetBool(deadParameter, player.IsDead);

        // 새 점프 신호는 다음 Animator 평가에서 한 번만 사용한다.
        animator.ResetTrigger(jumpParameter);
        if (currentJumpSequence != player.JumpSequence)
            animator.SetTrigger(jumpParameter);
        currentJumpSequence = player.JumpSequence;

        animator.ResetTrigger(attackParameter);
        if (attackRequested && player.IsAttacking)
            animator.SetTrigger(attackParameter);
        attackRequested = false;
    }
}
