using MoreMountains.Feedbacks;
using UnityEngine;

/// <summary>씬에 두면 로컬 플레이어가 생성될 때 연결해 피격 연출을 재생한다.</summary>
public sealed class PlayerHitFeedback : MonoBehaviour
{
    [SerializeField] private MMF_Player feedback;

    private PlayerFSM boundPlayer;

    // WorldManager는 순수 C# 싱글톤이라 씬 로드 순서와 관계없이 구독할 수 있다
    private void OnEnable()
    {
        WorldManager.Instance.OnSpawnedEvent += HandleSpawned;

        foreach (IWorldObj worldObject in WorldManager.Instance.Objects.GetList())
        {
            if (TryBind(worldObject))
            {
                break;
            }
        }
    }

    private void OnDisable()
    {
        WorldManager.Instance.OnSpawnedEvent -= HandleSpawned;
        Unbind();
    }

    private void HandleSpawned(IWorldObj worldObject)
    {
        TryBind(worldObject);
    }

    // 내 소유 플레이어면 이전 연결을 끊고 새로 연결한다
    private bool TryBind(IWorldObj worldObject)
    {
        if (worldObject is not PlayerFSM player || !player.IsMine)
        {
            return false;
        }

        Unbind();
        boundPlayer = player;
        boundPlayer.OnDamaged += HandleDamaged;
        return true;
    }

    private void Unbind()
    {
        if (boundPlayer != null)
        {
            boundPlayer.OnDamaged -= HandleDamaged;
        }

        boundPlayer = null;
    }

    private void HandleDamaged(int damage)
    {
        if (feedback != null)
        {
            feedback.PlayFeedbacks();
        }
    }
}
