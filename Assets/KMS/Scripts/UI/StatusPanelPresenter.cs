using System;

// 상태이상 Model ↔ StatusPanelView 연결 (뼈대).
public class StatusPanelPresenter : IDisposable
{
    // Model
    // TODO: StatusEffectController에 걸림/종료 이벤트가 추가되면 Model 필드와 구독을 연결한다.

    // View
    private readonly IStatusPanelView view;

    public StatusPanelPresenter(IStatusPanelView view)
    {
        this.view = view;
        view.ClearStatusIcons();
    }

    // 구독 해제 (Model 연결 후 이벤트 해제 코드를 넣는다)
    public void Dispose()
    {
    }
}
