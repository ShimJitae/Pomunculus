using System;
using UnityEngine;
public class GameManager : SingletonBehavior<GameManager>
{
    public override void Awake()
    {
        base.Awake();
        UserProfileInit();
    }
    public void UserProfileInit()
    {
        if(SaveManager.Instance.Load(UserProfile) == null)
        {
            var uid = Guid.NewGuid().ToString();
            string userName = "Sans";
            UserProfile up = new()
            {
                UID = uid,
                UserName = userName
            };
            var d = SaveData.CreateSaveData(up);
            SaveManager.Instance.Save(UserProfile,d);
            /*
            d.Child["uid"] = SaveData.CreateSaveData(uid);
            d.Child["userName"] = SaveData.CreateSaveData(userName);
            */
        }
    }

    [SerializeField]private int m_version;
    public int Version => m_version;
    public const string UserProfile = "UserProfile";
}
public class UserProfile
{
    public string UID{get;set;}
    public string UserName{get;set;}
}