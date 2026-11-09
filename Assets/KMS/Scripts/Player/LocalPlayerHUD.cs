using UnityEngine;

// 이 플레이어가 로컬(내 소유) 플레이어면 PlayerHUD를 생성해 연결하고, 플레이어가 사라지면 HUD도 제거한다.
[RequireComponent(typeof(PlayerFSM))]
public class LocalPlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerHUDBinder hudPrefab;

    private PlayerHUDBinder spawnedHud;

    // Owner는 생성 직후에 지정되므로 Start에서 내 플레이어인지 판정한다
    private void Start()
    {
        var player = GetComponent<PlayerFSM>();
        if (!player.IsMine || hudPrefab == null) return;

        spawnedHud = Instantiate(hudPrefab);
        spawnedHud.Bind(player);
    }

    // 플레이어가 파괴될 때(방 나가기·스냅샷 재구성) HUD도 함께 제거
    private void OnDestroy()
    {
        if (spawnedHud != null) Destroy(spawnedHud.gameObject);
    }
}