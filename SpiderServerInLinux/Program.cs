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

        static void Main(string[] args)
        {
            var TCPCmd = TCPCommand.Init(1000);
           // TCPCmd.StartListener();
            var DataBae = DataBaseCommand.Init();






        }
    }
}
