using UnityEngine;

[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new Vector2(0f, 2.5f); // 대상 위치에 더할 화면 구도용 좌우·상하 거리
    [SerializeField, Min(0f)] private float smoothTime = 0.12f; // 목표 위치에 접근하는 대략적인 시간(초). 0이면 즉시 이동

    private Vector3 followVelocity; // SmoothDamp가 다음 프레임으로 이어갈 이동 속도

    private void OnEnable()
    {
        followVelocity = Vector3.zero;

        if (target != null)
            transform.position = GetTargetPosition();
    }

    private void LateUpdate()
    {
        if (target == null || Time.deltaTime <= 0f)
            return;

        Vector3 targetPosition = GetTargetPosition();

        if (smoothTime <= 0f)
        {
            transform.position = targetPosition;
            followVelocity = Vector3.zero;
            return;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position, targetPosition, ref followVelocity, smoothTime);
    }

    private Vector3 GetTargetPosition()
    {
        return new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z);
    }
}
