using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityServerDLL;
using UnityServerDLL.VolleyBall;

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

public static class Server
{
    public static Dictionary<int, Room> rooms;//房間
    public static Dictionary<MessageType, ServerCallBack> _callBacks = new Dictionary<MessageType, ServerCallBack>();//訊息類型與回調方法

    public static List<Player> players;

    public static ConcurrentQueue<CallBack> _callBackQueue;

    private static Socket _serverTCP;
    private static Socket _serverUDP;

    #region Thread相關
    private static void _Callback()
    {
        while (true)
        {
            //如果隊列中有東西
            if (_callBackQueue.Count > 0)
            {
                if (_callBackQueue.TryDequeue(out CallBack callBack))
                {
                    //執行回調
                    callBack.Exucute();
                }
                //讓出Thread
                Thread.Sleep(10);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private static void _Await()
    {
        Socket tcpClient = null;
        Socket udpClient = null;

        while (true)
        {
            try
            {
                tcpClient = _serverTCP.Accept();
                udpClient = _serverUDP.Accept();

                string tcpEndPoint = tcpClient.RemoteEndPoint.ToString();
                string udpEndPoint = udpClient.RemoteEndPoint.ToString();

                //接收模式為(tcp, udp)
                Player player = new Player(tcpClient, udpClient);
                players.Add(player);

                Console.WriteLine($"{player.tcpSocket.RemoteEndPoint} TCP連線成功");
                Console.WriteLine($"{player.udpSocket.RemoteEndPoint} UDP連線成功");

                ParameterizedThreadStart receiveMethod = new ParameterizedThreadStart(_Receive);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    private static void _tcpReceive(object obj)
    {
        Player player = obj as Player;
        Socket tcpClient = player.tcpSocket;

        while (true)
        {
            //解析數據包
            byte[] data = new byte[4];

            int length = 0;
            MessageType type = MessageType.None;
            int receive = 0;

            try
            {
                receive = tcpClient.Receive(data);//同步接收消息
            }
            catch (Exception e)
            {
                Console.WriteLine($"{tcpClient.RemoteEndPoint}已經斷線:{e.Message}");
                player.OffLine();
                return;
            }

            //包頭接收不完整
            if (receive < data.Length)
            {
                Console.WriteLine($"{tcpClient.RemoteEndPoint}已經斷線 包頭接收不完整");
                player.OffLine();
                return;
            }

            using (MemoryStream stream = new MemoryStream(data))
            {
                BinaryReader binary = new BinaryReader(stream, Encoding.UTF8);
                try
                {
                    length = binary.ReadInt16();
                    type = (MessageType)binary.ReadUInt16();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{tcpClient.RemoteEndPoint}已經斷線:{e.Message}");
                    player.OffLine();
                    return;
                }
            }

            //如果有包頭
            if (length - 4 >0)
            {
                data = new byte[length - 4];
                receive = tcpClient.Receive(data);
                if (receive < data.Length)
                {
                    Console.WriteLine($"{tcpClient.RemoteEndPoint}已經斷線");
                    player.OffLine();
                    return;
                }
            }
            else
            {
                data = new byte[0];
                receive = 0;
            }
            Console.WriteLine($"接收到消息, 房間數量:{rooms.Count}, 玩家數量{players.Count}");

            //執行回調事件
            if (_callBacks.ContainsKey(type))
            {
                CallBack callBack = new CallBack(player, data, _callBacks[type]);
                //放入回調執行thread
                _callBackQueue.Enqueue(callBack);
            }
        }
    }
    #endregion

    public static void Start(string ip)
    {
        _callBackQueue = new ConcurrentQueue<CallBack>();

        rooms = new Dictionary<int, Room>();

        _serverTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _serverUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        players = new List<Player>();

        IPEndPoint point = new IPEndPoint(IPAddress.Parse(ip), 62222);

        _serverTCP.Bind(point);
        _serverUDP.Bind(point);

        //暫時連接佇列的最大長度 0貌似表示為開啟正常
        _serverTCP.Listen(0);
        _serverUDP.Listen(0);

        Thread t = new Thread(_Await) { IsBackground = true };
        t.Start();

        Thread handle = new Thread(_Callback) { IsBackground = true };
        handle.Start();
    }

    /// <summary>
    /// 註冊訊息回調事件
    /// </summary>
    /// <param name="type"></param>
    /// <param name="method"></param>
    public static void Register(MessageType type, ServerCallBack method)
    {
        if (!_callBacks.ContainsKey(type))
        {
            _callBacks.Add(type, method);
        }
        else
        {
            Console.WriteLine("註冊了相同的回調事件");
        }
    }

    public static void tcpSend(this Player _player, MessageType _type, byte[] _data = null)
    {
        //封裝訊息
        byte[] bytes = _Pack(_type, _data);

        //發送訊息
        try
        {

        }
        catch (Exception)
        {

            throw;
        }
    }

    public static void udpSend()
    {

    }

    /// <summary>
    /// Server接受玩家請求失敗時 玩家斷線 關閉房間
    /// </summary>
    /// <param name="_player"></param>
    public static void OffLine(this Player _player)
    {
        players.Remove(_player);

        //有人斷線就直接把房間關掉
        if (_player.inRoom)
        {
            rooms[_player.roomID].Close();
        }
    }

    /// <summary>
    /// 封裝數據
    /// </summary>
    /// <param name="type"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    private static byte[] _Pack(MessageType type, byte[] data = null)
    {
        MessagePacker packer = new MessagePacker();

        if (data != null)
        {
            packer.Add((ushort)(4 + data.Length));//訊息長度
            packer.Add((ushort)type);//訊息類型
            packer.Add(data);//訊息內容
        }
        else
        {
            packer.Add(4);//消息長度
            packer.Add((ushort)type);//消息類型
        }

        return packer.Package;
    }
}