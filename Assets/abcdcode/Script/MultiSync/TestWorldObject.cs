using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestWorldObject : MonoBehaviour, IPlayerObj, IWorldRPC, IDamageable
{
    // ReceiveRPC가 하나뿐이므로 payload 맨 앞에 종류를 써서 구분
    private enum RPCType : byte
    {
        RequestDamage, // 아무나 -> 방장 : 데미지 요청
        SyncHP,        // 방장 -> 모두 : 확정된 HP
    }

    public string PrefabId {get;set;}

    public int ID {get;set;}

    public bool IsMine => PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == Owner;

    public int Owner { get; set; }

    public int HP => m_hp;
    public int MaxHP => m_maxHP;

    private void Awake()
    {
        m_hp = m_maxHP;
    }

    public void InitWorldObj(int id, string prefabId)
    {
        ID = id;
        PrefabId = prefabId;
        SetOwner(Owner);
    }

    public void SetOwner(int actor)
    {
        Owner = actor;
        if (TryGetComponent(out WorldTransformSync sync)) sync.IsOwner = () => IsMine;
        // 내 큐브는 초록, 남의 큐브는 빨강
        if (TryGetComponent(out Renderer r)) r.material.color = IsMine ? Color.green : Color.red;
    }

    #region 데미지
    // 어느 클라이언트에서 호출해도 HP 계산은 방장만 함 (동시에 맞아도 HP가 어긋나지 않게)
    public void TakeDamage(int damage, Vector3 sourcePosition)
    {
        PacketData d = new();
        d.Write((byte)RPCType.RequestDamage);
        d.Write(damage);
        this.SendRPC(d, WorldRPCTarget.MasterClient); // 방장이 호출하면 즉시 로컬 실행
    }
    private void ApplyDamage(int damage)
    {
        if (m_hp <= 0) return;
        SetHP(Mathf.Max(0, m_hp - damage));

        PacketData d = new();
        d.Write((byte)RPCType.SyncHP);
        d.Write(m_hp);
        this.SendRPC(d, WorldRPCTarget.Others); // 방장은 이미 적용했으므로 나머지에게만
    }
    private void SetHP(int hp)
    {
        if (m_hp == hp) return;
        m_hp = hp;
        Debug.Log($"[TestWorldObject] {ID} HP : {m_hp}/{m_maxHP}");
    }
    #endregion

    public void ReceiveRPC(PacketData data)
    {
        var type = (RPCType)data.ReadByte();
        switch (type)
        {
            case RPCType.RequestDamage:
                if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) ApplyDamage(data);
                break;
            case RPCType.SyncHP:
                SetHP(data);
                break;
        }
    }

    // 클래스 안에서 SaveData가 메서드 이름으로 잡히므로 SaveData 타입의 정적 메서드 대신 인스턴스 메서드 사용
    // HP는 Child에 넣어서 Owner만 저장하던 이전 데이터와 호환. 늦게 들어온 플레이어도 스냅샷으로 HP를 받음
    public void LoadData(SaveData _data)
    {
        if (_data?.Data == null) return;
        SetOwner(_data.GetData<int>());
        if (_data.Child != null && _data.Child.TryGetValue(HPKey, out var hp)) m_hp = hp.GetData<int>();
    }

    public void SaveData(SaveData _data)
    {
        _data.SetData(Owner);
        _data.Child[HPKey] = global::SaveData.CreateSaveData(m_hp);
    }

    private void Update()
    {
        if (!IsMine || Keyboard.current == null) return;
        var k = Keyboard.current;
        var input = new Vector3(
            (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0),
            0,
            (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
        transform.position += input * (m_moveSpeed * Time.deltaTime);

        // 테스트용 : 스페이스로 내 큐브에 데미지
        if (k.spaceKey.wasPressedThisFrame) TakeDamage(10, transform.position);
    }

    private const string HPKey = "HP";

    [SerializeField] private float m_moveSpeed = 3f;
    [SerializeField] private int m_maxHP = 100;
    private int m_hp;
}
