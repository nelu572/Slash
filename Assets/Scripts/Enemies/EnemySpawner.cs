using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Skeleton enemyPrefab;
    [SerializeField] private Player player;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(24f, 10f); // 이 오브젝트를 중심으로 한 월드 좌표 영역
    [SerializeField, Min(0.1f)] private float spawnInterval = 2f;
    [SerializeField, Min(1)] private int maxEnemyCount = 5;
    [SerializeField, Min(1)] private int positionCheckCount = 20; // 유효한 생성 위치를 찾는 최대 시도 횟수
    [SerializeField] private float despawnY = -15f; // 맵 아래로 떨어진 적을 정리할 높이

    private readonly List<Skeleton> enemies = new List<Skeleton>();
    private Vector2 bodySize;
    private float remainingTime;

    private void Awake()
    {
        if (enemyPrefab == null || player == null || groundLayer.value == 0)
        {
            Debug.LogError("적 생성에 필요한 프리팹, 플레이어, 지형 레이어를 지정해야 합니다.", this);
            enabled = false;
            return;
        }

        bodySize = enemyPrefab.GetComponent<BoxCollider2D>().size;
        remainingTime = spawnInterval;
    }

    private void Update()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Skeleton enemy = enemies[i];
            if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.transform.position.y > despawnY)
                continue;

            // 사망 모션이 끝나 비활성화된 적도 제거해 오브젝트가 계속 쌓이지 않게 한다.
            if (enemy != null)
                Destroy(enemy.gameObject);
            enemies.RemoveAt(i);
        }

        if (player == null || !player.isActiveAndEnabled || player.IsDead || enemies.Count >= maxEnemyCount)
            return;

        remainingTime -= Time.deltaTime;
        if (remainingTime > 0f)
            return;

        remainingTime = spawnInterval;
        if (!TryGetSpawnPosition(out Vector2 position))
            return;

        Skeleton spawnedEnemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        spawnedEnemy.transform.SetParent(transform, true);
        spawnedEnemy.Initialize(player);
        enemies.Add(spawnedEnemy);
    }

    private bool TryGetSpawnPosition(out Vector2 position)
    {
        Vector2 areaMin = (Vector2)transform.position - spawnAreaSize * 0.5f;
        Vector2 areaMax = (Vector2)transform.position + spawnAreaSize * 0.5f;
        Vector2 halfSize = bodySize * 0.5f;
        position = Vector2.zero;
        if (spawnAreaSize.x < bodySize.x || spawnAreaSize.y < bodySize.y)
            return false;

        for (int i = 0; i < positionCheckCount; i++)
        {
            Vector2 origin = new Vector2(Random.Range(areaMin.x + halfSize.x, areaMax.x - halfSize.x),
                Random.Range(areaMin.y, areaMax.y));
            RaycastHit2D ground = Physics2D.Raycast(origin, Vector2.down, origin.y - areaMin.y, groundLayer);
            if (!ground || ground.collider.isTrigger || ground.normal.y < 0.9f)
                continue;

            Vector2 center = ground.point + Vector2.up * (halfSize.y + 0.05f);
            if (center.y + halfSize.y > areaMax.y)
                continue;

            bool isBlocked = false;
            foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, bodySize, 0f))
            {
                if (!hit.isTrigger)
                {
                    isBlocked = true;
                    break;
                }
            }

            if (isBlocked)
                continue;

            position = center;
            return true;
        }

        return false;
    }
}
