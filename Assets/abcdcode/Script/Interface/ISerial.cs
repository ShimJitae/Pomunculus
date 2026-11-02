public interface ISerial
{
    public byte[] Write();
    public void Read(byte[] _data,int offset);
}