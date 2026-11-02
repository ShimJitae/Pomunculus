using UnityEngine;
using UnityEngine.UIElements;

public class Entity : MonoBehaviour
{
    [SerializeField] private int currentHp;
    [SerializeField] private bool isDie;

    public Entity(int currentHp, bool isDie)
    {
        this.currentHp = currentHp;
        this.isDie = isDie;
    }

    public virtual void TakeDamage(int damage)
    {

    }
}
