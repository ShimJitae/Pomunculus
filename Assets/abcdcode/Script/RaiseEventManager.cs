using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// RaiseEvent로 주고받는 이벤트 코드. 0~199만 사용 가능 (200 이상은 PUN 내부 예약)
public enum RaiseEventCode : byte
{
    Normal =100,
    NNormal = 101
}

public class RaiseEventManager : Singleton<RaiseEventManager>, IOnEventCallback
{
    public void Init()
    {
        if (m_initialized) return;
        PhotonNetwork.AddCallbackTarget(this);
        m_raiseActionDic = new Dictionary<int, Action<EventData,PacketData>>();
        m_initialized = true;
    }
    public void Release()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        m_raiseActionDic.Clear();
        m_initialized = false;
    }
    public void RegisterEvent(int id, Action<EventData,PacketData> action)
    {
        Init();
        m_raiseActionDic[id] = action;
    }
    public void UnRegisterEvent(int id)
    {
        Init();
        m_raiseActionDic.Remove(id);
    }

    #region 송신
    public bool SendPacket(PacketData data)
    {
        return SendPacket(data,new(){Receivers = ReceiverGroup.All},SendOptions.SendReliable);
    }
    public bool SendPacket(PacketData data, RaiseEventOptions options,SendOptions sendOptions)
    {
        data.Save();
        var p = data.GetData();
        return PhotonNetwork.RaiseEvent((byte)RaiseEventCode.Normal,p,options,sendOptions);
    }
    #endregion

    #region 수신
    // PUN 내부 이벤트도 모두 이 콜백으로 들어오므로 코드로 걸러야 함
    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200) return;
        //패킷 이벤트
        if(photonEvent.Code == (byte)RaiseEventCode.Normal)
        {
            PacketData p = new PacketData((byte[])photonEvent.CustomData);
            int id = p;
            if(m_raiseActionDic.ContainsKey(id))
            {
                m_raiseActionDic[id](photonEvent,p);
            }
        }
    }
    #endregion
    private Dictionary<int,Action<EventData,PacketData>> m_raiseActionDic;

    private bool m_initialized;
}
public static class RaiseEventUtil
{
}