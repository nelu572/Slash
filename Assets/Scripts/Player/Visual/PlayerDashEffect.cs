using UnityEngine;

// PlayerAnimation이 스프라이트 방향을 갱신한 뒤 현재 모습을 잔상에 복사한다.
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Player))]
public class PlayerDashEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private SpriteRenderer[] afterimages = new SpriteRenderer[0];
    [SerializeField] private Color afterimageColor = new Color(0.65f, 0.85f, 1f, 0.4f);
    [SerializeField, Min(0.01f)] private float afterimageDuration = 0.18f;
    [SerializeField, Min(0.01f)] private float afterimageInterval = 0.05f;

    private Player player;
    private float[] remainingTimes;
    private float emissionRemainingTime;
    private int nextAfterimage;
    private bool wasDashing;

    private void Awake()
    {
        player = GetComponent<Player>();
        remainingTimes = new float[afterimages.Length];
    }

    private void LateUpdate()
    {
        // 대시가 끝나거나 피격으로 중단되어도 이미 남은 잔상은 끝까지 사라지게 한다.
        for (int i = 0; i < afterimages.Length; i++)
        {
            if (afterimages[i] == null || remainingTimes[i] <= 0f)
                continue;

            remainingTimes[i] = Mathf.Max(0f, remainingTimes[i] - Time.deltaTime);
            Color color = afterimageColor;
            color.a *= remainingTimes[i] / afterimageDuration;
            afterimages[i].color = color;
            afterimages[i].enabled = remainingTimes[i] > 0f;
        }

        bool isDashing = player.isActiveAndEnabled && !player.IsDead && !player.IsHit
            && player.State == PlayerState.Dashing;
        if (isDashing)
        {
            if (!wasDashing)
                emissionRemainingTime = 0f;

            emissionRemainingTime -= Time.deltaTime;
            if (emissionRemainingTime <= 0f)
            {
                ShowAfterimage();
                emissionRemainingTime = afterimageInterval;
            }
        }
        wasDashing = isDashing;
    }

    private void ShowAfterimage()
    {
        if (visual == null || visual.sprite == null || afterimages.Length == 0)
            return;

        SpriteRenderer image = afterimages[nextAfterimage];
        if (image == null)
            return;

        // 잔상은 월드 공간에 둔 렌더러를 재사용해, 지나간 위치에 남긴다.
        image.sprite = visual.sprite;
        image.flipX = visual.flipX;
        image.flipY = visual.flipY;
        image.transform.SetPositionAndRotation(visual.transform.position, visual.transform.rotation);
        image.transform.localScale = visual.transform.lossyScale;
        image.sortingLayerID = visual.sortingLayerID;
        image.sortingOrder = visual.sortingOrder - 1;
        image.color = afterimageColor;
        image.enabled = true;
        remainingTimes[nextAfterimage] = afterimageDuration;
        nextAfterimage = (nextAfterimage + 1) % afterimages.Length; // 가장 오래된 렌더러부터 순환 재사용
    }

    private void OnDisable()
    {
        if (afterimages == null)
            return;

        for (int i = 0; i < afterimages.Length; i++)
        {
            if (afterimages[i] != null)
                afterimages[i].enabled = false;
            if (remainingTimes != null)
                remainingTimes[i] = 0f;
        }
        wasDashing = false;
    }
}
