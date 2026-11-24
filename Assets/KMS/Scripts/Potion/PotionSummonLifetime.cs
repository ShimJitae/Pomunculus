// 포션으로 소환한 월드 오브젝트를 수명이 끝나거나 소환한 플레이어가 나가면 방장이 제거한다
using Photon.Pun;
using UnityEngine;

public class PotionSummonLifetime : MonoBehaviour
{
    private float despawnTime;
    private bool hasLifetime;
    private GameObject summoner;
    private bool hasSummoner;
    private bool isDespawning;

    // seconds가 0보다 크면 그 시간 뒤 제거, summoner가 있으면 그 플레이어가 사라질 때 제거하도록 예약한다
    public void Initialize(float seconds, GameObject summoner)
    {
        hasLifetime = seconds > 0f;
        despawnTime = Time.time + seconds;
        this.summoner = summoner;
        hasSummoner = summoner != null;
    }

    void Update()
    {
        if (isDespawning) return;

        bool isExpired = hasLifetime && Time.time >= despawnTime;
        bool hasSummonerLeft = hasSummoner && summoner == null;   // 나간 플레이어의 오브젝트는 파괴되어 null이 된다
        if (!isExpired && !hasSummonerLeft) return;

        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;   // 제거는 방장만 전파할 수 있다
        isDespawning = true;
        if (TryGetComponent(out IWorldObj worldObject)) worldObject.Despawn();
    }
}
