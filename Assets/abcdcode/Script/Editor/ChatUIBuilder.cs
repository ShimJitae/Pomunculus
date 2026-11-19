using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 열려 있는 씬에 채팅 UI(ChatCanvas)를 만들고 ChatUI 참조를 연결. 월드 씬에서 한 번 실행
// 만든 뒤에는 위치/크기/색을 자유롭게 바꿔도 되고, 프리팹으로 만들어 다른 씬에 써도 됨
public static class ChatUIBuilder
{
    private const string FontPath = "Assets/TextMesh Pro/Fonts/Maplestory Light SDF.asset";
    private const float FontSize = 22f;
    private const string UndoName = "Build Chat UI";

    [MenuItem("Tools/Chat/Build Chat UI")]
    private static void Build()
    {
        if (Object.FindAnyObjectByType<ChatUI>(FindObjectsInactive.Include) != null)
        {
            EditorUtility.DisplayDialog("Chat UI", "씬에 이미 ChatUI가 있습니다.", "확인");
            return;
        }
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) font = TMP_Settings.defaultFontAsset;

        // ChatManager는 게임 시작 시 자동 생성되므로 씬에 만들지 않음
        EnsureEventSystem();

        // 캔버스
        var canvasGo = new GameObject("ChatCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 미리보기 (닫혀 있을 때 새 메시지)
        var preview = CreateText("Preview", canvasGo.transform, font);
        SetBottomLeft(preview.rectTransform, new Vector2(20f, 80f), new Vector2(620f, 240f));
        preview.alignment = TextAlignmentOptions.BottomLeft;
        preview.raycastTarget = false;

        // 채팅창
        var window = CreateUI("Window", canvasGo.transform, typeof(Image), typeof(CanvasGroup));
        SetBottomLeft(window, new Vector2(20f, 20f), new Vector2(620f, 360f));
        window.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        // 기록. 내용이 짧으면 아래쪽에 붙도록 content의 pivot을 아래로 둠
        var scrollRect = CreateUI("Log", window, typeof(ScrollRect));
        Stretch(scrollRect, new Vector2(10f, 60f), new Vector2(-10f, -10f));
        var viewport = CreateUI("Viewport", scrollRect, typeof(RectMask2D));
        Stretch(viewport, Vector2.zero, Vector2.zero);
        var log = CreateText("Content", viewport, font);
        var logRect = log.rectTransform;
        logRect.anchorMin = Vector2.zero;
        logRect.anchorMax = new Vector2(1f, 0f);
        logRect.pivot = Vector2.zero;
        logRect.anchoredPosition = Vector2.zero;
        logRect.sizeDelta = Vector2.zero;
        log.alignment = TextAlignmentOptions.BottomLeft;
        log.raycastTarget = false;
        log.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = scrollRect.GetComponent<ScrollRect>();
        scroll.content = logRect;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;
        scroll.scrollSensitivity = 30f;

        // 입력창
        var inputGo = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        inputGo.name = "Input";
        var inputRect = (RectTransform)inputGo.transform;
        inputRect.SetParent(window, false);
        inputRect.anchorMin = Vector2.zero;
        inputRect.anchorMax = new Vector2(1f, 0f);
        inputRect.pivot = new Vector2(0.5f, 0f);
        inputRect.offsetMin = new Vector2(10f, 10f);
        inputRect.offsetMax = new Vector2(-10f, 50f);
        inputGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
        var input = inputGo.GetComponent<TMP_InputField>();
        foreach (var text in inputGo.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = font;
            text.fontSize = FontSize;
        }
        input.pointSize = FontSize;
        input.textComponent.color = Color.white;
        input.caretColor = Color.white;
        input.customCaretColor = true;
        if (input.placeholder is TMP_Text placeholder)
        {
            placeholder.text = "메시지 입력 (Enter 전송 / Esc 닫기)";
            placeholder.color = new Color(1f, 1f, 1f, 0.4f);
        }

        // ChatUI 연결
        var ui = canvasGo.AddComponent<ChatUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("m_previewText").objectReferenceValue = preview;
        so.FindProperty("m_window").objectReferenceValue = window.GetComponent<CanvasGroup>();
        so.FindProperty("m_logText").objectReferenceValue = log;
        so.FindProperty("m_logScroll").objectReferenceValue = scroll;
        so.FindProperty("m_input").objectReferenceValue = input;
        so.ApplyModifiedPropertiesWithoutUndo();

        SetLayerRecursively(canvasGo.transform, LayerMask.NameToLayer("UI"));
        Undo.RegisterCreatedObjectUndo(canvasGo, UndoName);
        Selection.activeGameObject = canvasGo;
        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;
        Undo.RegisterCreatedObjectUndo(new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)), UndoName);
    }
    private static RectTransform CreateUI(string name, Transform parent, params System.Type[] components)
    {
        var go = new GameObject(name, typeof(RectTransform));
        foreach (var type in components) go.AddComponent(type);
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }
    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font)
    {
        var text = CreateUI(name, parent, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = FontSize;
        text.color = Color.white;
        text.richText = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }
    private static void SetBottomLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
    private static void SetLayerRecursively(Transform t, int layer)
    {
        if (layer < 0) return;
        t.gameObject.layer = layer;
        foreach (Transform child in t) SetLayerRecursively(child, layer);
    }
}
