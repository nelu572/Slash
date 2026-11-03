using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Player), typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PlayerLandingEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem landingDust;
    [SerializeField, Min(0f)] private float minimumFallSpeed = 2f;

    private Player player;
    private Rigidbody2D rigidbody2d;
    private BoxCollider2D bodyCollider;
    private bool wasGrounded;
    private float fallSpeed;

    private void Awake()
    {
        player = GetComponent<Player>();
        rigidbody2d = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
    }

    private void OnEnable()
    {
        wasGrounded = true;
        fallSpeed = 0f;
    }

    private void LateUpdate()
    {
        if (player.IsDead || landingDust == null)
            return;

        // 착지 프레임에는 수직 속도가 0이 되므로, 공중에서 기록한 낙하 속도로 먼지 양을 정한다.
        bool isGrounded = player.State == PlayerState.Grounded;
        if (isGrounded && !wasGrounded && fallSpeed >= minimumFallSpeed)
        {
            Bounds bounds = bodyCollider.bounds;
            landingDust.transform.position = new Vector3(bounds.center.x, bounds.min.y + 0.03f, transform.position.z);
            landingDust.Emit(fallSpeed >= 12f ? 6 : 4);
        }

        fallSpeed = isGrounded ? 0f : Mathf.Max(fallSpeed, -rigidbody2d.linearVelocity.y);
        wasGrounded = isGrounded;
    }
}
