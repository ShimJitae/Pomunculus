using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SelectAttackPattern", story: "[Self] selects SkillPattern into [AttackTrigger] [TelegraphTime] [RecoveryTime]", category: "Action", id: "5abc469d8a87dbeffde12de4092771f5")]
public partial class SelectAttackPatternAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<string> AttackTrigger;
    [SerializeReference] public BlackboardVariable<float> TelegraphTime;
    [SerializeReference] public BlackboardVariable<float> RecoveryTime;

    protected override Status OnStart()
    {
        if (Self.Value == null || !Self.Value.TryGetComponent(out Enemy enemy))
            return Status.Failure;

        EnemyPatternDataSO currentPattern = enemy.UseNextPattern();

        if (currentPattern == null)
            return Status.Failure;

        AttackTrigger.Value = currentPattern.AnimatorTrigger;
        TelegraphTime.Value = currentPattern.Telegraph;
        RecoveryTime.Value = currentPattern.Recovery;

        return Status.Success;
    }
}

