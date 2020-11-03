using HtmlAgilityPack;
using LiteDB;
using Shadowsocks.Controller;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using xNet;

namespace SpiderServerInLinux
{
    public class DownloadManage : IDisposable
    {
        public CancellationTokenSource NyaaDownloadCancel = new CancellationTokenSource();
        public CancellationTokenSource NyaaOldDownloadCancel = new CancellationTokenSource();
        public CancellationTokenSource JavDownloadCancel = new CancellationTokenSource();
        public CancellationTokenSource MiMiDownloadCancel = new CancellationTokenSource();
        public CancellationTokenSource MiMiAiStoryDownloadCancel = new CancellationTokenSource();
        public CancellationTokenSource T66yDownloadCancel = new CancellationTokenSource();

        public System.Timers.Timer GetNyaaNewDataTimer = null;
        public System.Timers.Timer GetJavNewDataTimer = null;
        public System.Timers.Timer GetMiMiNewDataTimer = null;
        public System.Timers.Timer GetMiMiAiStoryDataTimer = null;
        public System.Timers.Timer GetT66yDataTimer = null;

        public Stopwatch MiMiSpan = new Stopwatch();
        public Stopwatch MiMiStorySpan = new Stopwatch();
        public Stopwatch JavSpan = new Stopwatch();
        public Stopwatch NyaaSpan = new Stopwatch();
        public Stopwatch GetT66ySpan = new Stopwatch();

        public bool JavOldDownloadRunning = false;

        public DownloadManage()
        {
            Load();
        }

        public async void Load()
        {
            if (Setting.CheckOnline(Setting._GlobalSet.SocksCheck))
            {
                Loger.Instance.LocalInfo("网络连接正常，正在加载下载进程");
                Loger.Instance.LocalInfo("初始化下载");
                //await Task.WhenAll(GetJavNewData(), GetNyaaNewData(), GetMiMiData(), GetMiMiAiStoryData());
            }
            else
            {
                Loger.Instance.LocalInfo("外网访问失败，等待操作");
            }
            await Task.WhenAll(GetT66yStoryData());
        }

        private Task GetMiMiAiStoryData()
        {
            //HandleStoryPage(DownloadMainPage(_Uri: $"http://www.mmroad.com/viewthread.php?tid=1274532"));
            long NewSize = 0;
            return Task.Run(() =>
            {
                GetMiMiAiStoryDataTimer = new System.Timers.Timer(1000);
                var HtmlDoc = new HtmlDocument();
                GetMiMiAiStoryDataTimer.Elapsed += delegate
               {
                   GetMiMiAiStoryDataTimer.Stop();
                   MiMiStorySpan.Reset();
                   var RunSpan = new Stopwatch();
                   RunSpan.Start();
                   MiMiAiStoryDownloadCancel = new CancellationTokenSource();
                   var DownloadPage = 1;
                   NewSize = 0;
                   //var CheckLastPage = DataBaseCommand.GetDataFromMiMi("CheckMiMiStoryLastPage");
                   /*if (!CheckLastPage)
                     {
                         DownloadPage = Setting._GlobalSet.MiMiAiStoryPageIndex;
                         Loger.Instance.LocalInfo($"从{DownloadPage}页开始获得MiMi小说信息");
                     }
                     else
                     {
                         Loger.Instance.LocalInfo("开始获取新MiMi小说信息");
                     }*/
                   bool SaveFlag = true;
                   while (!MiMiAiStoryDownloadCancel.IsCancellationRequested)
                   {
                       Setting.MiMiAiStoryDownLoadNow = $"当前下载第{DownloadPage}";
                       var DoanloadPageHtml = DownloadMainPage(DownloadPage);
                       if (string.IsNullOrEmpty(DoanloadPageHtml))
                       {
                           Loger.Instance.LocalInfo("MiMiStory下载到空网页，退出下载进程");
                           break;
                       }
                       var PageCount = 1;
                       foreach (var tempData in AnalyMiMiMainPage(HtmlDoc, DoanloadPageHtml, true))
                       {
                           // List<MiMiAiStory> ItemList = new List<MiMiAiStory>();
                           var AddTemp = HandleStoryPage(DownloadMainPage(_Uri: $"http://{new Uri(Setting._GlobalSet.MiMiAiAddress).Host}/{tempData[0]}"), tempData);
                           if (AddTemp != null)
                           {
                               SaveFlag = DataBaseCommand.SaveToMiMiStoryDataUnit(UnitData: AddTemp);
                               //SaveFlag = true;
                               //if (AddTemp.id == 140503) SaveFlag = false;
                           }

                           var time = new Random().Next(1000, 5000);
                           for (var i = time; i > 0; i -= 1000)
                           {
                               if (MiMiAiStoryDownloadCancel.IsCancellationRequested) break;
                               Setting.MiMiAiStoryDownLoadNow = $"当前下载第{PageCount}-{i / 1000}";
                               Thread.Sleep(1000);
                           }
                           Interlocked.Increment(ref PageCount);
                           // ItemList.Clear(); ItemList = null;
                           if (!SaveFlag) break;
                       }
                       GC.Collect();
                       Interlocked.Increment(ref DownloadPage);
                       Setting._GlobalSet.MiMiAiStoryPageIndex = DownloadPage;
                       if (!SaveFlag) break;
                   }

                   GetMiMiAiStoryDataTimer.Interval = new Random().Next(12, 24) * 3600 * 1000;
                   Setting.MiMiAiStoryDownLoadNow = DateTime.Now.AddMilliseconds(GetMiMiAiStoryDataTimer.Interval).ToString("MM-dd|HH:mm");
                   Loger.Instance.LocalInfo($"MiMi小说:下载完毕,耗时{RunSpan.Elapsed:mm\\分ss\\秒},消耗流量{HumanReadableFilesize(NewSize)}");
                   RunSpan.Stop();
                   MiMiStorySpan.Restart();
                   GetMiMiAiStoryDataTimer.Start();
               };
                GetMiMiAiStoryDataTimer.Enabled = true;
            });

            string DownloadMainPage(int Index = -1, string _Uri = "")
            {
                using (var request = new HttpRequest()
                {
                    UserAgent = Http.ChromeUserAgent(),
                    ConnectTimeout = 5000,
                    CharacterSet = Encoding.GetEncoding("GBK")
                })
                {
                    if (Setting._GlobalSet.SocksCheck)
                    {
                        if (Setting.Socks5Point != 0)
                            request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                    }
                    try
                    {
                        int ErrorCount = 1;
                        while (!MiMiAiStoryDownloadCancel.IsCancellationRequested || ErrorCount != 0)
                        {
                            var downurl = Index > -1 ? new Uri($"http://{new Uri(Setting._GlobalSet.MiMiAiAddress).Authority}/forumdisplay.php?fid=11&filter=0&orderby=dateline&page={Index}") : new Uri($"{_Uri}");
                            try
                            {
                                HttpResponse response = request.Get(downurl);
                                var RetS = response.ToString();
                                NewSize += response.Ret.Length;
                                return RetS;
                            }
                            catch (Exception ex)
                            {
                                if (request.Response.RedirectAddress != null)
                                {
                                    if (request.Response.RedirectAddress.Authority != downurl.Authority)
                                    {
                                        Loger.Instance.LocalInfo($"MiMiAi网址变更为{request.Response.RedirectAddress.Authority}");
                                        Setting._GlobalSet.MiMiAiAddress = Setting._GlobalSet.MiMiAiAddress.Replace(downurl.Authority, request.Response.RedirectAddress.Authority);
                                        downurl = Index > -1 ? new Uri($"http://{new Uri(Setting._GlobalSet.MiMiAiAddress).Authority}/forumdisplay.php?fid=11&filter=0&orderby=dateline&page={Index}") : new Uri($"{_Uri}");
                                        continue;
                                    }
                                }
                                if (ex.Message.StartsWith("Cannot access a disposed object"))
                                {
                                    Loger.Instance.LocalInfo($"SSR异常，退出全部下载进程");
                                    MiMiAiStoryDownloadCancel.Cancel();
                                    break;
                                }
                                Loger.Instance.LocalInfo($"{ex.Message}");
                                Loger.Instance.LocalInfo($"下载{downurl}失败，计数{ErrorCount}次");
                                var time = new Random().Next(5000, 10000);
                                for (var i = time; i > 0; i -= 1000)
                                {
                                    if (MiMiAiStoryDownloadCancel.IsCancellationRequested) break;
                                    Loger.Instance.WaitTime(i / 1000);
                                    Thread.Sleep(1000);
                                }
                                Interlocked.Increment(ref ErrorCount);
                                if (ErrorCount == 5)
                                {
                                    ErrorCount = 0;
                                    Loger.Instance.LocalInfo($"下载{downurl}失败，退出下载进程");
                                    if (!Setting.CheckOnline(Setting._GlobalSet.SocksCheck))
                                    {
                                        Loger.Instance.LocalInfo($"检测到网络连接异常，退出全部下载方式");
                                        Setting.CancelSign.Cancel();
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    catch (UriFormatException UriError)
                    {
                        Loger.Instance.LocalInfo($"地址错误{UriError.Message}");
                        return $"地址错误{UriError.Message}";
                    }
                }
                return null;
            }
            MiMiAiStory HandleStoryPage(string Html, string[] PageData = null)
            {
                try
                {
                    var Temp = new MiMiAiStory();
                    var _HtmlDoc = new HtmlDocument();
                    _HtmlDoc.LoadHtml(Html);
                    Temp.Data = Encoding.Default.GetBytes(_HtmlDoc.DocumentNode.SelectNodes("//div[@class='t_msgfont']")[0].InnerHtml);
                    var OutPutText = new StringBuilder();
                    var CN = _HtmlDoc.DocumentNode.SelectNodes("//div[@class='t_msgfont']")[0].ChildNodes;
                    while (CN.Count == 1)
                    {
                        if (CN[0].ChildNodes.Count == 0) break;
                        CN = CN[0].ChildNodes;
                    }
                    foreach (var Child in CN)
                    {
                        if (Child.Name == "#text")
                        {
                            var TempText = Child.InnerText.Replace("\r\n", "").Replace(" ", "").Replace("", "").Replace("&nbsp;", "");
                            if (!string.IsNullOrEmpty(TempText))
                            {
                                OutPutText.Append(TempText);
                            }
                        }
                    }
                    Temp.id = int.Parse(PageData[0].Split("=")[1]);
                    Temp.Title = PageData[1];
                    if (string.IsNullOrEmpty(OutPutText.ToString()))
                    {
                        Loger.Instance.LocalInfo($"MiMiStory页面{PageData[0]}解析错误");
                        File.AppendAllLines("MiMiStory.txt", new string[] { $"{PageData[0]}|{PageData[1]}" });
                    }
                    Temp.Story = OutPutText.ToString();
                    return Temp;
                }
                catch (Exception ex)
                {
                    Loger.Instance.LocalInfo($"MiMiStory页面解析错误");
                }
                return null;
            }
        }

        private Task GetT66yStoryData()
        {
            //HandleStoryPage(DownloadMainPage(_Uri: $"http://www.mmroad.com/viewthread.php?tid=1274532"));
            long NewSize = 0;
            return Task.Run(() =>
            {
                GetT66yDataTimer = new System.Timers.Timer(1000);
                var HtmlDoc = new HtmlDocument();
                GetT66yDataTimer.Elapsed += delegate
                {
                    GetT66yDataTimer.Stop();
                    GetT66ySpan.Reset();
                    var RunSpan = new Stopwatch();
                    RunSpan.Start();
                    T66yDownloadCancel = new CancellationTokenSource();
                    var DownloadPage = 1;
                    NewSize = 0;
                    //var CheckLastPage = DataBaseCommand.GetDataFromMiMi("CheckMiMiStoryLastPage");
                    /*if (!CheckLastPage)
                      {
                          DownloadPage = Setting._GlobalSet.MiMiAiStoryPageIndex;
                          Loger.Instance.LocalInfo($"从{DownloadPage}页开始获得MiMi小说信息");
                      }
                      else
                      {
                          Loger.Instance.LocalInfo("开始获取新MiMi小说信息");
                      }*/
                    bool SaveFlag = true;
                    while (!T66yDownloadCancel.IsCancellationRequested)
                    {
                        Setting.T66yDownLoadNow = $"当前下载第{DownloadPage}";
                        var DoanloadPageHtml = DownloadMainPage(DownloadPage);
                        if (string.IsNullOrEmpty(DoanloadPageHtml))
                        {
                            Loger.Instance.LocalInfo("T66y下载到空网页，退出下载进程");
                            break;
                        }
                        //var PageCount = 1;
                        List<T66yData> ItemList = new List<T66yData>();
                        foreach (var tempData in AnalyT66yMainPage(HtmlDoc, DoanloadPageHtml))
                        {
                            T66yData AddTemp = new T66yData();
                            //AddTemp.id=
                            //var AddTemp = HandleStoryPage(DownloadMainPage(_Uri: $"http://{new Uri(Setting._GlobalSet.T66yAddress).Host}/{tempData[0]}"), tempData);
                            if (AddTemp != null)
                            {
                                ItemList.Add(AddTemp);
                                //SaveFlag = DataBaseCommand.SaveToT66yDataUnit();
                            }
                            var time = new Random().Next(1000, 10000);
                            for (var i = time; i > 0; i -= 1000)
                            {
                                if (MiMiAiStoryDownloadCancel.IsCancellationRequested) break;
                                Setting.T66yDownLoadNow = $"当前下载第{DownloadPage}-{ItemList.Count}-{i / 1000}";
                                Thread.Sleep(1000);
                            }
                            //Interlocked.Increment(ref PageCount);
                            if (!SaveFlag) break;
                        }
                        DataBaseCommand.SaveToT66yDataUnit(ItemList);
                        ItemList.Clear(); ItemList = null;
                        GC.Collect();
                        Interlocked.Increment(ref DownloadPage);
                        if (!SaveFlag) break;
                    }

                    GetT66yDataTimer.Interval = new Random().Next(12, 24) * 3600 * 1000;
                    Setting.T66yDownLoadNow = DateTime.Now.AddMilliseconds(GetT66yDataTimer.Interval).ToString("MM-dd|HH:mm");
                    Loger.Instance.LocalInfo($"T66y:下载完毕,耗时{RunSpan.Elapsed:mm\\分ss\\秒},消耗流量{HumanReadableFilesize(NewSize)}");
                    RunSpan.Stop();
                    GetT66ySpan.Restart();
                    GetT66yDataTimer.Start();
                };
                GetT66yDataTimer.Enabled = true;
            });

            string DownloadMainPage(int Index = -1, string _Uri = "")
            {
                using (var request = new HttpRequest()
                {
                    UserAgent = Http.ChromeUserAgent(),
                    ConnectTimeout = 5000,
                    CharacterSet = Encoding.GetEncoding("GBK")
                })
                {
                    if (Setting._GlobalSet.SocksCheck)
                    {
                        if (Setting.Socks5Point != 0)
                            request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                    }
                    try
                    {
                        int ErrorCount = 1;
                        while (!T66yDownloadCancel.IsCancellationRequested || ErrorCount != 0)
                        {
                            var downurl = Index > -1 ? new Uri($"{Setting._GlobalSet.T66yAddress}{Index}") : new Uri($"{_Uri}");
                            try
                            {
                                HttpResponse response = request.Get(downurl);
                                var RetS = response.ToString();
                                File.WriteAllBytes("Html", response.Ret);
                                NewSize += response.Ret.Length;
                                return RetS;
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message.StartsWith("Cannot access a disposed object"))
                                {
                                    Loger.Instance.LocalInfo($"SSR异常，退出全部下载进程");
                                    T66yDownloadCancel.Cancel();
                                    break;
                                }
                                Loger.Instance.LocalInfo($"{ex.Message}");
                                Loger.Instance.LocalInfo($"下载{downurl}失败，计数{ErrorCount}次");
                                var time = new Random().Next(5000, 10000);
                                for (var i = time; i > 0; i -= 1000)
                                {
                                    if (T66yDownloadCancel.IsCancellationRequested) break;
                                    Loger.Instance.WaitTime(i / 1000);
                                    Thread.Sleep(1000);
                                }
                                Interlocked.Increment(ref ErrorCount);
                                if (ErrorCount == 5)
                                {
                                    ErrorCount = 0;
                                    Loger.Instance.LocalInfo($"下载{downurl}失败，退出下载进程");
                                    if (!Setting.CheckOnline(Setting._GlobalSet.SocksCheck))
                                    {
                                        Loger.Instance.LocalInfo($"检测到网络连接异常，退出全部下载方式");
                                        Setting.CancelSign.Cancel();
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    catch (UriFormatException UriError)
                    {
                        Loger.Instance.LocalInfo($"地址错误{UriError.Message}");
                        return $"地址错误{UriError.Message}";
                    }
                }
                return null;
            }
            MiMiAiStory HandleStoryPage(string Html, string[] PageData = null)
            {
                try
                {
                    var Temp = new MiMiAiStory();
                    var _HtmlDoc = new HtmlDocument();
                    _HtmlDoc.LoadHtml(Html);
                    Temp.Data = Encoding.Default.GetBytes(_HtmlDoc.DocumentNode.SelectNodes("//div[@class='t_msgfont']")[0].InnerHtml);
                    var OutPutText = new StringBuilder();
                    var CN = _HtmlDoc.DocumentNode.SelectNodes("//div[@class='t_msgfont']")[0].ChildNodes;
                    while (CN.Count == 1)
                    {
                        if (CN[0].ChildNodes.Count == 0) break;
                        CN = CN[0].ChildNodes;
                    }
                    foreach (var Child in CN)
                    {
                        if (Child.Name == "#text")
                        {
                            var TempText = Child.InnerText.Replace("\r\n", "").Replace(" ", "").Replace("", "").Replace("&nbsp;", "");
                            if (!string.IsNullOrEmpty(TempText))
                            {
                                OutPutText.Append(TempText);
                            }
                        }
                    }
                    Temp.id = int.Parse(PageData[0].Split("=")[1]);
                    Temp.Title = PageData[1];
                    if (string.IsNullOrEmpty(OutPutText.ToString()))
                    {
                        Loger.Instance.LocalInfo($"MiMiStory页面{PageData[0]}解析错误");
                        File.AppendAllLines("MiMiStory.txt", new string[] { $"{PageData[0]}|{PageData[1]}" });
                    }
                    Temp.Story = OutPutText.ToString();
                    return Temp;
                }
                catch (Exception ex)
                {
                    Loger.Instance.LocalInfo($"MiMiStory页面解析错误");
                }
                return null;
            }
            IEnumerable<string[]> AnalyT66yMainPage(HtmlDocument HtmlDoc, string Page)
            {
                if (string.IsNullOrEmpty(Page))
                {
                    Loger.Instance.LocalInfo($"T66y页面为空"); yield break;
                }
                HtmlDoc.LoadHtml(Page);
                foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[2]/div[2]/table[1]/tbody[1]/tr"))
                {
                    if (string.IsNullOrEmpty(item.InnerHtml)) continue;
                    if (item.InnerLength < 50)
                        continue;
                    if (item.Attributes["class"].Value == "tr3 t_one tac" && item.SelectSingleNode("td[1]").InnerHtml.Contains(".::"))
                    {
                        var temp = HtmlNode.CreateNode(item.OuterHtml);
                        var TempData = new string[]
                        {
                        temp.SelectSingleNode("td[2]/h3/a").Attributes["href"].Value,
                        temp.SelectSingleNode("td[2]/h3/a").InnerHtml,
                        temp.SelectSingleNode("td[3]/div/span").Attributes["title"].Value.Split(' ')[2],
                        bool.FalseString
                        };
                        yield return TempData;
                    }
                    yield break;
                }
            }
        }

        private Task GetNyaaNewData()
        {
            return Task.Run(() =>
            {
                {
                    GetNyaaNewDataTimer = new System.Timers.Timer(1000);
                    GetNyaaNewDataTimer.Elapsed += async delegate
                    {
                        Loger.Instance.LocalInfo("开始获取新Nyaa信息");
                        NyaaSpan.Reset();
                        var RunSpan = new Stopwatch();
                        RunSpan.Start();
                        var OldSize = new FileInfo(@"Nyaa.db").Length;
                        NyaaDownloadCancel = new CancellationTokenSource();
                        GetNyaaNewDataTimer.Stop();
                        var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                        //await Task.WhenAll(DownloadLoop(Setting._GlobalSet.NyaaAddress, Setting._GlobalSet.NyaaFin ? 0 : Setting._GlobalSet.NyaaLastPageIndex, DownloadCollect, NyaaNewDownloadCancel), HandlerNyaaHtml(DownloadCollect));
                        await Task.WhenAll(DownloadLoop(Setting._GlobalSet.NyaaAddress, 0, DownloadCollect, NyaaDownloadCancel, false, true), HandlerNyaaHtml(DownloadCollect));
                        Thread.Sleep(1000);
                        GetNyaaNewDataTimer.Interval = new Random().Next(6, 12) * 3600 * 1000;
                        Setting.NyaaDownLoadNow = DateTime.Now.AddMilliseconds(GetNyaaNewDataTimer.Interval).ToString("MM-dd|HH:mm");
                        var NewSize = new FileInfo(@"Nyaa.db").Length;
                        Setting._GlobalSet.totalDownloadBytes += NewSize - OldSize;
                        Loger.Instance.LocalInfo($"Nyaa:下载完毕,耗时{RunSpan.Elapsed:mm\\分ss\\秒},消耗流量{HumanReadableFilesize(NewSize - OldSize)}");
                        Loger.Instance.LocalInfo($"下次获得新数据为{Setting.NyaaDownLoadNow}");
                        RunSpan.Stop();
                        NyaaSpan.Restart();
                        GetNyaaNewDataTimer.Start();
                    };
                    GetNyaaNewDataTimer.Enabled = true;
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
                    var FirstPageDay = "";
                    var _DayF = "";
                    var _DayL = "";
                    var PageCount = 0;
                    foreach (var item in SaveData.GetConsumingEnumerable())
                    {
                        try
                        {
                            PageCount += 1;
                            Setting.NyaaDownLoadNow = $"当前下载页面：{item.Item1}";
                            if (item.Item1 == 0)
                            {
                                FirstPageDay = item.Item2.First().Day;
                            }
                            if (!DataBaseCommand.SaveToNyaaDataBaseRange(item.Item2))
                            {
                                foreach (var AddTemp in item.Item2)
                                {
                                    DataBaseCommand.SaveToNyaaDataBaseOneObject(AddTemp);
                                }
                            }
                            if (item.Item1 > Setting._GlobalSet.NyaaLastPageIndex || true)
                            {
                                if (_DayL != item.Item2.Last().Day)
                                {
                                    PageCount = 0;
                                    _DayL = item.Item2.Last().Day;
                                    var Retdate = DataBaseCommand.GetNyaaDateInfo(_DayL);
                                    if (_DayF != FirstPageDay)
                                    {
                                        DataBaseCommand.SaveToNyaaDateInfo(new DateRecord() { _id = _DayF, Page = PageCount, Status = true });
                                    }
                                    if (Retdate != null)
                                    {
                                        if (Retdate.Status)
                                        {
                                            var TempDate = DateTime.Parse(_DayL);
                                            var DateCount = -1;
                                            do
                                            {
                                                Interlocked.Decrement(ref DateCount);
                                                if (DateCount == -10)
                                                {
                                                    break;
                                                }
                                            } while (DataBaseCommand.GetWebInfoFromNyaa(TempDate.AddDays(DateCount).ToString("yyyy-MM-dd")));
                                            if (DateCount == -10)
                                            {
                                                Loger.Instance.LocalInfo($"判断Nyaa下载完成");
                                                NyaaDownloadCancel.Cancel();
                                                downloadCollect.CompleteAdding();
                                            }
                                            else
                                            {
                                                Loger.Instance.LocalInfo($"检测到Nyaa|{TempDate.AddDays(DateCount).ToString("yy-MM-dd")}未下载完成，继续下载任务");
                                            }
                                        }
                                        if (item.Item1 == 50)
                                        {
                                            Loger.Instance.LocalInfo($"到达最大Nyaa下载页面，停止下载");
                                            NyaaDownloadCancel.Cancel();
                                            downloadCollect.CompleteAdding();
                                        }
                                    }
                                    else
                                    {
                                        DataBaseCommand.SaveToNyaaDateInfo(new DateRecord() { _id = _DayL, Status = false });
                                    }
                                }
                                if (_DayF != item.Item2.First().Day)
                                {
                                    _DayF = item.Item2.First().Day;
                                    Loger.Instance.LocalInfo($"当前保存Nyaa下载日期{_DayF}");
                                    var Retdate = DataBaseCommand.GetNyaaDateInfo(_DayF);
                                    if (Retdate == null)
                                    {
                                        DataBaseCommand.SaveToNyaaDateInfo(new DateRecord() { _id = _DayF, Status = false });
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Loger.Instance.LocalInfo($"Nyaa保存出错{ex}");
                            break;
                        }
                    }
                    NyaaDownloadCancel.Cancel();
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
                        foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"//div[@class='table-responsive']/table/tbody/tr"))
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
                        File.WriteAllText("BugNyaaPage", Page.Item2);
                        Loger.Instance.LocalInfo($"Nyaa解析失败，失败原因{e}");
                        break;
                    }
                }
                downloadCollect.CompleteAdding();
                SaveData.CompleteAdding();
            });
        }

        private Task HandlerOldNyaaHtml(BlockingCollection<Tuple<int, string>> downloadCollect)
        {
            var SaveData = new BlockingCollection<Tuple<int, NyaaInfo>>();
            Task.Factory.StartNew(() =>
            {
                using (var request = new HttpRequest())
                {
                    List<NyaaInfo> Save = new List<NyaaInfo>();
                    foreach (var item in SaveData.GetConsumingEnumerable())
                    {
                        Save.Add(item.Item2);
                        if (item.Item2.Day != Setting.NyaaDay)
                        {
                            DataBaseCommand.SaveToNyaaDateInfo(new DateRecord() { _id = Setting.NyaaDay, Status = true });
                            Setting.NyaaDay = item.Item2.Day;
                            Loger.Instance.LocalInfo($"当前保存Nyaa下载日期{Setting.NyaaDay}");
                            Loger.Instance.LocalInfo($"当前保存Nyaa页面{Setting._GlobalSet.NyaaLastPageIndex}");
                        }
                        if (Save.Count > 25 || SaveData.IsCompleted)
                        {
                            if (!DataBaseCommand.SaveToNyaaDataBaseRange(Save))
                            {
                                foreach (var AddTemp in Save)
                                {
                                    DataBaseCommand.SaveToNyaaDataBaseOneObject(AddTemp, false);
                                }
                            }
                            Setting._GlobalSet.NyaaLastPageIndex = item.Item1;
                            Save.Clear();
                        }
                        if (item.Item1 >= 2737376)
                        {
                            Loger.Instance.LocalInfo($"Nyaa旧数据下载完毕，退出下载进程");
                            NyaaOldDownloadCancel.Cancel();
                            Setting._GlobalSet.NyaaFin = true;
                            SaveData.CompleteAdding();
                        }
                    }

                    if (Save.Count != 0)
                    {
                        if (!DataBaseCommand.SaveToNyaaDataBaseRange(Save))
                        {
                            foreach (var AddTemp in Save)
                            {
                                DataBaseCommand.SaveToNyaaDataBaseOneObject(AddTemp, false);
                            }
                        }
                        Save.Clear();
                    }
                    Loger.Instance.LocalInfo($"当前保存旧Nyaa线程退出");
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
                        var TempData = new NyaaInfo();
                        var temp = HtmlNode.CreateNode(HtmlDoc.DocumentNode.OuterHtml);
                        TempData.Class = temp.SelectSingleNode("/html/body/div[1]/div[2]").Attributes["class"]
                            .Value.Split('-')[1];
                        TempData.Catagory = $"{temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[1]/div[2]/a[1]").InnerText} - {temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[1]/div[2]/a[2]").InnerText}";
                        TempData.Title = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[1]/h3").InnerText.Replace("\r", "").Replace("\t", "").Replace("\n", "");
                        TempData.Url = $"/view/{Page.Item1}";
                        TempData.Torrent = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[3]/a[1]").Attributes["href"].Value;
                        TempData.Magnet = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[3]/a[2]").Attributes["href"].Value;
                        TempData.Size = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[4]/div[2]").InnerText;
                        TempData.Timestamp = int.Parse(temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[1]/div[4]").Attributes["data-timestamp"].Value);
                        TempData.Date = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[1]/div[4]").InnerText.Replace(" UTC", "");
                        TempData.Up = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[2]/div[4]/span").InnerText;
                        TempData.Leeches = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[3]/div[4]/span").InnerText;
                        TempData.Complete = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[4]/div[4]").InnerText;
                        SaveData.Add(new Tuple<int, NyaaInfo>(Page.Item1, TempData));
                    }
                    catch (Exception e)
                    {
                        Loger.Instance.LocalInfo($"Nyaa解析失败，失败原因{e}");
                        break;
                    }
                }
                NyaaDownloadCancel.Cancel();
                downloadCollect.CompleteAdding();
                SaveData.CompleteAdding();
            });
        }

        private Task GetJavNewData()
        {
            long NewSize = 0;
            return Task.Run(() =>
            {
                {
                    GetJavNewDataTimer = new System.Timers.Timer(1000);
                    GetJavNewDataTimer.Elapsed += async delegate
                    {
                        Loger.Instance.LocalInfo("开始获取新Jav信息");
                        JavDownloadCancel = new CancellationTokenSource();
                        GetJavNewDataTimer.Stop();
                        JavSpan.Reset();

                        var RunSpan = new Stopwatch();
                        RunSpan.Start();
                        Setting.JavPageCount = 0;
                        NewSize = 0;
                        var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                        await Task.WhenAll(DownloadLoop(Setting._GlobalSet.JavAddress, 0, DownloadCollect, JavDownloadCancel), HandlerJavHtml(DownloadCollect, true));
                        Thread.Sleep(5000);
                        GetJavNewDataTimer.Interval = new Random().Next(12, 24) * 3600 * 1000;
                        Setting.JavDownLoadNow = DateTime.Now.AddMilliseconds(GetJavNewDataTimer.Interval).ToString("MM-dd|HH:mm");
                        Setting._GlobalSet.totalDownloadBytes += NewSize;
                        Loger.Instance.LocalInfo($"Jav:下载完毕,耗时{RunSpan.Elapsed:mm\\分ss\\秒},消耗流量{HumanReadableFilesize(NewSize)}");
                        Loger.Instance.LocalInfo($"下次获得新数据为{Setting.JavDownLoadNow}");
                        RunSpan.Stop();
                        JavSpan.Restart();
                        GetJavNewDataTimer.Start();
                    };
                    GetJavNewDataTimer.Enabled = true;
                }
            });
            Task HandlerJavHtml(BlockingCollection<Tuple<int, string>> downloadCollect, bool GetNew = false)
            {
                var SaveData = new BlockingCollection<Tuple<int, JavInfo>>();
                var NewDate = "";
                if (GetNew)
                {
                    Task.Factory.StartNew(() =>
                    {
                        using (var request = new HttpRequest())
                        {
                            if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");

                            foreach (var item in SaveData.GetConsumingEnumerable())
                            {
                                try
                                {
                                    request.UserAgent = Http.ChromeUserAgent();
                                    try
                                    {
                                        HttpResponse response = request.Get(item.Item2.ImgUrl);
                                        if (!string.IsNullOrEmpty(response.ToString()))
                                        {
                                            NewSize += response.Ret.Length;
                                            item.Item2.Image = response.Ret;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                                        try
                                        {
                                            HttpResponse response = request.Get(item.Item2.ImgUrlError);
                                            if (!string.IsNullOrEmpty(response.ToString()))
                                            {
                                                NewSize += response.Ret.Length;
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
                                        if (DataBaseCommand.GetOrSaveWebInfoFromJav(item.Item2.Date))
                                        {
                                            var TempDate = DateTime.ParseExact(item.Item2.Date, "yy-MM-dd", CultureInfo.InvariantCulture);
                                            var DateCount = -1;
                                            do
                                            {
                                                Interlocked.Decrement(ref DateCount);
                                                if (DateCount == -10)
                                                {
                                                    break;
                                                }
                                            } while (DataBaseCommand.GetWebInfoFromJav(TempDate.AddDays(DateCount).ToString("yy-MM-dd")));
                                            if (DateCount == -10)
                                            {
                                                Loger.Instance.LocalInfo($"检测到Jav下载完毕，退出下载进程");
                                                break;
                                            }
                                            else
                                            {
                                                Loger.Instance.LocalInfo($"检测到Jav|{TempDate.AddDays(DateCount):yy-MM-dd}未下载完成，继续下载任务");
                                            }
                                        }
                                        /* if (NewDate == "19-09-01")
                                         {
                                             Loger.Instance.LocalInfo($"记录时间到达，退出下载进程");
                                             break;
                                         }*/
                                    }
                                    Setting.JavDownLoadNow = $"{ item.Item1} |{Setting.JavPageCount}|{item.Item2.Date}";
                                    DataBaseCommand.SaveToJavDataBaseOneObject(item.Item2);
                                }
                                catch (Exception ex)
                                {
                                    Loger.Instance.LocalInfo(ex);
                                }
                            }
                            JavDownloadCancel.Cancel();
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
                                            NewSize += response.Ret.Length;
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
                                                NewSize += response.Ret.Length;
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
                                if (string.IsNullOrEmpty(temp.GetClasses().FirstOrDefault())) continue;
                                TempData.ImgUrl = temp.SelectSingleNode("div/div/div[1]/img").Attributes["src"].Value;
                                //TempData.ImgUrlError = temp.SelectSingleNode("div/div/div[1]/img").Attributes["onerror"].Value.Split('\'')[1];
                                //TempData.id = temp.SelectSingleNode("div/div/div[2]/div/h5/a").InnerText.Replace("\n", "");
                                TempData.id = temp.SelectSingleNode("div/div/div[2]/div/h5/a").Attributes["href"].Value.Replace(@"/torrent/", "");
                                TempData.Size = temp.SelectSingleNode("div/div/div[2]/div/h5/span").InnerText;
                                TempData.Date = $"{DateTime.Parse(temp.SelectSingleNode("div/div/div[2]/div/p[1]/a").Attributes["href"].Value.Substring(1)):yy-MM-dd}";
                                var tags = new List<string>();
                                try
                                {
                                    foreach (var Tags in temp.SelectNodes(@"//div[@class='tags']/a"))
                                    {
                                        tags.Add(Tags.InnerText.Replace("\n", "").Replace("\r", ""));
                                    }
                                }
                                catch (NullReferenceException) { }
                                catch (Exception)
                                {
                                    Loger.Instance.LocalInfo($"Jav类型解析失败，退出下载");
                                    downloadCollect.CompleteAdding();
                                    SaveData.CompleteAdding();
                                    JavDownloadCancel.Cancel();
                                }
                                TempData.Tags = tags.ToArray();
                                TempData.Describe = temp.SelectSingleNode("div/div/div[2]/div/p[2]").InnerText.ReplaceEntities().Replace("\n", "");
                                var Actress = new List<string>();
                                try
                                {
                                    foreach (var Tags in temp.SelectNodes(@"//div[@class='panel']/a"))
                                    //foreach (var Tags in temp.SelectNodes(@"div/div/div[2]/div/div[2]"))
                                    {
                                        Actress.Add(Tags.InnerText.Replace("\n", ""));
                                    }
                                }
                                catch (NullReferenceException) { }
                                catch (Exception)
                                {
                                    Loger.Instance.LocalInfo($"Jav人名解析失败");
                                    File.WriteAllText($"错误{DateTime.Now:mm-dd}.html", Page.Item2);
                                }
                                TempData.Actress = Actress.ToArray();
                                TempData.Magnet = temp.SelectSingleNode("div/div/div[2]/div/a[1]").Attributes["href"].Value;
                                SaveData.Add(new Tuple<int, JavInfo>(Page.Item1, TempData));
                            }
                        }
                        catch (Exception)
                        {
                            Loger.Instance.LocalInfo($"Jav解析失败，推测下载完毕");

                            File.WriteAllText($"错误{Guid.NewGuid().ToString()}.html", Page.Item2);

                            Setting._GlobalSet.JavFin = false;
                            downloadCollect.CompleteAdding();
                            SaveData.CompleteAdding();
                            JavDownloadCancel.Cancel();
                        }
                    }
                    SaveData.CompleteAdding();
                });
            }
            Task GetOldData()
            {
                return Task.Run(() =>
                {
                    {
                        Loger.Instance.LocalInfo($"开始Nyaa旧数据下载");
                        if (Setting._GlobalSet.NyaaFin) return;
                        var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                        NyaaOldDownloadCancel = new CancellationTokenSource();
                        if (Setting._GlobalSet.NyaaLastPageIndex < 2600000) Setting._GlobalSet.NyaaLastPageIndex = 2600000;
                        DownloadLoop(@"https://sukebei.nyaa.si/view/", Setting._GlobalSet.NyaaLastPageIndex, DownloadCollect, NyaaOldDownloadCancel, true, true);
                        HandlerOldNyaaHtml(DownloadCollect);
                    }
                    return;

                    if (!Setting._GlobalSet.MiMiFin)
                    {
                        var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                        MiMiDownloadCancel = new CancellationTokenSource();
                        /* DownloadLoop(Setting._GlobalSet.MiMiAiAddress, Setting._GlobalSet.MiMiAiPageIndex, DownloadCollect, MiMiDownloadCancel, true);
                         HandlerMiMiHtml(DownloadCollect);*/
                    }

                    if (!Setting._GlobalSet.NyaaFin)
                    {
                        var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                        NyaaDownloadCancel = new CancellationTokenSource();
                        DownloadLoop(Setting.NyaaAddress, Setting._GlobalSet.NyaaLastPageIndex, DownloadCollect, NyaaDownloadCancel, true);
                        HandlerOldNyaaHtml(DownloadCollect);
                    }
                    else
                    {
                        Loger.Instance.LocalInfo($"Nyaa下载完毕，跳过旧数据获取");
                    }
                    if (Setting._GlobalSet.JavFin)
                    {
                        var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                        DownloadLoop(Setting._GlobalSet.JavAddress, Setting._GlobalSet.JavLastPageIndex, DownloadCollect, JavDownloadCancel);
                        HandlerJavHtml(DownloadCollect);
                    }
                    else
                    {
                        Loger.Instance.LocalInfo($"JAV下载完毕，跳过旧数据获取");
                    }
                });
            }
        }

        private Task GetMiMiData()
        {
            long NewSize = 0;

            MiMiDownloadCancel = new CancellationTokenSource();
            return Task.Run(() =>
            {
                var HtmlDoc = new HtmlDocument();
                GetMiMiNewDataTimer = new System.Timers.Timer(1000);
                GetMiMiNewDataTimer.Elapsed += delegate
                {
                    Loger.Instance.LocalInfo("开始获取新MiMi信息");
                    GetMiMiNewDataTimer.Stop();
                    MiMiSpan.Reset();
                    var RunSpan = new Stopwatch();
                    RunSpan.Start();
                    NewSize = 0;
                    DownLoadWork(1);
                    Thread.Sleep(5000);
                    GetMiMiNewDataTimer.Interval = new Random().Next(12, 24) * 3600 * 1000;
                    Setting.MiMiDownLoadNow = DateTime.Now.AddMilliseconds(GetMiMiNewDataTimer.Interval).ToString("MM-dd|HH:mm");
                    Setting._GlobalSet.totalDownloadBytes += NewSize;
                    Loger.Instance.LocalInfo($"MiMi:下载完毕,耗时{RunSpan.Elapsed:mm\\分ss\\秒},消耗流量{HumanReadableFilesize(NewSize)}");
                    Loger.Instance.LocalInfo($"下次获得新数据为{Setting.MiMiDownLoadNow}");
                    RunSpan.Stop();
                    MiMiSpan.Restart();
                    GetMiMiNewDataTimer.Start();
                    //var ss = (TimeSpan.FromMilliseconds(GetMiMiNewDataTimer.Interval) - MiMiStop.Elapsed).ToString(@"hh\:mm\:ss");
                };
                GetMiMiNewDataTimer.Enabled = true;
                /*  if (!Setting._GlobalSet.MiMiFin)
                  {
                      while (!MiMiDownloadCancel.IsCancellationRequested)
                      {
                          DownLoadWork(Setting._GlobalSet.MiMiAiPageIndex);
                          Setting._GlobalSet.MiMiAiPageIndex += 1;
                      }
                  }*/
                void DownLoadWork(int index)
                {
                    var RTemp = DownLoadNew(index);
                    string RT = "";
                    if (RTemp != null)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(RTemp.Item1)) return;
                            foreach (var tempData in AnalyMiMiMainPage(HtmlDoc, RTemp.Item1))
                            {
                                if (DataBaseCommand.SaveToMiMiDataTablet(tempData, false))
                                {
                                    var Date = DateTime.Parse(tempData[3]).ToString("yyyy-MM-dd");
                                    Setting.MiMiDay = Date;
                                    var rtemp = DownLoadNew(_Uri: $"http://{new Uri(Setting._GlobalSet.MiMiAiAddress).Host}/{tempData[0]}");
                                    DataBaseCommand.GetWebInfoFromMiMi(tempData[0]);
                                    if (rtemp != null)
                                    {
                                        RT = rtemp.Item1;
                                        HandleMiMiPage(rtemp.Item1, Date);
                                        tempData[4] = bool.TrueString;
                                        DataBaseCommand.SaveToMiMiDataTablet(tempData);
                                    }
                                    else
                                    {
                                        DataBaseCommand.SaveToMiMiDataTablet(tempData);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Loger.Instance.LocalInfo($"MiMiAi页解析失败{ex.Message}");
                            File.WriteAllText("Error.html", RT);
                            if (ex.Message == "Object reference not set to an instance of an object.")
                            {
                                Setting._GlobalSet.MiMiFin = true;
                                MiMiDownloadCancel.Cancel();
                            }
                        }
                        finally
                        {
                            Loger.Instance.LocalInfo($"判断MiMi下载完成,退出下载进程");
                        }
                    }
                }
                void HandleMiMiPage(string PageData, string Date)
                {
                    try
                    {
                        var _HtmlDoc = new HtmlDocument();
                        _HtmlDoc.LoadHtml(PageData);
                        List<MiMiAiData> ItemList = new List<MiMiAiData>();
                        var Temp = new MiMiAiData();
                        var Index = 0;
                        Setting.MiMiDownLoadNow = "0";
                        foreach (var Child in _HtmlDoc.DocumentNode.SelectNodes("//div[@class='t_msgfont']")[0].ChildNodes)
                        {
                            Setting.MiMiDownLoadNow = ((double)Child.StreamPosition / _HtmlDoc.RemainderOffset).ToString("p");
                            if (Temp.InfoList == null) Temp.InfoList = new List<MiMiAiData.BasicData>();
                            switch (Child.Name)
                            {
                                case "a":
                                    {
                                        Temp.Index = Index;
                                        Temp.Date = Date;
                                        var DownloadData = DownLoadNew(Index: -3, _Uri: Child.Attributes["href"].Value, Mode: true);
                                        if (!string.IsNullOrEmpty(DownloadData.Item1))
                                        {
                                            DataBaseCommand.SaveToMiMiDataErrorUnit(new[]
                                                     {
                                        Temp.Date,
                                        Index.ToString(),
                                        Temp.InfoList.Count.ToString(),
                                        "torrent",
                                        Child.Attributes["href"].Value,
                                        DownloadData.Item1,
                                        bool.FalseString
                                    });
                                            Temp.InfoList.Add(new MiMiAiData.BasicData() { Type = "torrent", info = Child.Attributes["href"].Value });
                                        }
                                        else
                                        {
                                            Temp.InfoList.Add(new MiMiAiData.BasicData() { Type = "torrent", info = Child.Attributes["href"].Value, Data = DownloadData.Item2 });
                                        }
                                        ItemList.Add(Temp);
                                        Temp = new MiMiAiData();
                                        Interlocked.Increment(ref Index);
                                    }
                                    break;

                                case "#text":
                                    {
                                        var innerText = Child.InnerText.Replace("\r\n", "").Replace("&nbsp;", "");
                                        if (innerText.StartsWith(" ")) innerText = innerText.Remove(0, 1);
                                        if (string.IsNullOrEmpty(Temp.Title) && innerText != "\r\n")
                                        {
                                            Temp.Title = innerText;
                                            break;
                                        }
                                        if (!string.IsNullOrEmpty(innerText))
                                            Temp.InfoList.Add(new MiMiAiData.BasicData() { Type = "text", info = innerText });
                                    }
                                    break;

                                case "br":
                                    break;

                                case "img":
                                    {
                                        var DownloadData = DownLoadNew(Index: -2, _Uri: Child.Attributes["src"].Value, Mode: true);
                                        if (DownloadData == null) break;
                                        if (!string.IsNullOrEmpty(DownloadData.Item1))
                                        {
                                            DataBaseCommand.SaveToMiMiDataErrorUnit(new[]
                                                  {
                                        Temp.Date,
                                        Index.ToString(),
                                        Temp.InfoList.Count.ToString(),
                                        "img",
                                        Child.Attributes["src"].Value,
                                        DownloadData.Item1,
                                        bool.FalseString
                                    });
                                            Temp.InfoList.Add(new MiMiAiData.BasicData() { Type = "img", info = Child.Attributes["src"].Value });
                                        }
                                        else
                                        {
                                            // _md5.ComputeHash(DownloadData.Item2);
                                            Temp.InfoList.Add(new MiMiAiData.BasicData() { Type = "img", info = Child.Attributes["src"].Value, Data = DownloadData.Item2 });
                                        }
                                    }
                                    break;

                                default:
                                    break;
                            }
                        }
                        DataBaseCommand.SaveToMiMiDataUnit(ItemList);
                        ItemList.Clear();
                        ItemList = null;
                        Temp = null;
                        GC.Collect();
                    }
                    catch (Exception ex)
                    {
                        Loger.Instance.LocalInfo($"MiMi页面解析错误");
                    }
                }

                Tuple<string, byte[]> DownLoadNew(int Index = -1, string _Uri = "", bool Mode = false)
                {
                    using (var request = new HttpRequest()
                    {
                        UserAgent = Http.ChromeUserAgent(),
                        ConnectTimeout = 5000,
                        CharacterSet = Encoding.GetEncoding("GBK")
                    })
                    {
                        if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                        try
                        {
                            if (_Uri.StartsWith("images")) return null;
                            var downurl = Index > -1 ? new Uri($"{Setting._GlobalSet.MiMiAiAddress}{Index}") : new Uri($"{_Uri}");
                            if (!Mode)
                            {
                                int ErrorCount = 1;
                                while (!MiMiDownloadCancel.IsCancellationRequested || ErrorCount != 0)
                                {
                                    try
                                    {
                                        HttpResponse response = request.Get(downurl);
                                        var RetS = response.ToString();
                                        NewSize += response.Ret.Length;
                                        return new Tuple<string, byte[]>(RetS, null);
                                    }
                                    catch (Exception ex)
                                    {
                                        if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");

                                        if (request.Response.RedirectAddress != null)
                                        {
                                            if (request.Response.RedirectAddress.Authority != downurl.Authority)
                                            {
                                                Loger.Instance.LocalInfo($"MiMiAi网址变更为{request.Response.RedirectAddress.Authority}");
                                                Setting._GlobalSet.MiMiAiAddress = Setting._GlobalSet.MiMiAiAddress.Replace(downurl.Authority, request.Response.RedirectAddress.Authority);
                                                downurl = Index != -1 ? new Uri($"{Setting._GlobalSet.MiMiAiAddress}{Index}") : new Uri($"{_Uri.Replace(downurl.Authority, request.Response.RedirectAddress.Authority)}");
                                                continue;
                                            }
                                        }
                                        if (ex.Message.StartsWith("Cannot access a disposed object"))
                                        {
                                            Loger.Instance.LocalInfo($"SSR异常，退出全部下载进程");
                                            MiMiDownloadCancel.Cancel();
                                            break;
                                        }
                                        Loger.Instance.LocalInfo($"{ex.Message}");
                                        Loger.Instance.LocalInfo($"下载{downurl}失败，计数{ErrorCount}次");
                                        var time = new Random().Next(5000, 10000);
                                        for (var i = time; i > 0; i -= 1000)
                                        {
                                            if (MiMiDownloadCancel.IsCancellationRequested) break;
                                            Loger.Instance.WaitTime(i / 1000);
                                            Thread.Sleep(1000);
                                        }
                                        Interlocked.Increment(ref ErrorCount);
                                        if (ErrorCount == 5)
                                        {
                                            ErrorCount = 0;
                                            Loger.Instance.LocalInfo($"下载{downurl}失败，退出下载进程");
                                            if (!Setting.CheckOnline(Setting._GlobalSet.SocksCheck))
                                            {
                                                Loger.Instance.LocalInfo($"检测到网络连接异常，退出全部下载方式");
                                                Setting.CancelSign.Cancel();
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    switch (Index)
                                    {
                                        case -2:
                                            {
                                                var _PostData = request.Get(downurl);
                                                if (!string.IsNullOrEmpty(_PostData.ToString()))
                                                {
                                                    NewSize += _PostData.Ret.Length;
                                                    return new Tuple<string, byte[]>("", _PostData.Ret);
                                                }
                                                else
                                                {
                                                    throw new Exception("Unknown Error");
                                                }
                                            }

                                        case -3:
                                            {
                                                var _TempUri = downurl;
                                                var _TempDownloadUri = _TempUri.Scheme + Uri.SchemeDelimiter + _TempUri.Authority + "/load.php";
                                                var _PostData = request.Post(_TempDownloadUri, new RequestParams()
                                        {
                                            new KeyValuePair<string, string>("ref", _TempUri.Query.Split('=')[1]),
                                            new KeyValuePair<string, string>("submit ", "点击下载")
                                        }).ToBytes();
                                                if (_PostData.Length > 1000)
                                                {
                                                    return new Tuple<string, byte[]>("", _PostData);
                                                }
                                                else if (Encoding.Default.GetString(_PostData).StartsWith("No such file"))
                                                {
                                                    throw new Exception("No such file");
                                                }
                                            }
                                            break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return new Tuple<string, byte[]>(ex.Message, null);
                                }
                            }
                        }
                        catch (UriFormatException UriError)
                        {
                            Loger.Instance.LocalInfo($"地址错误{UriError.Message}");
                            return new Tuple<string, byte[]>($"地址错误{UriError.Message}", null);
                        }
                    }
                    return null;
                }
            }
            , MiMiDownloadCancel.Token);
        }

        private IEnumerable<string[]> AnalyMiMiMainPage(HtmlDocument HtmlDoc, string Page, bool RetAll = false)
        {
            if (string.IsNullOrEmpty(Page))
            {
                Loger.Instance.LocalInfo($"MiMi页面为空"); yield break;
            }
            HtmlDoc.LoadHtml(Page);
            foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/center/form/div[1]/div/table"))
            {
                var TempData = new string[]
                {
                        item.SelectSingleNode("tr/td[1]/a").Attributes["href"].Value,
                        item.SelectSingleNode("tr/td[3]/a[1]").InnerText,
                        item.SelectSingleNode("tr/td[4]/a").InnerText,
                        item.SelectSingleNode("tr/td[4]/span").InnerText,
                        bool.FalseString
                };
                if (TempData[2] == "mimi")
                {
                    if (TempData[1].ToUpper().Contains("BT"))
                    {
                        yield return TempData;
                    }
                }
                if (RetAll)
                {
                    yield return TempData;
                }
            }
            yield break;
        }

        private string HumanReadableFilesize(double size)
        {
            var units = new[] { "B", "KB", "MB", "GB", "TB", "PB" };
            double mod = 1024.0;
            var DoubleCount = new List<double>();
            while (size >= mod)
            {
                size /= mod;
                DoubleCount.Add(size);
            }
            var Ret = "";
            for (int j = DoubleCount.Count; j > 0; j--)
            {
                if (j == DoubleCount.Count)
                {
                    Ret += $"{Math.Floor(DoubleCount[j - 1])}{units[j]}";
                }
                else
                {
                    Ret += $"{Math.Floor(DoubleCount[j - 1] - (Math.Floor(DoubleCount[j]) * 1024))}{units[j]}";
                }
            }
            return Ret;
        }

        private Task HandlerMiMiHtml(BlockingCollection<Tuple<int, string>> downloadCollect)
        {
            return Task.WhenAny(Task.Run(() =>
             {
                 var HtmlDoc = new HtmlDocument();
                 foreach (var _Temp in downloadCollect.GetConsumingEnumerable())
                 {
                     try
                     {
                         HtmlDoc.LoadHtml(_Temp.Item2);
                         foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/center/form/div[1]/div/table"))
                         {
                             var TempData = new string[]
                             {
                            item.SelectSingleNode("tr/td[1]/a").Attributes["href"].Value,
                            item.SelectSingleNode("tr/td[3]/a[1]").InnerText,
                            item.SelectSingleNode("tr/td[4]/a").InnerText,
                            item.SelectSingleNode("tr/td[4]/span").InnerText,
                            bool.FalseString
                             };
                             if (TempData[2] == "mimi")
                             {
                                 if (TempData[1].Contains("BT合集"))
                                 {
                                     DataBaseCommand.SaveToMiMiDataTablet(TempData);
                                 }
                             }
                         }
                         Setting._GlobalSet.MiMiAiPageIndex = _Temp.Item1;
                     }
                     catch (Exception ex)
                     {
                         Loger.Instance.LocalInfo($"MiMiAi页解析失败{ex.Message}");
                         if (ex.Message == "Object reference not set to an instance of an object.")
                         {
                             Loger.Instance.LocalInfo($"判断下载完成,退出下载进程");
                             Setting._GlobalSet.MiMiFin = true;
                             MiMiDownloadCancel.Cancel();
                             break;
                         }
                         continue;
                     }
                 }
             }, MiMiDownloadCancel.Token), Task.Run(() =>
              {
                  void HandleMiMiPage(string PageData)
                  {
                      var HtmlDoc = new HtmlDocument();
                      HtmlDoc.LoadHtml(PageData);
                      List<MiMiAiData> ItemList = new List<MiMiAiData>();
                      var Temp = new MiMiAiData();
                      var Index = 0;
                      foreach (var Child in HtmlDoc.DocumentNode.SelectNodes("//div[@class='t_msgfont']")[0].ChildNodes)
                      {
                          if (Temp.InfoList == null) Temp.InfoList = new List<MiMiAiData.BasicData>();
                          switch (Child.Name)
                          {
                              case "a":
                                  {
                                      Temp.InfoList.Add(new MiMiAiData.BasicData() { Type = "torrent", info = Child.Attributes["href"].Value });
                                      Temp.Index = Index;
                                      Temp.Date = Setting.MiMiDay;
                                      ItemList.Add(Temp);
                                      Temp = new MiMiAiData();
                                      Interlocked.Increment(ref Index);
                                  }
                                  break;

                              case "#text":
                                  {
                                      var innerText = Child.InnerText.Replace("\r\n", "").Replace("&nbsp;", "");
                                      if (innerText.StartsWith(" ")) innerText = innerText.Remove(0, 1);
                                      if (string.IsNullOrEmpty(Temp.Title) && innerText != "\r\n")
                                      {
                                          Temp.Title = innerText;
                                          break;
                                      }
                                      if (!string.IsNullOrEmpty(innerText))
                                          Temp.InfoList.Add(new MiMiAiData.BasicData() { Type = "text", info = innerText });
                                  }
                                  break;

                              case "br":
                                  break;

                              case "img":
                                  Temp.InfoList.Add(new MiMiAiData.BasicData() { Type = "img", info = Child.Attributes["src"].Value });
                                  break;

                              default:
                                  break;
                          }
                      }
                      DataBaseCommand.SaveToMiMiDataUnit(ItemList);
                  }
                  using (var request = new HttpRequest()
                  {
                      UserAgent = Http.ChromeUserAgent(),
                      ConnectTimeout = 20000,
                      CharacterSet = Encoding.GetEncoding("GBK")
                  })
                  {
                      if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                      var ErrorCount = 0;
                      while (!MiMiDownloadCancel.Token.IsCancellationRequested)
                      {
                          var PageInfo = DataBaseCommand.GetDataFromMiMi("TabletInfo") as BsonDocument;
                          if (PageInfo != null)
                          {
                              Setting.MiMiDay = PageInfo["_id"].AsDateTime.ToString("yyyy-MM-dd");
                              var downurl = new Uri($"http://{new Uri(Setting._GlobalSet.MiMiAiAddress).Host}/{PageInfo["Uri"].AsString}");
                              try
                              {
                                  HttpResponse response = request.Get(downurl);
                                  /* if (response.Address.Authority != downurl.Authority)
                                   {
                                       Loger.Instance.LocalInfo($"MiMiAi网址变更为{response.Address.Authority}");
                                       Setting._GlobalSet.MiMiAiAddress.Replace(downurl.Authority, response.Address.Authority);
                                   }*/
                                  HandleMiMiPage(response.ToString());
                                  PageInfo["Status"] = bool.TrueString;
                                  DataBaseCommand.SaveToMiMiDataTablet(PageInfo);
                              }
                              catch (Exception ex)
                              {
                                  try
                                  {
                                      if (request.Response.RedirectAddress != null)
                                      {
                                          if (request.Response.RedirectAddress.Authority != downurl.Authority)
                                          {
                                              Loger.Instance.LocalInfo($"MiMiAi网址变更为{request.Response.RedirectAddress.Authority}");
                                              Setting._GlobalSet.MiMiAiAddress = Setting._GlobalSet.MiMiAiAddress.Replace(downurl.Authority, request.Response.RedirectAddress.Authority);
                                          }
                                      }
                                  }
                                  catch (Exception)
                                  {
                                  }

                                  if (MiMiDownloadCancel.Token.IsCancellationRequested)
                                  {
                                      break;
                                  }
                                  Interlocked.Increment(ref ErrorCount);
                                  Loger.Instance.LocalInfo($"{ex.Message}");
                                  Loger.Instance.LocalInfo($"下载MiMiAi数据失败，计数{ErrorCount}次");
                                  if (ex.Message.StartsWith("Cannot access a disposed object"))
                                  {
                                      Loger.Instance.LocalInfo($"SSR异常，退出全部下载进程");
                                      MiMiDownloadCancel.Cancel();
                                      break;
                                  }
                                  var time = new Random().Next(5000, 10000);
                                  for (var i = time; i > 0; i -= 1000)
                                  {
                                      Loger.Instance.WaitTime(i / 1000);
                                      Thread.Sleep(1000);
                                  }
                                  if (ErrorCount > 5)
                                  {
                                      ErrorCount = 0;
                                      Loger.Instance.LocalInfo($"下载MIMiAi数据错误超过5次，退出下载进程");
                                      if (!Setting.CheckOnline(Setting._GlobalSet.SocksCheck))
                                      {
                                          Loger.Instance.LocalInfo($"检测到网络连接异常，退出全部下载方式");
                                          Setting.CancelSign.Cancel();
                                      }
                                      MiMiDownloadCancel.Cancel();
                                      break;
                                  }
                              }
                              finally
                              {
                                  ErrorCount = 0;
                              }
                          }
                          else
                          {
                              Thread.Sleep(10000);
                          }
                      }
                  }
              }), Task.Run(() =>
              {
                  using (var request = new HttpRequest()
                  {
                      UserAgent = Http.ChromeUserAgent(),
                      ConnectTimeout = 10000,
                      CharacterSet = Encoding.GetEncoding("GBK")
                  })
                  {
                      if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                      while (!MiMiDownloadCancel.Token.IsCancellationRequested)
                      {
                          var UnitInfo = DataBaseCommand.GetDataFromMiMi("UnitInfo") as MiMiAiData;
                          if (UnitInfo != null)
                          {
                              Setting.MiMiDownLoadNow = UnitInfo.Date;
                              foreach (var Item in UnitInfo.InfoList)
                              {
                                  switch (Item.Type)
                                  {
                                      case "img":
                                          {
                                              try
                                              {
                                                  var _PostData = request.Get(Item.info);
                                                  if (!string.IsNullOrEmpty(_PostData.ToString()))
                                                  {
                                                      Item.Data = _PostData.Ret;
                                                  }
                                                  else
                                                  {
                                                      throw new Exception("Unknown Error");
                                                  }
                                              }
                                              catch (Exception ex)
                                              {
                                                  DataBaseCommand.SaveToMiMiDataErrorUnit(new[]
                                                    {
                                                      UnitInfo.Date,
                                                      UnitInfo.Index.ToString(),
                                                      UnitInfo.InfoList.IndexOf(Item).ToString(),
                                                      "img",
                                                      Item.info,
                                                      ex.Message,
                                                      bool.FalseString
                                                  });
                                              }
                                          }
                                          break;

                                      case "torrent":
                                          {
                                              var _TempUri = new Uri(Item.info);
                                              try
                                              {
                                                  var _TempDownloadUri = _TempUri.Scheme + Uri.SchemeDelimiter + _TempUri.Authority + "/load.php";
                                                  var _PostData = request.Post(_TempDownloadUri, new RequestParams()
                                              {
                                                  new KeyValuePair<string, string>("ref", _TempUri.Query.Split('=')[1]),
                                                  new KeyValuePair<string, string>("submit ", "点击下载")
                                              }).ToBytes();
                                                  if (_PostData.Length > 1000)
                                                  {
                                                      Item.Data = _PostData;
                                                  }
                                                  else if (Encoding.Default.GetString(_PostData).StartsWith("No such file"))
                                                  {
                                                      throw new Exception("No such file");
                                                  }
                                                  else
                                                  {
                                                      throw new Exception("Unknown Error");
                                                  }
                                              }
                                              catch (Exception ex)
                                              {
                                                  DataBaseCommand.SaveToMiMiDataErrorUnit(new[]
                                                  {
                                                      UnitInfo.Date,
                                                      UnitInfo.Index.ToString(),
                                                      UnitInfo.InfoList.IndexOf(Item).ToString(),
                                                      "torrent",
                                                      Item.info,
                                                      ex.Message,
                                                      bool.FalseString
                                                  });
                                              }
                                          }
                                          break;

                                      default:
                                          break;
                                  }
                              }
                              UnitInfo.Status = true;
                              DataBaseCommand.SaveToMiMiDataUnit(UnitInfo);
                          }
                          else
                          {
                              Thread.Sleep(10000);
                          }
                      }
                  }
              }));
        }

        private Task DownloadLoop(string Address, int LastPageIndex, BlockingCollection<Tuple<int, string>> downloadCollect, CancellationTokenSource token, bool CheckMode = false, bool NyaaSocks = false)
        {
            return Task.Factory.StartNew(() =>
              {
                  using (var request = new HttpRequest()
                  {
                      UserAgent = Http.ChromeUserAgent(),
                      ConnectTimeout = 20000,
                  })
                  {
                      Setting.JavPageCount = downloadCollect.Count;
                      int ErrorCount = 0;
                      if (NyaaSocks)
                      {
                          if (Setting._GlobalSet.NyaaSocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.NyaaSocks5Point}");
                      }
                      else if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                      while (!token.Token.IsCancellationRequested)
                      {
                          if (downloadCollect.Count > 3)
                          {
                              Task.Delay(10000);
                              continue;
                          }
                          if (CheckMode)
                          {
                              if (DataBaseCommand.GetNyaaCheckPoint(LastPageIndex))
                              {
                                  Interlocked.Increment(ref LastPageIndex);
                                  continue;
                              }
                          }
                          var downurl = new Uri($"{Address}{LastPageIndex}");
                          try
                          {
                              var time = new Random().Next(2000, 10000);
                              for (var i = time; i > 0; i -= 1000)
                              {
                                  if (token.Token.IsCancellationRequested)
                                  {
                                      break;
                                  }
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
                              if (!NyaaSocks)
                              {
                                  Setting.SSR.Stop();
                                  Setting.SSR.Start();
                              }
                              else
                              {
                                  Setting.NyaaSSR.Stop();
                                  Setting.NyaaSSR.Start();
                              }
                              if (NyaaSocks)
                              {
                                  if (Setting._GlobalSet.NyaaSocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.NyaaSocks5Point}");
                              }
                              else if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                              Interlocked.Increment(ref ErrorCount);
                              if (new Uri(Address).Host == new Uri(Setting.NyaaAddress).Host)
                              {
                                  if (ex.Message == "NotFound")
                                  {
                                      Interlocked.Increment(ref LastPageIndex);
                                      continue;
                                  }
                                  else if (ex.Message == "Forbidden")
                                  {
                                      Loger.Instance.LocalInfo($"检测到Nyaa下载页面超出50,结束下载进程");
                                      break;
                                  }
                              }
                              if (ex.Message.StartsWith("Cannot access a disposed object"))
                              {
                                  Loger.Instance.LocalInfo($"SSR异常，退出全部下载进程");
                                  token.Cancel();
                                  break;
                              }
                              Loger.Instance.LocalInfo($"{ex.Message}");
                              Loger.Instance.LocalInfo($"下载{downurl.ToString()}失败，计数{ErrorCount}次");
                              //Loger.Instance.LocalInfo(ex.Message);
                              var time = new Random().Next(5000, 10000);
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
                                  ErrorCount = 0;
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
            Setting.DownloadManage.JavDownloadCancel.Cancel();
            Setting.DownloadManage.NyaaDownloadCancel.Cancel();
            Setting.DownloadManage.MiMiDownloadCancel.Cancel();
            Setting.DownloadManage.MiMiAiStoryDownloadCancel.Cancel();

            while (!Setting.DownloadManage.JavOldDownloadRunning && !Setting.DownloadManage.JavDownloadCancel.IsCancellationRequested && !MiMiDownloadCancel.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }
        }
    }
}