using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

// NavMeshAgent 이동과 회전 담당.
[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMotor : MonoBehaviour
{
    
    public float MoveSpeed = 5f;
    public float Acceleration = 60f;          // 기본값 8은 액션 게임엔 너무 굼뜸
    public float RotateSpeed = 900f;          // 초당 회전 각도 (회전은 코드가 직접 처리)
    public float StoppingDistance = 0.1f;

    private NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.speed = MoveSpeed;
        agent.acceleration = Acceleration;
        agent.stoppingDistance = StoppingDistance;
        agent.updateRotation = false;   // 던질 때 즉시 회전해야 하므로 회전은 직접 처리
        agent.autoBraking = true;
    }

    bool AgentReady => agent.enabled && agent.isOnNavMesh;

    public bool AgentEnabled => agent.enabled;

    public void MoveTo(Vector3 destination)
    {
        if (!AgentReady) return;
        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    public void StopMoving()
    {
        if (!AgentReady) return;
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;   // 관성으로 미끄러지지 않게
    }

    // 경로 계산이 끝났고 목적지에 도착했는지
    public bool HasArrived =>
        !AgentReady ||
        (!agent.pathPending &&
         (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance));

    // 현재 이동 중인 목적지 (던진 뒤 이동 재개용)
    public bool TryGetCurrentDestination(out Vector3 destination)
    {
        destination = default;
        if (!AgentReady || (!agent.hasPath && !agent.pathPending)) return false;
        destination = agent.destination;
        return true;
    }

    // 회피·넉백처럼 경로와 무관한 이동. NavMesh 밖으로는 밀려나지 않는다.
    public void Displace(Vector3 velocity)
    {
        if (AgentReady) agent.Move(velocity * Time.deltaTime);
    }

    public void FaceMoveDirection() => RotateTowards(agent.desiredVelocity);

    public float NormalizedSpeed =>
        AgentReady ? Mathf.Clamp01(agent.velocity.magnitude / MoveSpeed) : 0f;

    public void SetAgentEnabled(bool enabled)
    {
        if (enabled)
        {
            agent.enabled = true;
            agent.Warp(transform.position);
        }
        else
        {
            StopMoving();
            agent.enabled = false;   // 사망 시 다른 Agent를 밀어내지 않도록
        }
    }

    public void RotateTowards(Vector3 dir, bool instant = false)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = instant
            ? target
            : Quaternion.RotateTowards(transform.rotation, target, RotateSpeed * Time.deltaTime);
    }
}
