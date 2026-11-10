// RaiseEventManager 패킷 맨 앞에 쓰는 id (RaiseEventCode와 별개)
// 새 패킷을 추가할 때는 여기에 추가해서 번호가 겹치지 않게 관리
public enum WorldEventID : int
{
    UpdatePosition = 0,     // RaiseEventUtil

    RequestSnapshot = 10,   // WorldManager
    WorldSnapshot = 11,
    SpawnObj = 12,
    DespawnObj = 13,

    AnimatorSync = 14,      // WorldAnimatorSync
    TransformSync = 15,     // WorldTransformSync
    AnimatorState = 17,     // WorldAnimatorSync (상태 재생)

    PlayerSpawnRequest = 16, // GameManager

    WorldRPC = 18,          // RPCHelper (IWorldRPC)

    SpawnRequest = 19,      // WorldManager (방장이 아닌 플레이어의 생성 요청)
}
