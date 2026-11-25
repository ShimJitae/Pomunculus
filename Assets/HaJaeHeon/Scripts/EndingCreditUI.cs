using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 엔딩 크레딧. 열리면 잠시 대기 후 천천히 스크롤하고,
/// 좌클릭을 누르고 있으면 빨라진다. 끝에 도달하면 버튼이 페이드인된다.
/// 크레딧 동안 로컬 플레이어 입력과 스크롤뷰 휠·드래그는 막는다.
/// </summary>
public class EndingCreditUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private ScrollRect creditScrollRect;
    [Tooltip("스크롤이 끝나면 나타날 버튼의 CanvasGroup")]
    [SerializeField] private List<CanvasGroup> endButtonCanvasGroupList;

    [Header("스크롤")]
    [Tooltip("열린 뒤 스크롤이 시작되기까지 대기 시간(초)")]
    [SerializeField] private float startDelay = 2f;
    [Tooltip("초당 내려가는 거리(픽셀)")]
    [SerializeField] private float scrollSpeed = 60f;
    [Tooltip("마우스 왼쪽 버튼을 누르고 있는 동안 곱할 속도 배율")]
    [SerializeField] private float fastScrollMultiplier = 4f;

    [Header("버튼 연출")]
    [Tooltip("버튼 페이드인 시간(초)")]
    [SerializeField] private float buttonFadeDuration = 0.5f;

    private RectTransform contentRect;
    private RectTransform viewportRect;
    private Coroutine scrollCoroutine;
    private Tween buttonFadeTween;
    //private PlayerFSM localPlayer;

    private void Awake()
    {
        contentRect = creditScrollRect.content;
        viewportRect = creditScrollRect.viewport != null
            ? creditScrollRect.viewport
            : (RectTransform)creditScrollRect.transform;

        // 휠·드래그 입력 차단. 위치는 코드에서 직접 움직인다
        creditScrollRect.enabled = false;
    }

    private void OnEnable()
    {
        HideEndButton();
        SetContentScrollY(0f);   // 맨 위에서 시작

        //localPlayer = FindLocalPlayer();
        //SetPlayerInputBlocked(true);

        scrollCoroutine = StartCoroutine(ScrollCreditRoutine());
    }

    private void OnDisable()
    {
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
        }

        buttonFadeTween?.Kill();
        buttonFadeTween = null;

        SetContentScrollY(0f);

        //SetPlayerInputBlocked(false);
        //localPlayer = null;
    }

    // 대기 후 끝까지 스크롤하고, 끝에 도달하면 버튼을 보여준다
    private IEnumerator ScrollCreditRoutine()
    {
        yield return new WaitForSecondsRealtime(startDelay);

        Canvas.ForceUpdateCanvases();   // ContentSizeFitter로 바뀐 높이를 반영
        float maxScrollY = Mathf.Max(0f, contentRect.rect.height - viewportRect.rect.height);
        float currentScrollY = 0f;

        while (currentScrollY < maxScrollY)
        {
            float currentSpeed = scrollSpeed;
            if (IsFastScrollHeld())
            {
                currentSpeed *= fastScrollMultiplier;
            }

            currentScrollY = Mathf.Min(currentScrollY + currentSpeed * Time.unscaledDeltaTime, maxScrollY);
            SetContentScrollY(currentScrollY);
            yield return null;
        }

        scrollCoroutine = null;
        ShowEndButton();
    }

    // 마우스 왼쪽 버튼(button 0)을 누르고 있는지
    private bool IsFastScrollHeld()
    {
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    private void SetContentScrollY(float scrollY)
    {
        contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, scrollY);
    }

    private void HideEndButton()
    {
        buttonFadeTween?.Kill();

        foreach (CanvasGroup endButtonCanvasGroup in endButtonCanvasGroupList)
        {
            endButtonCanvasGroup.alpha = 0f;
            endButtonCanvasGroup.interactable = false;
            endButtonCanvasGroup.blocksRaycasts = false;
            endButtonCanvasGroup.gameObject.SetActive(false);
        }
    }

    // 버튼을 페이드인하고, 다 보인 뒤에 클릭을 허용한다
    private void ShowEndButton()
    {
        foreach (CanvasGroup endButtonCanvasGroup in endButtonCanvasGroupList)
        {
            endButtonCanvasGroup.gameObject.SetActive(true);
            buttonFadeTween = endButtonCanvasGroup
                .DOFade(1f, buttonFadeDuration)
                .SetUpdate(true)   // timeScale 무시
                .OnComplete(() =>
                {
                    endButtonCanvasGroup.interactable = true;
                    endButtonCanvasGroup.blocksRaycasts = true;
                    buttonFadeTween = null;
                });
        }
    }

    //// 이 클라이언트가 조작하는 플레이어를 찾는다 (열릴 때 한 번만 호출)
    //private PlayerFSM FindLocalPlayer()
    //{
    //    PlayerFSM[] players = FindObjectsByType<PlayerFSM>(FindObjectsSortMode.None);
    //    foreach (PlayerFSM player in players)
    //    {
    //        if (player.IsMine)
    //        {
    //            return player;
    //        }
    //    }
    //    return null;
    //}

    //// 크레딧 동안 로컬 플레이어 입력을 막는다
    //private void SetPlayerInputBlocked(bool isBlocked)
    //{
    //    if (localPlayer == null) return;

    //    if (isBlocked)
    //    {
    //        localPlayer.Input.ClearMoveCommand();   // 이미 버퍼에 들어간 우클릭 이동 제거
    //        localPlayer.Motor.StopMoving();         // 이동 중이었다면 정지
    //    }

    //    localPlayer.Input.enabled = !isBlocked;
    //}
}