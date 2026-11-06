using System;
using System.Collections.Generic;
using UnityEngine;

// 로컬 플레이어의 Model과 HUD 각 패널 View를 Presenter로 묶고 수명을 관리한다.
public class PlayerHUDBinder : MonoBehaviour
{
    [SerializeField] private PlayerFSM player;

    [Header("View")]
    [SerializeField] private StatPanelView statPanelView;
    [SerializeField] private StatusPanelView statusPanelView;
    [SerializeField] private PotionSlotView potionSlotView;

    private readonly List<IDisposable> presenters = new();

    private void Start()
    {
        if (player != null) Bind(player);
    }

    // 플레이어와 HUD를 연결. 다시 호출하면 이전 연결을 해제하고 새로 묶는다 (부활·재접속 대비)
    public void Bind(PlayerFSM localPlayer)
    {
        UnbindAll();

        presenters.Add(new StatPanelPresenter(localPlayer.Stats, statPanelView));
        presenters.Add(new StatusPanelPresenter(localPlayer.GetComponent<StatusEffectController>(), statusPanelView));
        presenters.Add(new PotionSlotPresenter(potionSlotView));
    }

    // 모든 Presenter의 구독을 해제
    public void UnbindAll()
    {
        for (int i = 0; i < presenters.Count; i++)
        {
            presenters[i].Dispose();
        }

        presenters.Clear();
    }

    private void OnDestroy()
    {
        UnbindAll();
    }
}
