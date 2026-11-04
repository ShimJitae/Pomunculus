using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

#if UNITY_EDITOR
[CreateAssetMenu(menuName = "Behavior/Event Channels/EnemyAttack")]
#endif
[Serializable, GeneratePropertyBag]
[EventChannelDescription(name: "EnemyAttack", message: "[Attacker] Attack on [Target]", category: "Events", id: "9341a1ce6ed9d49c7890ad6f8eedac7e")]
public sealed partial class EnemyAttack : EventChannel<GameObject, GameObject> { }

