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
    private static UdpClient _serverUDP;

    private static IPEndPoint udpClientIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
    private static Thread udpConnect;
    private static byte[] udpResult = new byte[1024];
    private static int udpSendCount;

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

    private static void _Await()
    {
        Socket tcpClient = null;
        UdpClient udpClient = null;

        while (true)
        {
            try
            {
                //同步等待
                tcpClient = _serverTCP.Accept();

                //獲取client端
                string tcpEndPoint = tcpClient.RemoteEndPoint.ToString();
                
                //接收模式為(tcp, udp)
                Player player = new Player(tcpClient, udpClient);
                players.Add(player);

                Console.WriteLine($"{player.tcpSocket.RemoteEndPoint} TCP連線成功");

                ParameterizedThreadStart tcpReceiveMethod = new ParameterizedThreadStart(_tcpReceive);
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

    private static void _udpReceive(object obj)
    {
        Player player = obj as Player;
        Socket udpClient = player.udpSocket;

        while (true)
        {
            //解析數據包
            byte[] data = new byte[4];

            int length = 0;
            MessageType type = MessageType.None;
            int receive = 0;

            try
            {
                receive = udpClient.Receive(data);//同步接收消息
            }
            catch (Exception e)
            {
                Console.WriteLine($"{udpClient.RemoteEndPoint}已經斷線:{e.Message}");
                player.OffLine();
                return;
            }

            //包頭接收不完整
            if (receive < data.Length)
            {
                Console.WriteLine($"{udpClient.RemoteEndPoint}已經斷線 包頭接收不完整");
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
                    Console.WriteLine($"{udpClient.RemoteEndPoint}已經斷線:{e.Message}");
                    player.OffLine();
                    return;
                }
            }

            //如果有包頭
            if (length - 4 > 0)
            {
                data = new byte[length - 4];
                receive = udpClient.Receive(data);
                if (receive < data.Length)
                {
                    Console.WriteLine($"{udpClient.RemoteEndPoint}已經斷線");
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

        players = new List<Player>();

        IPEndPoint point = new IPEndPoint(IPAddress.Parse(ip), 62222);
       
        _serverTCP.Bind(point);

        //暫時連接佇列的最大長度 0貌似表示為開啟正常 或預設? 或無限大?
        _serverTCP.Listen(0);

        Thread t = new Thread(_Await) { IsBackground = true };
        t.Start();

        Thread handle = new Thread(_Callback) { IsBackground = true };
        handle.Start();


        _serverUDP = new UdpClient(62222);
        
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
            _player.tcpSocket.Send(bytes);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            _player.OffLine();
        }
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

    #region UDP


    private static Dictionary<string, UdpClient> udpClientDic;

    public static void udpInitial()
    {
        IPEndPoint ipEnd = new IPEndPoint(IPAddress.Parse("192.168.88.53"), 62222);
        _serverUDP = new UdpClient(ipEnd);
        udpClientDic = new Dictionary<string, UdpClient>();
        udpSendCount = 0;

        udpClientIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
        Console.WriteLine("UDP等待數據連接");

        udpConnect = new Thread(new ThreadStart(_udpReceive()));
        udpConnect.Start();

        System.Timers.Timer t = new System.Timers.Timer(3000);
        t.Elapsed += new System.Timers.ElapsedEventHandler();
        t.AutoReset = true;
        t.Enabled = true;
    }

    public void SendToClient(object source, System.Timers.ElapsedEventArgs e)
    {
        Send(GetAllClientMessage());
    }

    public void Send(string data)
    {
        if (m_clientMessageDic == null || m_clientMessageDic.Count == 0)
        {
            return;
        }
        try
        {
            NetBufferWriter writer = new NetBufferWriter();
            writer.WriteString(data);
            byte[] msg = writer.Finish();
            foreach (var point in m_clientMessageDic)
            {
                Console.WriteLine("send to  " + point.Key + "  " + data);
                m_udpClient.Send(msg, writer.finishLength, point.Value.clientIPEndPoint);
            }
            m_sendCount++;
        }
        catch (Exception ex)
        {
            Console.WriteLine("send error   " + ex.Message);
        }
    }

    //服务器接收
    void Receive()
    {
        while (true)
        {
            try
            {
                m_result = new byte[1024];
                m_result = m_udpClient.Receive(ref m_clientIpEndPoint);
                NetBufferReader reader = new NetBufferReader(m_result);
                string data = reader.ReadString();
                string clientId = string.Format("{0}:{1}", m_clientIpEndPoint.Address, m_clientIpEndPoint.Port);

                if (data.Equals(SocketDefine.udpConnect))
                {
                    AddNewClient(clientId, new ClientMessage(clientId, m_clientIpEndPoint, data));
                }
                else if (data.Equals(SocketDefine.udpDisconnect))
                {
                    RemoveClient(clientId);
                }
                else
                {
                    if (m_clientMessageDic != null && m_clientMessageDic.ContainsKey(clientId))
                    {
                        m_clientMessageDic[clientId].recieveMessage = data;
                    }
                }

                Console.WriteLine(m_clientIpEndPoint + "  数据内容：{0}", data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("receive error   " + ex.Message);
            }
        }
    }

    //连接关闭
    void Close()
    {
        //关闭线程
        if (m_connectThread != null)
        {
            m_connectThread.Interrupt();
            //Thread abort is not supported on this platform.
            //m_connectThread.Abort();
            m_connectThread = null;
        }

        m_clientMessageDic.Clear();

        if (m_udpClient != null)
        {
            m_udpClient.Close();
            m_udpClient.Dispose();
        }
        Console.WriteLine("断开连接");
    }

    void AddNewClient(string id, ClientMessage msg)
    {
        if (m_clientMessageDic != null && !m_clientMessageDic.ContainsKey(id))
        {
            m_clientMessageDic.Add(id, msg);
        }
    }

    void RemoveClient(string id)
    {
        if (m_clientMessageDic != null && m_clientMessageDic.ContainsKey(id))
        {
            m_clientMessageDic.Remove(id);
        }
    }

    string GetAllClientMessage()
    {
        string allMsg = "m_sendCount    " + m_sendCount + "\n";
        foreach (var msg in m_clientMessageDic)
        {
            allMsg += (msg.Value.clientId + "->" + msg.Value.recieveMessage + "\n");
        }
        return allMsg;
    }


    public static void udpSend(this Player _player, MessageType _type, byte[] _data = null)
    {
        //封裝訊息
        byte[] bytes = _Pack(_type, _data);

        //發送訊息
        try
        {
            _player.udpSocket.Send(bytes);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            _player.OffLine();
        }
    }
    #endregion
}