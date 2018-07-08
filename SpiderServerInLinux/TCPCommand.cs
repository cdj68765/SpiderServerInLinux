using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SpiderServerInLinux
{
    internal class TCPCommand
    {
        private readonly CancellationTokenSource CancelInfo = new CancellationTokenSource();
        private readonly Socket socket;

        private TCPCommand(Socket socket)
        {
            this.socket = socket;
        }

        internal static TCPCommand Init(int port)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp); //建立流连接
            socket.Bind(new IPEndPoint(IPAddress.Any, port)); //绑定地址
            socket.Listen(10);
            return new TCPCommand(socket);
        }

        internal void StartListener()
        {
            Loger.Instance.Info("等待监听");
            var send = socket.Accept(); //就让线程卡在这里
            Loger.Instance.Info($"{send.RemoteEndPoint}Connection");
            WaitCmd(send);
        }

        private void WaitCmd(Socket send)
        {
            try
            {
                while (true)
                {
                    var array = new byte[1024];
                    var DataSize = send.Receive(array);
                }
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionReset) //假如错误是远程连接断开
                {
                    Console.WriteLine("远程连接断开,重启监听");
                    StartListener(); //就重启连接
                }
            }
        }
    }
}