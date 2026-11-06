using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// PhotonView.RPC를 따라한 ID 기반 RPC. PhotonView의 ViewID 대신 IWorldObj.ID로 대상을 찾는다
// 송신 측은 this.SendRPC(data)만 호출하면 되고, 모든 클라이언트에서 같은 ID를 가진 오브젝트의 ReceiveRPC가 호출됨
// ReceiveRPC로 넘어오는 PacketData는 헤더(이벤트 id, 오브젝트 ID)를 읽은 뒤라 송신 측이 쓴 순서 그대로 읽으면 됨
public interface IWorldRPC : IWorldObj
{
    public void ReceiveRPC(PacketData data);
}

// PUN의 RpcTarget 중 버퍼(Buffered) 계열을 제외한 것
public enum WorldRPCTarget
{
    All,          // 로컬은 즉시 실행 + 다른 플레이어에게 전송
    Others,       // 나를 제외한 플레이어
    MasterClient, // 방장만 (방장이 보내면 즉시 로컬 실행)
    AllViaServer, // 나를 포함해서 서버를 거쳐 전송. 모든 클라이언트가 같은 순서로 받음
}

public static class RPCHelper
{
    #region 송신
    public static void SendRPC(this IWorldRPC iwo, PacketData data, WorldRPCTarget target = WorldRPCTarget.All, bool reliable = true)
    {
        data.Save();
        // 오프라인이면 본인이 방장이므로 Others 외에는 로컬 실행만
        if (!PhotonNetwork.InRoom)
        {
            if (target != WorldRPCTarget.Others) iwo.ReceiveRPC(new PacketData(data.GetData()));
            return;
        }
        switch (target)
        {
            case WorldRPCTarget.All:
                iwo.ReceiveRPC(new PacketData(data.GetData()));
                Send(iwo, data, new() { Receivers = ReceiverGroup.Others }, reliable);
                break;
            case WorldRPCTarget.Others:
                Send(iwo, data, new() { Receivers = ReceiverGroup.Others }, reliable);
                break;
            case WorldRPCTarget.MasterClient:
                if (PhotonNetwork.IsMasterClient) iwo.ReceiveRPC(new PacketData(data.GetData()));
                else Send(iwo, data, new() { Receivers = ReceiverGroup.MasterClient }, reliable);
                break;
            case WorldRPCTarget.AllViaServer:
                Send(iwo, data, new() { Receivers = ReceiverGroup.All }, reliable);
                break;
        }
    }
    // PhotonView.RPC(name, Player, ...)와 같이 특정 플레이어에게만 전송
    public static void SendRPC(this IWorldRPC iwo, PacketData data, int targetActor, bool reliable = true)
    {
        data.Save();
        if (!PhotonNetwork.InRoom || targetActor == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            iwo.ReceiveRPC(new PacketData(data.GetData()));
            return;
        }
        Send(iwo, data, new() { TargetActors = new[] { targetActor } }, reliable);
    }
    private static void Send(IWorldRPC iwo, PacketData data, RaiseEventOptions options, bool reliable)
    {
        PacketData p = new();
        p.Write((int)WorldEventID.WorldRPC);
        p.Write(iwo.ID);
        var payload = data.GetData();
        if (payload != null) p.Write(payload);
        RaiseEventManager.Instance.SendPacket(p, options, reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable);
    }
    #endregion

    #region 수신
    // WorldManager.Init에서 등록
    public static void OnReceive(EventData e, PacketData p)
    {
        int id = p;
        // 스냅샷 적용 전이면 대상 오브젝트가 아직 없으므로 적용 후 실행
        WorldManager.Instance.RunOrQueue(() =>
        {
            var obj = WorldManager.Instance.Find(id);
            if (obj is IWorldRPC rpc) rpc.ReceiveRPC(p);
            else if (obj == null) Debug.LogWarning($"[RPCHelper] 대상 오브젝트 없음 : {id}");
            else Debug.LogWarning($"[RPCHelper] IWorldRPC가 아닌 오브젝트 : {id} ({obj.PrefabId})");
        });
    }
    #endregion
}
