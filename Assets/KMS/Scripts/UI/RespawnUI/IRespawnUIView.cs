using System;

public interface IRespawnUIView
{
    // 부활 버튼을 눌렀거나 카운트다운이 끝났을 때 발생
    public event Action OnRespawnRequested;

    public void Show(float respawnDelay);
    public void Hide();
}
