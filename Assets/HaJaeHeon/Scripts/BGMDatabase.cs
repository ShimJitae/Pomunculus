using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BGMDatabase", menuName = "Scriptable Objects/BGMDatabase")]
public class BGMDatabase : ScriptableObject
{
    [Serializable]
    private struct RegionBgmStruct
    {
        public RegionType region;
        public AudioSO bgmData;
    }

    [SerializeField] private List<RegionBgmStruct> regionBGMList = new();

    private readonly Dictionary<RegionType, AudioSO> regionBGM = new Dictionary<RegionType, AudioSO>();

    private void OnEnable()
    {
        InitDatabase();
    }

#if UNITY_EDITOR
    // 플레이 중 인스펙터에서 목록을 고쳐도 바로 반영
    private void OnValidate()
    {
        InitDatabase();
    }
#endif
    private void InitDatabase()
    {
        regionBGM.Clear();

        foreach (RegionBgmStruct bgm in regionBGMList)
        {
            if (bgm.bgmData == null)
                continue;

            if(!regionBGM.TryAdd(bgm.region, bgm.bgmData))
                Debug.LogWarning($"{name}: {bgm.region} 지역 BGM이 중복 등록되어 있습니다.", this);
        }
    }
    
    // 등록되지 않은 지역이면 null
    public AudioSO GetBgm(RegionType region)
    {
        regionBGM.TryGetValue(region, out AudioSO bgmData);

        return bgmData;
    }
}
