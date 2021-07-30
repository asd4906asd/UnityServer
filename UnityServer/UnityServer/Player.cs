using System.Net.Sockets;

/// <summary>
/// 玩家資料類別
/// 一個實體代表一個玩家
/// </summary>
public class Player
{
    public Socket tcpSocket;
    public UdpClient udpSocket;

    public string playerName;

    public bool inRoom;

    public int roomID;

    /// <summary>
    /// 建構子
    /// </summary>
    public Player(Socket _tcp, UdpClient _udp)
    {
        tcpSocket = _tcp;
        udpSocket = _udp;
        playerName = "Player Unknown";
        inRoom = false;
        roomID = 0;
    }

    public void EnterRoom(int _roomID)
    {
        inRoom = true;
        roomID = _roomID;
    }

    public void ExitRoom()
    {
        inRoom = false;
        roomID = 0;
    }
}