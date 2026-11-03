using UnityEngine;

[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new Vector2(0f, 2.5f); // 대상 위치에 더할 화면 구도용 좌우·상하 거리
    [SerializeField, Min(0f)] private float smoothTime = 0.12f; // 목표 위치에 접근하는 대략적인 시간(초). 0이면 즉시 이동

    private Vector3 followVelocity; // SmoothDamp가 다음 프레임으로 이어갈 이동 속도
    private Vector3 followPosition; // 흔들림이 다음 프레임의 추적 위치에 누적되지 않도록 분리
    private float shakeDuration;
    private float shakeRemainingTime;
    private float shakeStrength;

    private void OnEnable()
    {
        followVelocity = Vector3.zero;

        if (target != null)
            transform.position = GetTargetPosition();

        followPosition = transform.position;
        shakeRemainingTime = 0f;
    }

    private void LateUpdate()
    {
        if (target == null || Time.deltaTime <= 0f)
            return;

        Vector3 targetPosition = GetTargetPosition();

        if (smoothTime <= 0f)
        {
            followPosition = targetPosition;
            followVelocity = Vector3.zero;
        }
        else
        {
            followPosition = Vector3.SmoothDamp(
                followPosition, targetPosition, ref followVelocity, smoothTime);
        }

        // 카메라 흔들림 처리
        Vector2 shakeOffset = Vector2.zero;
        if (shakeRemainingTime > 0f)
        {
            shakeRemainingTime = Mathf.Max(0f, shakeRemainingTime - Time.deltaTime);
            float strength = shakeStrength * shakeRemainingTime / shakeDuration;
            shakeOffset = Random.insideUnitCircle * strength;
        }

        transform.position = followPosition + (Vector3)shakeOffset;
    }

    public void Shake(float duration, float strength)
    {
        shakeDuration = duration;
        shakeRemainingTime = duration;
        shakeStrength = strength;
    }

    private Vector3 GetTargetPosition()
    {
        return new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z);
    }
}
