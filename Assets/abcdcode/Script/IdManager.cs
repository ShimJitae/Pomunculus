public class IdManager : Singleton<IdManager>
{
    public int Next
    {
        get
        {
            return id++;
        }
    }
    private int id = 0;
}