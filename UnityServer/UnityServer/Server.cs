using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityServerDLL;

public delegate void ServerCallBack(Player client, byte[] data);

public class CallBack
{
    public Player player;

    public byte[] data;

    public ServerCallBack serverCallBack;

    public CallBack(Player _player, byte[] _data, ServerCallBack _serverCallBack)
    {
        player = _player;
        data = _data;
        serverCallBack = _serverCallBack;
    }

    public void Exucute()
    {
        serverCallBack(player, data);
    }
}