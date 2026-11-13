using System;
using UnityEngine;

// 상태이상 Model ↔ StatusPanelView 연결 (뼈대).
public class StatusPanelPresenter : IDisposable
{
    // Model
    // TODO: StatusEffectController에 걸림/종료 이벤트가 추가되면 Model 필드와 구독을 연결한다.
    private readonly StatusEffectController SEModel;
    // View
    private readonly IStatusPanelView view;

    public StatusPanelPresenter(StatusEffectController SEModel ,IStatusPanelView view)
    {
        this.SEModel = SEModel;
        this.view = view;

        SEModel.OnEffectRegistered += HandleEffectRegistered;
        SEModel.OnEffectReapplied += HandleEffectReapplied;
        SEModel.OnEffectRemoved += HandleEffectRemoved;

        // 늦게 Bind돼도 이미 걸려 있는 상태를 표시
        view.ClearStatusIcons();
        foreach (StatusEffect effect in SEModel.ActiveStatusEffects)
            HandleEffectRegistered(effect);
    }

    // 새 상태이상 → 아이콘 추가
    private void HandleEffectRegistered(StatusEffect effect)
        => view.AddStatusIcon(effect.Type, GetIcon(effect), effect.Category, effect.StackCount, effect.RemainingDuration, effect.Data.Duration);
    
    // 중첩 → 스택·남은 시간 갱신
    private void HandleEffectReapplied(StatusEffect effect)
        => view.UpdateStatusIcon(effect.Type, effect.StackCount, effect.RemainingDuration, effect.Data.Duration);

    // 종료 → 아이콘 제거 (풀 반납 전에 호출되므로 Type 읽기 안전)
    private void HandleEffectRemoved(StatusEffect effect)
        => view.RemoveStatusIcon(effect.Type);

    // 아이콘 조회. 데이터 위치가 정해지면 여기만 고치면 됨
    private Sprite GetIcon(StatusEffect effect) => null;

    // 구독 해제 + 아이콘 정리
    public void Dispose()
    {
        SEModel.OnEffectRegistered -= HandleEffectRegistered;
        SEModel.OnEffectReapplied -= HandleEffectReapplied;
        SEModel.OnEffectRemoved -= HandleEffectRemoved;
        view.ClearStatusIcons();
    }
}
