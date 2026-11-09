using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemySkillPattern
{
    [SerializeField] private List<EnemyPatternDataSO> candidatePatterns = new();
    [SerializeField] private EnemyPatternDataSO nextPattern;
    [SerializeField] private EnemyPatternDataSO lastPattern;

    public EnemyPatternDataSO NextPattern => nextPattern;

    public void LoadPatternList(IReadOnlyList<EnemyPatternDataSO> enemyPatterns, EnemyPatternDataSO normalAttackPattern)
    {
        candidatePatterns.Clear();
        lastPattern = null;

        if(enemyPatterns != null)
        {
            foreach (EnemyPatternDataSO patternData in enemyPatterns)
            {
                AddCandidate(patternData);
            }

        }
        AddCandidate(normalAttackPattern);

        nextPattern = PickSkillPattern();
    }

    public EnemyPatternDataSO UseSkill()
    {
        EnemyPatternDataSO currentPattern = nextPattern;

        lastPattern = currentPattern;
        nextPattern = PickSkillPattern();

        return currentPattern;
    }

    private void AddCandidate(EnemyPatternDataSO patternData)
    {
        if (patternData == null || patternData.PatternWeight <= 0 || candidatePatterns.Contains(patternData))
            return;

        candidatePatterns.Add(patternData);
    }

    private EnemyPatternDataSO PickSkillPattern()
    {
        int totalWeight = 0;

        foreach (EnemyPatternDataSO patternData in candidatePatterns)
        {
            if (patternData != lastPattern)
                totalWeight += patternData.PatternWeight;
        }

        if (totalWeight <= 0)
            return candidatePatterns.Count > 0 ? candidatePatterns[0] : null;

        int rollWeight = UnityEngine.Random.Range(0, totalWeight);

        foreach(EnemyPatternDataSO patternData in candidatePatterns)
        {
            if (patternData == lastPattern)
                continue;

            rollWeight -= patternData.PatternWeight;

            if (rollWeight < 0)
                return patternData;
        }

        return null;
    }
}
