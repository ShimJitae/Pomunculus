using UnityEngine;

/// <summary>
/// 이동 마커가 켜질 때마다 한 번 재생되는 연출.
/// 살짝 큰 크기에서 제자리로 줄어들며 나타나고, 잠시 머문 뒤 작아지며 사라진 다음 스스로 꺼진다.
/// 다시 보여주려면 오브젝트를 껐다 켜면 처음부터 재생된다.
/// </summary>
public class MoveMarkerEffect : MonoBehaviour
{
    private static readonly int baseColorPropertyId = Shader.PropertyToID("_BaseColor");

    [Header("시간 (초)")]
    [SerializeField] private float appearDuration = 0.12f;
    [SerializeField] private float holdDuration = 0.15f;
    [SerializeField] private float fadeDuration = 0.35f;

    [Header("크기 배율")]
    [SerializeField] private float startScaleMultiplier = 1.3f;
    [SerializeField] private float endScaleMultiplier = 0.85f;

    [Header("투명도")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.9f;

    private Renderer markerRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Color baseColor = Color.white;
    private Vector3 baseScale;
    private float elapsedTime;

    private float TotalDuration => appearDuration + holdDuration + fadeDuration;

    // 원래 크기와 색, 렌더러를 캐시한다
    private void Awake()
    {
        baseScale = transform.localScale;
        markerRenderer = GetComponentInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        if (markerRenderer != null && markerRenderer.sharedMaterial != null
            && markerRenderer.sharedMaterial.HasProperty(baseColorPropertyId))
        {
            baseColor = markerRenderer.sharedMaterial.GetColor(baseColorPropertyId);
        }
    }

    // 켜질 때마다 연출을 처음부터 시작한다
    private void OnEnable()
    {
        elapsedTime = 0f;
        ApplyFrame(0f);
    }

    // 꺼질 때 크기를 되돌려 다음 재생에 영향이 없게 한다
    private void OnDisable()
    {
        transform.localScale = baseScale;
    }

    // 시간을 진행시키고, 연출이 끝나면 스스로 꺼진다
    private void Update()
    {
        elapsedTime += Time.deltaTime;

        if (elapsedTime >= TotalDuration)
        {
            gameObject.SetActive(false);
            return;
        }

        ApplyFrame(elapsedTime);
    }

    // 경과 시간에 맞는 크기와 투명도를 적용한다
    private void ApplyFrame(float time)
    {
        float scaleMultiplier;
        float alpha;

        if (time < appearDuration)
        {
            // 나타남: 살짝 큰 크기에서 부드럽게 줄어들며 선명해진다
            float progress = GetProgress(time, appearDuration);
            float easedProgress = 1f - (1f - progress) * (1f - progress);
            scaleMultiplier = Mathf.Lerp(startScaleMultiplier, 1f, easedProgress);
            alpha = easedProgress;
        }
        else if (time < appearDuration + holdDuration)
        {
            // 유지
            scaleMultiplier = 1f;
            alpha = 1f;
        }
        else
        {
            // 사라짐: 조금씩 작아지며 흐려진다
            float progress = GetProgress(time - appearDuration - holdDuration, fadeDuration);
            scaleMultiplier = Mathf.Lerp(1f, endScaleMultiplier, progress);
            alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
        }

        transform.localScale = baseScale * scaleMultiplier;
        ApplyAlpha(alpha * maxAlpha);
    }

    // 머티리얼을 복제하지 않고 이 렌더러의 투명도만 바꾼다
    private void ApplyAlpha(float alpha)
    {
        if (markerRenderer == null)
            return;

        Color color = baseColor;
        color.a = baseColor.a * alpha;

        markerRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(baseColorPropertyId, color);
        markerRenderer.SetPropertyBlock(propertyBlock);
    }

    // 구간 길이가 0이어도 안전하게 0~1 진행도를 구한다
    private static float GetProgress(float time, float duration)
    {
        if (duration <= 0f)
            return 1f;

        return Mathf.Clamp01(time / duration);
    }
}
