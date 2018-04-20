using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpiderServerInLinux
{
    class Program
    {
        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
            public string[] Phones { get; set; }
            public bool IsActive { get; set; }
        }

        static void Main(string[] args)
        {
            /*CancellationTokenSource CancelInfo = new CancellationTokenSource();
            Task.Factory.StartNew(delegate
            {
                Socket socket = new Socket(1, 6);
                IPAddress any = IPAddress.Any;
                IPEndPoint iPEndPoint = new IPEndPoint(any, 1000);
                socket.Bind(iPEndPoint);
                Console.WriteLine("Listen Success");
                socket.Listen(10);
                while (true)
                {
                    Socket send = socket.Accept();
                    string arg = send.get_RemoteEndPoint().ToString();
                    Console.WriteLine(string.Format("{0}Connection", arg));
                    ThreadPool.QueueUserWorkItem(delegate (object obj)
                    {
                        while (true)
                        {
                            byte[] array = new byte[2097152];
                            int num = send.Receive(array);
                            bool flag = num == 0;
                            if (flag)
                            {
                                break;
                            }
                            string @string = Encoding.get_UTF8().GetString(array, 0, num);
                            Console.WriteLine(@string);
                            byte[] bytes = Encoding.get_UTF8().GetBytes("Server Return Message");
                            bool flag2 = @string == "exit";
                            if (flag2)
                            {
                                CancelInfo.Cancel();
                            }
                            send.Send(bytes);
                        }
                    }, CancelInfo.Token);
                }
            }, CancelInfo.Token, 2, TaskScheduler.Default);
            while (!CancelInfo.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }*/


            var CancelInfo = new CancellationTokenSource();


            Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            //socket绑定监听地址
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 1000));
            Console.WriteLine("Listen Success");
            //设置同时连接个数
            serverSocket.Listen(10);
            //等待连接并且创建一个负责通讯的socket

            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.Completed += (obj,acceptEventArgs)=>
            {
                Console.WriteLine("Client connection accepted. Local Address: {0}, Remote Address: {1}",
        acceptEventArgs.AcceptSocket.LocalEndPoint, acceptEventArgs.AcceptSocket.RemoteEndPoint);
            };
            serverSocket.AcceptAsync(e);
         /*   serverSocket.BeginAccept(ar =>
            {

                        //初始化一个SOCKET，用于其它客户端的连接
                        Socket server1 = (Socket)ar.AsyncState;
                var Client = server1.EndAccept(ar);
                        //获取链接的IP地址
                        var sendIpoint = Client.RemoteEndPoint.ToString();
                Console.WriteLine($"{sendIpoint}Connection");
                byte[] buffer = new byte[10];
                var save =  new List<ArraySegment<byte>>() { new byte[1024] };
                Client.BeginReceive(save,SocketFlags.None, GetRec, Client);
                void GetRec(IAsyncResult get)
                {
                    Socket ts = (Socket)get.AsyncState;
                    var c = ts.EndReceive(get);
                    get.AsyncWaitHandle.Close();
                    var str = Encoding.UTF8.GetString(buffer);
                    Console.WriteLine(str);
                    ts.BeginReceive(buffer, 0, buffer.Length, 0, GetRec
                , Client);
                };
                        //开启一个新线程不停接收消息
                        /*  ThreadPool.QueueUserWorkItem(obj =>
                          {
                              while (true)
                              {
                                  //获取发送过来的消息容器
                                  byte[] buffer = new byte[1024 * 1024 * 2];
                                  var effective = serverSocket.Receive(buffer);
                                  //有效字节为0则跳过
                                  if (effective == 0)
                                  {
                                      break;
                                  }
                                  var str = Encoding.UTF8.GetString(buffer, 0, effective);
                                  Console.WriteLine(str);
                                  var buffers = Encoding.UTF8.GetBytes("Server Return Message");
                                  if (str == "exit")
                                  {
                                      CancelInfo.Cancel();
                                  }

                                  serverSocket.Send(buffers);

                              }
                          }, CancelInfo.Token);*/

        /*    }
        , serverSocket);*/



            while (!CancelInfo.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }

            /*  using (var db = new LiteDatabase(@"test.db"))
              {
                  // Get customer collection
                  var col = db.GetCollection<Customer>("customers");
                  // Create your new customer instance
                  var customer = new Customer
                  {
                      Name = "John Doe",
                      Phones = new string[] { "8000-0000", "9000-0000" },
                      Age = 39,
                      IsActive = true
                  };

                  // Create unique index in Name field
                  col.EnsureIndex(x => x.Name, true);
                  // Insert new customer document (Id will be auto-incremented)
                  col.Insert(customer);

                  // Update a document inside a collection
                  customer.Name = "Joana Doe";
                  col.Update(customer);
                  // Use LINQ to query documents (with no index)
                  var results = col.Find(x => x.Age > 20);

              }*/
        }

    }
}
