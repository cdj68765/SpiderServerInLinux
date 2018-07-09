using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SpiderServerInLinux
{
    internal class Program
    {
        private static void Main(string[] args)
        {

            Loger.Instance.LocalInfo($"启动");
            DataBaseCommand.Init();
            Loger.Instance.LocalInfo("数据库初始化完毕");
            new DownWork();
            /*   var Web= new  WebClient();
                  try
                  {
                      Web.DownloadString("https://sukebei.nyaa.si/?p=1");
                  }
                  catch (Exception ex)
                  {
                      while (ex != null)
                      {
                          Console.WriteLine(ex.Message);
                          ex = ex.InnerException;
      
                      }
                  }*/

            // Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd"));


            // new DownWork();
            /*  Task.Factory.StartNew(() =>
              {
                  Loger.Instance.Info("开始获取网页数据");
                 new DownloadHelp(Setting.LastPageIndex);

              }, Setting.CancelSign.Token);
              Task.Factory.StartNew(() =>
              {
                  Loger.Instance.Info("启动网页数据分析和保存");
                  //管道剩余待处理数据
                  //剩余处理时间
                  //处理完毕且管道已经清空
                  foreach (var item in DownloadHelp.DownloadCollect.GetConsumingEnumerable())
                  {

                  }

              }, Setting.CancelSign.Token);*/

            //test
            //new WebPageGet(@"https://sukebei.nyaa.si/?p=500000");
            //GetDataFromDataBase();
            // var ret = new HandlerHtml(File.ReadAllText("save.txt"));
            // SaveToDataBaseFormList(ret.AnalysisData.Values);
            //SaveToDataBaseOneByOne(ret.AnalysisData.Values);
            var TCPCmd = TCPCommand.Init(1000);
            TCPCmd.StartListener();
        }
    }
}