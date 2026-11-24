using System;
using UnityEngine;

/// <summary>
/// 씬에 하나 두고, 이 클라이언트가 조종하는 로컬 플레이어를 찾아 알려준다.
/// 플레이어는 UI를 모르고, UI가 OnLocalPlayerChanged를 구독해 스스로 바인딩한다.
/// 플레이어가 사라지면 null로 알린다.
/// </summary>
public class LocalPlayerProvider : MonoBehaviour
{
    public static PlayerFSM Current { get; private set; }

    public static event Action<PlayerFSM> OnLocalPlayerChanged;

    private WorldManager world;

    // 앞으로 생성될 플레이어를 구독하고, 이미 생성된 내 플레이어가 있으면 바로 지정한다
    private void OnEnable()
    {
        world = WorldManager.Instance;
        if (world == null)
            return;

        world.OnSpawnedEvent += HandleWorldObjectSpawned;

        foreach (IWorldObj worldObject in world.Objects.GetList())
        {
            if (TrySetLocalPlayer(worldObject))
                break;
        }
    }

    // 구독을 해제하고 로컬 플레이어를 비운다
    private void OnDisable()
    {
        if (world != null)
            world.OnSpawnedEvent -= HandleWorldObjectSpawned;

        world = null;
        SetCurrent(null);
    }

    // WorldManager에 Despawn 이벤트가 없어 매 프레임 로컬 플레이어가 파괴됐는지 확인한다
    private void Update()
    {
        if (!ReferenceEquals(Current, null) && Current == null)
            SetCurrent(null);
    }

    // 새로 생성된 오브젝트가 내 플레이어인지 확인한다
    private void HandleWorldObjectSpawned(IWorldObj worldObject)
    {
        TrySetLocalPlayer(worldObject);
    }

    // 내 소유 플레이어면 로컬 플레이어로 지정하고 true를 반환한다
    private bool TrySetLocalPlayer(IWorldObj worldObject)
    {
        if (worldObject is not PlayerFSM player || !player.IsMine)
            return false;

        SetCurrent(player);
        return true;
    }

    // 로컬 플레이어가 바뀌었을 때만 갱신하고 구독자에게 알린다
    private static void SetCurrent(PlayerFSM player)
    {
        if (ReferenceEquals(Current, player))
            return;

        Current = player;
        OnLocalPlayerChanged?.Invoke(player);
    }
}
