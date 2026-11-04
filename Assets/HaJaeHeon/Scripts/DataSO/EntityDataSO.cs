using UnityEngine;

[CreateAssetMenu(fileName = "EntityDataSO", menuName = "Scriptable Objects/EntityDataSO")]
public class EntityDataSO : ScriptableObject
{
    [SerializeField] protected int hp;
    [SerializeField] protected int maxHp;

    [SerializeField] protected float moveSpeed;

    public int GetHp()
    {
        return hp;
    }

    public int GetMaxHp()
    {
        return maxHp;
    }
    public float GetSpeed() => moveSpeed;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxHp <= 0) Debug.LogWarning($"{name}: maxHp가 0 이하입니다.", this);
        else if (hp > maxHp) Debug.LogWarning($"{name}: hp({hp})가 maxHp({maxHp})보다 큽니다.", this);
        if (moveSpeed <= 0f) Debug.LogWarning($"{name}: moveSpeed가 0 이하입니다.", this);
    }
#endif
}
