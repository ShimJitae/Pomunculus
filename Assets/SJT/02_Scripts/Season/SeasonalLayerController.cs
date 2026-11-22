using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 현재 계절의 Forest 장식 레이어를 이 오브젝트 아래에 하나만 유지한다.
/// 계절이 바뀌면 새 레이어를 불러와 붙이고, 붙은 뒤에 이전 레이어를 끄고 여러 프레임에 나눠 파괴·해제한다.
/// </summary>
public sealed class SeasonalLayerController : MonoBehaviour
{
    private const int c_seasonCount = 4;
    private const int c_destroyPerFrame = 200;   // 이전 레이어를 정리할 때 프레임당 파괴 개수

    [Tooltip("봄·여름·가을·겨울 순서의 계절 레이어 프리팹")]
    [SerializeField] private AssetReferenceGameObject[] layers = new AssetReferenceGameObject[c_seasonCount];

    // 지금 씬에 붙어 있는 레이어
    private Season? activeSeason;
    private GameObject activeInstance;
    private AsyncOperationHandle<GameObject> activeHandle;

    // 마지막으로 요청한 계절과 요청 번호. 불러오는 사이 다른 요청이 오면 이전 결과는 버린다
    private Season? targetSeason;
    private int requestId;

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
    /// 해당 계절 레이어를 불러와 비동기로 생성하고, 생성이 끝나면 이전 레이어와 바꾼다.
    /// 기다리는 동안 다른 계절이 요청되거나 이 오브젝트가 파괴되면, 결과를 버리고 불러온 에셋을 해제한다.
    /// 취소 토큰으로 대기를 끊지 않는 것은, 끊기면 이미 불러온 핸들을 해제할 기회가 사라지기 때문이다.
    /// </summary>
    private async UniTaskVoid SwapToAsync(Season season)
    {
        if (targetSeason == season)
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
            targetSeason = activeSeason;   // 다음 계절 이벤트에서 다시 시도할 수 있게 되돌림
            return;
        }

        AsyncInstantiateOperation<GameObject> op = Object.InstantiateAsync(handle.Result, transform);
        await UniTask.WaitUntil(() => op.isDone);
        if (this == null || id != requestId)
        {
            foreach (GameObject instance in op.Result)
            {
                Destroy(instance);
            }
            Addressables.Release(handle);
            return;
        }

        BatchGround(op.Result[0]);

        if (activeInstance != null)
        {
            DestroyGraduallyAsync(activeInstance, activeHandle).Forget();   // 이전 레이어는 끄고 천천히 정리
        }
        else if (activeHandle.IsValid())
        {
            Addressables.Release(activeHandle);
        }
        activeInstance = op.Result[0];
        activeHandle = handle;
        activeSeason = season;
    }

    /// <summary>
    /// 생성된 계절의 바닥 모듈만 한 번 정적 배칭한다. 배칭할 수 없으면 원래 렌더러로 표시한다.
    /// </summary>
    private static void BatchGround(GameObject layer)
    {
        Transform ground = layer.transform.Find("Cover/Ground");
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
