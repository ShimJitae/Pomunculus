using UnityEngine;
using System.Collections.Generic;

public static class StatusEffectPool
{
    private static Queue<StatusEffect> pool = new Queue<StatusEffect>(100);

    public static StatusEffect Get()
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        return new StatusEffect();
    }

    public static void Return(StatusEffect statusEffect)
    {
        statusEffect.Reset();
        pool.Enqueue(statusEffect);
    }
}
