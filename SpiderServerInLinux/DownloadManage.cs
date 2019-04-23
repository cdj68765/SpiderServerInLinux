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
    public class DownloadManage : IDisposable
    {
        public CancellationTokenSource NyaaNewDownloadCancel = new CancellationTokenSource();
        public System.Timers.Timer GetNyaaNewDataTiner = new System.Timers.Timer();
        public CancellationTokenSource JavOldDownloadCancel = new CancellationTokenSource();
        public CancellationTokenSource JavNewDownloadCancel = new CancellationTokenSource();
        public bool JavOldDownloadRunning = false;

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
            GetOldDate();
            await Task.WhenAll(GetJavNewData(), GetNyaaNewData());
        }

        private Task GetNyaaNewData()
        {
            return Task.Run(() =>
            {
                {
                    GetNyaaNewDataTiner = new System.Timers.Timer(10000);
                    GetNyaaNewDataTiner.Elapsed += async delegate
                    {
                        Loger.Instance.LocalInfo("开始获取新Nyaa信息");
                        NyaaNewDownloadCancel = new CancellationTokenSource();
                        GetNyaaNewDataTiner.Stop();
                        var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                        await Task.WhenAll(DownloadLoop(Setting._GlobalSet.NyaaAddress, 0, DownloadCollect, NyaaNewDownloadCancel), HandlerNyaaHtml(DownloadCollect));
                        GetNyaaNewDataTiner = new System.Timers.Timer(new Random().Next(2, 12) * 3600 * 1000);
                        Setting.NyaaDownLoadNow = DateTime.Now.AddMilliseconds(GetNyaaNewDataTiner.Interval).ToString("MM-dd|hh:mm");
                        Loger.Instance.LocalInfo($"下次获得新数据为{Setting.NyaaDownLoadNow}");
                        GetNyaaNewDataTiner.Start();
                    };
                    GetNyaaNewDataTiner.Enabled = true;
                }
            });
        }

        private Task HandlerNyaaHtml(BlockingCollection<Tuple<int, string>> downloadCollect)
        {
            var SaveData = new BlockingCollection<Tuple<int, List<NyaaInfo>>>();
            Task.Factory.StartNew(() =>
            {
                using (var request = new HttpRequest())
                {
                    foreach (var item in SaveData.GetConsumingEnumerable())
                    {
                        try
                        {
                            if (item.Item1 >= Setting._GlobalSet.NyaaLastPageIndex)
                            {
                                Setting._GlobalSet.NyaaLastPageIndex = item.Item1;
                            }
                        }
                        catch (Exception ex)
                        {
                            Loger.Instance.LocalInfo($"Nyaa保存出错{ex}");
                        }
                    }
                    NyaaNewDownloadCancel.Cancel();
                    downloadCollect.CompleteAdding();
                }
            });
            return Task.Run(() =>
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
                    try
                    {
                        var TempSave = new List<NyaaInfo>();
                        foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[1]/div[2]/table/tbody/tr"))
                        {
                            var TempData = new NyaaInfo();
                            var temp = HtmlNode.CreateNode(item.OuterHtml);
                            TempData.Class = item.Attributes["class"].Value;
                            TempData.Catagory = temp.SelectSingleNode("td[1]/a").Attributes["title"]
                                .Value;
                            TempData.Title = temp.SelectSingleNode("td[2]/a").Attributes["title"]
                                .Value;
                            TempData.Url = temp.SelectSingleNode("td[2]/a").Attributes["href"]
                                .Value;
                            TempData.Torrent = temp.SelectSingleNode("td[3]/a[1]").Attributes["href"].Value;
                            if (TempData.Torrent.StartsWith("magnet"))
                            {
                                TempData.Magnet = TempData.Torrent;
                                TempData.Torrent = "";
                            }
                            else
                            {
                                TempData.Magnet = temp.SelectSingleNode("td[3]/a[2]").Attributes["href"].Value;
                            }

                            TempData.Size = temp.SelectSingleNode("td[4]").InnerText;
                            TempData.Timestamp = int.Parse(temp.SelectSingleNode("td[5]").Attributes["data-timestamp"].Value);
                            TempData.Date = temp.SelectSingleNode("td[5]").InnerText;
                            TempData.Up = temp.SelectSingleNode("td[6]").InnerText;
                            TempData.Leeches = temp.SelectSingleNode("td[7]").InnerText;
                            TempData.Complete = temp.SelectSingleNode("td[8]").InnerText;
                            TempSave.Add(TempData);
                        }
                        SaveData.Add(new Tuple<int, List<NyaaInfo>>(Page.Item1, TempSave));
                    }
                    catch (Exception e)
                    {
                        Loger.Instance.LocalInfo($"Nyaa解析失败，失败原因{e}");
                    }
                    finally
                    {
                        downloadCollect.CompleteAdding();
                        SaveData.CompleteAdding();
                    }
                }
            });
        }

        public System.Timers.Timer GetJavNewDataTiner = new System.Timers.Timer();

        private Task GetJavNewData()
        {
            return Task.Run(() =>
            {
                {
                    GetJavNewDataTiner = new System.Timers.Timer(10000);
                    GetJavNewDataTiner.Elapsed += async delegate
                    {
                        Loger.Instance.LocalInfo("开始获取新Jav信息");
                        JavNewDownloadCancel = new CancellationTokenSource();
                        GetJavNewDataTiner.Stop();
                        var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                        await Task.WhenAll(DownloadLoop(Setting._GlobalSet.JavAddress, 0, DownloadCollect, JavNewDownloadCancel), HandlerJavHtml(DownloadCollect, true));
                        GetJavNewDataTiner = new System.Timers.Timer(new Random().Next(2, 12) * 3600 * 1000);
                        Loger.Instance.LocalInfo($"下次获得新数据为{DateTime.Now.AddMilliseconds(GetJavNewDataTiner.Interval).ToString("F")}");
                        GetJavNewDataTiner.Start();
                    };
                    GetJavNewDataTiner.Enabled = true;
                }
            });
        }

        private void GetOldDate()
        {
            if (JavOldDownloadRunning)
            {
                Loger.Instance.LocalInfo("下载Jav数据进程已经运行");
                return;
            }
            Task.Run(() =>
           {
               if (Setting._GlobalSet.NyaaFin)
               {
                   var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
               }
               if (Setting._GlobalSet.JavFin)
               {
                   var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                   DownloadLoop(Setting._GlobalSet.JavAddress, Setting._GlobalSet.JavLastPageIndex, DownloadCollect, JavOldDownloadCancel);
                   HandlerJavHtml(DownloadCollect);
               }
               else
               {
                   Loger.Instance.LocalInfo($"JAV下载完毕，跳过旧数据获取");
               }
           }, JavOldDownloadCancel.Token);
        }

        private Task HandlerJavHtml(BlockingCollection<Tuple<int, string>> downloadCollect, bool GetNew = false)
        {
            var SaveData = new BlockingCollection<Tuple<int, JavInfo>>();
            var NewDate = "";
            if (GetNew)
            {
                Task.Factory.StartNew(() =>
                {
                    using (var request = new HttpRequest())
                    {
                        foreach (var item in SaveData.GetConsumingEnumerable())
                        {
                            try
                            {
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
                                if (NewDate != item.Item2.Date)
                                {
                                    Loger.Instance.LocalInfo($"当前保存Jav下载日期{item.Item2.Date}");
                                    NewDate = item.Item2.Date;
                                }
                                if (!DataBaseCommand.SaveToJavDataBaseOneObject(item.Item2))
                                {
                                    Loger.Instance.LocalInfo($"找到重复Jav项退出当前下载项");
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Loger.Instance.LocalInfo(ex);
                            }
                        }
                        JavNewDownloadCancel.Cancel();
                        downloadCollect.CompleteAdding();
                    }
                });
            }
            else
            {
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
                                    if (NewDate != item.Item2.Date)
                                    {
                                        Loger.Instance.LocalInfo($"当前保存Jav下载日期{item.Item2.Date}");
                                        NewDate = item.Item2.Date;
                                    }
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
                                        Setting._GlobalSet.JavLastPageIndex = item.Item1;
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
                        JavOldDownloadRunning = false;
                    }
                });
            }
            return Task.Run(() =>
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
                    try
                    {
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
                    catch (Exception)
                    {
                        Loger.Instance.LocalInfo($"Jav解析失败，推测下载完毕");
                        Setting._GlobalSet.JavFin = false;
                        downloadCollect.CompleteAdding();
                        SaveData.CompleteAdding();
                        JavOldDownloadCancel.Cancel();
                    }
                }
                SaveData.CompleteAdding();
            });
        }

        private Task DownloadLoop(string Address, int LastPageIndex, BlockingCollection<Tuple<int, string>> downloadCollect, CancellationTokenSource token)
        {
            return Task.Factory.StartNew(() =>
              {
                  using (var request = new HttpRequest())
                  {
                      int ErrorCount = 0;
                      request.UserAgent = Http.ChromeUserAgent();
                      request.ConnectTimeout = 20000;
                      if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                      while (!token.Token.IsCancellationRequested)
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
                              if (token.Token.IsCancellationRequested)
                              {
                                  break;
                              }
                              Interlocked.Increment(ref ErrorCount);
                              Loger.Instance.LocalInfo($"下载{downurl.ToString()}失败，计数{ErrorCount}次");
                              Loger.Instance.LocalInfo(ex.Message);
                              var time = new Random().Next(10000, 100000);
                              for (var i = time; i > 0; i -= 1000)
                              {
                                  if (token.Token.IsCancellationRequested)
                                  {
                                      break;
                                  }
                                  Loger.Instance.WaitTime(i / 1000);
                                  Thread.Sleep(1000);
                              }
                              if (ErrorCount > 5)
                              {
                                  Loger.Instance.LocalInfo($"下载{downurl.ToString()}失败，退出下载进程");
                                  if (!Setting.CheckOnline(Setting._GlobalSet.SocksCheck))
                                  {
                                      Loger.Instance.LocalInfo($"检测到网络连接异常，退出全部下载方式");
                                      Setting.CancelSign.Cancel();
                                  }
                                  token.Cancel();
                                  break;
                              }
                          }
                      }
                      downloadCollect.CompleteAdding();
                  }
              }).ContinueWith(a =>
              {
                  Loger.Instance.LocalInfo($"接收到退出信号，已经退出{new Uri(Address).Authority}的下载进程");
              });
        }

        public void Dispose()
        {
            Setting.DownloadManage.JavNewDownloadCancel.Cancel();
            Setting.DownloadManage.JavOldDownloadCancel.Cancel();
            GetJavNewDataTiner.Dispose();
            while (!Setting.DownloadManage.JavOldDownloadRunning && !Setting.DownloadManage.JavNewDownloadCancel.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }
        }
    }
}