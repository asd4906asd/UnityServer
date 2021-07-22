using System;

namespace UnityServer
{
    class Start
    {
        static void Main(string[] args)
        {
            string ip = NetworkUtils.GetLocalIPv4();
            new Network(ip);

            Console.WriteLine("Server啟動");
            Console.WriteLine($"IP位置為 : {ip}");

            Console.ReadKey();
        }
    }
}
