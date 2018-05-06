using System;
using System.Collections.Generic;
using System.IO;
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
            var ret = new HandlerHtml(File.ReadAllText("save.txt"));
          // SaveToDataBaseFormList(ret.AnalysisData.Values);
            //SaveToDataBaseOneByOne(ret.AnalysisData.Values);
            new WebPageGet().DownloadInit();
            new WebPageGet();
            TCPCmd.StartListener();
        }
    }
}
