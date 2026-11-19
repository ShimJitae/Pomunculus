using System;
using System.Collections.Generic;
using System.Text;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public enum ChatMessageType
{
    Player, // 플레이어가 보낸 채팅
    System, // 입장/퇴장 등 알림
}
// 채팅 한 줄. ReceivedTime(Time.unscaledTime)은 미리보기를 얼마나 보여줄지 계산할 때 사용
public readonly struct ChatMessage
{
    public ChatMessage(ChatMessageType type, string sender, string text, bool isMine)
    {
        Type = type;
        Sender = sender;
        Text = text;
        IsMine = isMine;
        ReceivedTime = Time.unscaledTime;
    }
    public ChatMessageType Type { get; }
    public string Sender { get; }
    public string Text { get; }
    public bool IsMine { get; }
    public float ReceivedTime { get; }
}

// 게임 중 채팅. RaiseEventManager로 메시지를 주고받고, 플레이어 입장/퇴장은 시스템 메시지로 알림
// 표시는 ChatUI가 OnMessageAddedEvent를 구독해서 처리
// 게임 시작 시 자동으로 생성되어 씬이 바뀌어도 유지됨 (씬에 직접 배치해도 됨. 중복은 파괴됨)
// 채팅은 Photon 방 안에 있을 때만 보낼 수 있고(CanChat), '/' 명령어는 방 밖에서도 실행 가능
public class ChatManager : SingletonBehavior<ChatManager>
{
    // 시작 씬에 ChatManager가 없으면 만들어 둠
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateOnStart()
    {
        if (FindAnyObjectByType<ChatManager>() != null) return;
        new GameObject(nameof(ChatManager)).AddComponent<ChatManager>();
    }

    public override void Awake()
    {
        base.Awake();
        if (Instance != this) return; // 중복 ChatManager는 파괴되므로 구독하지 않음
        DontDestroyOnLoad(gameObject);
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.Chat, OnChatReceived);
        var network = NetworkManager.Instance;
        network.OnJoinedRoomEvent += OnJoinedRoom;
        network.OnPlayerEnteredRoomEvent += OnPlayerEnteredRoom;
        network.OnPlayerLeftRoomEvent += OnPlayerLeftRoom;
        m_registered = true;
    }
    // NetworkManager와 RaiseEventManager는 씬 이동 후에도 남아 있으므로 반드시 해제
    private void OnDestroy()
    {
        if (!m_registered) return;
        RaiseEventManager.Instance.UnRegisterEvent((int)WorldEventID.Chat);
        var network = NetworkManager.Instance;
        network.OnJoinedRoomEvent -= OnJoinedRoom;
        network.OnPlayerEnteredRoomEvent -= OnPlayerEnteredRoom;
        network.OnPlayerLeftRoomEvent -= OnPlayerLeftRoom;
        m_registered = false;
    }

    #region 송신
    // 방 안이면 다른 플레이어에게 전송하고, 내 화면에는 바로 추가. 방 밖(오프라인)이면 내 화면에만 추가
    // '/'로 시작하면 채팅을 보내지 않고 명령어로 실행 (ExecuteCommand)
    // 명령어는 어디서나 실행되지만, 채팅은 방 안에서만 보낼 수 있음 (CanChat)
    public bool Send(string text)
    {
        text = Sanitize(text);
        if (text.Length == 0) return false;
        if (text[0] == CommandPrefix) return ExecuteCommand(text);
        if (!CanChat)
        {
            Notify("방에 있을 때만 채팅을 보낼 수 있습니다.");
            return false;
        }
        if (PhotonNetwork.InRoom)
        {
            PacketData p = new();
            p.Write((int)WorldEventID.Chat);
            p.Write(text);
            if (!RaiseEventManager.Instance.SendPacket(p, new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable))
            {
                Notify("메시지를 보내지 못했습니다.");
                return false;
            }
        }
        AddMessage(new ChatMessage(ChatMessageType.Player, LocalName, text, true));
        return true;
    }
    // 시스템 알림. 내 화면에만 표시됨
    public void Notify(string text)
    {
        AddMessage(new ChatMessage(ChatMessageType.System, null, text, false));
    }
    #endregion

    #region 명령어
    // "/give potion 3" -> OnCommandEvent("give", ["potion", "3"])
    // 명령어 이름은 소문자로 바꿔서 전달. 인자는 공백으로 나누며 대소문자를 그대로 유지
    // 다른 플레이어에게는 전송되지 않고 내 클라이언트에서만 실행됨
    public bool ExecuteCommand(string text)
    {
        var tokens = text.Substring(1).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return false;
        string command = tokens[0].ToLowerInvariant();
        string[] args = tokens.Length > 1 ? tokens[1..] : Array.Empty<string>();

        Notify(text); // 입력한 명령어를 내 채팅창에 남김
        if (OnCommandEvent == null)
        {
            Notify($"명령어를 처리할 곳이 없습니다 : {command}");
            return true;
        }
        // 구독자 하나에서 예외가 나도 나머지 구독자는 실행되도록 하나씩 호출
        foreach (Action<string, string[]> handler in OnCommandEvent.GetInvocationList())
        {
            try
            {
                handler(command, args);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Notify($"명령어 실행 중 오류 : {command}");
            }
        }
        return true;
    }
    #endregion

    #region 수신
    private void OnChatReceived(EventData e, PacketData p)
    {
        string text = Sanitize(p);
        if (text.Length == 0) return;
        // 이름은 패킷에 싣지 않고 보낸 사람의 ActorNumber로 찾음 (다른 이름을 사칭할 수 없도록)
        var sender = PhotonNetwork.CurrentRoom?.GetPlayer(e.Sender);
        AddMessage(new ChatMessage(ChatMessageType.Player, sender?.NickName ?? $"#{e.Sender}", text, false));
    }
    // 새 방에 들어가면 이전 방의 기록을 지움
    private void OnJoinedRoom()
    {
        m_history.Clear();
        OnHistoryClearedEvent?.Invoke();
    }
    private void OnPlayerEnteredRoom(Player player) => Notify($"{player.NickName}님이 입장했습니다.");
    private void OnPlayerLeftRoom(Player player) => Notify($"{player.NickName}님이 퇴장했습니다.");
    #endregion

    private void AddMessage(ChatMessage message)
    {
        m_history.Add(message);
        if (m_history.Count > MaxHistory) m_history.RemoveAt(0);
        OnMessageAddedEvent?.Invoke(message);
    }
    // 앞뒤 공백과 줄바꿈 등 제어 문자를 제거하고 최대 길이로 자름 (받은 메시지도 똑같이 처리)
    private static string Sanitize(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var sb = new StringBuilder(Mathf.Min(text.Length, MaxLength));
        foreach (char c in text.Trim())
        {
            if (char.IsControl(c)) continue;
            if (sb.Length >= MaxLength) break;
            sb.Append(c);
        }
        return sb.ToString().Trim();
    }
    private static string LocalName => string.IsNullOrEmpty(PhotonNetwork.NickName) ? GameManager.Instance.LocalUserProfile.UserName : PhotonNetwork.NickName;

    // 오래된 메시지부터 최대 MaxHistory개
    public IReadOnlyList<ChatMessage> History => m_history;
    public event Action<ChatMessage> OnMessageAddedEvent;
    public event Action OnHistoryClearedEvent;
    // 채팅을 보낼 수 있는지. 메시지는 RaiseEvent로 방 안의 플레이어에게만 전달되므로 Photon 방 안에 있을 때만 가능 (명령어는 제외)
    public static bool CanChat => PhotonNetwork.InRoom;
    // 명령어 입력 시 (명령어 이름, 인자). 모든 구독자가 모든 명령어를 받으므로, 자기 명령어인지 이름으로 확인해서 처리
    // 예) ChatManager.Instance.OnCommandEvent += (cmd, args) => { if (cmd == "heal") ... };
    public event Action<string, string[]> OnCommandEvent;

    public const char CommandPrefix = '/';

    public const int MaxLength = 100;
    public const int MaxHistory = 50;
    private readonly List<ChatMessage> m_history = new();
    private bool m_registered;
}
