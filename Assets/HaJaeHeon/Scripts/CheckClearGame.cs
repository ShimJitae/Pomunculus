using System;
using UnityEngine;

public class CheckClearGame : SingletonBehavior<CheckClearGame>
{
    public static bool IsGameClear => IsGameClear;
    public static event Action OnChangeGameState;

    private Enemy bossEnemy;
    private bool hasCleared;

    public override void Awake()
    {
        bossEnemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        bossEnemy.OnHpChanged += HandleBossHpChanged;
    }

    private void OnDisable()
    {
        bossEnemy.OnHpChanged -= HandleBossHpChanged;
    }

    private void HandleBossHpChanged(int currentHp, int maxHp)
    {
        if (hasCleared || currentHp > 0)
            return;

        hasCleared = true;
        Debug.Log("보스 클리어");
        OnChangeGameState.Invoke();
    }
}
