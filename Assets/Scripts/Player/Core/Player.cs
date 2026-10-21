using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
[RequireComponent(typeof(PlayerMovement), typeof(PlayerDash), typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerWallCling))]
public class Player : MonoBehaviour
{
    private void Awake()
    {
        var playerComponents = CacheComponents();
        InitializeComponents(playerComponents);
    }

    private PlayerComponents CacheComponents()
    {
        return new PlayerComponents
        {
            rigidbody2d = GetComponent<Rigidbody2D>(),
            collider2d = GetComponent<Collider2D>(),
            playerMovement = GetComponent<PlayerMovement>(),
            playerDash = GetComponent<PlayerDash>(),
            playerWallCling = GetComponent<PlayerWallCling>(),
            playerInputHandler = GetComponent<PlayerInputHandler>()
        };
    }

    private void InitializeComponents(PlayerComponents playerComponents)
    {
        playerComponents.playerMovement.Initialize(playerComponents);
        playerComponents.playerDash.Initialize(playerComponents);
        playerComponents.playerWallCling.Initialize(playerComponents);
        playerComponents.playerInputHandler.Initialize(playerComponents);
    }
}
