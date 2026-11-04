using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상태 데이터 목록을 보관하고 상태 종류로 해당 SO를 조회한다.
/// </summary>
[CreateAssetMenu(fileName = "StatusEffectDatabase", menuName = "Scriptable Objects/StatusEffectDatabase")]
public class StatusEffectDatabase : ScriptableObject
{
    [Tooltip("조회에 사용할 상태 데이터 목록. 상태 종류는 중복될 수 없음")]
    [SerializeField] private List<StatusEffectData> statusEffectDatas = new List<StatusEffectData>();

    private Dictionary<StatusEffectType, StatusEffectData> dataDic;
    private Dictionary<StatusEffectType, IStatusBehavior> behaviorDic;
    private bool isInitialized;

    // 목록을 검증하고 조회표를 구성한다. 잘못된 항목이 있으면 조회를 허용하지 않는다.
    public bool Initialize()
    {
        isInitialized = true;
        dataDic = null;
        behaviorDic = null;

        if (statusEffectDatas == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[StatusEffectDatabase] 상태 데이터 목록이 없습니다.", this);
#endif
            return false;
        }

        var lookup = new Dictionary<StatusEffectType, StatusEffectData>(statusEffectDatas.Count);
        bool isValid = true;

        for (int i = 0; i < statusEffectDatas.Count; i++)
        {
            StatusEffectData data = statusEffectDatas[i];
            if (data == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"[StatusEffectDatabase] 상태 데이터 목록의 {i}번 항목이 비어 있습니다.", this);
#endif
                isValid = false;
                continue;
            }

            StatusEffectType type = data.StatusEffectType;
            if (type == StatusEffectType.None)
            {
#if UNITY_EDITOR
                Debug.LogError($"[StatusEffectDatabase] 상태 데이터 목록의 {i}번 항목에 상태 종류가 지정되지 않았습니다.", data);
#endif
                continue;
            }

            if (lookup.ContainsKey(type))
            {
#if UNITY_EDITOR
                Debug.LogError($"[StatusEffectDatabase] 상태 종류 {type}이 중복 등록되어 있습니다.", this);
#endif
                isValid = false;
                continue;
            }

            lookup.Add(type, data);
        }

        if (!isValid)
        {
            return false;
        }

        var behaviorLookup = new Dictionary<StatusEffectType, IStatusBehavior>
        {
            [StatusEffectType.Burn] = new BurnBehavior(),
            [StatusEffectType.Chill] = new ChillBehavior(),
            [StatusEffectType.Poison] = new PoisonBehavior(),
            [StatusEffectType.Slime] = new SlimeBehavior(),
            [StatusEffectType.Wet] = new WetBehavior(),
            [StatusEffectType.Stun] = new StunBehavior(),
            [StatusEffectType.Freeze] = new FreezeBehavior(),
            [StatusEffectType.Bind] = new BindBehavior(),
            [StatusEffectType.Vulnerable] = new VulnerableBehavior(),
            [StatusEffectType.Haste] = new HasteBehavior(),
            [StatusEffectType.Petrify] = new PetrifyBehavior(),
            [StatusEffectType.Invincible] = new InvincibleBehavior()
        };

        // 등록된 모든 데이터에 대응하는 행동이 있는지 검사한다.
        foreach (StatusEffectType type in lookup.Keys)
        {
            /*
            if (!behaviorLookup.ContainsKey(type))
            {
#if UNITY_EDITOR
                Debug.LogError($"[StatusEffectDatabase] {type}의 행동이 등록되지 않았습니다.", this);
#endif
                return false;
            }
            */
        }

        // 데이터와 행동이 모두 준비된 뒤 조회표를 공개한다.
        dataDic = lookup;
        behaviorDic = behaviorLookup;
        return true;
    }

    /// <summary>
    /// 상태 종류에 해당하는 데이터를 조회한다.
    /// </summary>
    /// <param name="type">상태 종류</param>
    /// <param name="data">조회된 상태 데이터</param>
    /// <returns>조회 성공 여부</returns>
    public bool TryGetData(StatusEffectType type, out StatusEffectData data)
    {
        data = null;

        if (!isInitialized)
        {
#if UNITY_EDITOR
            Debug.LogError($"[StatusEffectDatabase] 초기화 되지 않았습니다.", this);
#endif
            return false;
        }

        if (dataDic == null || !dataDic.TryGetValue(type, out data) || data == null)
        {
            data = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 상태 종류에 해당하는 행동을 조회한다.
    /// </summary>
    /// <param name="type">상태 종류</param>
    /// <param name="behavior">조회된 상태 행동</param>
    /// <returns>조회 성공 여부</returns>
    public bool TryGetBehavior(StatusEffectType type, out IStatusBehavior behavior)
    {
        behavior = null;

        if (!isInitialized)
        {
#if UNITY_EDITOR
            Debug.LogError($"[StatusEffectDatabase] 초기화 되지 않았습니다.", this);
#endif
            return false;
        }

        if (behaviorDic == null || !behaviorDic.TryGetValue(type, out behavior) || behavior == null)
        {
            behavior = null;
            return false;
        }

        return true;
    }
}
