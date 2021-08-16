using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;

public class UdpClientMessage
{
    public string clientID;
    public IPEndPoint clientIPEndPoint;
    public string receiveMessage;

    public UdpClientMessage(string ID, IPEndPoint point, string msg)
    {
        clientID = ID;
        clientIPEndPoint = new IPEndPoint(point.Address, point.Port);
        receiveMessage = msg;
    }
}

public class UdpDefine
{
    public const string udpConnect = "Connect";
    public const string udpDisconnect = "Disconnect";
}