using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

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
            var CancelInfo = new CancellationTokenSource();
            /*TcpListener tcpListener = new TcpListener(IPAddress.Any, 1000);
            tcpListener.Start();
            TCPASYNC();
            async void TCPASYNC()
            {
                byte[] buffer = new byte[65535];
                var c = await tcpListener.AcceptTcpClientAsync();//连接请求只会出现一次
                var stream = c.GetStream();//这是一个网络流，从这个网络流可以去的从客户端发来的数据  
                stream.BeginRead(buffer, 0, buffer.Length, ReadAsyncCallBack, null);

                void ReadAsyncCallBack(IAsyncResult asyncResult)
                {
                    var readCount = c.GetStream().EndRead(asyncResult);
                    string @string = Encoding.Default.GetString(buffer, 0, readCount);
                    Console.WriteLine(@string);
                    buffer = new byte[1024];
                    stream.BeginRead(buffer, 0, readCount, ReadAsyncCallBack, null);

                }
            }*/

            Socket newsock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            newsock.Bind(new IPEndPoint(IPAddress.Any, 1100));

            Console.WriteLine("This is a Server, host name is {0}", Dns.GetHostName());

            //等待客户机连接
            Console.WriteLine("Waiting for a client");
            //得到客户机IP
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint Remote = (EndPoint)(sender);
            byte[] data = new byte[1024];
            var   recv = newsock.ReceiveFrom(data, ref Remote);
            Console.WriteLine("Message received from {0}: ", Remote.ToString());
            Console.WriteLine(Encoding.ASCII.GetString(data, 0, recv));
            //客户机连接成功后，发送信息
            string welcome = "Call ! ";
            //字符串与字节数组相互转换
            data = Encoding.Default.GetBytes(welcome);
            newsock.SendTo(data, data.Length, SocketFlags.None, Remote);
            while (true)
            {
                data = new byte[1024];
                //发送接收信息
                recv = newsock.ReceiveFrom(data, ref Remote);

                Console.WriteLine(Encoding.ASCII.GetString(data, 0, recv));
                newsock.SendTo(data, recv, SocketFlags.None, Remote);
            }

            /* Task.Factory.StartNew(()=>
              {
                  Socket socket = new Socket(SocketType.Stream,ProtocolType.Tcp);//建立流连接
                  socket.Bind(new IPEndPoint(IPAddress.Any, 1000));//绑定地址
                  socket.Listen(10);//建立流监听个数
                  while (true)
                  {
                      Socket send = socket.Accept();
                      string arg = send.RemoteEndPoint.ToString();
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
                              string @string = Encoding.Default.GetString(array, 0, num);
                              Console.WriteLine(@string);
                              byte[] bytes = Encoding.Default.GetBytes("Server Return Message");
                              bool flag2 = @string == "exit";
                              if (flag2)
                              {
                                  CancelInfo.Cancel();
                              }
                              send.Send(bytes);
                          }
                      }, CancelInfo.Token);
                  }
              }, CancelInfo.Token,TaskCreationOptions.None, TaskScheduler.Default);*/
            /*    Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                //socket绑定监听地址
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, 1000));
                Console.WriteLine("Listen Success");
                //设置同时连接个数
                serverSocket.Listen(10);
                //等待连接并且创建一个负责通讯的socket

                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
             SocketAsyncEventArgs r = new SocketAsyncEventArgs();
             r.Completed += (obj, acceptEventArgs) =>
               {
                   Console.WriteLine();
               };
             e.Completed += (obj,acceptEventArgs)=>
                {
                    Console.WriteLine("Client connection accepted. Local Address: {0}, Remote Address: {1}",
            acceptEventArgs.AcceptSocket.LocalEndPoint, acceptEventArgs.AcceptSocket.RemoteEndPoint);


                    while (true)
                    {
                        acceptEventArgs.AcceptSocket.ReceiveAsync(r);
                    }


                };
                serverSocket.AcceptAsync(e);*/

            /*  Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
              serverSocket.Bind(new IPEndPoint(IPAddress.Any, 1000));
              serverSocket.Listen(10);
              serverSocket.BeginAccept(ar =>
                 {

                             //初始化一个SOCKET，用于其它客户端的连接
                             Socket server1 = (Socket)ar.AsyncState;
                     var Client = server1.EndAccept(ar);
                             //获取链接的IP地址
                             var sendIpoint = Client.RemoteEndPoint.ToString();
                     Console.WriteLine($"{sendIpoint}Connection");
                     byte[] buffer = new byte[Client.Receive(buffer)];
                     var save =  new List<ArraySegment<byte>>() { new byte[Client.Receive(buffer)] };
                     Client.BeginReceive(save,SocketFlags.None, GetRec, Client);
                     void GetRec(IAsyncResult get)
                     {
                         Socket ts = (Socket)get.AsyncState;
                         var c = ts.EndReceive(get);
                         get.AsyncWaitHandle.Close();
                         var str = Encoding.UTF8.GetString(save);
                         Console.WriteLine(str);
                         ts.BeginReceive(buffer, 0, c, 0, GetRec
                     , Client);
                     };*/
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

            /* }
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
        // Implements the connection logic for the socket server.  
        // After accepting a connection, all data read from the client 
        // is sent back to the client. The read and echo back to the client pattern 
        // is continued until the client disconnects.
     /*   class Server
        {
            private int m_numConnections;   // the maximum number of connections the sample is designed to handle simultaneously 
            private int m_receiveBufferSize;// buffer size to use for each socket I/O operation 
            System.ServiceModel.Channels. BufferManager m_bufferManager;  // represents a large reusable set of buffers for all socket operations
            const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
            Socket listenSocket;            // the socket used to listen for incoming connection requests
                                            // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
            SocketAsyncEventArgsPool m_readWritePool;
            int m_totalBytesRead;           // counter of the total # bytes received by the server
            int m_numConnectedSockets;      // the total number of clients connected to the server 
            Semaphore m_maxNumberAcceptedClients;

            // Create an uninitialized server instance.  
            // To start the server listening for connection requests
            // call the Init method followed by Start method 
            //
            // <param name="numConnections">the maximum number of connections the sample is designed to handle simultaneously</param>
            // <param name="receiveBufferSize">buffer size to use for each socket I/O operation</param>
            public Server(int numConnections, int receiveBufferSize)
            {
                m_totalBytesRead = 0;
                m_numConnectedSockets = 0;
                m_numConnections = numConnections;
                m_receiveBufferSize = receiveBufferSize;
                // allocate buffers such that the maximum number of sockets can have one outstanding read and 
                //write posted to the socket simultaneously  
                m_bufferManager = new BufferManager(receiveBufferSize * numConnections * opsToPreAlloc,
                    receiveBufferSize);

                m_readWritePool = new SocketAsyncEventArgsPool(numConnections);
                m_maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);
            }

            // Initializes the server by preallocating reusable buffers and 
            // context objects.  These objects do not need to be preallocated 
            // or reused, but it is done this way to illustrate how the API can 
            // easily be used to create reusable objects to increase server performance.
            //
            public void Init()
            {
                // Allocates one large byte buffer which all I/O operations use a piece of.  This gaurds 
                // against memory fragmentation
                m_bufferManager.InitBuffer();

                // preallocate pool of SocketAsyncEventArgs objects
                SocketAsyncEventArgs readWriteEventArg;

                for (int i = 0; i < m_numConnections; i++)
                {
                    //Pre-allocate a set of reusable SocketAsyncEventArgs
                    readWriteEventArg = new SocketAsyncEventArgs();
                    readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                    readWriteEventArg.UserToken = new AsyncUserToken();

                    // assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
                    m_bufferManager.SetBuffer(readWriteEventArg);

                    // add SocketAsyncEventArg to the pool
                    m_readWritePool.Push(readWriteEventArg);
                }

            }

            // Starts the server such that it is listening for 
            // incoming connection requests.    
            //
            // <param name="localEndPoint">The endpoint which the server will listening 
            // for connection requests on</param>
            public void Start(IPEndPoint localEndPoint)
            {
                // create the socket which listens for incoming connections
                listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(localEndPoint);
                // start the server with a listen backlog of 100 connections
                listenSocket.Listen(100);

                // post accepts on the listening socket
                StartAccept(null);

                //Console.WriteLine("{0} connected sockets with one outstanding receive posted to each....press any key", m_outstandingReadCount);
                Console.WriteLine("Press any key to terminate the server process....");
                Console.ReadKey();
            }


            // Begins an operation to accept a connection request from the client 
            //
            // <param name="acceptEventArg">The context object to use when issuing 
            // the accept operation on the server's listening socket</param>
            public void StartAccept(SocketAsyncEventArgs acceptEventArg)
            {
                if (acceptEventArg == null)
                {
                    acceptEventArg = new SocketAsyncEventArgs();
                    acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
                }
                else
                {
                    // socket must be cleared since the context object is being reused
                    acceptEventArg.AcceptSocket = null;
                }

                m_maxNumberAcceptedClients.WaitOne();
                bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
                if (!willRaiseEvent)
                {
                    ProcessAccept(acceptEventArg);
                }
            }

            // This method is the callback method associated with Socket.AcceptAsync 
            // operations and is invoked when an accept operation is complete
            //
            void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
            {
                ProcessAccept(e);
            }

            private void ProcessAccept(SocketAsyncEventArgs e)
            {
                Interlocked.Increment(ref m_numConnectedSockets);
                Console.WriteLine("Client connection accepted. There are {0} clients connected to the server",
                    m_numConnectedSockets);

                // Get the socket for the accepted client connection and put it into the 
                //ReadEventArg object user token
                SocketAsyncEventArgs readEventArgs = m_readWritePool.Pop();
                ((AsyncUserToken)readEventArgs.UserToken).Socket = e.AcceptSocket;

                // As soon as the client is connected, post a receive to the connection
                bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessReceive(readEventArgs);
                }

                // Accept the next connection request
                StartAccept(e);
            }

            // This method is called whenever a receive or send operation is completed on a socket 
            //
            // <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
            void IO_Completed(object sender, SocketAsyncEventArgs e)
            {
                // determine which type of operation just completed and call the associated handler
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.Receive:
                        ProcessReceive(e);
                        break;
                    case SocketAsyncOperation.Send:
                        ProcessSend(e);
                        break;
                    default:
                        throw new ArgumentException("The last operation completed on the socket was not a receive or send");
                }

            }

            // This method is invoked when an asynchronous receive operation completes. 
            // If the remote host closed the connection, then the socket is closed.  
            // If data was received then the data is echoed back to the client.
            //
            private void ProcessReceive(SocketAsyncEventArgs e)
            {
                // check if the remote host closed the connection
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    //increment the count of the total bytes receive by the server
                    Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);
                    Console.WriteLine("The server has read a total of {0} bytes", m_totalBytesRead);

                    //echo the data received back to the client
                    e.SetBuffer(e.Offset, e.BytesTransferred);
                    bool willRaiseEvent = token.Socket.SendAsync(e);
                    if (!willRaiseEvent)
                    {
                        ProcessSend(e);
                    }

                }
                else
                {
                    CloseClientSocket(e);
                }
            }

            // This method is invoked when an asynchronous send operation completes.  
            // The method issues another receive on the socket to read any additional 
            // data sent from the client
            //
            // <param name="e"></param>
            private void ProcessSend(SocketAsyncEventArgs e)
            {
                if (e.SocketError == SocketError.Success)
                {
                    // done echoing data back to the client
                    AsyncUserToken token = (AsyncUserToken)e.UserToken;
                    // read the next block of data send from the client
                    bool willRaiseEvent = token.Socket.ReceiveAsync(e);
                    if (!willRaiseEvent)
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }

            private void CloseClientSocket(SocketAsyncEventArgs e)
            {
                AsyncUserToken token = e.UserToken as AsyncUserToken;

                // close the socket associated with the client
                try
                {
                    token.Socket.Shutdown(SocketShutdown.Send);
                }
                // throws if client process has already closed
                catch (Exception) { }
                token.Socket.Close();

                // decrement the counter keeping track of the total number of clients connected to the server
                Interlocked.Decrement(ref m_numConnectedSockets);
                m_maxNumberAcceptedClients.Release();
                Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", m_numConnectedSockets);

                // Free the SocketAsyncEventArg so they can be reused by another client
                m_readWritePool.Push(e);
            }

        }*/

    }

}
