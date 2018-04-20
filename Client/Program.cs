using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {/*
            //创建实例
            Socket socketClient = new Socket(SocketType.Stream, ProtocolType.Tcp);
            //IPAddress ip = IPAddress.Parse("163.43.82.143");
           // IPEndPoint point = new IPEndPoint(ip, 31998);
            IPAddress ip = IPAddress.Parse("127.0.0.1");
           IPEndPoint point = new IPEndPoint(ip, 1000);
            //进行连接
            socketClient.Connect(point);
            //不停的接收服务器端发送的消息
            ThreadPool.QueueUserWorkItem(obj =>
            {
                while (true)
                {
                    //获取发送过来的消息
                    byte[] buffer = new byte[1024 * 1024 * 2];
                    var effective = socketClient.Receive(buffer);
                    if (effective == 0)
                    {
                        break;
                    }
                    var str = Encoding.UTF8.GetString(buffer, 0, effective);
                    Console.WriteLine(str);
                }
            });
            while (true)
            {
                var buffter = Encoding.UTF8.GetBytes(Console.ReadLine());
                var temp = socketClient.Send(buffter);
            }*/
            byte[] data = new byte[1024];
            string input, stringData;

            //构建TCP 服务器
            Console.WriteLine("This is a Client, host name is {0}", Dns.GetHostName());

            //设置服务IP，设置TCP端口号
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1100);

            //定义网络类型，数据连接类型和网络协议UDP
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            string welcome = "Hello! ";
            data = Encoding.ASCII.GetBytes(welcome);
            server.SendTo(data, data.Length, SocketFlags.None, ip);
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint Remote = (EndPoint)sender;

            data = new byte[1024];
            //对于不存在的IP地址，加入此行代码后，可以在指定时间内解除阻塞模式限制
            int recv = server.ReceiveFrom(data, ref Remote);
            Console.WriteLine("Message received from {0}: ", Remote.ToString());
            Console.WriteLine(Encoding.ASCII.GetString(data, 0, recv));
            while (true)
            {
                input = Console.ReadLine();
                if (input == "exit")
                    break;
                server.SendTo(Encoding.ASCII.GetBytes(input), Remote);
                data = new byte[1024];
                recv = server.ReceiveFrom(data, ref Remote);
                stringData = Encoding.ASCII.GetString(data, 0, recv);
                Console.WriteLine(stringData);
            }
            Console.WriteLine("Stopping Client.");
            server.Close();
        }
    }
}
