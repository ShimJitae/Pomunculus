using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RecordSpawnPosition", story: "Store Agent position in [SpawnPosition]", category: "Action", id: "65062a86eb5dd4838b7596b81a401f88")]
public partial class RecordSpawnPositionAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Target; 
    [SerializeReference] public BlackboardVariable<Vector3> SpawnPosition;

    // 행동 시작 시 적의 현재 위치를 소환 위치로 저장한다.
    protected override Status OnStart()
    {
        if (Target == null || Target.Value == null || SpawnPosition == null)
        {
            return Status.Failure;
        }

        SpawnPosition.Value = Target.Value.transform.position;
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

