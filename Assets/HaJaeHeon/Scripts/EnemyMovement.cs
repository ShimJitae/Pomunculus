using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public Vector3 SpawnCenter => spawnCenter;

    [SerializeField] private float patrolRadius = 8f;
    [SerializeField] private float minAngleStep = 60f;
    [SerializeField] private float maxAngleStep = 150f;

    private Vector3 spawnCenter;
    private float currentAngle;

    private void OnEnable()
    {
        currentAngle = Random.Range(0f, 360f);
    }

    // 순찰 위치 계산에 사용 할 중심 좌표를 설정한다.
    public void InitSpawnCenter(Vector3 position)
    {
        spawnCenter = position;
    }

    // 순찰 각도를 바꾸며 NavMesh 위치를 최대 3회 탐색하고, 탐색에 실패하면 순찰 중심을 반환한다.
    public Vector3 GetNextPatrolPoint()
    {
        // NavMesh 보정 실패 대비
        for (int i = 0; i < 3; i++)   
        {
            float step = Random.Range(minAngleStep, maxAngleStep);
            if (Random.value < 0.5f) step = -step;
            float angle = Mathf.Repeat(currentAngle + step, 360f);

            float angleRadian = angle * Mathf.Deg2Rad;
            Vector3 point = spawnCenter + new Vector3(Mathf.Cos(angleRadian), 0f, Mathf.Sin(angleRadian)) * patrolRadius;

            if (NavMesh.SamplePosition(point, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                currentAngle = angle;
                return hit.position;
            }
        }
        return spawnCenter;
    }

    // 추적 → 순찰 복귀 시 호출
    public void SyncAngleToCurrentPosition()
    {
        Vector3 directionFromCenter = transform.position - spawnCenter;
        currentAngle = Mathf.Atan2(directionFromCenter.z, directionFromCenter.x) * Mathf.Rad2Deg;
    }
}
