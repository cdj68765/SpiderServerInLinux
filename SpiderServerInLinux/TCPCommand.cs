using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpiderServerInLinux
{
    internal class TCPCommand
    {
        private readonly Socket socket;
        readonly CancellationTokenSource CancelInfo = new CancellationTokenSource();

        private TCPCommand(Socket socket)
        {
            this.socket = socket;
        }

        internal static TCPCommand Init(int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);//建立流连接
            socket.Bind(new IPEndPoint(IPAddress.Any, port));//绑定地址
            socket.Listen(10);
            return  new TCPCommand(socket);
        }

        internal void StartListener()
        {
            Socket send = socket.Accept();//就让线程卡在这里
            Console.WriteLine($"{send.RemoteEndPoint}Connection");
            WaitCmd(send);
        }

        private void WaitCmd(Socket send)
        {
            try
            {
                while (true)
                {
                    byte[] array = new byte[1024];
                    int DataSize = send.Receive(array);
                }
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionReset) //假如错误是远程连接断开
                {
                    StartListener();//就重启连接
                }
            }
  
        }
    }
}
