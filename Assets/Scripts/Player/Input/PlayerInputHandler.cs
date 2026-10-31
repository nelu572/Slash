using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public float MoveInput { get; private set; }
    public float ClimbInput { get; private set; }
    public Vector2 AimInput { get; private set; } // 공격 방향을 정할 마우스 화면 좌표

    private Player player;

    public void Initialize(Player player)
    {
        this.player = player;
    }

    public void OnMove(InputAction.CallbackContext callbackContext)
    {
        MoveInput = callbackContext.ReadValue<float>();
    }

    public void OnClimb(InputAction.CallbackContext callbackContext)
    {
        ClimbInput = callbackContext.ReadValue<float>();
    }

    public void OnJump(InputAction.CallbackContext callbackContext)
    {
        if (callbackContext.performed)
            player.Jump();
        else if (callbackContext.canceled)
            player.ReleaseJump();
    }

    public void OnDash(InputAction.CallbackContext callbackContext)
    {
        if (callbackContext.performed)
            player.Dash();
    }
    public void OnAim(InputAction.CallbackContext callbackContext)
    {
        AimInput = callbackContext.ReadValue<Vector2>();
    }


}
