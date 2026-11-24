using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// 아이템 획득 알림 텍스트. Model(ItemPickupController)은 PickupUIBinder가 PickupUIPresenter로 연결한다.
public class PickupUIView : MonoBehaviour, IPickupView
{
    [SerializeField] private PickupToastView toastPrefab;
    [SerializeField] private RectTransform container;   // ToastContainer
    [SerializeField, Min(1)] private int maxVisible = 5;
    [SerializeField, Min(0)] private int prewarmCount = 6;

    private ObjectPool<PickupToastView> pool;
    private readonly List<PickupToastView> activeToasts = new();

    // 토스트 풀을 만들고 미리 몇 개 생성해 둔다
    private void Awake()
    {
        pool = new ObjectPool<PickupToastView>(
            createFunc: () => Instantiate(toastPrefab, container),
            actionOnGet: toast => toast.gameObject.SetActive(true),
            actionOnRelease: toast =>
            {
                toast.ResetForPool();
                toast.gameObject.SetActive(false);
            },
            actionOnDestroy: toast => Destroy(toast.gameObject),
            collectionCheck: false,
            defaultCapacity: prewarmCount,
            maxSize: 10);

        Prewarm();
    }

    // prewarmCount개를 한꺼번에 꺼냈다가 반환해서 풀에 비활성 상태로 쌓아 둔다
    private void Prewarm()
    {
        var temp = new List<PickupToastView>(prewarmCount);

        for (int i = 0; i < prewarmCount; i++)
            temp.Add(pool.Get());      // 풀이 비어 있으니 createFunc로 새로 생성됨

        foreach (var toast in temp)
            pool.Release(toast);       // actionOnRelease → 비활성화 후 풀에 보관
    }

    // 새 토스트를 맨 아래에 띄우고, 보이는 개수가 넘치면 가장 오래된 것을 내보낸다
    public void ShowPickup(PickupToastData data)
    {
        if (CountVisibleToasts() >= maxVisible)
            DismissOldestToast();

        PickupToastView toast = pool.Get();
        toast.transform.SetAsLastSibling();
        activeToasts.Add(toast);
        toast.Play(data, ReleaseToast);
    }
    // 퇴장 중이 아닌 토스트 수
    private int CountVisibleToasts()
    {
        int count = 0;
        foreach (var toast in activeToasts)
            if (!toast.IsLeaving) count++;
        return count;
    }

    // 퇴장 중이 아닌 토스트 중 가장 오래된 것을 퇴장시킨다
    private void DismissOldestToast()
    {
        foreach (var toast in activeToasts)
        {
            if (toast.IsLeaving) continue;
            toast.Dismiss();
            return;
        }
    }

    // 퇴장이 끝난 토스트를 목록에서 빼고 풀에 반환한다
    private void ReleaseToast(PickupToastView toast)
    {
        if (!activeToasts.Remove(toast))
            return;

        pool.Release(toast);
    }

    // 비활성화될 때 화면에 남은 토스트를 모두 풀로 돌려보낸다
    private void OnDisable()
    {
        for (int i = activeToasts.Count - 1; i >= 0; i--)
            pool.Release(activeToasts[i]);

        activeToasts.Clear();
    }
}
