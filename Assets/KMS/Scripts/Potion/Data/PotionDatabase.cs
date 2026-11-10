// 포션 Id로 PotionData를 찾는 DB
using Pomunculus.Items;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PotionDatabase", menuName = "Pomunculus/Items/PotionDatabase")]
public class PotionDatabase : ScriptableObject
{
    [SerializeField] private List<PotionData> potions = new List<PotionData>();

    private Dictionary<int, PotionData> potionById;
    private static PotionDatabase instance;

    // Resources에서 한 번만 불러온다
    public static PotionDatabase Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<PotionDatabase>("PotionDatabase");
            return instance;
        }
    }

    // 에셋이 다시 로드되면 사전을 버려서 다음 조회 때 새로 만들게 한다
    private void OnEnable() => potionById = null;

    // 에디터에서 목록이 바뀌면(인스펙터 수정, 포션 DB 갱신) 사전을 버린다
    private void OnValidate() => potionById = null;

    // Id로 포션을 찾는다. 없으면 null
    public PotionData Find(int potionId)
    {
        if (potionById == null) BuildLookup();
        return potionById.TryGetValue(potionId, out PotionData potion) ? potion : null;
    }

    // 목록을 Id 사전으로 만든다
    private void BuildLookup()
    {
        potionById = new Dictionary<int, PotionData>();
        foreach (PotionData potion in potions)
        {
            if (potion != null) potionById.TryAdd(potion.Id, potion);
        }
    }
}