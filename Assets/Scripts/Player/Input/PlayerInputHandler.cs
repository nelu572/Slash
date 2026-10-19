using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public float MoveInput { get; private set; }
    private PlayerMovement playerMovement;
    private PlayerDash playerDash;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerDash = GetComponent<PlayerDash>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<float>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
            playerMovement.Jump();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed)
            playerDash.Dash();
    }
}
