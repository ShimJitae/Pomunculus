using System;

public class RespawnUIPresenter : IDisposable
{
    // Model
    private readonly PlayerFSM player;

    // View
    private readonly IRespawnUIView view;

    public RespawnUIPresenter(PlayerFSM player, IRespawnUIView view)
    {
        this.player = player;
        this.view = view;

        player.OnDied += HandleDied;
        player.OnRevived += view.Hide;
        view.OnRespawnRequested += HandleRespawnRequested;

        // 이미 죽은 상태로 시작한 경우(세이브 로드 등)에도 창을 맞춘다
        if (player.IsDead) HandleDied();
        else view.Hide();
    }

    // 사망하면 부활 대기 시간으로 창을 연다
    private void HandleDied() => view.Show(player.Tuning.RespawnDelay);

    // 부활 요청이 오면 아직 죽어 있을 때만 부활시킨다
    private void HandleRespawnRequested()
    {
        if (player.IsDead) player.Revive();
    }

    public void Dispose()
    {
        player.OnDied -= HandleDied;
        player.OnRevived -= view.Hide;
        view.OnRespawnRequested -= HandleRespawnRequested;
    }
}
