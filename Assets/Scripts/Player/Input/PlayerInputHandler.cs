using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public float MoveInput { get; private set; }
    public float ClimbInput { get; private set; }
    private PlayerMovement playerMovement;
    private PlayerDash playerDash;

    public void Initialize(PlayerComponents playerComponents)
    {
        playerMovement = playerComponents.playerMovement;
        playerDash = playerComponents.playerDash;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<float>();
    }

    public void OnClimb(InputAction.CallbackContext context)
    {
        ClimbInput = context.ReadValue<float>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
            playerMovement.Jump();
        else if (context.canceled)
            playerMovement.ReleaseJump();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed)
            playerDash.Dash();
    }

    private void OnDisable()
    {
        ClimbInput = 0f;
    }
}
