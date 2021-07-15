using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

/// <summary>
/// 網路工具類別
/// 序列化 反序列化 獲取IPv4
/// BinarryFormatter因為安全性問題被淘汰 這邊改成JsonSerializer
/// </summary>
public static class NetworkUtils
{
    /// <summary>
    /// 序列化方法
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static byte[] Serialize(object obj)
    {
        if (obj == null || !obj.GetType().IsSerializable)
        {
            return null;
        }

        byte[] data = JsonSerializer.SerializeToUtf8Bytes(obj);

        return data;
    }

    /// <summary>
    /// 反序列化方法
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static T Deserialize<T>(byte[] data) where T : class
    {
        if (data == null || typeof(T).IsSerializable)
        {
            return null;
        }

        var obj = new ReadOnlySpan<byte>(data);
        T D_obj = JsonSerializer.Deserialize<T>(obj);

        return D_obj;
    }

    /// <summary>
    /// 獲得本機IPv4
    /// </summary>
    /// <returns></returns>
    public static string GetLocalIPv4()
    {
        string hostName = Dns.GetHostName();
        IPHostEntry iPEntry = Dns.GetHostEntry(hostName);
        for (int i = 0; i < iPEntry.AddressList.Length; i++)
        {
            if (iPEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
            {
                return iPEntry.AddressList[i].ToString();
            }
        }
        return null;
    }
}