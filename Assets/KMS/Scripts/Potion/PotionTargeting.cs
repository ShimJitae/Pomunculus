// 포션 효과를 누가 적용할지, 대상이 아군인지 적인지 판정한다
using Photon.Pun;

public static class PotionTargeting
{
    // 이 클라이언트가 대상의 결정권자인지 확인한다 (적 = 방장, 플레이어 = 본인)
    public static bool HasAuthority(Entity entity)
    {
        if (!PhotonNetwork.InRoom) return true;
        if (entity is PlayerFSM player) return player.IsMine;
        return PhotonNetwork.IsMasterClient;
    }

    // 플레이어(던진 사람 포함)는 아군, 그 외는 적으로 본다
    public static PotionTargetFlags GetRelation(Entity entity)
        => entity is PlayerFSM ? PotionTargetFlags.Ally : PotionTargetFlags.Enemy;
}