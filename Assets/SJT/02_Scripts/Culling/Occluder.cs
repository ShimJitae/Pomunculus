using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카메라와 플레이어 사이를 가릴 수 있는 장애물. OccluderDetector가 가린다고 알려주면
/// 투명 머티리얼로 바꾼 뒤 알파를 서서히 낮추고, 벗어나면 다시 올려 원본 머티리얼로 되돌린다.
/// 평소에는 꺼져 있고 페이드하는 동안만 Update가 돈다.
/// </summary>
public sealed class Occluder : MonoBehaviour
{
    private const float c_fadeAlpha = 0.35f;     // 가렸을 때의 알파
    private const float c_fadeDuration = 0.25f;  // 전환 시간(초)
    private const float c_fadeSpeed = (1f - c_fadeAlpha) / c_fadeDuration;

    private static readonly int c_baseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly List<Occluder> all = new();
    private static MaterialPropertyBlock block;

    private Renderer targetRenderer;
    private Material originalMaterial;
    private Material fadeMaterial;   // 반투명 상태일 때만 값이 있다
    private Color baseColor;         // fadeMaterial의 색. 알파만 바꿔 쓴다
    private bool isOccluding;
    private float alpha = 1f;

    /// <summary>씬에 있는 모든 Occluder. OccluderDetector가 검사 대상으로 읽는다</summary>
    public static IReadOnlyList<Occluder> All => all;

    /// <summary>판정에 쓸 월드 공간 범위</summary>
    public Bounds Bounds => targetRenderer.bounds;

    // 컴포넌트를 꺼둔 채 지내므로 등록은 OnEnable이 아니라 오브젝트 수명(Awake/OnDestroy)에 묶는다
    private void Awake()
    {
        targetRenderer = GetComponentInChildren<Renderer>();
        originalMaterial = targetRenderer.sharedMaterial;
        enabled = false;
        all.Add(this);
    }

    private void OnDestroy()
    {
        all.Remove(this);
    }// 페이드하는 동안만 켜져 있다
    private void Update()
    {
        float target = isOccluding ? c_fadeAlpha : 1f;
        alpha = Mathf.MoveTowards(alpha, target, c_fadeSpeed * Time.deltaTime);

        // 머티리얼을 복제하지 않고 이 렌더러의 알파만 덮어쓴다
        baseColor.a = alpha;
        block.SetColor(c_baseColorId, baseColor);
        targetRenderer.SetPropertyBlock(block);

        // 목표 알파에 도달할 때까지 다음 프레임에도 갱신한다
        if (alpha != target)
        {
            return;
        }

        if (!isOccluding)
        {
            RestoreOriginal();
        }

        enabled = false;
    }

    /// <summary>가림 여부를 바꾼다. 가리기 시작하면 투명 머티리얼로 교체하고 페이드를 시작한다</summary>
    public void SetOccluding(bool occluding, Material fadeMaterialToUse)
    {
        if (isOccluding == occluding)
        {
            return;
        }

        isOccluding = occluding;

        // 선명해지는 도중에 다시 가려지면 이미 투명 머티리얼이므로 교체하지 않는다
        if (occluding && fadeMaterial == null)
        {
            fadeMaterial = fadeMaterialToUse;
            baseColor = fadeMaterial.GetColor(c_baseColorId);
            block ??= new MaterialPropertyBlock();
            targetRenderer.sharedMaterial = fadeMaterial;
        }

        enabled = true;
    }

    // PropertyBlock이 남아 있으면 SRP Batcher에서 빠지므로 반드시 비운다
    private void RestoreOriginal()
    {
        targetRenderer.SetPropertyBlock(null);
        targetRenderer.sharedMaterial = originalMaterial;
        fadeMaterial = null;
    }
}