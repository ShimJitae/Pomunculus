using System;
using Photon.Pun;
using UnityEngine;
public class GameManager : SingletonBehavior<GameManager>
{
    public override void Awake()
    {
        base.Awake();
        UserProfileInit();
        NetworkManager.Instance.Init();
        PhotonNetwork.PrefabPool = new NetworkPrefabPool(Resources.Load<NetworkPrefabCatalog>("NetworkPrefabCatalog"));

    }
    public void UserProfileInit()
    {
        if(SaveManager.Instance.Load(UserProfile) == null)
        {
            var uid = Guid.NewGuid().ToString();
            string userName = "Sans";
            LocalUserProfile = new()
            {
                UID = uid,
                UserName = userName
            };
            //SaveProfile();
            /*
            d.Child["uid"] = SaveData.CreateSaveData(uid);
            d.Child["userName"] = SaveData.CreateSaveData(userName);
            */
        }
        else
        {
            LocalUserProfile = SaveManager.Instance.Load(UserProfile).GetData<UserProfile>();
        }
    }
    public void SaveProfile()
    {
        var d = SaveData.CreateSaveData(LocalUserProfile);
        SaveManager.Instance.Save(UserProfile,d);
    }

    [SerializeField]private int m_version;
    private UserProfile m_profile;
    public UserProfile LocalUserProfile
    {
        get
        {
            return m_profile.Copy();
        }
        private set
        {
            m_profile = value;
            SaveProfile();
        }
    }
    public int Version => m_version;
    public const string UserProfile = "UserProfile";
}
public class UserProfile
{
    public string UID{get;set;}
    public string UserName{get;set;}
    public UserProfile Copy()
    {
        return new()
        {
            UID = this.UID,
            UserName = this.UserName
        };
    }
}