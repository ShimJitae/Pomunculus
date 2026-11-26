using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 붙은 카메라가 모니터 비율과 관계없이 화면 가운데 16:9 영역에만 그리도록 Viewport Rect를 맞춘다.
/// 넓은 화면은 좌우, 좁은 화면은 위아래를 자른다. 남는 영역은 런타임에 만드는 배경 카메라가 검게 지운다.
/// Play 중에만 동작한다(에디터에서 rect를 바꾸면 씬이 수정 상태가 되므로).
/// </summary>
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public sealed class AspectLetterbox : MonoBehaviour
{
    private const float c_targetAspect = 16f / 9f;
    private const string c_backgroundName = "LetterboxBG";

    private static readonly Rect s_fullRect = new Rect(0f, 0f, 1f, 1f);

    private Camera cam;
    private Camera backgroundCamera;
    private int lastWidth;
    private int lastHeight;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        backgroundCamera = CreateBackgroundCamera();
    }

    private void OnEnable()
    {
        if (backgroundCamera != null)
        {
            backgroundCamera.enabled = true;
        }
        // 다시 켜질 때 바로 계산하도록 기록을 비운다
        lastWidth = 0;
        lastHeight = 0;
    }

    private void OnDisable()
    {
        // 스크립트를 끄면 원래대로 화면 전체에 그린다
        if (cam != null)
        {
            cam.rect = s_fullRect;
        }
        if (backgroundCamera != null)
        {
            backgroundCamera.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (backgroundCamera != null)
        {
            Destroy(backgroundCamera.gameObject);
        }
    }

    private void LateUpdate()
    {
        // 해상도·창 크기가 바뀌었을 때만 다시 계산한다
        if (Screen.width == lastWidth && Screen.height == lastHeight)
        {
            return;
        }
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        cam.rect = CalculateRect(lastWidth, lastHeight);
    }

    /// <summary>화면 크기에 맞춰 가운데 16:9 영역을 0~1 비율 Rect로 계산한다</summary>
    private static Rect CalculateRect(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return s_fullRect;
        }

        float scale = ((float)width / height) / c_targetAspect;

        if (scale < 1f)
        {
            // 16:9보다 좁다(16:10, 4:3) → 위아래 레터박스
            return new Rect(0f, (1f - scale) * 0.5f, 1f, scale);
        }

        // 16:9보다 넓다(21:9) → 좌우 필러박스. 정확히 16:9면 전체 화면이 된다
        float widthRatio = 1f / scale;
        return new Rect((1f - widthRatio) * 0.5f, 0f, widthRatio, 1f);
    }

    /// <summary>화면 전체를 검게 지우기만 하는 카메라를 만든다. 오브젝트는 그리지 않는다</summary>
    private Camera CreateBackgroundCamera()
    {
        var go = new GameObject(c_backgroundName);
        go.transform.SetParent(transform, false);

        var background = go.AddComponent<Camera>();
        background.clearFlags = CameraClearFlags.SolidColor;
        background.backgroundColor = Color.black;
        background.cullingMask = 0;
        background.depth = cam.depth - 1f;   // 메인 카메라보다 먼저 그린다
        background.rect = s_fullRect;
        background.useOcclusionCulling = false;
        background.allowHDR = false;
        background.allowMSAA = false;

        var urpData = go.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderType = CameraRenderType.Base;
        urpData.renderPostProcessing = false;
        urpData.renderShadows = false;
        urpData.requiresDepthTexture = false;
        urpData.requiresColorTexture = false;
        urpData.antialiasing = AntialiasingMode.None;

        return background;
    }
}
