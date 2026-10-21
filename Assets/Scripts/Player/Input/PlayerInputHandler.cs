using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public float MoveInput { get; private set; }
    public float ClimbInput { get; private set; }

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

}
