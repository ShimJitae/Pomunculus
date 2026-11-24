using System;

public class DayUIPresenter : IDisposable
{
    // Model
    private readonly DayClockModel clock;

    // View
    private readonly IDayUIView view;

    // 직전 Tick에서 월드가 준비돼 있었는지. 준비되는 순간(불러오기·스냅샷 수신)을 잡아 전체를 다시 그린다
    private bool wasReady;

    public DayUIPresenter(DayClockModel clock, IDayUIView view)
    {
        this.clock = clock;
        this.view = view;

        clock.OnDayChanged += HandleDayChanged;
        clock.OnSeasonChanged += view.SetCurrentSeason;
        view.OnHoverChanged += view.SetHover;

        view.SetHover(false);
        wasReady = clock.IsReady;
        if (wasReady) RefreshAll();
    }

    // 매 프레임 View에서 호출. 시간은 이벤트 없이 흐르므로 링과 계절 마커를 여기서 갱신한다
    public void Tick()
    {
        if (!clock.IsReady)
        {
            wasReady = false;
            return;
        }

        if (!wasReady)
        {
            wasReady = true;
            RefreshAll();
            return;
        }

        view.SetDayRemainRatio(clock.DayRemainRatio);
        view.SetYearProgress(clock.YearProgress);
    }

    // 날짜가 바뀌면 일수와 다음 계절까지 남은 날짜를 갱신한다
    private void HandleDayChanged(int dayNumber)
    {
        view.SetDay(dayNumber);
        view.SetNextSeason(clock.NextSeason, clock.DaysUntilNextSeason);
    }

    // 모든 표시 값을 현재 Model 값으로 맞춘다
    private void RefreshAll()
    {
        view.SetDay(clock.DayNumber);
        view.SetCurrentSeason(clock.CurrentSeason);
        view.SetNextSeason(clock.NextSeason, clock.DaysUntilNextSeason);
        view.SetDayRemainRatio(clock.DayRemainRatio);
        view.SetYearProgress(clock.YearProgress);
    }

    // Model과 View 이벤트 구독을 해제한다
    public void Dispose()
    {
        clock.OnDayChanged -= HandleDayChanged;
        clock.OnSeasonChanged -= view.SetCurrentSeason;
        view.OnHoverChanged -= view.SetHover;
    }
}
