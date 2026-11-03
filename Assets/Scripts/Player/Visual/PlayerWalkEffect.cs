using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Player), typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PlayerWalkEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem walkingDust;
    [SerializeField, Min(0.1f)] private float stepDistance = 1.1f; // 먼지 두 입자를 방출할 때마다 필요한 지상 이동 거리(유닛)

    private Player player;
    private Rigidbody2D rigidbody2d;
    private BoxCollider2D bodyCollider;
    private Vector3 previousPosition;
    private float travelledDistance;

    private void Awake()
    {
        player = GetComponent<Player>();
        rigidbody2d = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
        travelledDistance = 0f;
    }

    private void LateUpdate()
    {
        float distance = Mathf.Abs(transform.position.x - previousPosition.x);
        // 공중·피격 중 이동한 거리가 다음 걸음에 합산되지 않도록 위치는 항상 갱신한다.
        previousPosition = transform.position;
        if (!player.isActiveAndEnabled || player.IsDead || player.IsHit || walkingDust == null
            || player.State != PlayerState.Grounded || Mathf.Abs(rigidbody2d.linearVelocity.x) < 1f)
        {
            travelledDistance = 0f;
            return;
        }

        travelledDistance += distance;
        if (travelledDistance < stepDistance)
            return;

        travelledDistance = 0f;
        float direction = Mathf.Sign(rigidbody2d.linearVelocity.x);
        Bounds bounds = bodyCollider.bounds;
        // walkingDust는 월드 공간으로 설정해, 방출한 먼지가 캐릭터를 따라오지 않게 한다.
        walkingDust.transform.position = new Vector3(bounds.center.x - direction * 0.2f,
            bounds.min.y + 0.04f, transform.position.z);
        for (int i = 0; i < 2; i++)
        {
            var particle = new ParticleSystem.EmitParams();
            particle.velocity = new Vector3(-direction * Random.Range(0.4f, 1f), Random.Range(0.3f, 0.8f), 0f);
            walkingDust.Emit(particle, 1);
        }
    }
}
