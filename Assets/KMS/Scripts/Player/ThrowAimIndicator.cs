using Pomunculus.Items;
using UnityEngine;

// Shift(Aim)를 누르는 동안 포션 궤적과 착탄 범위를 내 화면에만 표시한다. 그리기는 ThrowTrajectoryPreview가 한다
[RequireComponent(typeof(ThrowTrajectoryPreview))]
public class ThrowAimIndicator : MonoBehaviour
{
    private PlayerFSM player;
    private ThrowTrajectoryPreview preview;

    void Awake()
    {
        player = GetComponent<PlayerFSM>();
        preview = GetComponent<ThrowTrajectoryPreview>();
    }

    // 상태·입력이 이번 프레임에 반영된 뒤 그리도록 LateUpdate에서 갱신한다
    void LateUpdate()
    {
        if (!TryGetAimPoint(player, out Vector3 mousePoint))
        {
            preview.Hide();
            return;
        }

        Vector3 goal = player.Thrower.ClampToThrowRange(mousePoint);
        PotionData potion = player.Actions.GetThrowablePotion();
        preview.Show(player.Thrower, player.Thrower.GetThrowOrigin(), goal, potion);
    }

    // 이 플레이어가 내 캐릭터이고 Shift로 조준 중이면 마우스가 가리키는 지점을 돌려준다 (분신도 같은 조건을 쓴다)
    public static bool TryGetAimPoint(PlayerFSM aimingPlayer, out Vector3 mousePoint)
    {
        mousePoint = default;
        return aimingPlayer != null && aimingPlayer.IsMine && !aimingPlayer.IsDead
               && aimingPlayer.Input.IsAimHeld
               && aimingPlayer.Input.TryGetMouseWorldPoint(out mousePoint);
    }
}
