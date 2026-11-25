// 로컬 플레이어의 이동 명령을 구독해 클릭 위치에 마커를 표시한다
using UnityEngine;

public class MoveMarkerBinder : MonoBehaviour
{
    [Tooltip("클릭 위치에 표시할 오브젝트 (이 오브젝트와 별개여야 함)")]
    [SerializeField] private GameObject markerVisual;

    private PlayerInputReader boundInput;

    private void OnEnable()
    {
        LocalPlayerProvider.OnLocalPlayerChanged += Bind;
        Bind(LocalPlayerProvider.Current);
    }

    private void OnDisable()
    {
        LocalPlayerProvider.OnLocalPlayerChanged -= Bind;
        Unbind();
    }

    // 이전 플레이어 구독 해제 후 새 플레이어의 Input.OnMoveCommandIssued 구독 (부활·재접속 대비)
    private void Bind(PlayerFSM player)
    {
        Unbind();
        if (player == null) return;
        boundInput = player.Input;
        boundInput.OnMoveCommandIssued += ShowMarker;
        Debug.Log("무브마커 바인더 연결");

    }

    // boundInput 구독 해제, markerVisual 끄기
    private void Unbind()
    {
        if (boundInput != null)
        {
            boundInput.OnMoveCommandIssued -= ShowMarker;
            boundInput = null;
        }
            
        if (markerVisual != null)
            markerVisual.SetActive(false);
    }
    

    // 위치를 옮기고 껐다 켜서 파티클을 다시 재생한다
    private void ShowMarker(Vector3 position)
    {
        if (markerVisual == null) return;
        markerVisual.transform.position = position;
        markerVisual.SetActive(false);   // 껐다 켜서 파티클 등을 다시 재생
        markerVisual.SetActive(true);
        Debug.Log("무브마커 이벤트 짜잔");
    }
}