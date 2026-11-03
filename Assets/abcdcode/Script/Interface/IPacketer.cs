using System;
using System.Collections.Generic;
using Photon.Pun;

public interface IPacketer
{
    public void LoadPacket(byte[] data);
    public static void SendPacket(PhotonView view, PacketData data, RpcTarget target = RpcTarget.AllViaServer)
    {
        data.Save();
        if(data.IsEmpty()) return; // 보낼 패킷이 0이면 실행안함
        view.RPC("LoadPacket",target,data.GetData());
    }
    public Dictionary<string,Action<PacketData>> RPCEvent{get;set;}
}