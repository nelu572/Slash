using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class PlayerAnimation : MonoBehaviour
{
    private static readonly int stateParameter = Animator.StringToHash("State");
    private static readonly int movingParameter = Animator.StringToHash("IsMoving");
    private static readonly int risingParameter = Animator.StringToHash("IsRising");
    private static readonly int climbParameter = Animator.StringToHash("ClimbDirection");
    private static readonly int airJumpParameter = Animator.StringToHash("IsAirJump");
    private static readonly int jumpParameter = Animator.StringToHash("Jump");

    private Player player;
    private PlayerInputHandler playerInputHandler;
    private Rigidbody2D rigidbody2d;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private int currentJumpSequence;

    public void Initialize(Player player, Rigidbody2D rigidbody2d, PlayerInputHandler playerInputHandler)
    {
        this.player = player;
        this.rigidbody2d = rigidbody2d;
        this.playerInputHandler = playerInputHandler;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (player == null || !player.isActiveAndEnabled)
            return;

        spriteRenderer.flipX = player.FacingDirection < 0;

        if (!animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            return;

        Vector2 velocity = rigidbody2d.linearVelocity;
        animator.SetInteger(stateParameter, (int)player.State);
        animator.SetBool(movingParameter, Mathf.Abs(velocity.x) > 0.1f);
        animator.SetBool(risingParameter, velocity.y > 0f);
        animator.SetInteger(climbParameter, System.Math.Sign(playerInputHandler.ClimbInput));
        animator.SetBool(airJumpParameter, player.IsAirJump);

        // 새 점프 신호는 다음 Animator 평가에서 한 번만 사용한다.
        animator.ResetTrigger(jumpParameter);
        if (currentJumpSequence != player.JumpSequence)
            animator.SetTrigger(jumpParameter);
        currentJumpSequence = player.JumpSequence;
    }
}
