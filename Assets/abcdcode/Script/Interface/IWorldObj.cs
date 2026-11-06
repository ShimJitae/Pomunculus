using Unity.VisualScripting;
using UnityEngine;

public interface IWorldObj : IdOwner,ISavable
{
    public GameObject gameObject{get;}
    public string PrefabId{get;set;} // NetworkPrefabCatalog 이름
    public void InitWorldObj(int id, string prefabId); // WorldManager가 생성 직후 호출
}