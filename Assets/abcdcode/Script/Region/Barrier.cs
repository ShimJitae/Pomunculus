using System;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Pomunculus.Items;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 최종보스를 막고 있는 결계. RegionSpawner가 결계 지역의 보스 포인트에 한 번 소환하고, 이후로는 디스폰하지 않는다
// 근처에서 E키로 상호작용한 플레이어가 지역 보스 드랍 아이템을 모두 가지고 있으면 1개씩 소모하고,
// 결계를 지운 뒤 그 자리에 최종보스를 소환한다 (RegionSpawner.SummonFinalBoss)
// 판정은 방장이 방장 쪽 인벤토리 사본(소유자가 SyncInventory로 맞춤)으로 하고, 아이템 소모는 인벤토리 소유자 클라이언트에서 한다
public class Barrier : MonoBehaviour, IWorldRPC
{
    [Serializable]
    private class RequiredItem
    {
        public int ItemId;
        public string FallbackName; // MaterialDatabase에서 이름을 찾지 못할 때 표시
    }

    // ReceiveRPC가 하나뿐이므로 payload 맨 앞에 종류를 써서 구분
    private enum RPCType : byte
    {
        RequestBreak, // 요청자 -> 방장 : 해제 요청 (요청자 ActorNumber)
        ConsumeItems, // 방장 -> 요청자 : 해제 확정. 요청자가 아이템 소모
        Rejected,     // 방장 -> 요청자 : 해제 실패
    }

    public string PrefabId { get; set; }
    public int ID { get; set; }

    public void InitWorldObj(int id, string prefabId)
    {
        ID = id;
        PrefabId = prefabId;
    }

    // 결계는 따로 보관할 상태가 없다. 진행 단계는 WorldData.FinalBoss에 저장됨
    public void SaveData(SaveData _data) { }
    public void LoadData(SaveData _data) { }

    private void Awake()
    {
        SetTextVisible(false);
        ApplyColor();
    }

    #region 상호작용
    private void OnTriggerEnter(Collider other)
    {
        if (!IsLocalPlayer(other)) return;
        m_nearby.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        m_nearby.Remove(other);
    }

    private void OnDisable()
    {
        m_nearby.Clear();
        SetTextVisible(false);
    }

    private void Update()
    {
        if (m_waitTimer > 0f) m_waitTimer -= Time.deltaTime;
        if (m_messageTimer > 0f) m_messageTimer -= Time.deltaTime;

        // 플레이어가 죽거나 비활성화되면 Exit이 오지 않으므로 직접 정리
        m_nearby.RemoveWhere(s_invalidCollider);
        if (m_nearby.Count == 0)
        {
            SetTextVisible(false);
            return;
        }

        var player = GetLocalPlayer();
        bool ready = player != null && HasRequiredItems(player.Inventory);
        if (ready && m_waitTimer <= 0f && Keyboard.current?.eKey.wasPressedThisFrame == true)
            RequestBreak();

        SetTextVisible(true);
        if (m_interactionText != null) m_interactionText.text = BuildText(player, ready);
    }

    // 상호작용 텍스트가 항상 카메라를 보도록
    private void LateUpdate()
    {
        if (m_textRoot == null || !m_textRoot.gameObject.activeSelf) return;
        var cam = Camera.main;
        if (cam != null) m_textRoot.rotation = cam.transform.rotation;
    }

    private string BuildText(PlayerFSM player, bool ready)
    {
        if (m_waitTimer > 0f) return "결계를 해제하는 중...";
        if (m_messageTimer > 0f) return m_message;
        if (ready) return "[E] 결계 해제";

        var sb = new StringBuilder("결계를 해제하려면 다음 아이템이 필요합니다");
        foreach (var item in m_requiredItems)
        {
            int has = player != null && player.Inventory != null ? player.Inventory.GetItemCount(item.ItemId) : 0;
            int need = NeededCount(item.ItemId);
            sb.Append('\n').Append(GetItemName(item)).Append(' ').Append(Mathf.Min(has, need)).Append('/').Append(need);
        }
        return sb.ToString();
    }

    private void SetTextVisible(bool visible)
    {
        if (m_textRoot != null && m_textRoot.gameObject.activeSelf != visible) m_textRoot.gameObject.SetActive(visible);
    }
    #endregion

    #region 해제 요청/처리
    private void RequestBreak()
    {
        m_waitTimer = RequestTimeout; // 응답이 올 때까지 연타로 여러 번 요청하지 않도록
        PacketData d = new();
        d.Write((byte)RPCType.RequestBreak);
        d.Write(LocalActor);
        this.SendRPC(d, WorldRPCTarget.MasterClient); // 방장이 호출하면 즉시 로컬 실행
    }

    // 방장 : 요청자의 인벤토리 사본으로 다시 확인하고, 최종보스를 소환한 뒤 요청자에게 소모를 지시하고 결계를 지운다
    // 소모 지시를 디스폰보다 먼저 보내야 요청자 쪽에서 결계가 남아 있는 상태로 RPC를 받는다 (같은 방장이 보낸 reliable 이벤트는 순서 보장)
    private void HandleBreakRequest(int actor)
    {
        if (!IsMaster) return;
        var player = FindPlayer(actor);
        if (player == null || !HasRequiredItems(player.Inventory))
        {
            SendRejected(actor, "필요한 아이템이 부족합니다");
            return;
        }
        if (!RegionSpawner.Instance.SummonFinalBoss(this))
        {
            SendRejected(actor, "결계를 해제할 수 없습니다");
            return;
        }

        PacketData d = new();
        d.Write((byte)RPCType.ConsumeItems);
        this.SendRPC(d, actor);
        this.Despawn();
    }

    private void SendRejected(int actor, string message)
    {
        PacketData d = new();
        d.Write((byte)RPCType.Rejected);
        d.Write(message);
        this.SendRPC(d, actor);
    }

    // 요청자 : 인벤토리 소유자 쪽에서 소모해야 SyncInventory로 방장/저장 데이터까지 맞춰진다
    private void ConsumeItems()
    {
        var player = GetLocalPlayer();
        if (player == null || player.Inventory == null) return;
        foreach (var item in m_requiredItems)
        {
            if (!player.Inventory.RemoveItem(item.ItemId, 1))
                Debug.LogWarning($"[Barrier] 아이템 소모 실패 : {item.ItemId}");
        }
    }

    public void ReceiveRPC(PacketData data)
    {
        var type = (RPCType)data.ReadByte();
        switch (type)
        {
            case RPCType.RequestBreak:
                HandleBreakRequest(data);
                break;
            case RPCType.ConsumeItems:
                m_waitTimer = 0f;
                ConsumeItems();
                break;
            case RPCType.Rejected:
                m_waitTimer = 0f;
                m_message = data;
                m_messageTimer = MessageDuration;
                break;
        }
    }
    #endregion

    #region 아이템
    // 해제에 필요한 아이템 ID. 같은 아이템이 여러 번 있으면 그만큼 필요
    public IEnumerable<int> RequiredItemIds
    {
        get
        {
            foreach (var item in m_requiredItems) yield return item.ItemId;
        }
    }

    private bool HasRequiredItems(Inventory inventory)
    {
        if (inventory == null) return false;
        foreach (var item in m_requiredItems)
        {
            if (inventory.GetItemCount(item.ItemId) < NeededCount(item.ItemId)) return false;
        }
        return true;
    }

    // 같은 아이템이 목록에 여러 번 있으면 그만큼 필요
    private int NeededCount(int itemId)
    {
        int count = 0;
        foreach (var item in m_requiredItems)
        {
            if (item.ItemId == itemId) count++;
        }
        return count;
    }

    private static string GetItemName(RequiredItem item)
    {
        var data = MaterialDatabase.Instance != null ? MaterialDatabase.Instance.Find(item.ItemId) : null;
        if (data != null && !string.IsNullOrEmpty(data.ItemName)) return data.ItemName;
        return string.IsNullOrEmpty(item.FallbackName) ? item.ItemId.ToString() : item.FallbackName;
    }
    #endregion

    #region 플레이어
    private static PlayerFSM GetLocalPlayer()
    {
        foreach (var obj in WorldManager.Instance.Objects.GetList())
        {
            if (obj is PlayerFSM p && p != null && p.IsMine) return p;
        }
        return null;
    }

    // 방장 쪽에서 요청자의 플레이어 사본 찾기. 오프라인이면 본인
    private static PlayerFSM FindPlayer(int actor)
    {
        if (!PhotonNetwork.InRoom) return GetLocalPlayer();
        foreach (var obj in WorldManager.Instance.Objects.GetList())
        {
            if (obj is PlayerFSM p && p != null && p.Owner == actor) return p;
        }
        return null;
    }

    private static bool IsLocalPlayer(Collider other)
    {
        PlayerFSM player = other.GetComponentInParent<PlayerFSM>();
        if (player != null) return player.IsMine;
        return other.CompareTag("Player") ||
               (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"));
    }

    private static int LocalActor => PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
    private static bool IsMaster => !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient; // 오프라인이면 본인이 방장
    #endregion

    // 임시 외형 : 공용 Lit 재질을 쓰므로 색만 바꿔서 결계처럼 보이게
    private void ApplyColor()
    {
        var block = new MaterialPropertyBlock();
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, m_color);
            block.SetColor(ColorId, m_color);
            r.SetPropertyBlock(block);
        }
    }

    private static readonly Predicate<Collider> s_invalidCollider =
        c => c == null || !c.enabled || !c.gameObject.activeInHierarchy;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private const float RequestTimeout = 3f;   // 방장 응답을 기다리는 최대 시간
    private const float MessageDuration = 2f;  // 실패 메시지 표시 시간

    [Header("해제에 필요한 아이템 (각 1개씩 소모)")]
    [SerializeField] private List<RequiredItem> m_requiredItems = new()
    {
        new RequiredItem { ItemId = 25041, FallbackName = "용의 비늘" }, // 레드 드래곤 (화산)
        new RequiredItem { ItemId = 25034, FallbackName = "만년설" },   // 서리 거인 (설산)
        new RequiredItem { ItemId = 25038, FallbackName = "합성표본" }, // 키메라 (독늪)
    };

    [Header("E키 안내")]
    [SerializeField] private Transform m_textRoot;       // 카메라를 바라보게 돌릴 월드 캔버스
    [SerializeField] private TMP_Text m_interactionText;

    [Header("외형")]
    [SerializeField] private Color m_color = new(0.6f, 0.3f, 1f, 1f);

    private readonly HashSet<Collider> m_nearby = new();
    private float m_waitTimer;
    private float m_messageTimer;
    private string m_message;
}
