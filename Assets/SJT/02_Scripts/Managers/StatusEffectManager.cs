using UnityEngine;

public class StatusEffectManager : MonoBehaviour
{
    private static StatusEffectManager instance;
    public static StatusEffectManager Instance => instance;
    [SerializeField] private StatusEffectDatabase statusEffectDatabase;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        statusEffectDatabase.Initialize();
    }

    public StatusEffectData GetSOData(StatusEffectType seType)
    {
        if (!statusEffectDatabase.TryGetData(seType, out StatusEffectData data))
        {
#if UNITY_EDITOR
            Debug.LogError($"[StatusEffectManager] statusEffectDatabase에서 {seType}에 해당하는 데이터를 찾을 수 없습니다.", this);
#endif
            return null;
        }
        return data;
    }

    public IStatusBehavior GetStatusBehavior(StatusEffectType seType)
    {
        if (!statusEffectDatabase.TryGetBehavior(seType, out IStatusBehavior behavior))
        {
#if UNITY_EDITOR
            Debug.LogError($"[StatusEffectManager] statusEffectDatabase에서 {seType}에 해당하는 행동을 찾을 수 없습니다.", this);
#endif
            return null;
        }
        return behavior;
    }
}
