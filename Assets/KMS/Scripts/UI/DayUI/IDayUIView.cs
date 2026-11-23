using System;

public interface IDayUIView
{
    // 시계에 마우스가 올라가면 true, 벗어나면 false
    public event Action<bool> OnHoverChanged;

    public void SetDay(int dayNumber);
    public void SetDayRemainRatio(float remainRatio);
    public void SetYearProgress(float yearProgress);
    public void SetCurrentSeason(Season season);
    public void SetNextSeason(Season nextSeason, int daysUntilNextSeason);
    public void SetHover(bool isHovered);
}
