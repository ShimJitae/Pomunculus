using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Pick Patrol Point", story: "[Self] picks patrol point into [PatrolPoint]", category: "Action", id: "b33715206dd3ddcb9f5ec6a1044ab1a4")]
public partial class PickPatrolPointAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Vector3> PatrolPoint;

    protected override Status OnStart()
    {
        if (Self.Value == null || !Self.Value.TryGetComponent(out EnemyMovement movement))
            return Status.Failure;

        PatrolPoint.Value = movement.GetNextPatrolPoint();
        return Status.Success;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

