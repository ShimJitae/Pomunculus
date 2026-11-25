using DG.Tweening;
using UnityEngine;

public sealed class WorldMapUI : PopupUI
{
    [SerializeField] private Transform worldRoot;
    [SerializeField] private Transform target;
    [SerializeField] private RectTransform playerPin;
    [SerializeField] private WorldMapCalibration calibration;
    [SerializeField] private RectTransform mapBG;
    [SerializeField, Min(0f)] private float openDuration = 0.3f;
    [SerializeField, Min(0f)] private float closeDuration = 0.2f;
    [SerializeField] private float closedAngle = 8f;
    [Tooltip("열린 위치에서 숨김 위치까지의 UI 좌표 오프셋입니다.")]
    [SerializeField] private Vector2 closedOffset = new Vector2(0f, -1200f);

    private Vector2 openedPosition;
    private Vector3 openedRotation;
    private Sequence transition;

    private void Awake()
    {
        if (mapBG == null)
        {
            return;
        }

        openedPosition = mapBG.anchoredPosition;
        openedRotation = mapBG.localEulerAngles;
    }

    private void OnDisable()
    {
        transition?.Kill();

        if (mapBG != null)
        {
            mapBG.anchoredPosition = openedPosition;
            mapBG.localEulerAngles = openedRotation;
        }
    }

    [ContextMenu("Open 테스트")]
    protected override void OnOpened()
    {
        if (mapBG != null)
        {
            if (transition == null)
            {
                mapBG.anchoredPosition = openedPosition + closedOffset;
                mapBG.localEulerAngles = openedRotation + Vector3.forward * closedAngle;
            }

            AnimateTo(openedPosition, openedRotation, openDuration, Ease.OutCubic);
        }

        RefreshPlayerPin();
    }

    protected override void PlayHideTransition()
    {
        if (mapBG == null)
        {
            FinishHide();
            return;
        }

        AnimateTo(openedPosition + closedOffset, openedRotation + Vector3.forward * closedAngle, closeDuration, Ease.InCubic);
        transition.OnComplete(() => gameObject.SetActive(false));
    }

    private void AnimateTo(Vector2 position, Vector3 rotation, float duration, Ease ease)
    {
        transition?.Kill();

        transition = DOTween.Sequence().OnKill(() => transition = null);
        transition.Join(mapBG.DOAnchorPos(position, duration).SetEase(ease));
        transition.Join(mapBG.DOLocalRotate(rotation, duration).SetEase(ease));
    }

    private void RefreshPlayerPin()
    {

        if (playerPin == null)
        {
            return;
        }

        playerPin.gameObject.SetActive(false);

        if (target == null)
        {
            target = FindLocalPlayer();
        }

        if (target == null || !target.gameObject.activeInHierarchy ||
            worldRoot == null || calibration == null)
        {
            return;
        }

        Vector3 localPosition = worldRoot.InverseTransformPoint(target.position);
        Vector2 worldXZ = new Vector2(localPosition.x, localPosition.z);

        if (!calibration.TryGetMapUV(worldXZ, out Vector2 imageUV))
        {
            return;
        }

        playerPin.anchorMin = imageUV;
        playerPin.anchorMax = imageUV;
        playerPin.anchoredPosition = Vector2.zero;
        playerPin.gameObject.SetActive(true);
    }

    private static Transform FindLocalPlayer()
    {
        foreach (IWorldObj worldObject in WorldManager.Instance.Objects.GetList())
        {
            if (worldObject is PlayerFSM player && player != null && player.IsMine && player.gameObject.activeInHierarchy)
            {
                return player.transform;
            }
        }

        return null;
    }
}
