using System.Collections.Generic;
using UnityEngine;

/// <summary>트리거 범위 안의 Entity에게 간격마다 상태이상을 건다. 간격은 파생 클래스가 정한다.</summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class SESource : MonoBehaviour
{
    private struct ApplyEntry
    {
        public Entity entity;
        public float lastTime; // 마지막으로 부여한 시각
    }

    // 범위 안 대상. 구조체라 할당 없음
    private readonly List<ApplyEntry> entries = new();

    protected abstract SESourceData Data { get; }

    /// <summary>이 대상에게 지금 적용할 부여 간격(초)</summary>
    protected abstract float GetInterval(Entity target);

    /// <summary>이 Entity에게 상태이상을 걸 수 있는지. 기본은 SO의 무시 레이어가 아닌 Entity</summary>
    protected virtual bool CanTarget(Entity entity)
    {
        SESourceData data = Data;
        return data == null || (data.IgnoreLayers & (1 << entity.gameObject.layer)) == 0;
    }

    /// <summary>대상을 범위 안에 처음 등록했을 때 호출한다.</summary>
    protected virtual void OnTargetEntered(Entity entity) { }

    // 플레이어에게 Rigidbody가 없으므로 트리거 이벤트를 받으려면 이쪽에 kinematic Rigidbody가 필요하다
    protected virtual void Awake()
    {
        GetComponent<Rigidbody>().isKinematic = true;
        foreach (Collider col in GetComponents<Collider>())
            col.isTrigger = true;
    }

    // 다시 켜지면 겹쳐 있는 대상의 OnTriggerEnter가 새로 들어온다
    private void OnDisable() => entries.Clear();

    private void OnTriggerEnter(Collider other)
    {
        Entity entity = other.GetComponentInParent<Entity>();
        if (entity == null || !CanTarget(entity) || IndexOf(entity) >= 0)
        {
            return;
        }

        entries.Add(new ApplyEntry { entity = entity, lastTime = Time.time });
        Apply(entity); // 들어오자마자 1회
        OnTargetEntered(entity);
    }

    private void OnTriggerExit(Collider other)
    {
        Entity entity = other.GetComponentInParent<Entity>();
        if (entity == null) return;

        int index = IndexOf(entity);
        if (index >= 0)
        {
            RemoveAt(index); // 다시 들어오면 즉시 부여부터
        }
    }

    private void Update()
    {
        if (entries.Count == 0 || Data == null) return;

        float now = Time.time;
        // 역순: 지울 때 마지막 원소를 이 자리로 옮기는데, 그 원소는 이미 처리했다
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            ApplyEntry entry = entries[i];
            Entity entity = entry.entity;

            // 파괴·비활성은 OnTriggerExit가 오지 않을 수 있으므로 여기서 정리한다
            if (entity == null || !entity.isActiveAndEnabled)
            {
                RemoveAt(i);
                continue;
            }

            if (entity.CheckDie()) continue;
            if (now - entry.lastTime < GetInterval(entity)) continue;

            entry.lastTime = now;
            entries[i] = entry; // 구조체 복사본을 고쳤으므로 다시 써 넣는다
            Apply(entity);
        }
    }

    // Unity의 ==는 파괴된 오브젝트끼리 같다고 볼 수 있어 참조로 비교한다
    private int IndexOf(Entity entity)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (ReferenceEquals(entries[i].entity, entity)) return i;
        }
        return -1;
    }

    // 마지막 원소를 지울 자리로 옮기고 끝을 지운다. 뒤를 당기지 않으므로 O(1)
    private void RemoveAt(int index)
    {
        int last = entries.Count - 1;
        entries[index] = entries[last];
        entries.RemoveAt(last);
    }

    private void Apply(Entity entity)
    {
        SESourceData data = Data;
        if (data == null || entity.CheckDie()) return;

        // 한 번 등록할 때마다 1중첩이므로 중첩 수만큼 등록한다. 저항이 있으면 1회씩 막힌다
        IReadOnlyList<SEApplyEntry> list = data.ApplyList;
        for (int i = 0; i < list.Count; i++)
        {
            SEApplyEntry entry = list[i];
            int count = Mathf.Max(1, entry.stackCount); // 인스펙터에서 새로 추가한 항목은 0으로 시작한다
            for (int s = 0; s < count; s++)
                entity.RegisterStatusEffect(entry.type);
        }
    }
}
