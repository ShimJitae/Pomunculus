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

    public void InitSpawnCenter(Vector3 position)
    {
        spawnCenter = position;
    }

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
