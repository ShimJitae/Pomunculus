using System.Collections.Generic;
using UnityEngine;

/// <summary>트리거 범위 안의 Entity에게 간격마다 상태이상을 건다. 간격은 파생 클래스가 정한다.</summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class SESource : MonoBehaviour
{
    // 범위 안 Entity → 마지막으로 부여한 시각
    private readonly Dictionary<Entity, float> applyEntityDic = new();
    // Update에서 딕셔너리 값을 바꾸며 돌기 위한 키 복사본. 재사용하므로 할당 없음
    private readonly List<Entity> buffer = new();

    protected abstract SESourceData Data { get; }

    /// <summary>이 대상에게 지금 적용할 부여 간격(초)</summary>
    protected abstract float GetInterval(Entity target);

    /// <summary>이 Entity에게 상태이상을 걸 수 있는지. 기본은 모든 Entity</summary>
    protected virtual bool CanTarget(Entity entity) => true;

    // 플레이어에게 Rigidbody가 없으므로 트리거 이벤트를 받으려면 이쪽에 kinematic Rigidbody가 필요하다
    protected virtual void Awake()
    {
        GetComponent<Rigidbody>().isKinematic = true;
        foreach (Collider col in GetComponents<Collider>())
            col.isTrigger = true;
    }

    // 다시 켜지면 겹쳐 있는 대상의 OnTriggerEnter가 새로 들어온다
    private void OnDisable() => applyEntityDic.Clear();

    private void OnTriggerEnter(Collider other)
    {
        Entity entity = other.GetComponentInParent<Entity>();
        if (entity == null || !CanTarget(entity) || applyEntityDic.ContainsKey(entity))
        {
            return;
        }

        applyEntityDic.Add(entity, Time.time);
        Apply(entity); // 들어오자마자 1회
    }

    private void OnTriggerExit(Collider other)
    {
        Entity entity = other.GetComponentInParent<Entity>();
        if (entity != null)
        {
            applyEntityDic.Remove(entity); // 다시 들어오면 즉시 부여부터
        }
    }

    private void Update()
    {
        if (applyEntityDic.Count == 0 || Data == null) return;

        buffer.Clear();
        foreach (Entity entity in applyEntityDic.Keys)
        {
            buffer.Add(entity);
        }

        float now = Time.time;
        for (int i = 0; i < buffer.Count; i++)
        {
            Entity entity = buffer[i];

            // 파괴·비활성은 OnTriggerExit가 오지 않을 수 있으므로 여기서 정리한다
            if (entity == null || !entity.isActiveAndEnabled)
            {
                applyEntityDic.Remove(entity);
                continue;
            }

            if (entity.CheckDie()) continue;
            if (now - applyEntityDic[entity] < GetInterval(entity)) continue;

            applyEntityDic[entity] = now;
            Apply(entity);
        }
    }

    private void Apply(Entity entity)
    {
        SESourceData data = Data;
        if (data == null || entity.CheckDie()) return;

        IReadOnlyList<StatusEffectType> list = data.StatusTypeList;
        for (int i = 0; i < list.Count; i++)
            entity.RegisterStatusEffect(list[i]);
    }
}
