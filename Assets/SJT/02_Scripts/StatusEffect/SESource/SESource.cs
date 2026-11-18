using System.Collections.Generic;
using UnityEngine;

/// <summary>트리거 범위 안의 Entity에게 간격마다 상태이상을 건다. 간격은 파생 클래스가 정한다.</summary>
public abstract class SESource : CullingTickBehaviour
{
    private struct ApplyEntry
    {
        public Entity entity;
        public float lastTime; // 마지막으로 부여한 시각
    }

    // 범위 안 대상. 구조체라 할당 없음
    private readonly List<ApplyEntry> entries = new();

    // 화면 판정용 구를 트리거 범위에 맞춰 계산할 때 쓴다
    private Collider[] colliders;

    protected abstract SESourceData Data { get; }

    /// <summary>이 대상에게 지금 적용할 부여 간격(초)</summary>
    protected abstract float GetInterval(Entity target);

    /// <summary>이 Entity에게 상태이상을 걸 수 있는지. 기본은 SO의 대상 레이어에 속한 Entity</summary>
    protected virtual bool CanTarget(Entity entity)
    {
        SESourceData data = Data;
        return data != null && (data.TargetLayers & (1 << entity.gameObject.layer)) != 0;
    }

    protected virtual void Awake()
    {
        // 대상이 아닌 레이어와는 물리 쌍을 만들지 않는다. 범위 안의 지형·소품마다 들어오던
        // OnTriggerEnter 호출과 GetComponentInParent 비용이 사라진다
        SESourceData data = Data;
        int exclude = data != null ? ~data.TargetLayers.value : ~0;
        colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.isTrigger = true;
            col.excludeLayers = exclude;
        }
    }

    // 다시 켜지면 겹쳐 있는 대상의 OnTriggerEnter가 새로 들어온다
    protected override void OnDisable()
    {
        base.OnDisable();
        entries.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        Entity entity = other.GetComponentInParent<Entity>();
        if (entity == null || !CanTarget(entity) || IndexOf(entity) >= 0)
        {
            return;
        }

        entries.Add(new ApplyEntry { entity = entity, lastTime = Time.time });
        Apply(entity); // 들어오자마자 1회
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

    // 트리거 범위 전체를 감싸는 구. 화면 안의 대상이 범위에 있으면 이 구도 반드시 화면에 걸린다
    protected override BoundingSphere GetCullingSphere()
    {
        // 꺼진 콜라이더의 bounds는 원점의 빈 상자라서 합치면 구가 원점까지 커진다. 켜진 것만 합친다
        bool hasBounds = false;
        Bounds bounds = default;
        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null || !col.enabled)
                {
                    continue;
                }

                if (hasBounds)
                {
                    bounds.Encapsulate(col.bounds);
                }
                else
                {
                    bounds = col.bounds;
                    hasBounds = true;
                }
            }
        }

        if (!hasBounds)
        {
            return new BoundingSphere(transform.position, 0f);
        }
        return new BoundingSphere(bounds.center, bounds.extents.magnitude);
    }

    // 화면 안에 있을 때만 CullingManager가 호출한다. 화면 밖에서는 주기 부여가 멈춘다
    protected override void CullingTick(float now)
    {
        if (entries.Count == 0 || Data == null) return;

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
            entity.RegisterStatusEffect(entry.type, count);
        }
    }
}
