#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Photon.Pun;
using UnityEngine;

// 멀티 테스트용 인스턴스 정보 표시 (IMGUI, 화면 오른쪽 위)
// 여러 에디터 창을 띄웠을 때 어느 창이 어떤 프로필/플레이어인지 바로 알 수 있도록 함
// 에디터와 Development Build에서만 자동 생성되며, 접은 상태로 둘 수 있음
public class MultiplayerTestOverlay : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (s_instance != null) return;
        var go = new GameObject(nameof(MultiplayerTestOverlay));
        s_instance = go.AddComponent<MultiplayerTestOverlay>();
        DontDestroyOnLoad(go);
    }

    private void OnGUI()
    {
        GUI.depth = -1000;
        float scale = Screen.height / ReferenceHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float screenWidth = Screen.width / scale;

        if (!m_expanded)
        {
            if (GUI.Button(new Rect(screenWidth - 130f, 10f, 120f, 24f), $"[{MultiplayerTestProfile.Name}]")) m_expanded = true;
            return;
        }

        GUILayout.BeginArea(new Rect(screenWidth - PanelWidth - 10f, 10f, PanelWidth, ReferenceHeight - 20f));
        GUILayout.BeginVertical(GUI.skin.box);
        if (GUILayout.Button("접기")) m_expanded = false;

        GUILayout.Label($"프로필 : {MultiplayerTestProfile.Name}{(MultiplayerTestProfile.IsVirtualPlayer ? " (가상 플레이어)" : "")}");
        DrawProfile();
        DrawNetwork();
        DrawWorld();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawProfile()
    {
        if (GameManager.Instance == null) return;
        var profile = GameManager.Instance.LocalUserProfile;
        GUILayout.Label($"이름 : {profile.UserName}");
        GUILayout.Label($"UID : {Short(profile.UID)}");
        GUILayout.Label($"슬롯 : {GameManager.Instance.SelectedWorldSlot}");
    }
    private void DrawNetwork()
    {
        GUILayout.Space(4f);
        GUILayout.Label($"네트워크 : {NetworkManager.Instance.State}");
        if (!PhotonNetwork.InRoom) return;
        GUILayout.Label($"방 : {PhotonNetwork.CurrentRoom.Name} ({PhotonNetwork.CurrentRoom.PlayerCount}명)");
        GUILayout.Label($"Actor : {PhotonNetwork.LocalPlayer.ActorNumber}{(PhotonNetwork.IsMasterClient ? "  [방장]" : "")}");
        foreach (var p in PhotonNetwork.PlayerList)
        {
            string uid = p.CustomProperties[NetworkManager.UidProperty] as string;
            GUILayout.Label($"  {p.ActorNumber} {p.NickName} ({Short(uid)}){(p.IsMasterClient ? " *" : "")}");
        }
    }
    private void DrawWorld()
    {
        var world = WorldManager.Instance;
        if (world.CurWorldData == null) return;
        GUILayout.Space(4f);
        GUILayout.Label($"월드 : Day {world.CurWorldData.Day}  ({world.CurWorldData.Time:F0}s)  준비 {world.IsReady}");
        GUILayout.Label($"오브젝트 : {world.Objects.GetList().Count}  저장 슬롯 : {world.CurSlot}");

        PlayerFSM local = null;
        try { local = world.GetLocalPlayer(); } catch { } // 아직 내 플레이어가 생성되지 않음
        if (local == null || local.Inventory == null) return;
        int total = 0;
        foreach (var slot in local.Inventory.Slots) total += slot.Amount;
        GUILayout.Label($"내 인벤토리 : {local.Inventory.Slots.Count}종 / {total}개");
        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) return;
        if (GUILayout.Button("내 상태 방장에게 전송")) world.SendLocalPlayerState();
    }

    private static string Short(string uid) => string.IsNullOrEmpty(uid) ? "-" : uid.Length > 8 ? uid.Substring(0, 8) : uid;

    private static MultiplayerTestOverlay s_instance;
    private bool m_expanded = true;
    private const float ReferenceHeight = 720f;
    private const float PanelWidth = 280f;
}
#endif
