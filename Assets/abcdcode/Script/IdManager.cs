public class IdManager : Singleton<IdManager>
{
    public int Next
    {
        get
        {
            return id++;
        }
    }
    public void SetLastIndex(int i)
    {
        id = i;
    }
    public int Current=>id;
    private int id = 0;
}