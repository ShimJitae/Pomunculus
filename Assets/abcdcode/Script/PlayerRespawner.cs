using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어가 사망하면 일정 시간 뒤 지정한 위치에서 부활시킴. 월드 씬에 배치
// Player 코드를 건드리지 않고 공개된 수단만 사용
//  - 사망 감지 : PlayerFSM.Stats.OnHpChanged 이벤트에서 IsDead 확인 (TakeDamage에서 OnDie가 끝난 뒤 호출됨)
//  - 부활 : PlayerFSM.Revive() 후 NavMeshAgent를 껐다 켜서 리스폰 위치로 Warp
// 부활은 소유자 화면에서만 처리. HP는 Revive가 SendHpSync로, 위치는 WorldTransformSync가 다른 플레이어에게 전파
public class PlayerRespawner : MonoBehaviour
{
    private void OnEnable()
    {
        WorldManager.Instance.OnSpawnedEvent += OnWorldObjSpawned;
        // 이 컴포넌트보다 먼저 생성된 플레이어도 등록
        foreach (var obj in WorldManager.Instance.Objects.GetList()) OnWorldObjSpawned(obj);
    }
    private void OnDisable()
    {
        WorldManager.Instance.OnSpawnedEvent -= OnWorldObjSpawned;
        foreach (var (player, handler) in m_handlers)
            if (player != null) player.Stats.OnHpChanged -= handler;
        m_handlers.Clear();
        m_respawning.Clear();
        StopAllCoroutines();
    }

    private void OnWorldObjSpawned(IWorldObj obj)
    {
        if (obj is not PlayerFSM player) return;
        // 풀에서 재사용되는 플레이어는 이미 구독되어 있으므로 중복 구독하지 않음
        if (!m_handlers.ContainsKey(player))
        {
            void Handler(int hp, int maxHp) => CheckDeath(player);
            m_handlers[player] = Handler;
            player.Stats.OnHpChanged += Handler;
        }
        // 저장 시점에 죽어 있던 플레이어는 생성 직후부터 사망 상태이므로 바로 확인
        CheckDeath(player);
    }
    private void CheckDeath(PlayerFSM player)
    {
        if (player == null || !player.IsMine || !player.IsDead || m_respawning.Contains(player)) return;
        StartCoroutine(RespawnRoutine(player));
    }
    private IEnumerator RespawnRoutine(PlayerFSM player)
    {
        m_respawning.Add(player);
        yield return new WaitForSeconds(m_respawnDelay);
        m_respawning.Remove(player);
        // 대기 중 제거(방 퇴장 등)됐거나, 다른 수단(디버그 부활 등)으로 이미 살아났으면 무시
        if (player == null || !player.gameObject.activeInHierarchy || !player.IsMine || !player.IsDead) yield break;
        Respawn(player);
    }
    private void Respawn(PlayerFSM player)
    {
        player.Revive();
        // NavMeshAgent가 켜진 상태에서 transform만 옮기면 Agent가 원래 위치로 되돌리므로 껐다 켜서 Warp
        Vector3 position = m_respawnPoint != null ? m_respawnPoint.position : transform.position;
        Quaternion rotation = m_respawnPoint != null ? m_respawnPoint.rotation : transform.rotation;
        player.Motor.SetAgentEnabled(false);
        player.transform.SetPositionAndRotation(position, rotation);
        player.Motor.SetAgentEnabled(true);
        Debug.Log($"[PlayerRespawner] 플레이어 부활 (ID : {player.ID}, 위치 : {position})");
    }

    [SerializeField] private Transform m_respawnPoint; // 비어 있으면 이 오브젝트의 위치
    [SerializeField] private float m_respawnDelay = 3f; // 사망 후 부활까지 대기 시간(초)

    private readonly Dictionary<PlayerFSM, System.Action<int, int>> m_handlers = new();
    private readonly HashSet<PlayerFSM> m_respawning = new();
}
