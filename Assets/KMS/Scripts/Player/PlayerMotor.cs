using UnityEngine;
using UnityEngine.AI;

// NavMeshAgent 이동과 회전 담당.
[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMotor : MonoBehaviour
{
    private NavMeshAgent agent;
    private PlayerTuningSO tuning;
    private float baseMoveSpeed;


    private bool AgentReady => agent.enabled && agent.isOnNavMesh;
    public bool AgentEnabled => agent.enabled;

    public void Initialize(PlayerTuningSO tuning, float baseMoveSpeed)
    {
        agent = GetComponent<NavMeshAgent>();

        this.tuning = tuning;
        this.baseMoveSpeed = baseMoveSpeed;
        agent.speed = baseMoveSpeed;
        agent.acceleration = tuning.Acceleration;
        agent.stoppingDistance = tuning.StoppingDistance;
        agent.updateRotation = false;
        agent.autoBraking = false;
    }

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
        agent.velocity = Vector3.zero;
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


    // 기본 이동속도 대비 현재 속도 비율
    public float NormalizedSpeed =>
        AgentReady && baseMoveSpeed > 0f
            ? Mathf.Clamp01(agent.velocity.magnitude / baseMoveSpeed)
            : 0f;

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
            : Quaternion.RotateTowards(transform.rotation, target, tuning.RotateSpeed * Time.deltaTime);
    }

    // 상태이상까지 반영된 최종 이동속도를 적용한다.
    public void SetMoveSpeed(float moveSpeed) => agent.speed = moveSpeed;
}
