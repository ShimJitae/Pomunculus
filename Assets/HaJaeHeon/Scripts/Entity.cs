using Unity.VisualScripting;
using UnityEngine;

public class Entity : MonoBehaviour
{
    private int currentHp;

    public int CurrentHp
    {
        get
        {
            return currentHp;
        }
        private set
        {
            currentHp = Mathf.Clamp(value, 0, so.GetMaxHp());
        }
    }
    
    [SerializeField] protected bool isDie;
    [SerializeField] protected EntityDataSO so;

    protected virtual void OnEnable()
    {
        CurrentHp = so.GetHp();
        isDie = CurrentHp <= 0;
    }

    public virtual void TakeDamage(int damage)
    {
        if(isDie)
        {
            Debug.Log("이미 죽은 상태입니다.");
            return;
        }

        if(damage <= 0)
        {
            Debug.Log($"{damage}가 0보다 작거나 같습니다.");
            return;
        }

        CurrentHp -= damage;

        if (CurrentHp <= 0)
        {
            isDie = true;
        }
    }

    public virtual bool CheckDie()
    {
        return isDie;
    }
}
