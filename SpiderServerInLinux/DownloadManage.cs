using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xNet;

namespace SpiderServerInLinux
{
    public class DownloadManage
    {
        public CancellationTokenSource NewAllGetCancelSign = new CancellationTokenSource();
        public CancellationTokenSource OldAllGetCancelSign = new CancellationTokenSource();

        public DownloadManage()
        {
            if (Setting.CheckOnline(Setting._GlobalSet.SocksCheck))
            {
                Loger.Instance.LocalInfo("网络连接正常，正在加载下载进程");
                Load();
            }
            else
            {
                Loger.Instance.LocalInfo("外网访问失败，等待操作");
            }
        }

        private async void Load()
        {
            Loger.Instance.LocalInfo("初始化下载");
            NewAllGetCancelSign = new CancellationTokenSource();
            OldAllGetCancelSign = new CancellationTokenSource();
            await GetOldDate();
            await GetNewDate();
        }

        private Task GetNewDate()
        {
            return Task.Run(() =>
            {
                if (Setting._GlobalSet.JavLastPageIndex == 0)
                {
                }
                if (Setting._GlobalSet.NyaaLastPageIndex == 0)
                {
                }
            }, NewAllGetCancelSign.Token);
        }

        private Task GetOldDate()
        {
            var JavDownloadCancel = new CancellationTokenSource();

            OldAllGetCancelSign.Token.Register(() =>
                {
                    JavDownloadCancel.Cancel();
                });
            return Task.Run(() =>
            {
                if (Setting._GlobalSet.NyaaFin)
                {
                    var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                }
                if (Setting._GlobalSet.JavFin)
                {
                    var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                    DownloadLoop(Setting._GlobalSet.JavAddress, Setting._GlobalSet.JavLastPageIndex, DownloadCollect, JavDownloadCancel);
                    HandlerJavHtml(DownloadCollect, JavDownloadCancel);
                }
            }, OldAllGetCancelSign.Token);
        }

        private void HandlerJavHtml(BlockingCollection<Tuple<int, string>> downloadCollect, CancellationTokenSource javDownloadCancel)
        {
            var SaveData = new BlockingCollection<Tuple<int, JavInfo>>();
            Task.Factory.StartNew(() =>
           {
               int PageIndex = 0;
               using (var request = new HttpRequest())
               {
                   List<JavInfo> Save = new List<JavInfo>();
                   foreach (var item in SaveData.GetConsumingEnumerable())
                   {
                       try
                       {
                           if (PageIndex != item.Item1)
                           {
                               PageIndex = item.Item1;
                               Loger.Instance.LocalInfo($"当前保存Jav下载日期{item.Item2.Date}");
                               Setting._GlobalSet.JavLastPageIndex = item.Item1;
                               if (Save.Count != 0)
                               {
                                   try
                                   {
                                       DataBaseCommand.SaveToJavDataBaseRange(Save);
                                   }
                                   catch (Exception e)
                                   {
                                       Loger.Instance.LocalInfo(e);
                                       DataBaseCommand.SaveToJavDataBaseOneByOne(Save);
                                   }
                                   Save.Clear();
                               }
                           }
                           request.UserAgent = Http.ChromeUserAgent();
                           if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                           try
                           {
                               HttpResponse response = request.Get(item.Item2.ImgUrl);
                               if (!string.IsNullOrEmpty(response.ToString()))
                               {
                                   item.Item2.Image = response.Ret;
                               }
                           }
                           catch (Exception)
                           {
                               try
                               {
                                   HttpResponse response = request.Get(item.Item2.ImgUrlError);
                                   if (!string.IsNullOrEmpty(response.ToString()))
                                   {
                                       item.Item2.Image = response.Ret;
                                   }
                               }
                               catch (Exception)
                               {
                               }
                           }
                           Save.Add(item.Item2);
                       }
                       catch (Exception ex)
                       {
                           Loger.Instance.LocalInfo(ex);
                       }
                   }
                   Loger.Instance.LocalInfo($"当前保存Jav解析进程退出");
               }
           });
            Task.Factory.StartNew(() =>
           {
               var HtmlDoc = new HtmlDocument();
               foreach (var Page in downloadCollect.GetConsumingEnumerable())
               {
                   if (string.IsNullOrEmpty(Page.Item2))
                   {
                       Task.Delay(1000);
                       continue;
                   }
                   HtmlDoc.LoadHtml(Page.Item2);
                   foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[1]/div"))
                   {
                       var TempData = new JavInfo();
                       var temp = HtmlNode.CreateNode(item.OuterHtml);
                       TempData.ImgUrl = temp.SelectSingleNode("div/div/div[1]/img").Attributes["src"].Value;
                       TempData.ImgUrlError = temp.SelectSingleNode("div/div/div[1]/img").Attributes["onerror"].Value.Split('\'')[1];
                       TempData.id = temp.SelectSingleNode("div/div/div[2]/div/h5/a").InnerText.Replace("\n", "");
                       TempData.Size = temp.SelectSingleNode("div/div/div[2]/div/h5/span").InnerText;
                       TempData.Date = $"{DateTime.Parse(temp.SelectSingleNode("div/div/div[2]/div/p[1]/a").Attributes["href"].Value.Substring(1)):yy-MM-dd}";
                       var tags = new List<string>();
                       try
                       {
                           foreach (var Tags in temp.SelectNodes(@"div/div/div[2]/div/div[1]/a"))
                           {
                               tags.Add(Tags.InnerText.Replace("\n", ""));
                           }
                       }
                       catch (Exception)
                       {
                       }
                       TempData.Tags = tags.ToArray();
                       TempData.Describe = temp.SelectSingleNode("div/div/div[2]/div/p[2]").InnerText.ReplaceEntities().Replace("\n", ""); ;
                       var Actress = new List<string>();
                       try
                       {
                           foreach (var Tags in temp.SelectNodes(@"div/div/div[2]/div/div[2]/a"))
                           {
                               Actress.Add(Tags.InnerText.Replace("\n", ""));
                           }
                       }
                       catch (Exception)
                       {
                       }
                       TempData.Actress = Actress.ToArray();
                       TempData.Magnet = temp.SelectSingleNode("div/div/div[2]/div/a[1]").Attributes["href"].Value;
                       SaveData.Add(new Tuple<int, JavInfo>(Page.Item1, TempData));
                   }
               }
           }, javDownloadCancel.Token);
        }

        private void DownloadLoop(string Address, int LastPageIndex, BlockingCollection<Tuple<int, string>> downloadCollect, CancellationTokenSource token)
        {
            Task.Factory.StartNew(() =>
           {
               using (var request = new HttpRequest())
               {
                   int ErrorCount = 0;
                   request.UserAgent = Http.ChromeUserAgent();
                   if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                   while (!token.IsCancellationRequested)
                   {
                       var downurl = new Uri($"{Address}{LastPageIndex}");
                       try
                       {
                           var time = new Random().Next(1000, 10000);
                           for (var i = time; i > 0; i -= 1000)
                           {
                               Loger.Instance.WaitTime(i / 1000);
                               Thread.Sleep(1000);
                           }
                           HttpResponse response = request.Get(downurl);
                           downloadCollect.Add(new Tuple<int, string>(LastPageIndex, response.ToString()));
                           Interlocked.Increment(ref LastPageIndex);
                           ErrorCount = 0;
                       }
                       catch (Exception ex)
                       {
                           Interlocked.Increment(ref ErrorCount);
                           Loger.Instance.LocalInfo($"下载{downurl.ToString()}失败，计数{ErrorCount}次");
                           Loger.Instance.LocalInfo(ex.Message);
                           var time = new Random().Next(10000, 100000);
                           for (var i = time; i > 0; i -= 1000)
                           {
                               Loger.Instance.WaitTime(i / 1000);
                               Thread.Sleep(1000);
                           }
                           if (ErrorCount > 5)
                           {
                               Loger.Instance.LocalInfo($"下载{downurl.ToString()}失败，退出下载进程");
                               downloadCollect.CompleteAdding();
                           }
                       }
                   }
                   Loger.Instance.LocalInfo($"接收到退出信号，已经退出{Address}的下载进程");
               }
           }, token.Token);
        }
    }
}