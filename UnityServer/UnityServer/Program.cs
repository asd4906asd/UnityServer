using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Windows;
using System.IO;

namespace UnityServer
{
    class Program
    {
        //創建一清單 放客戶端
        static List<Socket> socketList = new List<Socket>();

        //Ip & Port結構
        private struct Internet_Struct
        {
            public string ip;
            public int port;
        }

        static System.Timers.Timer GameTimer;

        static public int GameTimeNum;

        /// <summary>
        /// 這邊是主程式啦 我估計他只會執行一次
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("伺服器啟動...");

            //現在時間
            Program obj = new Program();
            Console.WriteLine("ThreadID-- " + Thread.CurrentThread.ManagedThreadId.ToString());

            //抓取obj的執行時間
            //Thread thread = new Thread(new ThreadStart(obj.Timer));
            Thread thread = new Thread(new ThreadStart(obj.ServerTime));
            thread.Start();

            //設定IP
            Internet_Struct internet;
            internet.ip = "192.168.88.53";
            internet.port = 62222;
            IPAddress ip = new IPAddress(new byte[] { 192, 168, 88, 53 });

            //Socket公式
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);         
            EndPoint point = new IPEndPoint(IPAddress.Parse(internet.ip), internet.port);
            server.Bind(point);
            server.Listen(10);

            //開始非同步作業 做new AsyncCallback(AcceptClient) 對server做
            //應該是非同步的去新增伺服器端的客戶端
            //靠杯喔 AcceptClient在下面  記得看一下
            //開始非同步作業以接受連入的連接嘗試(官方說明)
            server.BeginAccept(new AsyncCallback(AcceptClient), server);

            GameTime();
        }

        //新client端進入時
        static void AcceptClient(IAsyncResult ar)
        {
            Socket myserver = ar.AsyncState as Socket;
            Socket client = myserver.EndAccept(ar);
            Console.WriteLine("有新客戶端連接...");
            socketList.Add(client);
            Console.WriteLine("客戶IP訊息 : " + client.RemoteEndPoint);
            string msg = "系統訊息:歡迎來到UnityServer";
            byte[] data = Encoding.UTF8.GetBytes(msg);
            client.Send(data);
            Thread t = new Thread(ReceiveMsg);
            t.Start(client);
            
            //假設我的假設是對的
            //上面的接收只會做到第一個人被接收
            //所以這邊接收的同時還要再槓一行
            myserver.BeginAccept(new AsyncCallback(AcceptClient), myserver);
        }

        /// <summary>
        /// 接收訊息
        /// </summary>
        /// <param name="socket"></param>
        static void ReceiveMsg(object socket)
        {
            Socket mySocket = socket as Socket;
            while (true)
            {
                byte[] buffer = new byte[1024];
                int length = 0;
                try
                {
                    length = mySocket.Receive(buffer);
                }
                catch (Exception e)
                {
                    Console.WriteLine("例外 : " + e.Message);
                    IPEndPoint point = mySocket.RemoteEndPoint as IPEndPoint;
                    string ipp = point.Address.ToString();
                    Console.WriteLine(ipp + "退出Server");
                    socketList.Remove(mySocket);
                    sendAllMsg(ipp + "有人退出Server");
                    break;
                }

                string resMsg = Encoding.UTF8.GetString(buffer, 0, length);

                IPEndPoint po = mySocket.RemoteEndPoint as IPEndPoint;
                string ip = po.Address.ToString();
                //resMsg = ip + ":" + resMsg;
                
                Console.WriteLine(resMsg);
                sendAllMsg(resMsg);
            }
        }

        //全體發送
        static void sendAllMsg(string resMsg)
        {
            try
            {
                for (int i = 0; i < socketList.Count; i++)
                {
                    socketList[i].Send(Encoding.UTF8.GetBytes(resMsg));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("例外狀況-- " + e);
                throw;
            }
        }

        /// <summary>
        /// 伺服器時間
        /// 這邊不要+static  會無法使用new
        /// </summary>
        void ServerTime()
        {
            while (true)
            {
                Console.WriteLine(DateTime.Now.ToString() + " --Server正常-- " + Thread.CurrentThread.ManagedThreadId.ToString());
                Thread.CurrentThread.Join(5000);
            }
        }

        /// <summary>
        /// 遊戲計時器
        /// </summary>
        static void GameTime()
        {
            int timeStart;
            timeStart = Int32.Parse(Console.ReadLine());
            if (timeStart == 555)
            {
                Console.WriteLine("GameTimeStart");
                GameTimer = new System.Timers.Timer(1000);
                GameTimer.Elapsed += OnTimeEvent;
                GameTimer.AutoReset = true;
                GameTimer.Enabled = true;
            }



            GameTime();
        }

        /// <summary>
        /// 遊戲計時器的事件 Unity端的計時器從這邊取值
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        static void OnTimeEvent(Object source, ElapsedEventArgs e)
        {
            //Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss}", e.SignalTime);
            GameTimeNum += 1;
        }
    }
}