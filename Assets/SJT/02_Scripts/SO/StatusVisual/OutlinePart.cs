using UnityEngine;

/// <summary>
/// 상태가 걸려 있는 동안 대상의 외곽선을 지정한 색으로 표시하는 독점형 파츠이다.
/// 여러 상태가 동시에 외곽선을 요청하면 우선순위가 가장 높은 하나만 보인다.
/// </summary>
[CreateAssetMenu(fileName = "OutlinePart", menuName = "Scriptable Objects/StatusVisual/OutlinePart")]
public sealed class OutlinePart : StatusVisualPart
{
    [Tooltip("외곽선을 그릴 셰이더. _OutlineColor, _OutlineWidth 프로퍼티가 필요함 (예: Custom/InteractOutline)")]
    [SerializeField] private Shader outlineShader;

    [Tooltip("외곽선 색")]
    [SerializeField] private Color outlineColor = Color.red;

    [Min(0f)]
    [Tooltip("외곽선 두께. 단위는 픽셀")]
    [SerializeField] private float outlineWidth = 4f;

    [Tooltip("여러 상태가 겹칠 때 높은 값의 외곽선이 우선 표시됨. 한 상태에는 OutlinePart를 하나만 넣음 (여러 개면 마지막 것만 적용)")]
    [SerializeField] private int priority;

    /// <summary>외곽선을 그릴 셰이더를 반환한다.</summary>
    public Shader OutlineShader => outlineShader;

    /// <summary>외곽선 색을 반환한다.</summary>
    public Color OutlineColor => outlineColor;

    /// <summary>외곽선 두께를 반환한다. 단위는 픽셀</summary>
    public float OutlineWidth => outlineWidth;

    /// <summary>겹칠 때 사용할 우선순위를 반환한다. 높을수록 우선</summary>
    public int Priority => priority;

    /// <summary>이 상태의 외곽선을 요청한다.</summary>
    public override void OnStart(StatusEffectVisualizer host, StatusEffect effect)
    {
        host.OutlineChannel.Request(effect, this);
    }

    /// <summary>재활성화된 대상에 남아 있던 상태의 외곽선을 다시 요청한다.</summary>
    public override void OnRestore(StatusEffectVisualizer host, StatusEffect effect)
    {
        host.OutlineChannel.Request(effect, this);
    }

    /// <summary>이 상태의 외곽선 요청을 해제한다. 남은 요청이 있으면 그 색으로 바뀐다.</summary>
    public override void OnEnd(StatusEffectVisualizer host, StatusEffect effect)
    {
        host.OutlineChannel.Release(effect);
    }
}
