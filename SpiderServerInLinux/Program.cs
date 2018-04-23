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
using System.IO.Pipes;


namespace SpiderServerInLinux
{
    class Program
    {

        static void Main(string[] args)
        {

              var TCPCmd = TCPCommand.Init(1000);
              DataBaseCommand.Init();
              new WebPageGet();
              TCPCmd.StartListener();
        }
    }
}
