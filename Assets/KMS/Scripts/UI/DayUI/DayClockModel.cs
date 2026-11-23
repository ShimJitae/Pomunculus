using System;
using UnityEngine;

// 시계 UI가 쓰는 날짜·계절 정보를 WorldManager에서 읽어 정리해 주는 Model.
// WorldManager의 날짜/계절 변경 이벤트를 받아 화면용 값으로 다시 알린다.
public class DayClockModel : IDisposable
{
    private static readonly int seasonCount = Enum.GetValues(typeof(Season)).Length;
    private static int YearLength => WorldData.SeasonLength * seasonCount;

    private readonly WorldManager worldManager;

    // 날짜가 바뀌면 화면 표시용 생존 일수(1일차부터)로 알린다
    public event Action<int> OnDayChanged;
    // 계절이 바뀌면 새 계절을 알린다
    public event Action<Season> OnSeasonChanged;

    public DayClockModel(WorldManager worldManager)
    {
        this.worldManager = worldManager;
        worldManager.OnDayChangedEvent += HandleDayChanged;
        worldManager.OnSeasonChangedEvent += HandleSeasonChanged;
    }

    // 월드를 불러왔거나 스냅샷을 받아 값을 읽을 수 있는 상태인지
    public bool IsReady => worldManager.IsReady && worldManager.CurWorldData != null;

    // 0부터 세는 내부 날짜
    private int Day => worldManager.CurWorldData?.Day ?? 0;

    // 오늘 하루 중 지난 비율 (0 ~ 1)
    private float DayElapsedRatio => Mathf.Clamp01((worldManager.CurWorldData?.Time ?? 0f) / WorldManager.DayLength);

    // 화면 표시용 생존 일수 (Day 0 = 1일차)
    public int DayNumber => Day + 1;

    // 오늘 남은 시간 비율 (하루 시작 1 -> 하루 끝 0)
    public float DayRemainRatio => 1f - DayElapsedRatio;

    public Season CurrentSeason => WorldData.GetSeason(Day);

    public Season NextSeason => (Season)(((int)CurrentSeason + 1) % seasonCount);

    // 다음 계절이 시작될 때까지 남은 날짜 수 (계절 첫날이면 SeasonLength)
    public int DaysUntilNextSeason => WorldData.SeasonLength - Day % WorldData.SeasonLength;

    // 1년(봄~겨울) 중 지난 비율 (0 ~ 1). 오늘 지난 시간까지 포함
    public float YearProgress => (Day % YearLength + DayElapsedRatio) / YearLength;

    // WorldManager의 0부터 세는 날짜를 표시용 일수로 바꿔 전달한다
    private void HandleDayChanged(int day) => OnDayChanged?.Invoke(day + 1);

    // WorldManager의 계절 변경을 그대로 전달한다
    private void HandleSeasonChanged(Season season) => OnSeasonChanged?.Invoke(season);

    // WorldManager 이벤트 구독을 해제한다
    public void Dispose()
    {
        worldManager.OnDayChangedEvent -= HandleDayChanged;
        worldManager.OnSeasonChangedEvent -= HandleSeasonChanged;
    }
}
