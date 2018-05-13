using System;
using System.IO;

namespace SpiderServerInLinux
{
    internal class Program
    {
        private static void Main(string[] args)
        {
           Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd"));
            var TCPCmd = TCPCommand.Init(1000);
            DataBaseCommand.Init();
       //    new DownWork();
            //test
            //new WebPageGet(@"https://sukebei.nyaa.si/?p=500000");
            //GetDataFromDataBase();
            // var ret = new HandlerHtml(File.ReadAllText("save.txt"));
            // SaveToDataBaseFormList(ret.AnalysisData.Values);
            //SaveToDataBaseOneByOne(ret.AnalysisData.Values);
            TCPCmd.StartListener();
        }
    }
}