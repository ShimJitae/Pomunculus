using UnityEngine;

// PickupUI Canvas에 붙어서 로컬 플레이어가 바뀔 때마다 Model(PlayerFSM의 아이템 지급 이벤트)과 View를 Presenter로 묶는다.
public class PickupUIBinder : MonoBehaviour
{
    [SerializeField] private PickupUIView pickupUIView;

    private PickupUIPresenter presenter;

    // 로컬 플레이어 변경을 구독하고, 이미 있으면 바로 연결한다
    private void OnEnable()
    {
        LocalPlayerProvider.OnLocalPlayerChanged += Bind;
        Bind(LocalPlayerProvider.Current);
    }

    // 구독과 연결을 모두 해제한다
    private void OnDisable()
    {
        LocalPlayerProvider.OnLocalPlayerChanged -= Bind;
        Unbind();
    }

    // 이전 연결을 해제하고 새 플레이어와 연결한다 (null이면 해제만 한다)
    private void Bind(PlayerFSM localPlayer)
    {
        Unbind();

        if (localPlayer == null)
            return;

        presenter = new PickupUIPresenter(localPlayer, pickupUIView);
    }

    // 현재 Presenter의 구독을 해제한다
    private void Unbind()
    {
        presenter?.Dispose();
        presenter = null;
    }
}
