using System;

// 포션 장착 슬롯 Model ↔ PotionSlotView 연결 (뼈대).
public class PotionSlotPresenter : IDisposable
{
    // Model
    // TODO: 장착 슬롯 Model이 생기면 연결한다. 아이콘은 PotionData.Icon을 사용한다.

    // View
    private readonly IPotionSlotView view;

    public PotionSlotPresenter(IPotionSlotView view)
    {
        this.view = view;
    }

    // 구독 해제 (Model 연결 후 이벤트 해제 코드를 넣는다)
    public void Dispose()
    {
    }
}
