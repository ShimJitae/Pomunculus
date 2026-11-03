using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class PacketData
{
    private static List<byte> buffer = new List<byte>();
    
    public PacketData()
    {
        buffer.Clear();
        offset = 0;
    }
    public PacketData(byte[] data)
    {
        this.data = data;
        offset = 0;
    }
    public void Write(Span<byte> bytes)
{
        
    for (int i = 0; i < bytes.Length; i++)
        buffer.Add(bytes[i]);
        
}
    public void Write<T>(T value) where T : unmanaged
{
    Span<T> span = new T[] { value };
    Write(MemoryMarshal.AsBytes(span));
}

    public void Write(string value)
	{
        var tmp = Encoding.UTF8.GetBytes(value);
        this.Write(tmp.Length);
        this.Write(tmp);
	}
    
    public void Write(Vector2 value)
    {
        this.Write(value.x);
        this.Write(value.y);
    }
    
    public void WriteIList<T>(IList<T> l, Action<T> writer)
    {
        Write(l.Count);
        for (int i = 0; i < l.Count; i++)writer(l[i]);
    }
    public void WriteList<T>(List<T> list, Action<T> writer)
{
    Write(list.Count);
    for (int i = 0; i < list.Count; i++)writer(list[i]);
}
public void WriteArray<T>(T[] array, Action<T> writer)
{
    Write(array.Length);
    for (int i = 0; i < array.Length; i++)writer(array[i]);
}
public void WriteDic<K,V>(Dictionary<K,V>dic, Action<K> w1, Action<V> w2)
    {
        Write(dic.Count);
        foreach(var pair in dic)
        {
            w1(pair.Key);
            w2(pair.Value);
            
        }
    }
public bool ReadBool()
    {
        var result = BitConverter.ToBoolean(data,offset);
        offset += sizeof(bool);
        return result;
    }
    public byte ReadByte()
    {
        var result = data[offset];
        offset += 1;
        return result;
    }
public short ReadShort()
    {
        var result = BitConverter.ToInt16(data,offset);
        offset += sizeof(short);
        return result;
    }
    public ushort ReadUShort()
    {
        var result = BitConverter.ToUInt16(data,offset);
        offset += sizeof(ushort);
        return result;
    }
public int ReadInt()
    {
        var result = BitConverter.ToInt32(data,offset);
        offset += sizeof(int);
        return result;
    }
    public long ReadLong()
    {
        var result = BitConverter.ToInt64(data,offset);
        offset += sizeof(long);
        return result;
    }
    public ulong ReadULong()
    {
        var result = BitConverter.ToUInt64(data,offset);
        offset += sizeof(ulong);
        return result;
    }
    public float ReadFloat()
    {
        var result = BitConverter.ToSingle(data,offset);
        offset += sizeof(float);
        return (float)result;
    }
    public double ReadDouble()
    {
        
        var result = BitConverter.ToDouble(data,offset);
        offset += sizeof(double);
        return result;
    }
    public Vector2 ReadVector2()
    {
        return new Vector2(this,this);
    }
    public string ReadString()
	{
            int num = this;
            var result = Encoding.UTF8.GetString(data, offset, num);
            offset += num;
			return result;
	}
    public List<T> ReadList<T>(Func<T> reader)
    {
        List<T> list = new List<T>();
		int count = this;
		for(int i = 0; i < count;i++)
        {
			T value = reader();
			list.Add(value);
        }
		return list;
    }
    public T[] ReadArray<T>(Func<T> reader)
    {
        int length = this;
        T[] array = new T[length];
        for(int i = 0 ; i<length; i++)
        {
            array[i] = reader();
        }
        return array;
    }
    public Dictionary<K,V> ReadDic<K,V>(Func<K> r1, Func<V> r2)
    {
        Dictionary<K,V> dic = new Dictionary<K, V>();
        int count = this;
        for(int i = 0; i < count;i++)
        {
            dic[r1()] = r2();
        }
        return dic;
    }
public void Save()
    {
        //var p = ArrayPool<byte>.Shared;
        //data = p.Rent(buffer.Count);
        data = new byte[buffer.Count];
        buffer.CopyTo(data);
        //data = buffer.ToArray();
        //buffer.Clear();
        offset = 0;
    }
    public byte[] GetData()
    {
        return data;
    }
    public void Dispose()
    {
        //var p = ArrayPool<byte>.Shared;
        //p.Return(data);
    }
    public bool IsEmpty()
    {
        return data == null || data.Length == 0;
    }
    public static implicit operator bool(PacketData p) => p.ReadBool();
    public static implicit operator byte(PacketData p) => p.ReadByte();
    public static implicit operator short(PacketData p) => p.ReadShort();
    public static implicit operator ushort(PacketData p) => p.ReadUShort();
    public static implicit operator int(PacketData p) => p.ReadInt();
    public static implicit operator long(PacketData p) => p.ReadLong();
    public static implicit operator ulong(PacketData p) => p.ReadULong();
    public static implicit operator float(PacketData p) => p.ReadFloat();
    public static implicit operator double(PacketData p) => p.ReadDouble();
    public static implicit operator string(PacketData p) => p.ReadString();
    public static implicit operator Vector2(PacketData p) => p.ReadVector2();

    public static implicit operator List<bool>(PacketData p) => p.ReadList(p.ReadBool);
    public static implicit operator List<short>(PacketData p) => p.ReadList(p.ReadShort);
    public static implicit operator List<int>(PacketData p) => p.ReadList(p.ReadInt);
    public static implicit operator List<long>(PacketData p) => p.ReadList(p.ReadLong);
    public static implicit operator List<float>(PacketData p) => p.ReadList(p.ReadFloat);
    public static implicit operator List<double>(PacketData p) => p.ReadList(p.ReadDouble);
    public static implicit operator List<string>(PacketData p) => p.ReadList(p.ReadString);
    public static implicit operator Vector2[](PacketData p) => p.ReadArray(p.ReadVector2);
    public static implicit operator Dictionary<int,int>(PacketData p) => p.ReadDic<int,int>(p.ReadInt,p.ReadInt);
    public static implicit operator Dictionary<int,string>(PacketData p) => p.ReadDic<int,string>(p.ReadInt,p.ReadString);
    public static implicit operator Dictionary<string,int>(PacketData p) => p.ReadDic<string,int>(p.ReadString,p.ReadInt);
    public static implicit operator Dictionary<string,string>(PacketData p) => p.ReadDic<string,string>(p.ReadString,p.ReadString);
    //public List<byte> buffer;
    public byte[] data;
    public int offset;
}