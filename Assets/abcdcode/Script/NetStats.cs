using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// RaiseEventManager를 거치는 패킷을 WorldEventID별로 집계
// 누적 값은 Reset 전까지 유지하고, 초당 값은 1초 단위로 갱신 (Tick)
// 바이트는 우리 페이로드 크기만 (포톤 헤더/직렬화/ACK 제외). 실제 송수신량은 Photon 항목(TrafficStats)으로 확인
public class NetStats : Singleton<NetStats>
{
    public struct Counter
    {
        public long Count;
        public long Bytes;
        public long Messages; // 포톤 요금 기준 메시지 수 추정 (송신 1 + 수신자 수)

        public void Add(int bytes, int messages)
        {
            Count++;
            Bytes += bytes;
            Messages += messages;
        }
        public Counter PerSecond(float elapsed) => new()
        {
            Count = (long)Math.Round(Count / elapsed),
            Bytes = (long)Math.Round(Bytes / elapsed),
            Messages = (long)Math.Round(Messages / elapsed)
        };
    }
    public class Entry
    {
        public int Id;
        public string Name => Enum.IsDefined(typeof(WorldEventID), Id) ? ((WorldEventID)Id).ToString() : Id.ToString();
        public Counter SendTotal, ReceiveTotal; // 누적
        public Counter SendRate, ReceiveRate;   // 직전 1초 기준 초당 값
        internal Counter SendWindow, ReceiveWindow;
    }
    // Photon TrafficStats 기준 연결 전체(PUN 내부 통신 포함)의 초당 값
    public struct PhotonRate
    {
        public long OutBytes, InBytes, OutMessages, InMessages;
    }

    // 사용하는 쪽(NetStatsGui 등)이 하나라도 있으면 집계. 처음 켜질 때 Photon TrafficStats도 같이 켬
    // bool 하나로 켜고 끄면 씬 전환 때 파괴되는 중복 GameManager의 NetStatsGui가 꺼버리므로 개수로 관리
    public bool Enabled => m_users > 0;
    public void AddUser()
    {
        if (m_users++ > 0) return;
        var peer = PhotonNetwork.NetworkingClient?.LoadBalancingPeer;
        if (peer != null) peer.TrafficStatsEnabled = true;
        // 꺼져 있던 동안의 시간/통신량이 첫 초당 값에 섞이지 않게 기준점을 새로 잡음
        m_windowStart = Time.unscaledTime;
        m_lastPhoton = ReadPhoton() ?? default;
    }
    public void RemoveUser()
    {
        if (m_users > 0) m_users--;
    }
    public IReadOnlyCollection<Entry> Entries => m_entries.Values;
    public PhotonRate Photon { get; private set; }

    #region 기록 (RaiseEventManager에서 호출)
    public void RecordSend(byte[] packet, RaiseEventOptions options)
    {
        if (!Enabled || packet == null || packet.Length < sizeof(int)) return;
        var e = GetEntry(BitConverter.ToInt32(packet, 0));
        int messages = 1 + CountReceivers(options);
        e.SendTotal.Add(packet.Length, messages);
        e.SendWindow.Add(packet.Length, messages);
        Tick();
    }
    public void RecordReceive(byte[] packet)
    {
        if (!Enabled || packet == null || packet.Length < sizeof(int)) return;
        var e = GetEntry(BitConverter.ToInt32(packet, 0));
        e.ReceiveTotal.Add(packet.Length, 1);
        e.ReceiveWindow.Add(packet.Length, 1);
        Tick();
    }
    // InterestGroup, 캐싱은 고려하지 않음
    private static int CountReceivers(RaiseEventOptions options)
    {
        if (options?.TargetActors != null) return options.TargetActors.Length;
        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return 0;
        return (options?.Receivers ?? ReceiverGroup.Others) switch
        {
            ReceiverGroup.All => room.PlayerCount,
            ReceiverGroup.MasterClient => 1,
            _ => room.PlayerCount - 1
        };
    }
    #endregion

    // 1초가 지났으면 초당 값 갱신. 기록할 때와 NetStatsGui.Update에서 호출
    public void Tick()
    {
        float now = Time.unscaledTime;
        float elapsed = now - m_windowStart;
        if (elapsed < 1f) return;
        foreach (var e in m_entries.Values)
        {
            e.SendRate = e.SendWindow.PerSecond(elapsed);
            e.ReceiveRate = e.ReceiveWindow.PerSecond(elapsed);
            e.SendWindow = default;
            e.ReceiveWindow = default;
        }
        SamplePhoton(elapsed);
        m_windowStart = now;
    }
    public void Reset()
    {
        m_entries.Clear();
        Photon = default;
        m_lastPhoton = default;
        m_windowStart = Time.unscaledTime;
        PhotonNetwork.NetworkingClient?.LoadBalancingPeer?.TrafficStatsReset();
    }

    private Entry GetEntry(int id)
    {
        if (!m_entries.TryGetValue(id, out var e)) m_entries[id] = e = new Entry { Id = id };
        return e;
    }
    // TrafficStats 누적 값. 꺼져 있으면 null
    private static PhotonRate? ReadPhoton()
    {
        var peer = PhotonNetwork.NetworkingClient?.LoadBalancingPeer;
        if (peer == null || !peer.TrafficStatsEnabled) return null;
        return new PhotonRate
        {
            OutBytes = peer.TrafficStatsOutgoing.TotalPacketBytes,
            InBytes = peer.TrafficStatsIncoming.TotalPacketBytes,
            OutMessages = peer.TrafficStatsGameLevel.TotalOutgoingMessageCount,
            InMessages = peer.TrafficStatsGameLevel.TotalIncomingMessageCount
        };
    }
    private void SamplePhoton(float elapsed)
    {
        if (ReadPhoton() is not PhotonRate cur) return;
        // 다른 곳(PhotonStatsGui 등)에서 리셋하면 음수가 나오므로 0으로
        Photon = new PhotonRate
        {
            OutBytes = (long)Math.Max(0, Math.Round((cur.OutBytes - m_lastPhoton.OutBytes) / elapsed)),
            InBytes = (long)Math.Max(0, Math.Round((cur.InBytes - m_lastPhoton.InBytes) / elapsed)),
            OutMessages = (long)Math.Max(0, Math.Round((cur.OutMessages - m_lastPhoton.OutMessages) / elapsed)),
            InMessages = (long)Math.Max(0, Math.Round((cur.InMessages - m_lastPhoton.InMessages) / elapsed))
        };
        m_lastPhoton = cur;
    }

    private readonly Dictionary<int, Entry> m_entries = new();
    private PhotonRate m_lastPhoton;
    private float m_windowStart;
    private int m_users;
}
