using UnityEngine;

// 공격 범위 예고 표시. 바닥에 눕힌 Quad 하나를 켜고 끄기만 한다.
public class EnemyAttackIndicator : MonoBehaviour
{
    private static readonly int FillId = Shader.PropertyToID("_Fill");
    private static readonly int AngleId = Shader.PropertyToID("_Angle");
    private static readonly int IsRectId = Shader.PropertyToID("_IsRect");

    [SerializeField] private MeshRenderer indicatorRenderer;
    [SerializeField] private float groundOffset = 0.05f;   // 바닥과 겹쳐 깜빡이는 것 방지
    [SerializeField] private float extraShowTime = 1f;     // 판정 이벤트 누락 대비 안전 숨김

    private MaterialPropertyBlock propertyBlock;
    private float telegraphDuration;
    private float elapsedTime;
    private bool isShowing;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        Hide();
    }

    public void Show(AttackAreaData area, Vector3 origin, Quaternion rotation, float duration)
    {
        if (area.shape == AttackAreaShape.None || indicatorRenderer == null)
            return;

        bool isRectangle = area.shape == AttackAreaShape.Rectangle;
        Vector3 attackForward = rotation * Vector3.forward;
        Vector3 flatOffset = new Vector3(area.offset.x, 0f, area.offset.z);   // 표시는 바닥에 붙임
        Vector3 areaCenter = origin + rotation * flatOffset;
        Transform indicatorTransform = indicatorRenderer.transform;

        if (isRectangle)
        {
            indicatorTransform.position = areaCenter + attackForward * (area.length * 0.5f) + Vector3.up * groundOffset;
            indicatorTransform.localScale = new Vector3(area.width, area.length, 1f);
        }
        else
        {
            indicatorTransform.position = areaCenter + Vector3.up * groundOffset;
            indicatorTransform.localScale = new Vector3(area.radius * 2f, area.radius * 2f, 1f);
        }

        // Quad를 바닥에 눕힘: 면은 위쪽, UV 위쪽(v)은 공격 방향
        indicatorTransform.rotation = rotation * Quaternion.Euler(90f, 0f, 0f);

        propertyBlock.SetFloat(AngleId, area.shape == AttackAreaShape.Sector ? area.angle : 360f);
        propertyBlock.SetFloat(IsRectId, isRectangle ? 1f : 0f);
        propertyBlock.SetFloat(FillId, 0f);
        indicatorRenderer.SetPropertyBlock(propertyBlock);

        telegraphDuration = Mathf.Max(duration, 0.01f);
        elapsedTime = 0f;
        isShowing = true;
        indicatorRenderer.enabled = true;
    }

    public void Hide()
    {
        isShowing = false;

        if (indicatorRenderer != null)
            indicatorRenderer.enabled = false;
    }

    private void Update()
    {
        if (!isShowing)
            return;

        elapsedTime += Time.deltaTime;

        propertyBlock.SetFloat(FillId, Mathf.Clamp01(elapsedTime / telegraphDuration));
        indicatorRenderer.SetPropertyBlock(propertyBlock);

        if (elapsedTime >= telegraphDuration + extraShowTime)
            Hide();
    }
}