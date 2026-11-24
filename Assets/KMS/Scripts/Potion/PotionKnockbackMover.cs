using UnityEngine;
using UnityEngine.AI;

// 정해진 이동량을 일정 시간 동안 NavMeshAgent로 나눠 민다 (NavMesh 밖으로는 밀려나지 않음)
public class PotionKnockbackMover : MonoBehaviour
{
    private NavMeshAgent agent;
    private Vector3 velocity;
    private float remainingTime;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    // 새 넉백으로 교체한다 (장판 틱이 겹쳐 누적되며 날아가는 것 방지)
    public void Push(Vector3 displacement, float duration)
    {
        velocity = displacement / duration;
        remainingTime = duration;
    }

    void Update()
    {
        if (remainingTime <= 0f) return;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            remainingTime = 0f;   // 사망 등으로 agent가 꺼지면 중단
            return;
        }

        float step = Mathf.Min(Time.deltaTime, remainingTime);
        agent.Move(velocity * step);
        remainingTime -= step;
    }
}