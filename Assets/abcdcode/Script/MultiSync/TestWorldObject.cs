using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestWorldObject : MonoBehaviour, IWorldObj
{
    public string PrefabId {get; private set;}

    public int ID {get;private set;}

    // 이 큐브를 조종하는 플레이어의 ActorNumber
    public int OwnerActor {get; private set;} = -1;
    public bool IsMine => PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == OwnerActor;

    public void InitWorldObj(int id, string prefabId)
    {
        ID = id;
        PrefabId = prefabId;
    }

    public void SetOwner(int actor)
    {
        OwnerActor = actor;
        if (TryGetComponent(out WorldTransformSync sync)) sync.IsOwner = () => IsMine;
        // 내 큐브는 초록, 남의 큐브는 빨강
        if (TryGetComponent(out Renderer r)) r.material.color = IsMine ? Color.green : Color.red;
    }

    // 클래스 안에서 SaveData가 메서드 이름으로 잡히므로 SaveData 타입의 정적 메서드 대신 인스턴스 메서드 사용
    public void LoadData(SaveData _data)
    {
        if (_data?.Data == null) return;
        SetOwner(_data.GetData<int>());
    }

    public void SaveData(SaveData _data)
    {
        _data.SetData(OwnerActor);
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
    }

    [SerializeField] private float m_moveSpeed = 3f;
}
