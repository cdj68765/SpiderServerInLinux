﻿using System.IO;
using static SpiderServerInLinux.DataBaseCommand;

namespace SpiderServerInLinux
{
    class Program
    {

        static void Main(string[] args)
        {
            var TCPCmd = TCPCommand.Init(1000);
            DataBaseCommand.Init();
            //test
            //new WebPageGet(@"https://sukebei.nyaa.si/?p=500000");
            GetDataFormDataBase();
             new HandlerHtml(File.ReadAllText("save.txt"));
           new WebPageGet().DownloadControl();
            //new WebPageGet();
            TCPCmd.StartListener();
        }
    }
}
