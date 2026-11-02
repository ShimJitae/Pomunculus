using UnityEngine;
public class GameManager : SingletonBehavior<GameManager>
{
    [SerializeField]private int m_version;
    public int Version => m_version;
}