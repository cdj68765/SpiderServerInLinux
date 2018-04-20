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
        {
            //创建实例
            Socket socketClient = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPAddress ip = IPAddress.Parse("163.43.82.143");
            IPEndPoint point = new IPEndPoint(ip, 31998);
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
            }
        }
    }
}
