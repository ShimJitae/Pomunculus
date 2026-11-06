using Photon.Pun;

public interface IPlayerObj : IWorldObj
{
    /// <summary>
    /// 플레이어 ActorNumber로 식별
    /// </summary>
    public int Owner{get;set;}
}