using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 현재 계절의 Forest 장식을 관리하고 계절 변경 시 바닥만 교차 페이드한다.
/// 첫 계절은 즉시 표시하며, 전환이 끝난 이전 레이어는 여러 프레임에 나눠 파괴·해제한다.
/// </summary>
public sealed class SeasonalLayerController : MonoBehaviour
{
    private const int c_seasonCount = 4;
    private const int c_destroyPerFrame = 200;   // 이전 레이어를 정리할 때 프레임당 파괴 개수
    private const string c_groundPath = "Cover/Ground";
    private const int c_previousRenderQueue = 3000;
    private const int c_nextRenderQueue = 3001;

    [Tooltip("봄·여름·가을·겨울 순서의 계절 레이어 프리팹")]
    [SerializeField] private AssetReferenceGameObject[] layers = new AssetReferenceGameObject[c_seasonCount];

    [Tooltip("계절 바닥의 전환 시간(초). 0이면 즉시 교체한다")]
    [SerializeField, Min(0f)] private float fadeDuration = 2f;
    [Tooltip("봄·여름·가을·겨울 순서의 바닥 전환용 머티리얼")]
    [SerializeField] private Material[] groundFadeMaterials = new Material[c_seasonCount];

    private static readonly int baseColorId = Shader.PropertyToID("_BaseColor");

    // 지금 씬에 붙어 있는 레이어
    private Season? activeSeason;
    private GameObject activeInstance;
    private AsyncOperationHandle<GameObject> activeHandle;

    // 유지 중인 요청과 요청 번호. 불러오는 사이 다른 요청이 오면 이전 결과는 버린다
    private Season? targetSeason;
    private int requestId;
    private bool isFading;

    private void OnEnable()
    {
        WorldManager.Instance.OnSeasonChangedEvent += HandleSeasonChanged;
    }

    private void OnDisable()
    {
        WorldManager.Instance.OnSeasonChangedEvent -= HandleSeasonChanged;
    }

    private void Start()
    {
        for (int i = 0; i < c_seasonCount; i++)
        {
            if (groundFadeMaterials != null && groundFadeMaterials.Length == c_seasonCount && groundFadeMaterials[i] != null && groundFadeMaterials[i].HasProperty(baseColorId))
            {
                continue;
            }
#if UNITY_EDITOR
            Debug.LogWarning($"[SeasonalLayerController] {(Season)i} 바닥 전환용 머티리얼을 확인해 주세요", this);
#endif
            enabled = false;
            return;
        }
        ApplyInitialAsync().Forget();
    }

    private void OnDestroy()
    {
        if (activeHandle.IsValid())
        {
            Addressables.Release(activeHandle);
        }
    }

    /// <summary>
    /// 월드 준비를 한 번만 기다린 뒤 현재 계절 레이어를 붙인다.
    /// 월드를 불러오거나 스냅샷을 받을 때는 계절 이벤트가 오지 않으므로 처음 한 번은 직접 확인한다.
    /// </summary>
    private async UniTaskVoid ApplyInitialAsync()
    {
        await UniTask.WaitUntil(() => WorldManager.Instance.IsReady, cancellationToken: destroyCancellationToken);
        SwapToAsync(WorldManager.Instance.CurSeason).Forget();
    }

    /// <summary>
    /// 계절이 바뀌면 그 계절 레이어로 교체한다.
    /// </summary>
    private void HandleSeasonChanged(Season season)
    {
        SwapToAsync(season).Forget();
    }

    /// <summary>
    /// 해당 계절 레이어를 불러와 숨겨진 상태로 생성하고, 바닥 전환 후 현재 레이어로 인계한다.
    /// 기다리는 동안 다른 계절이 요청되거나 이 오브젝트가 파괴되면, 결과를 버리고 불러온 에셋을 해제한다.
    /// 취소 토큰으로 대기를 끊지 않는 것은, 끊기면 이미 불러온 핸들을 해제할 기회가 사라지기 때문이다.
    /// </summary>
    private async UniTaskVoid SwapToAsync(Season season)
    {
        if (targetSeason == season || (!isFading && !targetSeason.HasValue && activeSeason == season))
        {
            return;   // 이미 붙어 있거나 불러오는 중
        }
        targetSeason = season;
        int id = ++requestId;

        // AssetReference.LoadAssetAsync는 참조 안에 핸들을 저장해, 이전 핸들이 살아 있으면 같은 계절을 다시 불러오지 못한다. 정적 API로 매번 새 핸들을 받는다
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(layers[(int)season]);
        await UniTask.WaitUntil(() => handle.IsDone);
        if (this == null || id != requestId)
        {
            Addressables.Release(handle);   // 파괴됐거나 더 새 요청이 있음
            return;
        }
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[SeasonalLayerController] {season} 레이어를 불러오지 못했습니다", this);
#endif
            Addressables.Release(handle);
            targetSeason = null;   // 실패한 요청을 비워 진행 중인 전환과 요청 상태가 어긋나지 않게 한다
            return;
        }

        GameObject stagingRoot = null;
        GameObject nextInstance = null;
        bool transferred = false;
        try
        {
            stagingRoot = new GameObject("SeasonalLayer_Staging");
            stagingRoot.SetActive(false);
            stagingRoot.transform.SetParent(transform, false);

            AsyncInstantiateOperation<GameObject> op = Object.InstantiateAsync(handle.Result, stagingRoot.transform);
            await UniTask.WaitUntil(() => op.isDone);
            nextInstance = op.Result[0];

            // 같은 바닥의 머티리얼을 두 전환 작업이 동시에 변경하지 않게 한다.
            await UniTask.WaitUntil(() => this == null || id != requestId || !isFading);
            if (this == null || id != requestId)
            {
                return;
            }

            if (activeInstance != null && fadeDuration > 0f)
            {
                isFading = true;
                try
                {
                    await FadeGroundAsync(nextInstance, season);
                }
                finally
                {
                    isFading = false;
                }
            }
            else
            {
                nextInstance.transform.SetParent(transform, false);
            }

            BatchGround(nextInstance);
            if (activeInstance != null)
            {
                DestroyGraduallyAsync(activeInstance, activeHandle).Forget();
            }
            else if (activeHandle.IsValid())
            {
                Addressables.Release(activeHandle);
            }
            activeInstance = nextInstance;
            activeHandle = handle;
            activeSeason = season;
            transferred = true;
        }
        finally
        {
            if (!transferred)
            {
                if (nextInstance != null)
                {
                    Destroy(nextInstance);
                }
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                if (this != null && id == requestId)
                {
                    targetSeason = null;
                }
            }
            if (stagingRoot != null)
            {
                Destroy(stagingRoot);
            }
        }
    }

    /// <summary>
    /// 각 바닥의 공유 머티리얼 하나씩만 갱신하며 교차 페이드하고, 끝나면 원본을 복구한다.
    /// </summary>
    private async UniTask FadeGroundAsync(GameObject nextInstance, Season nextSeason)
    {
        Transform previousGround = activeInstance.transform.Find(c_groundPath);
        MeshRenderer[] previousRenderers = previousGround.GetComponentsInChildren<MeshRenderer>(true);
        MeshRenderer[] nextRenderers = nextInstance.transform.Find(c_groundPath).GetComponentsInChildren<MeshRenderer>(true);
        Material previousOriginal = previousRenderers[0].sharedMaterial;
        Material nextOriginal = nextRenderers[0].sharedMaterial;
        Material previousFade = null;
        Material nextFade = null;

        try
        {
            previousFade = new Material(groundFadeMaterials[(int)activeSeason.Value]) { renderQueue = c_previousRenderQueue };
            nextFade = new Material(groundFadeMaterials[(int)nextSeason]) { renderQueue = c_nextRenderQueue };
            SetGroundMaterial(previousRenderers, previousFade);
            SetGroundMaterial(nextRenderers, nextFade);

            Color previousColor = previousFade.GetColor(baseColorId);
            Color nextColor = nextFade.GetColor(baseColorId);
            nextColor.a = 0f;
            nextFade.SetColor(baseColorId, nextColor);

            foreach (Transform group in activeInstance.transform)
            {
                if (group != previousGround.parent)
                {
                    group.gameObject.SetActive(false);
                }
            }
            nextInstance.transform.SetParent(transform, false);

            float startTime = Time.time;
            float duration = fadeDuration;
            while (true)
            {
                float progress = Mathf.Clamp01((Time.time - startTime) / duration);
                previousColor.a = 1f - progress;
                nextColor.a = progress;
                previousFade.SetColor(baseColorId, previousColor);
                nextFade.SetColor(baseColorId, nextColor);

                if (progress >= 1f)
                {
                    break;
                }
                await UniTask.Yield(cancellationToken: destroyCancellationToken);
            }
            activeInstance.SetActive(false);
        }
        finally
        {
            SetGroundMaterial(previousRenderers, previousOriginal);
            SetGroundMaterial(nextRenderers, nextOriginal);
            Destroy(previousFade);
            Destroy(nextFade);
        }
    }

    /// <summary>
    /// 단일 머티리얼 슬롯을 사용하는 바닥 모듈들에 같은 공유 머티리얼을 연결한다.
    /// </summary>
    private static void SetGroundMaterial(MeshRenderer[] renderers, Material material)
    {
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }
    }

    /// <summary>
    /// 생성된 계절의 바닥 모듈만 한 번 정적 배칭한다. 배칭할 수 없으면 원래 렌더러로 표시한다.
    /// </summary>
    private static void BatchGround(GameObject layer)
    {
        Transform ground = layer.transform.Find(c_groundPath);
        if (ground == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[SeasonalLayerController] Cover/Ground가 없어 바닥 정적 배칭을 생략합니다", layer);
#endif
            return;
        }

        foreach (MeshFilter filter in ground.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh != null && !mesh.isReadable)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[SeasonalLayerController] {mesh.name} 메시의 Read/Write가 꺼져 있어 바닥 정적 배칭을 생략합니다", filter);
#endif
                return;
            }
        }

        try
        {
            StaticBatchingUtility.Combine(ground.gameObject);
        }
        catch (System.Exception exception)
        {
            // 선택적인 렌더링 최적화의 실패가 계절 교체와 이전 레이어 정리를 막지 않게 한다.
#if UNITY_EDITOR
            Debug.LogWarning($"[SeasonalLayerController] 바닥 정적 배칭에 실패해 원래 렌더러로 표시합니다: {exception.Message}", layer);
#endif
        }
    }

    /// <summary>
    /// 이전 레이어를 바로 끄고, 자식부터 여러 프레임에 나눠 파괴한 뒤 에셋을 해제한다.
    /// 인스턴스가 남아 있는 동안 에셋을 해제하면 빌드에서 메시가 내려갈 수 있으므로 해제는 마지막에 한다.
    /// </summary>
    private async UniTaskVoid DestroyGraduallyAsync(GameObject layer, AsyncOperationHandle<GameObject> handle)
    {
        layer.SetActive(false);   // 새 레이어가 붙는 프레임에 바로 안 보이게
        Transform[] all = layer.GetComponentsInChildren<Transform>(true);
        for (int i = all.Length - 1; i > 0; i--)   // 부모가 자식보다 앞에 오므로 뒤에서부터 = 자식부터, 0번은 루트
        {
            Destroy(all[i].gameObject);
            if (i % c_destroyPerFrame == 0)
            {
                await UniTask.Yield();
                if (this == null)
                {
                    break;   // 남은 것은 부모와 함께 파괴됨
                }
            }
        }
        if (layer != null)
        {
            Destroy(layer);
        }
        if (handle.IsValid())
        {
            Addressables.Release(handle);
        }
    }
}
