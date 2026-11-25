using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

// 채팅 화면. ChatManager의 메시지를 표시하고 입력을 받음
// 닫힌 상태 : 새 메시지가 오면 m_previewDuration 동안 미리보기에 표시하고, 끝날 때 서서히 사라짐
// ` 키 : 채팅창(전체 기록 + 입력창)을 열고 닫음. 방 밖에서도 열리며, 이때는 '/' 명령어만 실행되고 채팅은 보내지지 않음. Enter로 전송해도 창은 유지되고(빈 입력의 Enter도 닫지 않음), ` 키, Esc, 다른 곳 클릭으로 닫힘
// 채팅창이 열려 있는 동안 IsTyping이 true. 키보드로 조작하는 스크립트는 이 값을 보고 입력을 무시해야 함
// 채팅창이 열려 있는 동안 Input System의 키보드 입력을 막음 (Keyboard.current, InputAction 모두). 다른 스크립트가 IsTyping을 확인하지 않아도
// 채팅 입력 중에 인벤토리/제작창 단축키, 이동 등이 동작하지 않음. 채팅 입력 자체는 TMP가 IMGUI 이벤트로 받으므로 영향 없음
// 씬 배치는 메뉴 Tools/Chat/Build Chat UI 사용
public class ChatUI : MonoBehaviour
{
    // 채팅을 입력 중인지. 닫은 프레임에도 true라서, 같은 프레임에 Esc/Enter를 다른 스크립트가 다시 처리하지 않음
    public static bool IsTyping => s_isOpen || s_closedFrame == Time.frameCount;
    private static ChatUI _instance;
    #region Unity
    private void Awake()
    {
        if(_instance != null) Destroy(this.gameObject);
        _instance = this;
        m_input.lineType = TMP_InputField.LineType.SingleLine;
        m_input.characterLimit = ChatManager.MaxLength;
        m_input.onValidateInput = ValidateChar;
        m_input.onSubmit.AddListener(OnSubmit);
        m_input.onEndEdit.AddListener(OnEndEdit);
        SetWindowVisible(false);
        m_previewText.text = "";
        DontDestroyOnLoad(this.gameObject);
    }
    private void Start()
    {
        m_chat = ChatManager.Instance;
        if (m_chat == null)
        {
            Debug.LogWarning("[ChatUI] 씬에 ChatManager가 없어 채팅을 사용할 수 없음");
            enabled = false;
            return;
        }
        m_chat.OnMessageAddedEvent += OnMessageAdded;
        m_chat.OnHistoryClearedEvent += RebuildLog;
        RebuildLog();
    }
    private void OnDestroy()
    {
        if (m_chat != null)
        {
            m_chat.OnMessageAddedEvent -= OnMessageAdded;
            m_chat.OnHistoryClearedEvent -= RebuildLog;
        }
        // 채팅창을 연 채로 씬이 바뀌어도 입력이 막힌 상태로 남지 않도록
        if (m_isOpen) s_isOpen = false;
    }
    private void OnEnable()
    {
        m_onInputEvent ??= OnInputEvent;
        InputSystem.onEvent += m_onInputEvent;
    }
    private void OnDisable()
    {
        InputSystem.onEvent -= m_onInputEvent;
        m_blockKeyboard = false;
    }
    private void Update()
    {
        // 열려 있을 때는 키보드 입력이 막혀 있으므로 ` 키는 OnInputEvent에서 감지함
        if (m_closeRequested)
        {
            m_closeRequested = false;
            Close();
        }
        if (m_blockKeyboard && !m_isOpen && Time.frameCount > m_unblockAfterFrame && !IsAnyKeyHeld()) m_blockKeyboard = false;
        var keyboard = Keyboard.current;
        if (!m_isOpen && !m_blockKeyboard && keyboard != null && keyboard.backquoteKey.wasPressedThisFrame && !IsOtherInputFieldFocused()) Open();
        if (m_reactivateInput && m_isOpen) ReactivateInput();
        UpdatePreview();
    }
    #endregion

    #region 키보드 입력 차단
    // 채팅창이 열려 있는 동안 키보드 상태 이벤트를 handled로 표시해서 키보드 장치에 반영되지 않게 함
    // 막는 동안 실제로 눌린 키는 m_heldKeys에 따로 기록해서, 닫을 때 누르고 있던 키(Esc, ` 등)를 뗀 뒤에 차단을 풂
    // (누른 채로 풀면 다른 스크립트가 그 키를 이번에 새로 눌린 것으로 처리함)
    private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (!m_blockKeyboard || device is not Keyboard keyboard) return;
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()) return;
        eventPtr.handled = true;
        var keys = keyboard.allKeys;
        if (m_heldKeys == null || m_heldKeys.Length != keys.Count) m_heldKeys = new bool[keys.Count];
        for (int i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            if (key == null || !key.ReadValueFromEvent(eventPtr, out float value)) continue;
            bool held = value >= key.pressPointOrDefault;
            if (held && !m_heldKeys[i] && m_isOpen && key == keyboard.backquoteKey) m_closeRequested = true;
            m_heldKeys[i] = held;
        }
    }
    private void StartBlockKeyboard()
    {
        m_blockKeyboard = true;
        m_closeRequested = false;
        if (m_heldKeys != null) System.Array.Clear(m_heldKeys, 0, m_heldKeys.Length);
        // 열 때 누르고 있던 키(이동키 등)가 눌린 상태로 남지 않도록 키보드 상태를 초기화
        foreach (var device in InputSystem.devices)
            if (device is Keyboard) InputSystem.ResetDevice(device);
    }
    private bool IsAnyKeyHeld()
    {
        if (m_heldKeys == null) return false;
        foreach (bool held in m_heldKeys)
            if (held) return true;
        return false;
    }
    #endregion

    #region 열기/닫기
    private void Open()
    {
        m_isOpen = s_isOpen = true;
        StartBlockKeyboard();
        SetWindowVisible(true);
        m_input.text = "";
        m_input.Select();
        m_input.ActivateInputField();
        ScrollToBottom();
    }
    private void Close()
    {
        if (!m_isOpen) return;
        m_isOpen = s_isOpen = false;
        m_reactivateInput = m_closeRequested = false;
        s_closedFrame = Time.frameCount;
        // 키보드 차단은 바로 풀지 않고, 다음 프레임 이후 누른 키를 모두 떼면 Update에서 풂
        // (Esc로 닫은 경우 Esc의 Input System 이벤트가 한 프레임 늦게 올 수 있음)
        m_unblockAfterFrame = Time.frameCount + 1;
        // 지금까지 온 메시지는 채팅창에서 봤으므로 닫은 뒤 미리보기로 다시 띄우지 않음
        m_previewHiddenUntil = Time.unscaledTime;
        if (m_input.isFocused) m_input.DeactivateInputField(); // OnEndEdit -> Close가 다시 불리지만 m_isOpen으로 무시됨
        m_input.text = "";
        var es = EventSystem.current;
        if (es != null && !es.alreadySelecting && es.currentSelectedGameObject == m_input.gameObject) es.SetSelectedGameObject(null);
        SetWindowVisible(false);
    }
    private void SetWindowVisible(bool visible)
    {
        m_window.alpha = visible ? 1f : 0f;
        m_window.interactable = visible;
        m_window.blocksRaycasts = visible;
        m_previewText.gameObject.SetActive(!visible);
    }
    // ESC 메뉴의 룸 코드 입력 등 다른 입력창에 타이핑 중이면 ` 키로 채팅을 열지 않음
    private bool IsOtherInputFieldFocused()
    {
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected == null || selected == m_input.gameObject) return false;
        return (selected.TryGetComponent(out TMP_InputField tmp) && tmp.isFocused) || (selected.TryGetComponent(out InputField legacy) && legacy.isFocused);
    }
    #endregion

    #region 입력
    // ` 는 채팅창을 여닫는 키라서 입력되지 않도록 막음 (열 때 누른 ` 가 입력창에 들어가는 것도 방지)
    private static char ValidateChar(string text, int charIndex, char addedChar) => addedChar == '`' ? '\0' : addedChar;
    // Enter. 입력 내용을 보내고(빈 입력이면 무시됨) 채팅창은 유지함. 직후 onEndEdit에서 입력창을 다시 활성화
    private void OnSubmit(string text)
    {
        m_submitFrame = Time.frameCount;
        if (m_chat != null) m_chat.Send(text);
    }
    // Esc, 다른 곳 클릭으로 입력이 끝나면 닫음. Enter로 끝난 경우(같은 프레임에 onSubmit이 먼저 불림)는 입력창만 비우고 계속 입력
    private void OnEndEdit(string text)
    {
        if (m_isOpen && m_submitFrame == Time.frameCount)
        {
            // TMP가 비활성화 처리를 마친 뒤 다시 활성화해야 하므로 다음 Update에서 처리
            m_reactivateInput = true;
            return;
        }
        Close();
    }
    private void ReactivateInput()
    {
        m_reactivateInput = false;
        m_input.text = "";
        m_input.Select();
        m_input.ActivateInputField();
    }
    #endregion

    #region 표시
    private void OnMessageAdded(ChatMessage message)
    {
        bool atBottom = m_logScroll.verticalNormalizedPosition <= 0.01f;
        RebuildLog();
        // 채팅창을 열고 위쪽 기록을 보는 중이면 스크롤을 건드리지 않음
        if (m_isOpen && atBottom) ScrollToBottom();
    }
    private void RebuildLog()
    {
        m_sb.Clear();
        var history = m_chat.History;
        for (int i = 0; i < history.Count; i++)
        {
            if (i > 0) m_sb.Append('\n');
            AppendLine(m_sb, history[i], 1f);
        }
        m_logText.SetText(m_sb);
    }
    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        m_logScroll.verticalNormalizedPosition = 0f;
    }
    // 닫혀 있을 때 표시 시간이 남은 최신 메시지만 m_previewLines개까지 보여줌
    private void UpdatePreview()
    {
        if (m_isOpen || m_chat == null) return;
        float now = Time.unscaledTime;
        var history = m_chat.History;
        int start = history.Count;
        for (int i = history.Count - 1; i >= 0 && history.Count - i <= m_previewLines; i--)
        {
            float time = history[i].ReceivedTime;
            if (now - time >= m_previewDuration || time <= m_previewHiddenUntil) break;
            start = i;
        }
        if (start == history.Count)
        {
            if (m_previewVisibleCount != 0) m_previewText.SetText("");
            m_previewVisibleCount = 0;
            return;
        }
        m_previewVisibleCount = history.Count - start;
        m_sb.Clear();
        for (int i = start; i < history.Count; i++)
        {
            float remain = m_previewDuration - (now - history[i].ReceivedTime);
            if (i > start) m_sb.Append('\n');
            AppendLine(m_sb, history[i], Mathf.Clamp01(remain / m_fadeDuration));
        }
        m_previewText.SetText(m_sb);
    }
    // 시스템 메시지는 노란색, 플레이어 채팅은 "이름 : 내용". alpha는 미리보기가 사라질 때 사용
    private static void AppendLine(StringBuilder sb, in ChatMessage message, float alpha)
    {
        string a = Mathf.RoundToInt(alpha * 255f).ToString("X2");
        if (message.Type == ChatMessageType.System)
        {
            sb.Append("<color=#").Append(SystemColor).Append(a).Append('>');
            AppendEscaped(sb, message.Text);
            sb.Append("</color>");
            return;
        }
        sb.Append("<color=#").Append(TextColor).Append(a).Append('>');
        sb.Append("<color=#").Append(message.IsMine ? MyNameColor : NameColor).Append(a).Append('>');
        AppendEscaped(sb, message.Sender);
        sb.Append("</color> : ");
        AppendEscaped(sb, message.Text);
        sb.Append("</color>");
    }
    // 플레이어가 입력한 <b> 같은 문자열이 리치 텍스트 태그로 해석되지 않도록 < 뒤에 폭 없는 공백(U+200B)을 넣음
    private static void AppendEscaped(StringBuilder sb, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        foreach (char c in text)
        {
            sb.Append(c);
            if (c == '<') sb.Append('​');
        }
    }
    #endregion

    [SerializeField] private TMP_Text m_previewText;     // 닫혀 있을 때 새 메시지를 잠깐 보여주는 텍스트
    [SerializeField] private CanvasGroup m_window;       // 채팅창 (기록 + 입력창)
    [SerializeField] private TMP_Text m_logText;         // 채팅창의 전체 기록. m_logScroll의 content
    [SerializeField] private ScrollRect m_logScroll;
    [SerializeField] private TMP_InputField m_input;
    [SerializeField] private float m_previewDuration = 5f; // 새 메시지를 미리보기로 보여주는 시간 (초)
    [SerializeField] private float m_fadeDuration = 1f;    // 미리보기가 끝날 때 사라지는 데 걸리는 시간 (초)
    [SerializeField] private int m_previewLines = 5;       // 미리보기 최대 줄 수

    private const string TextColor = "FFFFFF";
    private const string NameColor = "9CDCFE";
    private const string MyNameColor = "B5E48C";
    private const string SystemColor = "FFD966";

    private static bool s_isOpen;
    private static int s_closedFrame = -1;
    private ChatManager m_chat;
    private bool m_isOpen;
    private bool m_blockKeyboard;   // Input System 키보드 입력을 막는 중
    private bool m_closeRequested;  // 막는 중에 ` 키가 눌려서 다음 Update에서 닫음
    private bool[] m_heldKeys;      // 막는 동안 실제로 눌려 있는 키 (Keyboard.allKeys 순서)
    private int m_unblockAfterFrame;
    private System.Action<InputEventPtr, InputDevice> m_onInputEvent;
    private int m_submitFrame = -1;  // Enter로 제출한 프레임. 같은 프레임의 onEndEdit에서 닫지 않기 위해 사용
    private bool m_reactivateInput; // 다음 Update에서 입력창을 다시 활성화
    private float m_previewHiddenUntil = float.NegativeInfinity;
    private int m_previewVisibleCount;
    private readonly StringBuilder m_sb = new();
}
