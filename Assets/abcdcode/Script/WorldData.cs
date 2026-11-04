using UnityEngine;

public class WorldData
{
    public ITestData Data{get;set;}
}
public interface ITestData
{
    public float X{get;set;}
    public float Y{get;set;}
    public float Z{get;set;}
}
public class TestObjData : ITestData,IdOwner
{
    public float X{get;set;}
    public float Y{get;set;}
    public float Z{get;set;}

    public int ID{get;set;}
}