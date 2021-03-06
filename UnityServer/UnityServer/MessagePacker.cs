using System;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// 資料包類別
/// 添加各種類型資料
/// </summary>
public class MessagePacker
{
    private List<byte> bytes = new List<byte>();

    public byte[] Package
    {
        get { return bytes.ToArray(); }
    }

    public MessagePacker Add(byte[] data)
    {
        bytes.AddRange(data);
        return this;
    }

    public MessagePacker Add(ushort value)
    {
        byte[] data = BitConverter.GetBytes(value);
        bytes.AddRange(data);
        return this;
    }

    public MessagePacker Add(uint value)
    {
        byte[] data = BitConverter.GetBytes(value);
        bytes.AddRange(data);
        return this;
    }

    public MessagePacker Add(ulong value)
    {
        byte[] data = BitConverter.GetBytes(value);
        bytes.AddRange(data);
        return this;
    }

    public MessagePacker Add(string value)
    {
        byte[] data = Encoding.UTF8.GetBytes(value);
        bytes.AddRange(data);
        return this;
    }
}