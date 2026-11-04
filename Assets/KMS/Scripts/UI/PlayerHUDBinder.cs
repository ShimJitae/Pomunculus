using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHUDBinder : MonoBehaviour
{
    [SerializeField] private PlayerFSM player;
    [SerializeField] private PlayerHUD hud;

    private List<IDisposable> presenters;

    private PlayerHUDPresenter presenter;

    private void Start()
    {
        if (player != null) Bind(player);
    }

    public void Bind(PlayerFSM localPlayer)
    {
        presenters.Add(new PlayerHUDPresenter(localPlayer.Stats, hud));

        presenter?.Dispose();   // 이전 연결 해제 (부활·재접속 대비)
        presenter = new PlayerHUDPresenter(localPlayer.Stats, hud);


    }

    private void OnDestroy()
    {
        presenter?.Dispose();
    }

}
