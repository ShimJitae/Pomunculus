using UnityEngine;

[RequireComponent(typeof(Entity))]
public class ItemDropper : MonoBehaviour
{
    [System.Serializable]
    private class StatusDropRule
    {
        public StatusEffectType statusType;
        public GameObject[] itemPrefabs;
    }
    [Header("기본 드롭")]
    [SerializeField] private GameObject[] itemPrefabs;
    [Header("상태이상에 따른 드롭")]
    [SerializeField] private StatusDropRule[] statusDropRules;
    [Header("생성 높이")]
    [SerializeField, Min(0f)] private float spawnHeight = 0.5f;
    [Header("아이템 사이 높이 간격")]
    [SerializeField, Min(0.01f)] private float heightGap = 0.5f;
    private bool hasDropped;
    public void DropItems()
    {
        if (hasDropped)
            return;
        hasDropped = true;
        GameObject[] drops = GetDropPrefabs();
        float height = spawnHeight;

        if (drops != null)
        {
            foreach (GameObject prefab in drops)
            {
                if (prefab == null)
                    continue;

                Vector3 position = transform.position + Vector3.up * height;
                WorldManager.Instance.Spawn(prefab.name, position, Quaternion.identity);

                height += heightGap;
            }
        }
        Entity entity = GetComponent<Entity>();
        WorldManager.Instance.Despawn(entity.ID);
    }
    private GameObject[] GetDropPrefabs()
    {
        if (statusDropRules == null || statusDropRules.Length == 0)
            return itemPrefabs;

        if (!TryGetComponent<StatusEffectController>(out var controller))
            return itemPrefabs;

        var activeEffects = controller.ActiveStatusEffects;

        if (activeEffects == null)
            return itemPrefabs;

        foreach (StatusDropRule rule in statusDropRules)
        {
            if (rule == null || rule.statusType == StatusEffectType.None ||
                rule.itemPrefabs == null || rule.itemPrefabs.Length == 0)
                continue;

            foreach (StatusEffect effect in activeEffects)
            {
                if (effect != null && effect.Type == rule.statusType)
                    return rule.itemPrefabs;
            }
        }
        return itemPrefabs;
    }
}