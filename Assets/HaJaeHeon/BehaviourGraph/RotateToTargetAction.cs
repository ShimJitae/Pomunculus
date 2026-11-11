using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RotateToTarget", story: "[Self] rotates toward [Target] at [TurnSpeed]", category: "Action", id: "c0e9f12b31ba2788b881f632d78ccae8")]
public partial class RotateToTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> TurnSpeed;
    [SerializeReference] public BlackboardVariable<float> AngleTolerance = new BlackboardVariable<float>(5f); // 이 각도 안이면 완료

    protected override Status OnStart()
    {
        if (Self.Value == null || Target.Value == null)
            return Status.Failure;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target.Value == null)
            return Status.Failure;

        Transform selfTransform = Self.Value.transform;
        Vector3 directionToTarget = Target.Value.transform.position - selfTransform.position;
        directionToTarget.y = 0f;

        // 타겟과 겹쳐 방향을 정할 수 없으면 현재 방향 그대로 완료
        if (directionToTarget.sqrMagnitude < 0.0001f)
            return Status.Success;

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        selfTransform.rotation = Quaternion.RotateTowards(selfTransform.rotation, targetRotation, TurnSpeed.Value * Time.deltaTime);

        return Quaternion.Angle(selfTransform.rotation, targetRotation) <= AngleTolerance.Value
            ? Status.Success
            : Status.Running;
    }
}