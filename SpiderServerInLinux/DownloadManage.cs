using HtmlAgilityPack;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Shadowsocks.Controller;
using SocksSharp;
using SocksSharp.Proxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using xNet;

//copy /y "$(TargetPath)" "Z:\publish\"
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
        public CancellationTokenSource SISDownloadCancel = new CancellationTokenSource();

        public System.Timers.Timer GetNyaaNewDataTimer = null;
        public System.Timers.Timer GetJavNewDataTimer = null;
        public System.Timers.Timer GetMiMiNewDataTimer = null;
        public System.Timers.Timer GetMiMiAiStoryDataTimer = null;
        public System.Timers.Timer GetT66yDataTimer = null;
        public System.Timers.Timer GetSISDataTimer = null;

        public Stopwatch MiMiSpan = new Stopwatch();
        public Stopwatch MiMiStorySpan = new Stopwatch();
        public Stopwatch JavSpan = new Stopwatch();
        public Stopwatch NyaaSpan = new Stopwatch();
        public Stopwatch GetT66ySpan = new Stopwatch();
        public Stopwatch GetSISSpan = new Stopwatch();

        public bool JavOldDownloadRunning = false;

        public DownloadManage(bool v = true)
        {
            if (v)
                Load();
        }

        public async void Load()
        {
            if (Setting.CheckOnline(Setting._GlobalSet.SocksCheck))
            {
                Loger.Instance.LocalInfo("网络连接正常，正在加载下载进程");
                Loger.Instance.LocalInfo("初始化下载");
                await Task.WhenAll(GetJavNewData(), GetNyaaNewData(), GetMiMiData(), GetMiMiAiStoryData(), GetT66yData(), GetSis001Data());
            }
            else
            {
                Loger.Instance.LocalInfo("外网访问失败，等待操作");
            }
            //await Task.WhenAll(GetSis001Data());
        }

        // private static bool ReadSIS = false;

        public Task GetSis001Data()
        {
            //Setting._GlobalSet.SISSkip = 0;
            // Setting._GlobalSet.SISPageIndex = 220;
            var db = new LiteDatabase("Filename=SIS.db;ReadOnly=True");
            var SISDB = db.GetCollection<SISData>("SISData");
            //var STSDB = db.GetCollection<SISData>("SISData");
            var STSImg = db.GetCollection<SISImgData>("ImgData");

            if (!string.IsNullOrEmpty(Setting.SISDownLoadNow)) return Task.CompletedTask;
            int PageCount = 0;
            Task.Factory.StartNew(() =>
            {
                return;
                do
                {
                    //var dbimg = new LiteDatabase("SIS.db;Connection=Shared;ReadOnly=True");
                    var Fitem = SISDB.FindOne(x => !x.Status);
                    //dbimg.Dispose();
                    try
                    {
                        if (Fitem == null)
                        {
                            Setting.SISDownLoadNow = $"Fitem空";

                            continue;
                        }
                        Setting.SISDownLoadNow = $"当前下载{Fitem.Date}日\n\r第{Fitem.id}篇,计数{Setting._GlobalSet.SISSkip}";
                        if (!string.IsNullOrEmpty(Fitem.HtmlData))
                        {
                            HandleSISPage(Fitem, Fitem.HtmlData, Setting._GlobalSet.SISSkip);
                            Fitem.Status = true;
                            DataBaseCommand.SaveToSISDataUnit("SISData", UnitData: Fitem);
                        }
                        else
                        {
                            Setting.SISDownLoadNow = $"当前下载{Fitem.Date}日\n\r第{Fitem.id}篇页面,计数{Setting._GlobalSet.SISSkip}";

                            // Fitem.HtmlData = DownloadMainPage(_Uri: $"https://www.sis001.com/forum/viewthread.php?tid={Fitem.id}");
                            Fitem.HtmlData = DownloadMainPage(_Uri: $"http://www.sis001.com/forum/{Fitem.Uri}");
                            if (!string.IsNullOrEmpty(Fitem.HtmlData))
                            {
                                Setting.SISDownLoadNow = $"当前下载{Fitem.Date}日\n\r第{Fitem.id}篇页面下载完毕,计数{Setting._GlobalSet.SISSkip}";

                                HandleSISPage(Fitem, Fitem.HtmlData, Setting._GlobalSet.SISSkip);
                                Fitem.Status = true;
                                DataBaseCommand.SaveToSISDataUnit("SISData", UnitData: Fitem);
                            }
                        }

                        Setting._GlobalSet.SISSkip += 1;
                    }
                    catch (Exception ex)
                    {
                        Loger.Instance.LocalInfo($"SIS下载图片错误,错误信息{ex.Message}\n\r{ex.StackTrace}");
                        continue;
                    }
                } while (true);
            });
            Task.Factory.StartNew(() =>
            {
                var ContinueCount = 0;
                Loger.Instance.LocalInfo($"开始SIS下载,计数{ContinueCount}");
                Thread.Sleep(2000);
                while (!SISDownloadCancel.IsCancellationRequested)
                {
                    var DoanloadPageHtml = string.Empty;
                    Interlocked.Increment(ref Setting.SISPageCount);

                    Setting.SISDownLoadNow = $"当前下载第{Setting.SISPageCount}";
                    DoanloadPageHtml = DownloadMainPage(Setting.SISPageCount);
                    if (string.IsNullOrEmpty(DoanloadPageHtml))
                    {
                        Wait("SIS下载到空网页");
                        continue;
                    }
                    PageCount = 0;
                    foreach (var item in AnalySISMainPage(DoanloadPageHtml))
                    {
                        try
                        {
                            var Temp = new SISData();
                            if (item[3] == null) continue;
                            Temp.id = int.Parse(item[3]);
                            Setting.SISDownLoadNow = $"当前下载{Temp.Date}日第{Setting.SISPageCount}页\n\r第{PageCount}篇,计数{ContinueCount}";
                            //using (var db = new LiteDatabase(@"SIS.db"))
                            {
                                if (SISDB.Exists(x => x.id == Temp.id))
                                {
                                    //db.Dispose();
                                    Interlocked.Increment(ref ContinueCount);
                                    if (ContinueCount > 50)
                                    {
                                        {
                                            //var ImgDB = dbimg.GetCollection<SISImgData>("ImgData");
                                            /* try
                                             {
                                                 foreach (var Fitem in SISDB.Find(Query.All(), Setting._GlobalSet.SISSkip))
                                                 {
                                                     try
                                                     {
                                                         if (Fitem == null)
                                                         {
                                                             Setting.SISDownLoadNow = $"Fitem空";

                                                             continue;
                                                         }
                                                         Setting.SISDownLoadNow = $"当前下载{Fitem.Date}日\n\r第{Fitem.id}篇,计数{Setting._GlobalSet.SISSkip}";
                                                         if (!string.IsNullOrEmpty(Fitem.HtmlData))
                                                         {
                                                             HandleSISPage(Fitem, Fitem.HtmlData, Setting._GlobalSet.SISSkip);
                                                             Fitem.Status = true;
                                                             DataBaseCommand.SaveToSISDataUnit("SISData", UnitData: Temp);
                                                         }
                                                         Setting._GlobalSet.SISSkip += 1;
                                                         if (Setting.SISPageCount == 0) break;
                                                     }
                                                     catch (Exception ex)
                                                     {
                                                         File.WriteAllText(Fitem.id.ToString(), Fitem.HtmlData);
                                                         break;
                                                         Loger.Instance.LocalInfo($"SIS下载图片错误,错误信息{ex.Message}\n\r{ex.StackTrace}");
                                                     }
                                                 }
                                                 break;
                                             }
                                             catch (Exception ex)
                                             {
                                             }*/
                                            /* foreach (var ImgTemp in ImgDB.Find(x => !x.Status))
                                               {
                                                   if (ImgTemp != null)
                                                   {
                                                       Setting.SISDownLoadNow = $"开始{ImgTemp.Date}日图片下载{ImgTemp.FromList.First()}篇";
                                                       ImgTemp.img = DownloadImgAsync(ImgTemp.id).Result;
                                                       if (ImgTemp.img == null)
                                                           ImgTemp.img = new byte[] { 0 };
                                                       else
                                                           ImgTemp.Hash = Convert.ToBase64String(new MD5CryptoServiceProvider().ComputeHash(ImgTemp.img));
                                                       ImgTemp.img = null;
                                                   }
                                                   DataBaseCommand.SaveToSISDataUnit("img", UnitData: ImgTemp);
                                                   if (Setting.SISPageCount == 0) break;
                                               }*/
                                            //dbimg.Dispose();
                                        }
                                        ContinueCount = 0;
                                        Setting.SISPageCount = Setting._GlobalSet.SISPageIndex;
                                        break;
                                    }
                                    continue;
                                }
                                else
                                {
                                    ContinueCount = 0;
                                }
                            }
                            Temp.Date = item[2];
                            Temp.Type = item[4];
                            Temp.Title = item[1];
                            Temp.Uri = item[0];

                            Temp.HtmlData = DownloadMainPage(_Uri: $"http://www.sis001.com/forum/{Temp.Uri}");
                            if (!string.IsNullOrEmpty(Temp.HtmlData))
                            {
                                HandleSISPage(Temp, Temp.HtmlData, Setting.SISPageCount);
                            }
                            DataBaseCommand.SaveToSISDataUnit("SISData", UnitData: Temp);

                            /* if (Setting.SISPageCount < 5)
                             {
                                 if (DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd") == DateTime.Parse(Temp.Date).ToString("yyyy-MM-dd"))
                                 {
                                     if (Setting._GlobalSet.SISPageIndex > 5)
                                     {
                                         Setting.SISPageCount = Setting._GlobalSet.SISPageIndex;
                                         break;
                                     }
                                     else
                                     {
                                         if (Setting._GlobalSet.SISPageIndex != Setting.SISPageCount)
                                             Setting._GlobalSet.SISPageIndex = Setting.SISPageCount;
                                     }
                                 }
                             }*/
                        }
                        catch (Exception ex)
                        {
                            Loger.Instance.LocalInfo($"SIS下载{item[3]}错误,错误信息{ex.Message}\n\r{ex.StackTrace}");
                            File.AppendAllLines("SIS.txt", new string[] { item[3] });
                        }
                        if (Setting.SISPageCount == 0) break;
                        PageCount += 1;
                    }
                    if (Setting.SISPageCount > 100)
                    {
                        Setting._GlobalSet.SISPageIndex = Setting.SISPageCount;
                    }
                }
            }, SISDownloadCancel.Token);
            return Task.Run(() =>
            {
                GetSISDataTimer = new System.Timers.Timer(1000);
                GetSISDataTimer.Elapsed += delegate
                {
                    Loger.Instance.LocalInfo($"开始SIS下载");
                    GetSISDataTimer.Stop();
                    GetSISSpan.Reset();
                    Setting.SISPageCount = 0;
                    GetSISDataTimer.Interval = new Random().Next(12, 24) * 3600 * 1000;
                    Loger.Instance.LocalInfo($"SIS下次获得新数据为{DateTime.Now.AddMilliseconds(GetSISDataTimer.Interval).ToString("MM-dd|HH:mm")}");

                    GetSISSpan.Restart();
                    GetSISDataTimer.Start();
                };
                GetSISDataTimer.Enabled = true;
            }, SISDownloadCancel.Token);

            string DownloadMainPage(int Index = -1, string _Uri = "")
            {
                using (var request = new HttpRequest()
                {
                    UserAgent = Http.ChromeUserAgent(),
                    ConnectTimeout = 5000,
                })
                {
                    if (Setting._GlobalSet.NyaaSocksCheck && Setting.NyaaSocks5Point != 0)
                        request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.NyaaSocks5Point}");
                    try
                    {
                        int ErrorCount = 1;
                        while (!SISDownloadCancel.IsCancellationRequested || ErrorCount != 0)
                        {
                            var downurl = Index > -1 ? new Uri($"http://www.sis001.com/forum/forum-25-{Index}.html") : new Uri($"{_Uri}");
                            try
                            {
                                if (Index > -1)
                                {
                                    Setting.SISDownLoadNow = $"开始下载第{Index}页";
                                }
                                else Setting.SISDownLoadNow = $"开始下载{downurl.Segments.Last()}\n\r页";
                                HttpResponse response = request.Get(downurl);
                                response.ToString();
                                Setting._GlobalSet.totalDownloadBytes += response.Ret.Length;
                                if (Index > -1)
                                {
                                    Setting.SISDownLoadNow = $"第{Index}页下载完毕";
                                }
                                else Setting.SISDownLoadNow = $"第{downurl.Segments.Last()}\n\r页下载完毕";

                                return Encoding.Default.GetString(response.Ret);
                            }
                            catch (Exception ex)
                            {
                                if (Setting._GlobalSet.SocksCheck && Setting.Socks5Point != 0)
                                    request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.Socks5Point}");
                                if (ex.Message.StartsWith("Cannot access a disposed object"))
                                {
                                    Loger.Instance.LocalInfo($"SSR异常，退出全部下载进程");
                                    break;
                                }
                                Loger.Instance.LocalInfo($"{ex.Message}");
                                Loger.Instance.LocalInfo($"下载{downurl}失败，计数{ErrorCount}次");
                                var time = new Random().Next(5000, 10000);
                                for (var i = time; i > 0; i -= 1000)
                                {
                                    if (SISDownloadCancel.IsCancellationRequested) break;
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
            IEnumerable<string[]> AnalySISMainPage(string Page)
            {
                if (string.IsNullOrEmpty(Page))
                {
                    Loger.Instance.LocalInfo($"SIS001页面为空"); yield break;
                }
                var HtmlDoc = new HtmlDocument();

                HtmlDoc.LoadHtml(Page);
                HtmlNodeCollection htmlNodes = null;
                htmlNodes = HtmlDoc.DocumentNode.SelectNodes("/html/body/div[4]/div[1]/div[7]/form/table[5]/tbody");
                if (htmlNodes == null)
                    htmlNodes = HtmlDoc.DocumentNode.SelectNodes("/html/body/div[4]/div[1]/div[7]/form/table[2]/tbody");
                if (htmlNodes.Count < 20)
                    htmlNodes = HtmlDoc.DocumentNode.SelectNodes("/html/body/div[4]/div[1]/div[7]/form/table[4]/tbody");

                foreach (var item in htmlNodes)
                {
                    var TempData = new string[5];
                    try
                    {
                        if (item.Id.StartsWith("normalthread_"))
                        {
                            var id = item.Id.Replace("normalthread_", "");
                            var temp = HtmlNode.CreateNode(item.OuterHtml);
                            var TypeName = temp.SelectSingleNode("/tbody[1]/tr[1]/th[1]/em[1]").InnerText;
                            var Title = temp.SelectSingleNode("/tbody[1]/tr[1]/th[1]/span[1]").InnerText;
                            var uri = temp.SelectSingleNode("/tbody[1]/tr[1]/th[1]/span[1]/a[1]").Attributes["href"].Value;
                            var Date = temp.SelectSingleNode("/tbody[1]/tr[1]/td[3]/em[1]").InnerText;
                            TempData = new string[]
                            {
                                uri,//地址
                                Title,//标题
                                Date,//日期
                                id,//编号
                                TypeName,//类型
                            };
                        }
                    }
                    catch (Exception)
                    {
                        Loger.Instance.LocalInfo($"SIS001页面解析错误");
                        File.AppendAllLines("SIS001.txt", new string[] { item.OuterHtml });
                    }
                    yield return TempData;
                }
                yield break;
            }
            void HandleSISPage(SISData temp, string HTMLDATA, int PageNum)
            {
                temp.MainList = new List<string>();
                var ImageCount = 0;
                var HtmlDoc = new HtmlDocument();
                HtmlDoc.LoadHtml(HTMLDATA);
                var Title = HtmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[4]/div[1]/form[1]/div[1]/table[1]/tr[1]/td[2]/div[3]/h2");
                foreach (var item in HtmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[4]/div[1]/form[1]/div[1]/table[1]/tr[1]/td[2]/div[3]/div[3]").ChildNodes)
                {
                    switch (item.Name)
                    {
                        case "#text":
                            var StringTemp = item.InnerHtml.Replace("\r\n", "");
                            if (!string.IsNullOrEmpty(StringTemp))
                                temp.MainList.Add(item.InnerHtml.Replace("\r\n", ""));
                            break;

                        case "img":
                            try
                            {
                                ImageCount += 1;
                                //temp.MainList.Add(item.Attributes["src"].Value);
                                var ImgUri = item.Attributes["src"].Value;
                                //var db = new LiteDatabase("Filename=SIS.db;Connection=Shared;ReadOnly=True");

                                // var db = new LiteDatabase(@"SIS.db");
                                {
                                    if (STSImg.Exists(x => x.id == ImgUri))
                                    {
                                        try
                                        {
                                            Setting.SISDownLoadNow = $"{temp.Date}日第{PageNum}页第{PageCount + 1}篇\n\r第{ImageCount}张图片找到重复";
                                            var FindImg = STSImg.FindOne(x => x.id == ImgUri);
                                            if (FindImg.FromList != null)
                                            {
                                                if (!FindImg.FromList.Exists(x => x == temp.id))
                                                    FindImg.FromList.Add(temp.id);
                                            }
                                            else
                                            {
                                                FindImg.FromList = new List<int>() { temp.id };
                                            }
                                            if (FindImg.img == null)
                                            {
                                                Setting.SISDownLoadNow = $"{temp.Date}日第{PageNum}页第{PageCount + 1}篇\n\r第{ImageCount}张图片下载中";

                                                FindImg.img = DownloadImgAsync(ImgUri).Result;
                                            }
                                            temp.MainList.Add($"SIS-{FindImg.id}");
                                            DataBaseCommand.SaveToSISDataUnit("img", UnitData: FindImg);

                                            //STSImg.Update(FindImg);

                                            //db.Dispose();
                                        }
                                        catch (Exception ex)
                                        {
                                            Loger.Instance.LocalInfo($"SIS错误，在数据库内找到图片后发生错误{ex.Message}");
                                        }

                                        break;
                                    }
                                    else
                                    {                                            //db.Dispose();
                                        Setting.SISDownLoadNow = $"下载{temp.Date}日第{PageNum}页第{PageCount + 1}篇\n\r{temp.id}页第{ImageCount}张图片";
                                        var TempImg = new SISImgData() { id = ImgUri, Date = temp.Date };
                                        var imgdata = DownloadImgAsync(ImgUri).Result;
                                        try
                                        {
                                            if (imgdata != null)
                                            {
                                                // db = new LiteDatabase(@"SIS.db");
                                                TempImg.img = imgdata;
                                                if (STSImg.Exists(x => x.Hash == TempImg.Hash))
                                                {
                                                    Setting.SISDownLoadNow = $"{PageNum}-{PageCount + 1}-{ImageCount}在SIS找到重复项";
                                                    var FindImg = STSImg.FindOne(x => x.Hash == TempImg.Hash);
                                                    if (FindImg.FromList != null)
                                                    {
                                                        if (!FindImg.FromList.Exists(x => x == temp.id))
                                                            FindImg.FromList.Add(temp.id);
                                                    }
                                                    else
                                                    {
                                                        FindImg.FromList = new List<int>() { temp.id };
                                                    }
                                                    temp.MainList.Add($"SIS-{FindImg.Hash}");
                                                    //db.Dispose();
                                                }
                                                else if (!DataBaseCommand.T66yWriteIng)
                                                {
                                                    //var T66ydb = Setting.T66yDB;
                                                    using var T66ydb = new LiteDatabase("Filename=T66y.db;Connection=Shared;ReadOnly=True");
                                                    var T66yDB = T66ydb.GetCollection<T66yImgData>("ImgData");
                                                    if (T66yDB.Exists(x => x.Hash == TempImg.Hash))
                                                    {
                                                        Setting.SISDownLoadNow = $"{PageNum}-{PageCount + 1}-{ImageCount}在T66y找到重复项";
                                                        Setting.DataCount.T66y += 1;
                                                        temp.MainList.Add($"T66y-{TempImg.id}");
                                                        T66ydb.Dispose();
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        temp.MainList.Add($"SIS-{TempImg.id}");
                                                        Setting.SISDownLoadNow = $"{PageNum}-{PageCount + 1}-{ImageCount}保存到数据库";
                                                        //db.Dispose();
                                                    }
                                                }
                                                else
                                                {
                                                    temp.MainList.Add($"SIS-{TempImg.Hash}");
                                                    Setting.SISDownLoadNow = $"{PageNum}-{PageCount + 1}-{ImageCount}保存到数据库";
                                                    //db.Dispose();
                                                }
                                            }
                                            else
                                            {
                                                temp.MainList.Add($"SIS-{TempImg.id}");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Loger.Instance.LocalInfo($"SIS错误，在数据库内未找到图片后发生错误{ex.Message}");
                                        }

                                        TempImg.FromList = new List<int>();
                                        TempImg.FromList.Add(temp.id);
                                        DataBaseCommand.SaveToSISDataUnit("img", UnitData: TempImg);
                                        TempImg.img = null;
                                        TempImg = null;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Loger.Instance.LocalInfo($"SIS错误,图片下载{ex.Message}{ex.StackTrace}");
                            }

                            break;

                        case "font":
                            FontInPut(temp, item.ChildNodes);
                            break;

                        default:
                            break;
                    }
                }
                var HASH = HtmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[4]/div[1]/form[1]/div[1]/table[1]/tr[1]/td[2]/div[3]/div[4]/dl[1]/dt[1]/a[1]").Attributes["href"].Value;
                var DownloadUrl = HtmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[4]/div[1]/form[1]/div[1]/table[1]/tr[1]/td[2]/div[3]/div[4]/dl[1]/dt[1]/a[2]").Attributes["href"].Value;
                temp.MainList.Add(HASH);
                temp.MainList.Add(DownloadUrl);
                temp.Status = true;
                void FontInPut(SISData temp, HtmlNodeCollection childNodes)
                {
                    foreach (var item2 in childNodes)
                    {
                        switch (item2.Name)
                        {
                            case "#text":
                                var StringTemp = item2.InnerHtml.Replace("\r\n", "");
                                if (!string.IsNullOrEmpty(StringTemp))
                                    temp.MainList.Add(item2.InnerHtml.Replace("\r\n", ""));
                                break;

                            case "font":
                                FontInPut(temp, item2.ChildNodes);
                                break;

                            default:
                                break;
                        }
                    }
                }
            }
            void Wait(string message)
            {
                var time = new Random().Next(5000, 10000);
                for (var i = time; i > 0; i -= 1000)
                {
                    if (T66yDownloadCancel.IsCancellationRequested || Setting.T66yDownloadIng) break;
                    Setting.SISDownLoadNow = $"{message}-{i / 1000}";
                    Thread.Sleep(1000);
                }
            }
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
                                if (AddTemp.Title.StartsWith("少妇雅琪")) SaveFlag = true;
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
            string DownloadT66yPage(string Url = "", string DownloadPage = "")
            {
                if (string.IsNullOrEmpty(Url))
                    Url = $"http://www.t66y.com/read.php?tid={DownloadPage}";
                var Download = new HttpClient(new ProxyClientHandler<Socks5>(new ProxySettings { Host = "localhost", Port = Setting.NyaaSocks5Point }));
                for (int i = 0; i < 2; i++)
                {
                    try
                    {
                        var RET = Download.GetStringAsync(Url).Result;
                        Setting._GlobalSet.totalDownloadBytes += RET.Length;
                        return RET;
                    }
                    catch (Exception ex)
                    {
                        Download.Dispose();
                        Download = new HttpClient(new ProxyClientHandler<Socks5>(new ProxySettings { Host = "localhost", Port = Setting.Socks5Point }));
                    }
                }
                return null;
            }
            string DownloadMainPage(int Index = -1, string _Uri = "")
            {
                var down = Index > -1 ? new Uri($"http://{new Uri(Setting._GlobalSet.MiMiAiAddress).Authority}/forumdisplay.php?fid=11&filter=0&orderby=dateline&page={Index}") : new Uri($"{_Uri}");
                var Download = new HttpClient(new ProxyClientHandler<Socks5>(new ProxySettings { Host = "localhost", Port = Setting.Socks5Point }));
                for (int i = 0; i < 2; i++)
                {
                    try
                    {
                        var RET = Download.GetStringAsync(down).Result;
                        Setting._GlobalSet.totalDownloadBytes += RET.Length;
                        return RET;
                    }
                    catch (Exception ex)
                    {
                        Download.Dispose();
                        Download = new HttpClient(new ProxyClientHandler<Socks5>(new ProxySettings { Host = "localhost", Port = Setting.NyaaSocks5Point }));
                    }
                }
                return null;
                using (var request = new HttpRequest()
                {
                    UserAgent = Http.ChromeUserAgent(),
                    ConnectTimeout = 5000,
                    CharacterSet = Encoding.GetEncoding("GBK")
                })
                {
                    if (Setting._GlobalSet.SocksCheck && Setting.Socks5Point != 0)
                        request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.Socks5Point}");
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
                                if (Setting._GlobalSet.SocksCheck && Setting.Socks5Point != 0)
                                    request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.Socks5Point}");
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

        public Task GetT66yData()
        {
            return Task.Run(() =>
            {
                //HandleStoryPage(DownloadMainPage(_Uri: $"http://t66y.com/htm_data/2008/25/4069983.html"));
                //var SaveToDataBase = new BlockingCollection<Tuple<string, object>>();
                //var db = new LiteDatabase(@"T66y.db");
                Loger.Instance.LocalInfo($"开始T66y下载");

                #region 下载旧数据

                Setting.T66yDownLoadOldOther = $"停止下载";
                // copy /y "$(TargetPath)" "Z:\publish\"

                var GetOtherT66yPage = new Action(() =>
                {
                    Loger.Instance.LocalInfo($"开始T66y旧数据下载");

                    if (Setting.T66yDownLoadOldOther != $"停止下载") return;
                    if (Setting.T66yDownLoadNowOther != "T66y附加信息未启用") return;

                    //using var db = new LiteDatabase(@"T66y.db");
                    //var db = Setting.T66yDB;
                    using var db = new LiteDatabase("Filename=T66y.db;Connection=Shared;ReadOnly=True");
                    var DownloadPage = db.GetCollection<T66yData>("T66yData").Min(x => x.id).AsInt32;
                    var PageCount = 0;
                    var HisList = new List<string>();
                    if (File.Exists(@"T66yHis.txt"))
                    {
                        HisList = new List<string>(File.ReadAllLines(@"T66yHis.txt"));
                    }
                    while ((!T66yDownloadCancel.IsCancellationRequested && !Setting.T66yDownloadIng) || true)
                    {
                        if (T66yDownloadCancel.IsCancellationRequested ||
                        Setting.T66yDownloadIng) break; if (Setting.T66yDownLoadNowOther != "T66y附加信息未启用") break;
                        Interlocked.Decrement(ref DownloadPage);

                        if (HisList.Exists(x => x == DownloadPage.ToString())) continue; else File.AppendAllLines(@"T66yHis.txt", new[] { DownloadPage.ToString() });
                        if (DataBaseCommand.GetDataFromT66y("CheckT66yExists", DownloadPage.ToString()))
                            continue;
                        Interlocked.Increment(ref PageCount);
                        Wait($"等待下载{DownloadPage}页");
                        if (Setting.T66yDownloadIng) break;
                        Setting.T66yDownLoadOldOther = $"从谷歌开始{DownloadPage}页面下载{PageCount}";
                        var GoogleHtml = DownloadGooglePage(DownloadPage);
                        Setting.T66yDownLoadOldOther = $"从谷歌下载{DownloadPage}页面{PageCount}";
                        var T66yHtmlPage = string.Empty;
                        if (string.IsNullOrEmpty(GoogleHtml))
                        {
                            Setting.T66yDownLoadOldOther = $"从谷歌下载{DownloadPage}页面失败开始直接下载{PageCount}";
                            T66yHtmlPage = DownloadT66yPage(null);
                        }
                        else
                        {
                            Setting.T66yDownLoadOldOther = $"从T66y下载{DownloadPage}转换页面{PageCount}";
                            T66yHtmlPage = DownloadT66yPage(AnalyGooglePage(GoogleHtml));
                        }
                        if (!string.IsNullOrEmpty(T66yHtmlPage))
                        {
                            if (!string.IsNullOrEmpty(T66yHtmlPage))
                            {
                                var _HtmlDoc = new HtmlDocument();
                                _HtmlDoc.LoadHtml(T66yHtmlPage);
                                try
                                {
                                    var Uri = _HtmlDoc.DocumentNode.SelectSingleNode("/html/body/center/div/a[2]").Attributes["href"].Value;
                                    Setting.T66yDownLoadOldOther = $"从T66y下载{DownloadPage}主页面{PageCount}";
                                    var MainPage = DownloadT66yPage($"http://t66y.com/{Uri}");
                                    if (!string.IsNullOrEmpty(MainPage))
                                        HandleT66yPage(MainPage, Uri);
                                }
                                catch (Exception)
                                {
                                    _HtmlDoc.Save($"{DownloadPage}.html");
                                    Setting.T66yDownLoadOldOther = $"从T66y下载{DownloadPage}主页面失败{PageCount}";
                                    continue;
                                }
                            }
                        }
                    }
                    Setting.T66yDownLoadOldOther = $"停止下载";
                    string DownloadGooglePage(int DownloadPage)
                    {
                        using (var request = new HttpRequest()
                        {
                            UserAgent = Http.ChromeUserAgent(),
                            ConnectTimeout = 5000,
                            CharacterSet = Encoding.GetEncoding("GBK")
                        })
                        {
                            if (Setting._GlobalSet.NyaaSocksCheck && Setting.NyaaSocks5Point != 0)
                                request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.NyaaSocks5Point}");
                            try
                            {
                                int ErrorCount = 1;
                                while (ErrorCount != 0)
                                {
                                    try
                                    {
                                        HttpResponse response = request.Get($"https://www.google.co.jp/search?&source=hp&q=http://www.t66y.com/read.php?tid={DownloadPage}");
                                        var RetS = response.ToString();
                                        Setting._GlobalSet.totalDownloadBytes += RetS.Length;
                                        return Encoding.Default.GetString(response.Ret);
                                    }
                                    catch (Exception ex)
                                    {
                                        if (Setting._GlobalSet.SocksCheck && Setting.Socks5Point != 0)
                                            request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.Socks5Point}");
                                        var time = new Random().Next(30000, 60000);
                                        for (var i = time; i > 0; i -= 1000)
                                        {
                                            Loger.Instance.WaitTime(i / 1000);
                                            Thread.Sleep(1000);
                                        }
                                        Interlocked.Increment(ref ErrorCount);
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
                    string AnalyGooglePage(string Html)
                    {
                        try
                        {
                            var _HtmlDoc = new HtmlDocument();
                            _HtmlDoc.LoadHtml(Html);
                            foreach (var item in _HtmlDoc.DocumentNode.SelectNodes(@"//*[@id='search']"))
                            {
                                if (string.IsNullOrEmpty(item.InnerHtml)) break;
                                return item.SelectSingleNode("div/div/div/div/div[1]/a").Attributes["href"].Value;
                            }
                            return string.Empty;
                        }
                        catch (Exception)
                        {
                            return string.Empty;
                        }
                    }
                    string DownloadT66yPage(string Url)
                    {
                        if (string.IsNullOrEmpty(Url))
                            Url = $"http://www.t66y.com/read.php?tid={DownloadPage}";
                        var Download = new HttpClient(new ProxyClientHandler<Socks5>(new ProxySettings { Host = "localhost", Port = Setting.NyaaSocks5Point }));
                        for (int i = 0; i < 2; i++)
                        {
                            try
                            {
                                var RET = Download.GetStringAsync(Url).Result;
                                Setting._GlobalSet.totalDownloadBytes += RET.Length;
                                return RET;
                            }
                            catch (Exception ex)
                            {
                                Download.Dispose();
                                Download = new HttpClient(new ProxyClientHandler<Socks5>(new ProxySettings { Host = "localhost", Port = Setting.Socks5Point }));
                            }
                        }
                        return null;
                    }
                    void HandleT66yPage(string Html, string uri)
                    {
                        var TempData = new T66yData() { id = DownloadPage, HtmlData = Html, Uri = uri };
                        var ImgList = new List<T66yImgData>();
                        var TempList = new List<string>();
                        var Quote = new List<string>();
                        var _HtmlDoc = new HtmlDocument();
                        _HtmlDoc.LoadHtml(Html);
                        //_HtmlDoc.Save($"{DownloadPage}.html", Encoding.GetEncoding("gbk"));
                        try
                        {
                            if (_HtmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[2]/div[2]/table[1]/tr[2]/th[1]/center[1]/div[1]").InnerHtml.StartsWith("您沒有登錄或者您沒有權限訪問此頁面"))
                            {
                                return;
                            }
                            var ParentNode = _HtmlDoc.DocumentNode.SelectSingleNode("//div[@class='tiptop']").ParentNode;
                            TempData.Title = ParentNode.SelectSingleNode("//h4").InnerHtml;
                            var MainPage = ParentNode.SelectSingleNode("//div[4]");
                            var Type = ParentNode.SelectSingleNode("//*[@id='main']/div[1]/table[1]/tr[1]/td[1]/b[1]/a[2]");
                            Setting.T66yDownLoadOldOther = $"{DownloadPage}页面类型为{Type.InnerHtml}";
                            if (Type.InnerHtml != "國產原創區")
                            {
                                DataBaseCommand.SaveToT66yDataUnit(Type.InnerHtml, UnitData: TempData);
                                return;
                            }
                            var Time = ParentNode.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tbody[1]/tr[2]/th[1]/div[1]");//.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", "");
                            if (Time == null)
                                Time = ParentNode.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tr[2]/th[1]/div[1]");//.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", "");
                            if (Time.ChildNodes.Count < 2)
                                Time = ParentNode.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tr[2]/th[1]/div[3]");
                            if (Time != null)
                                if (DateTime.TryParse(Time.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", ""), out DateTime dateTime))
                                    TempData.Date = dateTime.ToString("yyyy-MM-dd");
                            var Start = false;
                            Setting.T66yDownLoadOldOther = $"{TempData.id}|{TempData.Date}页面解析页面中{PageCount}";
                            foreach (var item in MainPage.ChildNodes)
                            {
                                if (Start)
                                {
                                    if (item.Name == "blockquote")
                                    {
                                        if (item.ChildNodes.Count != 0)
                                        {
                                            SpildChild(item.ChildNodes, ref Quote);
                                        }
                                    }
                                    else if (!string.IsNullOrWhiteSpace(item.InnerHtml)) // !item.InnerHtml.Contains("&nbsp"))
                                    {
                                        if (item.ChildNodes.Count != 0)
                                            SpildChild(item.ChildNodes, ref TempList);
                                        else
                                            TempList.Add(item.InnerHtml);
                                    }
                                    else if (item.Name == "img")
                                    {
                                        var img = item.Attributes["ess-data"].Value.Replace(".th.jpg", ".jpg");
                                        SaveImg(img);
                                        TempList.Add(img);
                                    }
                                }
                                else if (FindFirst(item.InnerHtml, TempData.Title))
                                {
                                    Start = true;
                                    if (item.ChildNodes.Count != 0)
                                        SpildChild(item.ChildNodes, ref TempList);
                                    else
                                        TempList.Add(item.InnerHtml);
                                }
                            }
                            Setting.T66yDownLoadOldOther = $"{TempData.id}|{TempData.Date}页面解析完毕{PageCount}";

                            if (TempList.Count == 0)
                            {
                                Loger.Instance.LocalInfo($"T66y页面解析错误，分析列表为空{DownloadPage}");
                            }
                            TempList.RemoveAt(TempList.Count - 1);//删除 赞
                            for (int i = TempList.Count - 1; i > 0; i--)
                            {
                                if (TempList[i].ToLower().Contains("quote"))
                                    TempList.RemoveAt(i);
                                else if (TempList[i].ToLower().Contains("nbsp"))
                                {
                                    var RepS = TempList[i].ToLower().Replace("nbsp", "");
                                    if (RepS.Length < 2)
                                        TempList.RemoveAt(i);
                                    else
                                        TempList[i] = RepS;
                                }
                            }
                            var findrmdown = false;
                            if (!findrmdown)
                            {
                                foreach (var item3 in TempList)
                                {
                                    if (item3.Contains("rmdown"))
                                    {
                                        findrmdown = true;
                                    }
                                }
                            }
                            if (!findrmdown)
                            {
                                TempData.HtmlData = string.Empty;
                                Loger.Instance.LocalInfo($"{TempData.Title}页面未找到下载地址");
                            }
                            TempData.MainList = new List<string>(TempList);
                            TempData.QuoteList = new List<string>(Quote);
                            TempData.Status = true;
                            DataBaseCommand.SaveToT66yDataUnit(Collection: "T66yData", UnitData: TempData);
                            DataBaseCommand.SaveToT66yDataUnit(ImgList);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                if (!_HtmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[2]/div[2]/table[1]/tr[2]/th[1]/center[1]/div[1]").InnerHtml.StartsWith("您沒有登錄或者您沒有權限訪問此頁面"))
                                {
                                    Loger.Instance.LocalInfo($"T66y页面解析错误{ex.Message}");
                                    _HtmlDoc.Save($"{TempData.id}.html", Encoding.GetEncoding("gbk"));
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                        finally
                        {
                            ImgList.Clear();
                        }
                        void SpildChild(HtmlNodeCollection item, ref List<string> sL)
                        {
                            foreach (var item2 in item)
                            {
                                if (item2.ChildNodes.Count == 0)
                                {
                                    if (!string.IsNullOrWhiteSpace(item2.InnerHtml) && !item2.InnerHtml.Contains("&nbsp"))
                                        sL.Add(item2.InnerHtml);
                                    if (item2.Name == "img")
                                    {
                                        var img = item2.Attributes["ess-data"].Value.Replace(".th.jpg", ".jpg");
                                        sL.Add(img);
                                        SaveImg(img);
                                    }
                                }
                                else
                                {
                                    SpildChild(item2.ChildNodes, ref sL);
                                }
                            }
                        }
                        bool FindFirst(string src, string Title)
                        {
                            var SearchChar = new HashSet<string>() { "名称", "名稱", "rmdown", Title };
                            foreach (var item in SearchChar)
                            {
                                if (src.Contains(item))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                        void SaveImg(string img)
                        {
                            try
                            {
                                T66yImgData SearchImg = DataBaseCommand.GetDataFromT66y("img", img);
                                if (SearchImg != null)
                                {
                                    Setting.T66yDownLoadOldOther = "T66y找到重复图片";
                                    if (!SearchImg.Status)
                                    {
                                        var imgdata = DownloadImgAsync(img).Result;
                                        if (imgdata != null)
                                        {
                                            try
                                            {
                                                //imgdata = Compress(Image.FromStream(new MemoryStream(imgdata)), 75).ToArray();
                                                SearchImg.img = imgdata;
                                                SearchImg.Hash = Convert.ToBase64String(new MD5CryptoServiceProvider().ComputeHash(SearchImg.img));
                                            }
                                            catch (Exception)
                                            {
                                            }
                                        }
                                    }
                                    if (SearchImg.FromList != null)
                                    {
                                        if (SearchImg.FromList.Count > 1)
                                            SearchImg.FromList = SearchImg.FromList.Distinct().ToList();

                                        if (!SearchImg.FromList.Exists(x => x == TempData.id))
                                            SearchImg.FromList.Add(TempData.id);
                                    }
                                    else
                                    {
                                        SearchImg.FromList = new List<int>() { TempData.id };
                                    }
                                    DataBaseCommand.SaveToT66yDataUnit(UnitData: SearchImg);
                                }
                                else
                                {
                                    var TempImg = new T66yImgData() { id = img, Date = TempData.Date };
                                    Setting.T66yDownLoadOldOther = $"下载{TempData.id}|{TempData.Date}页面\r\n图片{PageCount}第{ImgList.Count + 1}张图片";
                                    var imgdata = DownloadImgAsync(img).Result;
                                    if (imgdata != null)
                                    {
                                        try
                                        {
                                            // TempImg.img = Compress(Image.FromStream(new
                                            // MemoryStream(imgdata)), 75).ToArray();
                                            TempImg.img = imgdata;
                                            TempImg.Hash = Convert.ToBase64String(new MD5CryptoServiceProvider().ComputeHash(TempImg.img));
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                    if (TempImg.FromList == null) TempImg.FromList = new List<int>();
                                    TempImg.FromList.Add(TempData.id);
                                    ImgList.Add(TempImg);
                                }
                            }
                            catch (Exception ex)
                            {
                                Loger.Instance.LocalInfo($"T66y错误,图片下载{ex.Message}{ex.StackTrace}");
                            }
                        }
                    }
                    void Wait(string message)
                    {
                        var time = new Random().Next(5000, 10000);
                        for (var i = time; i > 0; i -= 1000)
                        {
                            if (T66yDownloadCancel.IsCancellationRequested || Setting.T66yDownloadIng) break;
                            Setting.T66yDownLoadOldOther = $"{message}-{i / 1000}";
                            Thread.Sleep(1000);
                        }
                    }
                });

                #endregion 下载旧数据

                #region 下载图片数据

                var ImageDownload = new Action(() =>
                {
                    Setting.T66yDownLoadOldOther = $"开始图片下载";

                    //if (Setting.T66yDownLoadNowOther != "T66y附加信息未启用") return;
                    List<T66yImgData> FindList = DataBaseCommand.GetDataFromT66y("img");
                    var Count = 0;
                    foreach (T66yImgData ImgTemp in FindList)
                    {
                        Count += 1;
                        if (T66yDownloadCancel.IsCancellationRequested) break;
                        if (ImgTemp != null)
                        {
                            Setting.T66yDownLoadOldOther = $"开始{ImgTemp.Date}日图片下载{ImgTemp.FromList.First()}篇{Count}/{FindList.Count}";
                            ImgTemp.img = DownloadImgAsync(ImgTemp.id).Result;
                            if (ImgTemp.img == null)
                                ImgTemp.img = new byte[] { 0 };
                            else
                                ImgTemp.Hash = Convert.ToBase64String(new MD5CryptoServiceProvider().ComputeHash(ImgTemp.img));
                            DataBaseCommand.SaveToT66yDataUnit(UnitData: ImgTemp);
                            ImgTemp.img = null;
                        }
                        if (Setting.T66yDownLoadNowOther != "T66y附加信息未启用") break;
                        if (Setting.T66yDownloadIng) break;
                    }
                    Setting.T66yDownLoadOldOther = $"停止下载";
                });

                #endregion 下载图片数据

                #region 下载详细数据

                Setting.T66yDownLoadNowOther = "T66y附加信息未启用";
                var OtherDownload = new Action(() =>
                {
                    if (Setting.T66yDownLoadNowOther != "T66y附加信息未启用") return;
                    var PageCount = "";
                    int ErrorCount = 0;
                    Loger.Instance.LocalInfo($"开始T66y附加信息下载");
                    var HisList = new List<string>();
                    HttpClient client = new HttpClient(new ProxyClientHandler<Socks5>(new ProxySettings { Host = "localhost", Port = Setting.NyaaSocks5Point }));

                    //foreach (T66yData GetTempData in SearCH)

                    //while (!T66yDownloadCancel.IsCancellationRequested)
                    {
                        Setting.T66yDownLoadNowOther = $"正在搜索数据库";
                        //using var db = new LiteDatabase(@"T66y.db");
                        var db = new LiteDatabase("Filename=T66y.db;ReadOnly=True");
                        var GetTempDataL = db.GetCollection<T66yData>("T66yData").Find(x => x.Status == false).ToList();
                        GetTempDataL.Reverse();
                        db.Dispose();
                        foreach (var GetTempData in GetTempDataL)
                        {
                        start:
                            PageCount = $"{GetTempDataL.IndexOf(GetTempData)}|{GetTempDataL.Count}";
                            if (GetTempData == null) break;
                            Setting.T66yDownLoadNowOther = $"开始处理{GetTempData.id}|{GetTempData.Date}";
                            if (T66yDownloadCancel.IsCancellationRequested) break;
                            Wait($"{GetTempData.id}");
                            //GetTempData.Uri = "htm_data/2011/25/4170309.html";
                            // File.WriteAllText($"text.html", GetTempData.HtmlDate, Encoding.GetEncoding("gbk"));
                            if (string.IsNullOrEmpty(GetTempData.HtmlData))
                            {
                                try
                                {
                                    if (!GetTempData.Uri.StartsWith("htm"))
                                    {
                                        Setting.T66yDownLoadNowOther = $"从T66y下载{GetTempData.id}转换页面{PageCount}";
                                        var T66yHtmlPage = DownloadT66yPage(DownloadPage: GetTempData.id.ToString());
                                        var _HtmlDoc = new HtmlDocument();
                                        _HtmlDoc.LoadHtml(T66yHtmlPage);
                                        GetTempData.Uri = _HtmlDoc.DocumentNode.SelectSingleNode("/html/body/center/div/a[2]").Attributes["href"].Value;
                                    }
                                    Setting.T66yDownLoadNowOther = $"下载页面中{GetTempData.id},当前第{PageCount}页{GetTempData.Date}";
                                    GetTempData.HtmlData = client.GetStringAsync($"http://t66y.com/{GetTempData.Uri}").Result;
                                    Setting._GlobalSet.totalDownloadBytes += GetTempData.HtmlData.Length;
                                    ErrorCount = 0;
                                }
                                catch (Exception e)
                                {
                                    ErrorCount += 1;

                                    if (e.Message.Contains("403"))
                                    {
                                        Loger.Instance.LocalInfo($"T66y下载内容出错，错误信息{e.Message},推测IP已被Ban");
                                        client = new HttpClient(new ProxyClientHandler<Socks5>(new ProxySettings { Host = "localhost", Port = Setting.NyaaSocks5Point == 1088 ? Setting.Socks5Point : 1088 }));
                                    }
                                    else if (e.Message.Contains("404"))
                                    {
                                        Loger.Instance.LocalInfo($"T66y下载内容出错，错误信息{e.Message},地址不存在");
                                        //using var DelDB = new LiteDatabase(@"T66y.db");
                                        //var DelDB = Setting.T66yDB;
                                        using var DelDB = new LiteDatabase("Filename=T66y.db;Connection=Shared;ReadOnly=True");

                                        DelDB.GetCollection<T66yData>("T66yData").Delete(x => x.id == GetTempData.id);
                                        // db.Dispose();
                                    }
                                    else if (e.Message.Contains("proxy"))
                                    {
                                        Loger.Instance.LocalInfo($"T66y下载内容出错，错误信息{e.Message},推测代理错误");
                                    }
                                    else
                                    {
                                        Loger.Instance.LocalInfo($"T66y下载内容出错，错误信息{e.Message},计数{ErrorCount}");
                                    }
                                    if (ErrorCount == 6)
                                    {
                                        Loger.Instance.LocalInfo($"T66y下载内容出错,计数{ErrorCount},退出下载进程");
                                        continue;
                                    }
                                    client.Dispose();
                                    client = new HttpClient(new ProxyClientHandler<Socks5>(new ProxySettings { Host = "localhost", Port = Setting.NyaaSocks5Point }));
                                    Wait($"错误等待");
                                    goto start;
                                }
                            }
                            HandleT66yPage(GetTempData, client);
                            client.Dispose();
                            client = null;
                            client = new HttpClient(new ProxyClientHandler<Socks5>(new ProxySettings { Host = "localhost", Port = Setting.NyaaSocks5Point }));
                            // File.WriteAllText($"text.html", GetTempData.HtmlDate, Encoding.GetEncoding("gbk"));
                        }
                    }

                    Loger.Instance.LocalInfo($"T66y附加信息结束下载");
                    Setting.T66yDownLoadNowOther = "T66y附加信息未启用";
                    //Task.Factory.StartNew(GetOtherT66yPage);
                    //Task.Factory.StartNew(GetOtherT66yPage);
                    string DownloadT66yPage(string Url = "", string DownloadPage = "")
                    {
                        if (string.IsNullOrEmpty(Url))
                            Url = $"http://www.t66y.com/read.php?tid={DownloadPage}";
                        var Download = new HttpClient(new ProxyClientHandler<Socks5>(new ProxySettings { Host = "localhost", Port = Setting.NyaaSocks5Point }));
                        for (int i = 0; i < 2; i++)
                        {
                            try
                            {
                                var RET = Download.GetStringAsync(Url).Result;
                                Setting._GlobalSet.totalDownloadBytes += RET.Length;
                                return RET;
                            }
                            catch (Exception ex)
                            {
                                Download.Dispose();
                                Download = new HttpClient(new ProxyClientHandler<Socks5>(new ProxySettings { Host = "localhost", Port = Setting.Socks5Point }));
                            }
                        }
                        return null;
                    }
                    void HandleT66yPage(T66yData TempData, HttpClient client)
                    {
                        var ImgList = new List<T66yImgData>();
                        var TempList = new List<string>();
                        var Quote = new List<string>();
                        var _HtmlDoc = new HtmlDocument();
                        _HtmlDoc.LoadHtml(TempData.HtmlData);
                        // _HtmlDoc.Load("text.html", Encoding.GetEncoding("gbk"));
                        try
                        {
                            //_HtmlDoc.Save($"Html{count++}.html", Encoding.GetEncoding("gbk"));
                            var CN = _HtmlDoc.DocumentNode.SelectSingleNode("//div[@class='tiptop']").ParentNode;
                            var H4 = CN.SelectSingleNode("//h4");
                            var DIV4 = CN.SelectSingleNode("//div[4]");
                            if (string.IsNullOrEmpty(TempData.Date))
                            {
                                var Time = CN.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tbody[1]/tr[2]/th[1]/div[1]");//.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", "");
                                if (Time == null)
                                    Time = CN.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tr[2]/th[1]/div[1]");//.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", "");
                                if (Time.ChildNodes.Count < 2)
                                    Time = CN.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tr[2]/th[1]/div[3]");
                                if (Time != null)
                                    if (DateTime.TryParse(Time.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", ""), out DateTime dateTime))
                                        TempData.Date = dateTime.ToString("yyyy-MM-dd");
                            }
                            var Type = CN.SelectSingleNode("//*[@id='main']/div[1]/table[1]/tr[1]/td[1]/b[1]/a[2]");
                            if (Type.InnerHtml != "國產原創區")
                            {
                                //using var db = new LiteDatabase(@"T66y.db");
                                //var db = Setting.T66yDB;
                                //db.GetCollection<T66yData>("T66yData").Delete(x => x.id == TempData.id);
                                //db.Dispose();
                                DataBaseCommand.SaveToT66yDataUnit(Type.InnerHtml, UnitData: TempData);
                                return;
                            }
                            var Start = false;
                            foreach (var item in DIV4.ChildNodes)
                            {
                                Setting.T66yDownLoadNowOther = $"{TempData.id}|{TempData.Date}页面解析中{PageCount}";
                                if (Start)
                                {
                                    if (item.Name == "blockquote")//TempList.Last().Contains("引用") || TempList.Last().Contains("Quote"))
                                    {
                                        if (item.ChildNodes.Count != 0)
                                        {
                                            SpildChild(item.ChildNodes, ref Quote);
                                        }
                                    }
                                    else if (!string.IsNullOrWhiteSpace(item.InnerHtml)) // !item.InnerHtml.Contains("&nbsp"))
                                    {
                                        if (item.ChildNodes.Count != 0)
                                            SpildChild(item.ChildNodes, ref TempList);
                                        else
                                            TempList.Add(item.InnerHtml);
                                    }
                                    else if (item.Name == "img")
                                    {
                                        var img = item.Attributes["ess-data"].Value.Replace(".th.jpg", ".jpg");
                                        SaveImg(img);
                                        TempList.Add(img);
                                    }
                                }
                                else if (FindFirst(item.InnerHtml, TempData.Title))
                                {
                                    Start = true;
                                    if (item.ChildNodes.Count != 0)
                                        SpildChild(item.ChildNodes, ref TempList);
                                    else
                                        TempList.Add(item.InnerHtml);
                                }
                            }
                            if (TempList.Count == 0)
                            {
                                _HtmlDoc.Save($"{TempData.id}.html", Encoding.GetEncoding("gbk"));
                                Loger.Instance.LocalInfo($"T66y页面解析错误，分析列表为空");
                            }
                            if (TempList.Count != 0)
                            {
                                TempList.RemoveAt(TempList.Count - 1);//删除 赞
                                for (int i = TempList.Count - 1; i > 0; i--)
                                {
                                    if (TempList[i].ToLower().Contains("quote"))
                                        TempList.RemoveAt(i);
                                    else if (TempList[i].ToLower().Contains("nbsp"))
                                    {
                                        var RepS = TempList[i].ToLower().Replace("nbsp", "");
                                        if (RepS.Length < 2)
                                            TempList.RemoveAt(i);
                                        else
                                            TempList[i] = RepS;
                                    }
                                }

                                var findrmdown = false;
                                if (!findrmdown)
                                {
                                    foreach (var item3 in TempList)
                                    {
                                        if (item3.Contains("rmdown"))
                                        {
                                            findrmdown = true;
                                        }
                                    }
                                }
                                if (!findrmdown)
                                {
                                    _HtmlDoc.Save($"{TempData.id}.html", Encoding.GetEncoding("gbk"));
                                    TempData.HtmlData = string.Empty;
                                    Loger.Instance.LocalInfo($"{TempData.Title}页面未找到下载地址");
                                }
                            }
                            else
                            {
                                _HtmlDoc.Save($"{TempData.id}.html", Encoding.GetEncoding("gbk"));
                            }
                            TempData.MainList = new List<string>(TempList);
                            TempData.QuoteList = new List<string>(Quote);
                            // TempData.Status = findrmdown && TempData.MainList.Count != 0;
                            TempData.Status = true;
                            DataBaseCommand.SaveToT66yDataUnit("T66yData", UnitData: TempData);
                            DataBaseCommand.SaveToT66yDataUnit(ImgList);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                if (!_HtmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[2]/div[2]/table[1]/tr[2]/th[1]/center[1]/div[1]").InnerHtml.StartsWith("您沒有登錄或者您沒有權限訪問此頁面"))
                                {
                                    Loger.Instance.LocalInfo($"T66y页面解析错误{ex.Message}");
                                    _HtmlDoc.Save($"{TempData.id}.html", Encoding.GetEncoding("gbk"));
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                        finally
                        {
                            ImgList.Clear();
                        }
                        void SpildChild(HtmlNodeCollection item, ref List<string> sL)
                        {
                            foreach (var item2 in item)
                            {
                                if (item2.ChildNodes.Count == 0)
                                {
                                    if (!string.IsNullOrWhiteSpace(item2.InnerHtml) && !item2.InnerHtml.Contains("&nbsp"))
                                        sL.Add(item2.InnerHtml);
                                    if (item2.Name == "img")
                                    {
                                        var img = item2.Attributes["ess-data"].Value.Replace(".th.jpg", ".jpg");
                                        sL.Add(img);
                                        SaveImg(img);
                                    }
                                }
                                else
                                {
                                    SpildChild(item2.ChildNodes, ref sL);
                                }
                            }
                        }
                        bool FindFirst(string src, string Title)
                        {
                            var SearchChar = new HashSet<string>() { "名称", "名稱", "rmdown", Title };
                            foreach (var item in SearchChar)
                            {
                                if (src.Contains(item))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                        void SaveImg(string img)
                        {
                            try
                            {
                                Setting.T66yDownLoadNowOther = "T66y搜索重复图片";

                                T66yImgData SearchImg = DataBaseCommand.GetDataFromT66y("img", img);
                                if (SearchImg != null)
                                {
                                    Setting.T66yDownLoadNowOther = "T66y找到重复图片";
                                    if (!SearchImg.Status)
                                    {
                                        Setting.T66yDownLoadNowOther = $"第{PageCount}页{TempData.Date}-{TempData.id}\r\n下载第{ImgList.Count + 1}张图片";

                                        var imgdata = DownloadImgAsync(img).Result;
                                        if (imgdata != null)
                                        {
                                            try
                                            {
                                                // imgdata = Compress(Image.FromStream(new
                                                // MemoryStream(imgdata)), 75).ToArray();
                                                SearchImg.img = imgdata;
                                                SearchImg.Hash = Convert.ToBase64String(new MD5CryptoServiceProvider().ComputeHash(SearchImg.img));
                                            }
                                            catch (Exception)
                                            {
                                            }
                                        }
                                    }
                                    if (SearchImg.FromList != null)
                                    {
                                        if (SearchImg.FromList.Count > 1)
                                        {
                                            SearchImg.FromList = SearchImg.FromList.Distinct().ToList();
                                        }
                                        if (!SearchImg.FromList.Exists(x => x == TempData.id))
                                            SearchImg.FromList.Add(TempData.id);
                                    }
                                    else
                                    {
                                        SearchImg.FromList = new List<int>() { TempData.id };
                                    }
                                    DataBaseCommand.SaveToT66yDataUnit(UnitData: SearchImg);
                                }
                                else
                                {
                                    var TempImg = new T66yImgData() { id = img, Date = TempData.Date };

                                    Setting.T66yDownLoadNowOther = $"第{PageCount}页{TempData.Date}-{TempData.id}\r\n下载第{ImgList.Count + 1}张图片";
                                    var imgdata = DownloadImgAsync(img).Result;
                                    Setting.T66yDownLoadNowOther = "T66y下载图片完成";
                                    if (imgdata != null)
                                    {
                                        try
                                        {
                                            // TempImg.img = Compress(Image.FromStream(new
                                            // MemoryStream(imgdata)), 75).ToArray();
                                            TempImg.img = imgdata;
                                            TempImg.Hash = Convert.ToBase64String(new MD5CryptoServiceProvider().ComputeHash(TempImg.img));
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }

                                    if (TempImg.FromList == null) TempImg.FromList = new List<int>();
                                    TempImg.FromList.Add(TempData.id);
                                    ImgList.Add(TempImg);
                                }
                            }
                            catch (Exception ex)
                            {
                                Loger.Instance.LocalInfo($"T66y错误,图片下载{ex.Message}{ex.StackTrace}");
                            }
                        }
                    }
                    void Wait(string message)
                    {
                        var time = new Random().Next(1000, 5000);
                        for (var i = time; i > 0; i -= 1000)
                        {
                            if (T66yDownloadCancel.IsCancellationRequested) break;
                            Setting.T66yDownLoadNowOther = $"{message}-{i / 1000}";
                            Thread.Sleep(1000);
                        }
                    }
                });

                #endregion 下载详细数据

                var HtmlDoc = new HtmlDocument();
                GetT66yDataTimer = new System.Timers.Timer(1000);
                GetT66yDataTimer.Elapsed += async delegate
                {
                    Setting.T66yDownloadIng = true;
                    /*if (Setting.T66yDownloadIng) return;
                    while (Setting.T66yDownLoadOldOther != $"停止下载")
                    {
                        Thread.Sleep(1000);
                    }*/
                    GetT66yDataTimer.Stop();
                    GetT66ySpan.Reset();
                    var RunSpan = new Stopwatch();
                    RunSpan.Start();
                    var DownloadPage = 1;
                    bool SaveFlag = false;
                    await Task.Factory.StartNew(() =>
                    {
                        var ContinueCount = 0;
                        while (!T66yDownloadCancel.IsCancellationRequested)
                        {
                            var time = new Random().Next(1000, 10000);
                            for (var i = time; i > 0; i -= 1000)
                            {
                                if (T66yDownloadCancel.IsCancellationRequested) break;
                                Setting.T66yDownLoadNow = $"当前下载第{DownloadPage}-{i / 1000}";
                                Thread.Sleep(1000);
                            }
                            var DoanloadPageHtml = string.Empty;
                            try
                            {
                                Setting.T66yDownLoadNow = $"当前下载第{DownloadPage},计数{ContinueCount}";
                                DoanloadPageHtml = DownloadMainPage(DownloadPage);
                                if (string.IsNullOrEmpty(DoanloadPageHtml))
                                {
                                    Loger.Instance.LocalInfo("T66y下载到空网页，退出下载进程");
                                    break;
                                }
                                var PageCount = 1;
                                //List<T66yData> ItemList = new List<T66yData>();
                                foreach (var tempData in AnalyT66yMainPage(HtmlDoc, DoanloadPageHtml))
                                {
                                    /*   var TempData = new string[]
                                    {
                                       Url,//地址0
                                       temp.SelectSingleNode("td[2]/h3/a").InnerHtml,//标题1
                                       temp.SelectSingleNode("td[3]/div/span").Attributes["title"].Value.Split(' ')[2],//日期2
                                       HtmlTempData.Item1,//编号3
                                       HtmlTempData.Item2,//内容4
                                    };*/
                                    T66yData AddTemp = new T66yData();
                                    //var AddTemp = HandleStoryPage(DownloadMainPage(_Uri: $"http://{new Uri(Setting._GlobalSet.T66yAddress).Host}/{tempData[0]}"), tempData);
                                    if (tempData != null)
                                    {
                                        AddTemp.id = int.Parse(tempData[3]);
                                        AddTemp.Date = tempData[2];
                                        AddTemp.HtmlData = tempData[4];
                                        AddTemp.Status = false;
                                        AddTemp.Title = tempData[1];
                                        AddTemp.Uri = tempData[0];
                                        //ItemList.Add(AddTemp);
                                        Setting.T66yDownLoadNow = $"开始解析第{tempData[3]}页,日期{tempData[2]},计数{ContinueCount}";
                                        var db = new LiteDatabase(@"T66y.db");
                                        {
                                            var STSDB = db.GetCollection<T66yData>("T66yData");
                                            if (!STSDB.Exists(x => x.id == AddTemp.id))
                                            {
                                                ContinueCount = 0;
                                                db.Dispose();
                                                DataBaseCommand.SaveToT66yDataUnit("T66yData", UnitData: AddTemp);
                                            }
                                            else
                                            {
                                                Interlocked.Increment(ref ContinueCount);
                                            }
                                        }
                                        if (ContinueCount > 90)
                                        {
                                            SaveFlag = true;
                                        }
                                        //SaveFlag = DateTime.Now.ToString("yyyy-MM-dd") != AddTemp.Date;
                                    }
                                    Setting.T66yDownLoadNow = $"当前下载第{DownloadPage}-{PageCount}-{ContinueCount}";
                                    Interlocked.Increment(ref PageCount);
                                    if (SaveFlag) break;
                                }
                                //DataBaseCommand.SaveToT66yDataUnit(ItemList);
                                //ItemList.Clear(); ItemList = null;
                                Interlocked.Increment(ref DownloadPage);
                                //Setting._GlobalSet.T66yPageIndex = DownloadPage;
                                if (DownloadPage == 0) break;
                                if (SaveFlag) break;
                            }
                            catch (Exception ex)
                            {
                                File.WriteAllText($"T66yErrorPage{DateTime.Now:yyyy-MM-dd-ss}.html", DoanloadPageHtml);
                                Loger.Instance.LocalInfo($"T66y下载错误，错误信息{ex.Message},当前页{DownloadPage}");
                            }
                        }
                    });

                    Loger.Instance.LocalInfo($"T66y:下载完毕,耗时{RunSpan.Elapsed:mm\\分ss\\秒}");
                    GetT66yDataTimer.Interval = new Random().Next(12, 24) * 3600 * 1000;
                    Setting.T66yDownLoadNow = DateTime.Now.AddMilliseconds(GetT66yDataTimer.Interval).ToString("MM-dd|HH:mm");
                    RunSpan.Stop();
                    GetT66ySpan.Restart();
                    GetT66yDataTimer.Start();
                    Setting.T66yDownloadIng = false;
                    if (Setting.T66yDownLoadNowOther == "T66y附加信息未启用")
                    {
                        Thread.Sleep(1000);
                        Task.Factory.StartNew(OtherDownload);
                    }
                    /* if (T66yOtherDownloadTask == null)
                     {
                         T66yOtherDownloadTask = Task.WhenAll(
                             Task.Factory.StartNew(ImageDownload, T66yDownloadCancel.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current),
                             Task.Factory.StartNew(GetOtherT66yPage, T66yDownloadCancel.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current));
                     }
                     else if (T66yOtherDownloadTask.Status != TaskStatus.Running)
                     {
                         T66yOtherDownloadTask = null;
                         GC.Collect();
                         T66yOtherDownloadTask = Task.WhenAll(
                             Task.Factory.StartNew(ImageDownload, T66yDownloadCancel.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current),
                             Task.Factory.StartNew(GetOtherT66yPage, T66yDownloadCancel.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current));
                     }*/
                };
                GetT66yDataTimer.Enabled = true;
            }, T66yDownloadCancel.Token);
            string DownloadMainPage(int Index = -1, string _Uri = "")
            {
                using (var request = new HttpRequest()
                {
                    UserAgent = Http.ChromeUserAgent(),
                    ConnectTimeout = 5000,
                    CharacterSet = Encoding.GetEncoding("GBK")
                })
                {
                    if (Setting._GlobalSet.NyaaSocksCheck && Setting.NyaaSocks5Point != 0)
                        request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.NyaaSocks5Point}");
                    try
                    {
                        int ErrorCount = 1;
                        while (!T66yDownloadCancel.IsCancellationRequested || ErrorCount != 0)
                        {
                            var downurl = Index > -1 ? new Uri($"{Setting._GlobalSet.T66yAddress}{Index}") : new Uri($"{_Uri}");
                            try
                            {
                                Setting.T66yDownLoadNow = $"开始下载第{Index}页";
                                HttpResponse response = request.Get(downurl);
                                var RetS = response.ToString();
                                //File.WriteAllBytes("Html", response.Ret);
                                Setting._GlobalSet.totalDownloadBytes += response.Ret.Length;
                                Setting.T66yDownLoadNow = $"第{Index}页下载完毕";
                                return Encoding.GetEncoding("GBK").GetString(response.Ret);
                            }
                            catch (Exception ex)
                            {
                                if (Setting._GlobalSet.SocksCheck && Setting.Socks5Point != 0)
                                    request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.Socks5Point}");
                                if (ex.Message.StartsWith("Cannot access a disposed object"))
                                {
                                    Loger.Instance.LocalInfo($"SSR异常，退出全部下载进程");
                                    //T66yDownloadCancel.Cancel();
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
            IEnumerable<string[]> AnalyT66yMainPage(HtmlDocument HtmlDoc, string Page)
            {
                if (string.IsNullOrEmpty(Page))
                {
                    Loger.Instance.LocalInfo($"T66y页面为空"); yield break;
                }
                HtmlDoc.LoadHtml(Page);
                var Count = HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[2]/div[2]/table[1]/tbody[1]/tr").Count;
                foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[2]/div[2]/table[1]/tbody[1]/tr"))
                {
                    if (string.IsNullOrEmpty(item.InnerHtml)) continue;
                    if (item.InnerLength < 50)
                        continue;
                    if (item.Attributes["class"].Value == "tr3 t_one tac" && item.SelectSingleNode("td[1]").InnerHtml.Contains(".::"))
                    {
                        var TempData = new string[5];
                        try
                        {
                            var temp = HtmlNode.CreateNode(item.OuterHtml);
                            var Url = temp.SelectSingleNode("td[2]/h3/a").Attributes["href"].Value;
                            var Title = temp.SelectSingleNode("td[2]/h3/a").InnerHtml;
                            var id = "";
                            if (Url.StartsWith("htm"))
                            {
                                id = Url.Replace("htm_data", "").Replace(".html", "").Split('/')[3];
                            }
                            else
                            {
                                id = Url.Split('=')[1];
                            }
                            TempData = new string[]
                            {
                                Url,//地址
                                Title ,//标题
                                string.Empty ,//日期
                                id,//编号
                                string.Empty,//内容
                            };
                        }
                        catch (Exception)
                        {
                            Loger.Instance.LocalInfo($"T66y页面解析错误");
                            File.AppendAllLines("T66y.txt", new string[] { item.OuterHtml });
                        }
                        yield return TempData;
                    }
                }
                yield break;

                Tuple<string, string> Get(ref string Url)
                {
                    if (Url.StartsWith("htm"))
                    {
                        var UrlS = Url.Replace("htm_data", "").Replace(".html", "").Split('/');
                        return new Tuple<string, string>($"{UrlS[3]}", string.Empty);
                    }
                    else
                    {
                        var TempDoc = new HtmlDocument();
                        HttpClient client = new HttpClient();
                        if (Setting._GlobalSet.NyaaSocksCheck && Setting.NyaaSocks5Point != 0)
                            client = new HttpClient(new ProxyClientHandler<Socks5>(new ProxySettings { Host = "localhost", Port = Setting.NyaaSocks5Point }));
                        else
                            client = new HttpClient();
                        TempDoc.Load(client.GetStringAsync($"http://t66y.com/{Url}").Result);
                        Url = TempDoc.DocumentNode.SelectSingleNode("/html/body/center/div/a[2]").Attributes["href"].Value;
                        return Get(ref Url);
                    }
                }
            }
        }

        public async Task<byte[]> DownloadImgAsync(string url)
        {
            /*var serviceProvider = new ServiceCollection().AddHttpClient("zhihu", client =>
            {
                //todo
            }).AddHttpMessageHandler(c => c.);*/
            byte[] RetB = null;
            for (int i = 0; i < 4; i++)
            //{
            //Parallel.For(0, 4, (i, Status) =>
            {
                switch (i)
                {
                    case 10:
                        {
                            var Download = new BetterHttpClient.HttpClient(new BetterHttpClient.Proxy("localhost", 1088));

                            try
                            {
                                Download.Proxy.ProxyType = BetterHttpClient.ProxyTypeEnum.Socks;

                                var RET = await Download.DownloadDataTaskAsync(url);
                                Setting._GlobalSet.totalDownloadBytes += RET.Length;
                                if (RET == null) continue;
                                Setting.DataCount.D0 += 1;

                                RetB = RET;
                                goto Stop;

                                //ret = RET;
                                //Status.Stop();
                            }
                            catch (Exception)
                            {
                                Download.Dispose();
                            }
                        }
                        break;

                    case 9:
                        {
                            var Download = new BetterHttpClient.HttpClient();

                            try
                            {
                                var RET = await Download.DownloadDataTaskAsync(url);
                                Setting._GlobalSet.totalDownloadBytes += RET.Length;
                                if (RET == null) continue;
                                Setting.DataCount.D1 += 1;

                                RetB = RET;
                                goto Stop;

                                //ret = RET;
                                //Status.Stop();
                            }
                            catch (Exception)
                            {
                                Download.Dispose();
                            }
                            goto Stop;
                        }
                        break;

                    case 8:
                        {
                            try
                            {
                                using var socks5ProxyClient = new Proxy.Client.Socks5ProxyClient("localhost", 1088);
                                {
                                    var response = await socks5ProxyClient.GetAsync(url);
                                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        Setting.DataCount.D0 += 1;
                                        RetB = response.Bin;
                                        goto Stop;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                //Loger.Instance.LocalInfo($"下载错误{ex.Message}|{ex.StackTrace}");
                            }
                        }
                        break;

                    case 7:
                        {
                            // Setting.T66yDownLoadNowOther = $"第{PageCount}页{TempData.Date}-{TempData.id}\r\n下载第{ImgList.Count
                            // + 1}张图片第{i}次";
                            HttpClient Download = new HttpClient(new ProxyClientHandler<Socks5>(new ProxySettings { Host = "localhost", Port = Setting.NyaaSocks5Point }));

                            try
                            {
                                Download.Timeout = new TimeSpan(0, 0, 10);
                                var RET = await Download.GetByteArrayAsync(url);
                                Setting._GlobalSet.totalDownloadBytes += RET.Length;
                                if (RET == null) continue;
                                Setting.DataCount.D2 += 1;
                                RetB = RET;
                                goto Stop;
                                //ret = RET;
                                //Status.Stop();
                            }
                            catch (Exception)
                            {
                                Download.Dispose();
                            }
                            goto Stop;
                        }
                        break;

                    case 6:
                        {
                            HttpClient Download = new HttpClient();

                            try
                            {
                                Download.Timeout = new TimeSpan(0, 0, 10);
                                var RET = await Download.GetByteArrayAsync(url);
                                Setting._GlobalSet.totalDownloadBytes += RET.Length;
                                if (RET == null) continue;
                                Setting.DataCount.D1 += 1;

                                RetB = RET;
                                goto Stop;

                                //ret = RET;
                                //Status.Stop();
                            }
                            catch (Exception)
                            {
                                Download.Dispose();
                            }
                        }
                        break;

                    case 5:
                        {
                            HttpClient Download = new HttpClient(new ProxyClientHandler<Socks5>(new ProxySettings { Host = "localhost", Port = Setting.NyaaSocks5Point == 1088 ? Setting.Socks5Point : 1088 }));

                            try
                            {
                                Download.Timeout = new TimeSpan(0, 0, 10);
                                var RET = await Download.GetByteArrayAsync(url);
                                Setting._GlobalSet.totalDownloadBytes += RET.Length;
                                if (RET == null) continue;
                                Setting.DataCount.D2 += 1;

                                RetB = RET;
                                goto Stop;
                                //ret = RET;
                                //Status.Stop();
                            }
                            catch (Exception ex)
                            {
                                Download.Dispose();
                            }
                            goto Stop;
                        }
                        break;

                    case 0:
                        {
                            try
                            {
                                request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.NyaaSocks5Point}");
                                HttpResponse response = request.Get(url);
                                var RetS = response.ToString();
                                Setting._GlobalSet.totalDownloadBytes += response.Ret.Length;
                                if (response.Ret == null) continue;
                                Setting.DataCount.D0 += 1;
                                RetB = response.Ret;
                                goto Stop;
                                //ret = response.Ret;
                                //Status.Stop();
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message == "MovedPermanently")
                                    try
                                    {
                                        if (request.Response.RedirectAddress != null)
                                        {
                                            var downurl = new Uri(url);
                                            if (request.Response.RedirectAddress.Authority != downurl.Authority)
                                            {
                                                url = url.Replace(downurl.Authority, request.Response.RedirectAddress.Authority);
                                            }
                                        }
                                        HttpResponse response = request.Get(url);
                                        var RetS = response.ToString();
                                        Setting._GlobalSet.totalDownloadBytes += response.Ret.Length;
                                        if (response.Ret == null) continue;
                                        Setting.DataCount.D0 += 1;
                                        RetB = response.Ret;
                                        goto Stop;
                                    }
                                    catch (Exception)
                                    {
                                    }
                                request.Dispose();
                            }
                        }
                        break;

                    case 1:
                        {
                            try
                            {
                                request.Proxy = null;
                                HttpResponse response = request.Get(url);
                                var RetS = response.ToString();
                                Setting._GlobalSet.totalDownloadBytes += response.Ret.Length;
                                if (response.Ret == null) continue;
                                Setting.DataCount.D1 += 1;
                                RetB = response.Ret;
                                goto Stop;
                                //ret = response.Ret;
                                //Status.Stop();
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message == "MovedPermanently")
                                    try
                                    {
                                        if (request.Response.RedirectAddress != null)
                                        {
                                            var downurl = new Uri(url);
                                            if (request.Response.RedirectAddress.Authority != downurl.Authority)
                                            {
                                                url = url.Replace(downurl.Authority, request.Response.RedirectAddress.Authority);
                                            }
                                        }
                                        HttpResponse response = request.Get(url);
                                        var RetS = response.ToString();
                                        Setting._GlobalSet.totalDownloadBytes += response.Ret.Length;
                                        if (response.Ret == null) continue;
                                        Setting.DataCount.D1 += 1;
                                        RetB = response.Ret;
                                        goto Stop;
                                    }
                                    catch (Exception)
                                    {
                                    }
                                request.Dispose();
                            }
                        }
                        break;

                    case 2:
                        {
                            try
                            {
                                request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.Socks5Point}");
                                HttpResponse response = request.Get(url);
                                var RetS = response.ToString();
                                Setting._GlobalSet.totalDownloadBytes += response.Ret.Length;
                                if (response.Ret == null) continue;
                                Setting.DataCount.D2 += 1;
                                RetB = response.Ret;
                                goto Stop;
                                //ret = response.Ret;
                                //Status.Stop();
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message == "MovedPermanently")
                                    try
                                    {
                                        if (request.Response.RedirectAddress != null)
                                        {
                                            var downurl = new Uri(url);
                                            if (request.Response.RedirectAddress.Authority != downurl.Authority)
                                            {
                                                url = url.Replace(downurl.Authority, request.Response.RedirectAddress.Authority);
                                            }
                                        }
                                        HttpResponse response = request.Get(url);
                                        var RetS = response.ToString();
                                        Setting._GlobalSet.totalDownloadBytes += response.Ret.Length;
                                        if (response.Ret == null) continue;
                                        Setting.DataCount.D2 += 1;
                                        RetB = response.Ret;
                                        goto Stop;
                                    }
                                    catch (Exception)
                                    {
                                    }
                                request.Dispose();
                            }
                            goto Stop;
                        }
                        break;

                    default:
                        break;
                }
            }
        //});
        Stop:
            GC.Collect();
            if (RetB != null)
            {
                if (RetB.Length > 1024)
                {
                    using var mem = new MemoryStream(RetB);
                    try
                    {
                        Image img = Image.FromStream(mem);
                        if (img.RawFormat.Equals(ImageFormat.Gif))
                        {
                            img.Dispose();
                            return RetB;
                        }
                        else
                        {
                            RetB = Compress(img, 75).ToArray();
                            img.Dispose();
                            return RetB;
                        }
                    }
                    catch (Exception)
                    {
                        return RetB;
                    }
                }
            }
            return null;
        }

        private static HttpRequest request = new HttpRequest()
        {
            UserAgent = Http.ChromeUserAgent(),
            ConnectTimeout = 5000,
            KeepAliveTimeout = 10000,
            ReadWriteTimeout = 10000
        };

        private MemoryStream Compress(Image srcBitmap, long level)
        {
            var destStream = new MemoryStream();
            ImageCodecInfo myImageCodecInfo;
            System.Drawing.Imaging.Encoder myEncoder;
            EncoderParameter myEncoderParameter;
            EncoderParameters myEncoderParameters;

            // Get an ImageCodecInfo object that represents the JPEG codec.
            myImageCodecInfo = GetEncoderInfo("image/jpeg");

            // Create an Encoder object based on the GUID

            // for the Quality parameter category.
            myEncoder = System.Drawing.Imaging.Encoder.Quality;

            // Create an EncoderParameters object. An EncoderParameters object has an array of
            // EncoderParameter objects. In this case, there is only one

            // EncoderParameter object in the array.
            myEncoderParameters = new EncoderParameters(1);

            // Save the bitmap as a JPEG file with 给定的 quality level
            myEncoderParameter = new EncoderParameter(myEncoder, level);
            myEncoderParameters.Param[0] = myEncoderParameter;
            srcBitmap.Save(destStream, myImageCodecInfo, myEncoderParameters);

            ImageCodecInfo GetEncoderInfo(String mimeType)
            {
                int j;
                ImageCodecInfo[] encoders;
                encoders = ImageCodecInfo.GetImageEncoders();
                for (j = 0; j < encoders.Length; ++j)
                {
                    if (encoders[j].MimeType == mimeType)
                        return encoders[j];
                }
                return null;
            }
            return destStream;
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
                        NyaaDownloadCancel = new CancellationTokenSource();
                        GetNyaaNewDataTimer.Stop();
                        var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                        //await Task.WhenAll(DownloadLoop(Setting._GlobalSet.NyaaAddress, Setting._GlobalSet.NyaaFin ? 0 : Setting._GlobalSet.NyaaLastPageIndex, DownloadCollect, NyaaNewDownloadCancel), HandlerNyaaHtml(DownloadCollect));
                        await Task.WhenAll(DownloadLoop(Setting._GlobalSet.NyaaAddress, 0, DownloadCollect, NyaaDownloadCancel, false, true), HandlerNyaaHtml(DownloadCollect));
                        Thread.Sleep(1000);
                        GetNyaaNewDataTimer.Interval = new Random().Next(6, 12) * 3600 * 1000;
                        Setting.NyaaDownLoadNow = DateTime.Now.AddMilliseconds(GetNyaaNewDataTimer.Interval).ToString("MM-dd|HH:mm");
                        Loger.Instance.LocalInfo($"Nyaa:下载完毕,耗时{RunSpan.Elapsed:mm\\分ss\\秒}");
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

        public Task GetJavNewData()
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
                        var ContinueCount = 0;
                        using (var request = new HttpRequest())
                        {
                            if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.Socks5Point}");
                            foreach (var item in SaveData.GetConsumingEnumerable())
                            {
                                if (item == null) continue;
                                Setting.JavDownLoadNow = $"{ item.Item1} |{Setting.JavPageCount}|{item.Item2.Date}|{ContinueCount}";
                                try
                                {
                                    using (var db = new LiteDatabase(@"Jav.db"))
                                    {
                                        var JavDB = db.GetCollection<JavInfo>("JavDB");
                                        if (JavDB.Exists(x => x.id == item.Item2.id))
                                        {
                                            Interlocked.Increment(ref ContinueCount);
                                            db.Dispose();
                                            if (ContinueCount > 10)
                                            {
                                                ContinueCount = 0;
                                                break;
                                            }
                                            continue;
                                        }
                                        else
                                        {
                                            ContinueCount = 0;
                                        }
                                    }
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
                                        if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.Socks5Point}");
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
                                    try
                                    {
                                        if (item.Item2.Image != null)
                                            if (item.Item2.Image.Length != 0)
                                            {
                                                // var ImageComp = Compress(Bitmap.FromStream(new
                                                // MemoryStream(item.Item2.Image)), 75).ToArray();
                                                var ImageComp = item.Item2.Image;
                                                if (ImageComp.Length < item.Item2.Image.Length)
                                                    item.Item2.Image = ImageComp;
                                            }
                                    }
                                    catch (Exception)
                                    {
                                    }

                                    if (NewDate != item.Item2.Date)
                                    {
                                        Loger.Instance.LocalInfo($"当前保存Jav下载日期{item.Item2.Date}");
                                        NewDate = item.Item2.Date;
                                        if (DataBaseCommand.GetOrSaveWebInfoFromJav(item.Item2.Date))
                                        {
                                            var TempDate = DateTime.ParseExact(item.Item2.Date, "yy-MM-dd", CultureInfo.InvariantCulture);
                                            if (TempDate.ToString("yy-MM-dd") == DateTime.Now.ToString("yy-MM-dd")) goto Skip;
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
                                Skip:
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
                                    if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.Socks5Point}");
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
                return Task.Run(async () =>
                {
                    var HtmlDoc = new HtmlDocument();
                    foreach (var Page in downloadCollect.GetConsumingEnumerable())
                    {
                        if (string.IsNullOrEmpty(Page.Item2))
                        {
                            await Task.Delay(1000);
                            continue;
                        }
                        HtmlDoc.LoadHtml(Page.Item2);
                        var TempData = new JavInfo();

                        foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[1]/div"))
                        {
                            try
                            {
                                TempData = new JavInfo();
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
                                TempData.Describe = temp.SelectSingleNode("div/div/div[2]/div/p[2]").InnerText;
                                //TempData.Describe = temp.SelectSingleNode("div/div/div[2]/div/p[2]").InnerText.ReplaceEntities().Replace("\n", "");
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
                            }
                            catch (Exception)
                            {
                                Loger.Instance.LocalInfo($"Jav解析失败");

                                File.WriteAllText($"错误{Guid.NewGuid().ToString()}.html", Page.Item2);

                                /*Setting._GlobalSet.JavFin = false;
                                downloadCollect.CompleteAdding();
                                SaveData.CompleteAdding();
                                JavDownloadCancel.Cancel();*/
                            }
                            SaveData.Add(new Tuple<int, JavInfo>(Page.Item1, TempData));
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
                                            DataBaseCommand.SaveToMiMiDataErrorUnit(new[]{
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
                        if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.Socks5Point}");
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
                                        if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.Socks5Point}");

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
                                                    try
                                                    {
                                                        if (_PostData.Ret.Length != 0)
                                                        {
                                                            var ImageComp = Compress(Bitmap.FromStream(new MemoryStream(_PostData.Ret)), 75).ToArray();
                                                            if (ImageComp.Length < _PostData.Ret.Length)
                                                                return new Tuple<string, byte[]>("", ImageComp);
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Loger.Instance.LocalInfo($"MiMi下载图片出错{ex.Message}");
                                                    }
                                                    return new Tuple<string, byte[]>("", _PostData.Ret);
                                                }
                                                else
                                                {
                                                    Loger.Instance.LocalInfo($"MiMi下载图片出错");
                                                }
                                            }
                                            break;

                                        case -3:
                                            {
                                                var _TempUri = downurl;
                                                var _TempDownloadUri = _TempUri.Scheme + Uri.SchemeDelimiter + _TempUri.Authority + "/load.php";
                                                var _PostData = request.Post(_TempDownloadUri, new RequestParams()
                                                {
                                                    new KeyValuePair<string, string>("ref", _TempUri.Query.Split('=')[1]),
                                                    new KeyValuePair<string, string>("submit ", "点击下载")}).ToBytes();
                                                if (_PostData.Length > 1000)
                                                {
                                                    return new Tuple<string, byte[]>("", _PostData);
                                                }
                                                else if (Encoding.Default.GetString(_PostData).StartsWith("No such file"))
                                                {
                                                    Loger.Instance.LocalInfo($"MiMi下载种子出错");
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
            }, MiMiDownloadCancel.Token);
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
                      if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.Socks5Point}");
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
                      if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.Socks5Point}");
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
                          if (Setting._GlobalSet.NyaaSocksCheck) request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.NyaaSocks5Point}");
                      }
                      else if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.Socks5Point}");
                      while (!token.Token.IsCancellationRequested)
                      {
                          if (downloadCollect.Count > 3)
                          {
                              Thread.Sleep(1000);
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
                                  if (Setting._GlobalSet.NyaaSocksCheck) request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.NyaaSocks5Point}");
                              }
                              else if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"localhost:{Setting.Socks5Point}");
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
            Setting.DownloadManage.SISDownloadCancel.Cancel();
            Setting.DownloadManage.T66yDownloadCancel.Cancel();
            while (!JavDownloadCancel.IsCancellationRequested
                && !NyaaDownloadCancel.IsCancellationRequested
                && !MiMiDownloadCancel.IsCancellationRequested
                && !MiMiAiStoryDownloadCancel.IsCancellationRequested
                && !SISDownloadCancel.IsCancellationRequested
                && !T66yDownloadCancel.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }
            Setting.SISDownLoadNow = string.Empty;
        }
    }
}