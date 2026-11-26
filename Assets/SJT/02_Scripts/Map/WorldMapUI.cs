using System.Collections.Generic;
using DG.Tweening;
using Photon.Pun;
using TMPro;
using UnityEngine;

public sealed class WorldMapUI : PopupUI
{
    [SerializeField] private Transform worldRoot;
    [SerializeField] private Transform target;
    [SerializeField] private RectTransform playerPin;
    [Tooltip("다른 플레이어 핀 원본입니다. 비활성 상태로 두고 자식에 닉네임 TMP를 둡니다.")]
    [SerializeField] private RectTransform otherPinTemplate;
    [SerializeField] private WorldMapCalibration calibration;
    [SerializeField] private RectTransform mapBG;
    [SerializeField, Min(0f)] private float openDuration = 0.3f;
    [SerializeField, Min(0f)] private float closeDuration = 0.2f;
    [SerializeField] private float closedAngle = 8f;
    [Tooltip("열린 위치에서 숨김 위치까지의 UI 좌표 오프셋입니다.")]
    [SerializeField] private Vector2 closedOffset = new Vector2(0f, -1200f);

    private readonly List<(PlayerFSM player, RectTransform pin)> otherPins = new();

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

    private void LateUpdate()
    {
        RefreshPins();
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

        CreateOtherPins();

        TMP_Text myName = playerPin != null ? playerPin.GetComponentInChildren<TMP_Text>(true) : null;
        if (myName != null && GameManager.Instance != null) myName.text = GameManager.Instance.LocalUserProfile.UserName;

        RefreshPins();
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

    // 지도를 열 때 다른 플레이어 핀을 새로 만든다. 열려 있는 동안 들어온 플레이어는 다음에 열 때 표시된다.
    private void CreateOtherPins()
    {
        foreach (var other in otherPins)
        {
            Destroy(other.pin.gameObject);
        }
        otherPins.Clear();

        if (otherPinTemplate == null)
        {
            return;
        }

        foreach (IWorldObj worldObject in WorldManager.Instance.Objects.GetList())
        {
            if (worldObject is not PlayerFSM player || player == null || player.IsMine)
            {
                continue;
            }

            RectTransform pin = Instantiate(otherPinTemplate, otherPinTemplate.parent);
            var photonPlayer = PhotonNetwork.CurrentRoom?.GetPlayer(player.Owner);
            pin.GetComponentInChildren<TMP_Text>(true).text = photonPlayer != null ? photonPlayer.NickName : string.Empty;
            otherPins.Add((player, pin));
        }
    }

    // 나간 플레이어는 오브젝트가 파괴되어 null이 되므로 핀이 꺼진다.
    private void RefreshPins()
    {
        if (target == null)
        {
            target = FindLocalPlayer();
        }

        PlacePin(playerPin, target);

        foreach (var (player, pin) in otherPins)
        {
            PlacePin(pin, player != null ? player.transform : null);
        }
    }

    private void PlacePin(RectTransform pin, Transform worldTarget)
    {
        if (pin == null)
        {
            return;
        }

        Vector2 imageUV = default;
        bool visible = worldTarget != null && worldTarget.gameObject.activeInHierarchy &&
                       worldRoot != null && calibration != null &&
                       calibration.TryGetMapUV(ToWorldXZ(worldTarget), out imageUV);

        if (visible)
        {
            pin.anchorMin = imageUV;
            pin.anchorMax = imageUV;
            pin.anchoredPosition = Vector2.zero;
        }

        if (pin.gameObject.activeSelf != visible)
        {
            pin.gameObject.SetActive(visible);
        }
    }

    private Vector2 ToWorldXZ(Transform worldTarget)
    {
        Vector3 localPosition = worldRoot.InverseTransformPoint(worldTarget.position);
        return new Vector2(localPosition.x, localPosition.z);
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
