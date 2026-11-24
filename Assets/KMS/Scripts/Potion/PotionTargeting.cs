// 포션 효과를 누가 적용할지, 대상이 본인·아군·적·채집물 중 무엇인지 판정한다
using Photon.Pun;
using UnityEngine;

public static class PotionTargeting
{
    // 이 클라이언트가 대상의 결정권자인지 확인한다 (적 = 방장, 플레이어·분신 = 소유자)
    public static bool HasAuthority(Entity entity)
    {
        if (!PhotonNetwork.InRoom) return true;
        if (entity is PlayerFSM player) return player.IsMine;
        if (entity is PlayerClone clone) return clone.IsMine;
        return PhotonNetwork.IsMasterClient;
    }

    // 던진 사람은 Self, 다른 플레이어는 Ally, 몬스터는 Enemy, 그 외 Entity는 Gatherable로 본다
    // 분신은 소환한 플레이어와 같은 관계로 본다 (내 분신 = Self, 남의 분신 = Ally)
    public static PotionTargetFlags GetRelation(Entity entity, GameObject thrower)
    {
        if (entity is PlayerClone clone)
            return clone.Summoner != null && clone.Summoner.gameObject == thrower ? PotionTargetFlags.Self : PotionTargetFlags.Ally;
        if (thrower != null && entity != null && entity.gameObject == thrower) return PotionTargetFlags.Self;
        if (entity is PlayerFSM) return PotionTargetFlags.Ally;
        if (entity is Enemy) return PotionTargetFlags.Enemy;
        return PotionTargetFlags.Gatherable;
    }
}
