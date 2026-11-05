using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

// RaiseEvent 통신량 표시 창 (IMGUI). PhotonStatsGui처럼 씬의 아무 오브젝트에 붙여서 사용
// 컴포넌트가 켜져 있는 동안 집계하고, 토글 키로 창만 숨길 수 있음
public class NetStatsGui : MonoBehaviour
{
    private void OnEnable() => NetStats.Instance.AddUser();
    private void OnDisable() => NetStats.Instance.RemoveUser();
    private void Start()
    {
        if (m_rect.x <= 0) m_rect.x = Screen.width - m_rect.width;
    }
    private void Update()
    {
        NetStats.Instance.Tick();
        var k = Keyboard.current;
        if (k != null && k[m_toggleKey].wasPressedThisFrame) m_windowOn = !m_windowOn;
    }
    private void OnGUI()
    {
        if (!m_windowOn) return;
        m_rect = GUILayout.Window(m_windowId, m_rect, DrawWindow, $"RaiseEvent 통계 ({m_toggleKey})");
    }

    private void DrawWindow(int windowId)
    {
        var stats = NetStats.Instance;
        var p = stats.Photon;
        GUILayout.Label($"Photon 전체  Out {FormatBytes(p.OutBytes)}/s, {p.OutMessages} msg/s  |  In {FormatBytes(p.InBytes)}/s, {p.InMessages} msg/s");

        GUILayout.BeginHorizontal();
        m_showTotal = GUILayout.Toggle(m_showTotal, "누적 보기");
        if (GUILayout.Button("Reset")) stats.Reset();
        if (GUILayout.Button("To Log")) Debug.Log(BuildLog());
        GUILayout.EndHorizontal();

        Row("패킷", "송신", "송신량", "메시지", "수신", "수신량");
        long sc = 0, sb = 0, sm = 0, rc = 0, rb = 0;
        foreach (var e in stats.Entries.OrderByDescending(x => Send(x).Bytes + Receive(x).Bytes))
        {
            var s = Send(e);
            var r = Receive(e);
            Row(e.Name, s.Count.ToString(), FormatBytes(s.Bytes), s.Messages.ToString(), r.Count.ToString(), FormatBytes(r.Bytes));
            sc += s.Count; sb += s.Bytes; sm += s.Messages; rc += r.Count; rb += r.Bytes;
        }
        Row("합계", sc.ToString(), FormatBytes(sb), sm.ToString(), rc.ToString(), FormatBytes(rb));
        GUILayout.Label(m_showTotal ? "누적 값" : "초당 값 (직전 1초)");
        GUILayout.Label("메시지 = 송신 1 + 수신자 수 (포톤 요금 기준 추정). 바이트는 페이로드만");

        if (GUI.changed) m_rect.height = 100; // 행 수가 바뀌면 다음 프레임에 높이 재계산
        GUI.DragWindow();
    }
    private NetStats.Counter Send(NetStats.Entry e) => m_showTotal ? e.SendTotal : e.SendRate;
    private NetStats.Counter Receive(NetStats.Entry e) => m_showTotal ? e.ReceiveTotal : e.ReceiveRate;

    private static void Row(string name, string sendCount, string sendBytes, string messages, string receiveCount, string receiveBytes)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(name, GUILayout.Width(130f));
        GUILayout.Label(sendCount, GUILayout.Width(50f));
        GUILayout.Label(sendBytes, GUILayout.Width(70f));
        GUILayout.Label(messages, GUILayout.Width(55f));
        GUILayout.Label(receiveCount, GUILayout.Width(50f));
        GUILayout.Label(receiveBytes, GUILayout.Width(70f));
        GUILayout.EndHorizontal();
    }
    private static string FormatBytes(long bytes) => bytes < 1024 ? $"{bytes}B" : $"{bytes / 1024f:0.0}KB";

    private string BuildLog()
    {
        var stats = NetStats.Instance;
        var p = stats.Photon;
        var sb = new StringBuilder();
        sb.AppendLine($"[NetStats] Photon Out {p.OutBytes}B/s {p.OutMessages}msg/s | In {p.InBytes}B/s {p.InMessages}msg/s");
        foreach (var e in stats.Entries.OrderBy(x => x.Id))
        {
            sb.AppendLine($"{e.Name} : 초당 송신 {e.SendRate.Count}회 {e.SendRate.Bytes}B {e.SendRate.Messages}msg, 수신 {e.ReceiveRate.Count}회 {e.ReceiveRate.Bytes}B"
                + $" / 누적 송신 {e.SendTotal.Count}회 {e.SendTotal.Bytes}B {e.SendTotal.Messages}msg, 수신 {e.ReceiveTotal.Count}회 {e.ReceiveTotal.Bytes}B");
        }
        return sb.ToString();
    }

    [SerializeField] private bool m_windowOn = true;
    [SerializeField] private Key m_toggleKey = Key.F3;
    [SerializeField] private Rect m_rect = new(0, 100, 450, 100);
    [SerializeField] private int m_windowId = 101; // PhotonStatsGui(100)와 겹치지 않게
    private bool m_showTotal;
}
