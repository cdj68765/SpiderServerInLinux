using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static SpiderServerInLinux.DataBaseCommand;

namespace SpiderServerInLinux
{
    internal class WebPageGet
    {
        private readonly CancellationTokenSource CancelInfo = new CancellationTokenSource();
        private int CurrectPageIndex;
        Stopwatch stopwatch = new Stopwatch();
        private Uri AddOneDay()
        {
            Interlocked.Increment(ref CurrectPageIndex);
            if (CurrectPageIndex > Setting.LastPageIndex) Setting.LastPageIndex = CurrectPageIndex;
          
            return new Uri($"{Setting.Address}?p={CurrectPageIndex}");
        }

        internal int PageInDateStatus(string Date)
        {
            var Status = GetDateInfo(Date);
            if (Status == null) return -1;
            return Status.Status == false ? 0 : 1;
        }

        internal async void DownloadNewInit(string Date)
        {
            Console.WriteLine("新数据下载初始化");
            var WebClient = new WebClientEx.WebClientEx();
            var TheFirstRet = new HandlerHtml(await WebClient.DownloadStringTaskAsync(AddOneDay()), null,
                DateTime.Now.ToString("yyyy-MM-dd"));
            var FirstDay = TheFirstRet.AnalysisData.Values.ElementAt(0).Day;
            var LastDay = TheFirstRet.AnalysisData.Values.ElementAt(TheFirstRet.AnalysisData.Count - 1).Day;

            if (FirstDay == LastDay)
            {
                if (FirstDay == Date)
                {
                    DownloadNewLoop(TheFirstRet.AnalysisData, Date);
                }
                else
                {
                    DownloadNewInit(Date);
                }
            }
            else
            {
                if (LastDay == Date)
                {
                    DownloadNewLoop(TheFirstRet.NextDayData, Date);
                }
                else
                {
                    DownloadNewInit(Date);
                }
            }
        }

        void DownloadNewLoop(ConcurrentDictionary<int, TorrentInfo> theFirstRet, string Day)
        {
            stopwatch.Stop();
            Console.WriteLine($"初始化时间{stopwatch.Elapsed.TotalSeconds}");
            var Download = new WebClientEx.WebClientEx();
            Console.WriteLine("开始新数据循环获取阶段");
            Download.DownloadStringCompleted += (Sender, Object) =>
            {
                try
                {
                    Console.WriteLine($"新下载当前页{CurrectPageIndex}");
                    var Ret = new HandlerHtml(Object.Result, theFirstRet, Day);
                    if (!Ret.AddFin)
                    {
                        Download.DownloadStringAsync(AddOneDay());
                    }
                    else
                    {
                        var StatusNum = PageInDateStatus(Day);
                        if (StatusNum == 0)
                        {
                            stopwatch.Restart();
                            SaveToDataBaseOneByOne(Ret.AnalysisData.Values, CurrectPageIndex,true);
                            stopwatch.Stop();
                            Console.WriteLine($"各个添加耗时{stopwatch.Elapsed.TotalSeconds}");
                        }
                        else if (StatusNum == -1)
                        {
                            stopwatch.Restart();
                            SaveToDataBaseRange(Ret.AnalysisData.Values, CurrectPageIndex,true);
                        }

                        SaveStatus();
                        if (PageInDateStatus(Ret.NextDayData.Values.ElementAt(0).Day) != -1)
                        {
                            var NextData = new ConcurrentDictionary<int, TorrentInfo>(Ret.NextDayData);
                            Ret.Dispose();
                            DownloadNewLoop(NextData, NextData.Values.ElementAt(0).Day);
                            stopwatch.Stop();
                            Console.WriteLine($"集群添加耗时{stopwatch.Elapsed.TotalSeconds}");
                        }
                    }
                }
                catch (Exception e)
                {
                    ErrorDealWith(e, Download);
                }
            };
            Download.DownloadStringAsync(AddOneDay());
        }

        internal async void DownloadOldInit()
        {
            stopwatch.Start();
            Console.WriteLine("开始获得旧数据");
            /*思路设想
             1.先获取第一次数据，然后判断数据库内该时间段是否已经完成//分析获得数据第一个和最后一个的时间
             2.未完成的情况下，开始不断获取任务，获取一天的时间//获取阶段使用单线程分析
             3.检测每次获得的数据时间，如果出现差别就暂停任务，重置数据后进行新一轮下载
             */
            //首先获得一波页面数据分析
            var WebClient = new WebClientEx.WebClientEx();
          //  CurrectPageIndex = Setting.LastPageIndex;
            Console.WriteLine("开始第一次获取初始化");
            var TheFirstRet = new HandlerHtml(await WebClient.DownloadStringTaskAsync(
                new Uri($"{Setting.Address}?p={CurrectPageIndex}")));
            //于数据库交流，获得数据库时间状态
            //首先判断首尾是否相同 用来判断同一页是否有日期交替

            var FirstDay = TheFirstRet.AnalysisData.Values.ElementAt(0).Day;
            var LastDay = TheFirstRet.AnalysisData.Values.ElementAt(TheFirstRet.AnalysisData.Count - 1).Day;
            if (FirstDay == LastDay)
            {
                if (PageInDateStatus(FirstDay) == 1)
                {
                    AddOneDay();
                    DownloadOldInit();
                }
                else
                {
                    DownloadOldLoop(TheFirstRet.AnalysisData, LastDay);
                }
            }
            else
            {
                if (PageInDateStatus(FirstDay) == 1)
                {
                    if (PageInDateStatus(LastDay) == 1)
                    {
                        AddOneDay();
                        DownloadOldInit();
                    }
                    else
                    {
                        TheFirstRet.NextDayData = new ConcurrentDictionary<int, TorrentInfo>();
                        foreach (var VARIABLE in TheFirstRet.AnalysisData)
                        {
                            if (VARIABLE.Value.Day == LastDay)
                                TheFirstRet.NextDayData.TryAdd(VARIABLE.Key, VARIABLE.Value);
                            DownloadOldLoop(TheFirstRet.NextDayData, LastDay);
                        }
                    }
                }
                else
                {
                    DownloadOldLoop(TheFirstRet.AnalysisData, FirstDay);
                }
            }
        }

        private void DownloadOldLoop(ConcurrentDictionary<int, TorrentInfo> theFirstRet, string Day)
        {
            stopwatch.Stop();
            Console.WriteLine($"初始化耗时{stopwatch.Elapsed.TotalSeconds}");
            var Download = new WebClientEx.WebClientEx();
            Download.DownloadStringCompleted += (Sender, Object) =>
            {
                try
                {
                    Console.WriteLine($"旧数据当前页{CurrectPageIndex}");
                    var Ret = new HandlerHtml(Object.Result, theFirstRet, Day);
                    //检查是否遍历到了下一天
                    if (!Ret.AddFin)
                    {
                        stopwatch.Restart();
                        //是的话当前页+1，并下载
                        Download.DownloadStringAsync(AddOneDay());
                    }
                    else
                    {
                        stopwatch.Stop();
                        Console.WriteLine($"网页获取耗时{stopwatch.Elapsed.TotalSeconds}");
                        Console.WriteLine($"当前获取日期{Day}");
                        var StatusNum = PageInDateStatus(Day);
                        //假如未完成，从第一条开始进入获取状态
                        if (StatusNum == 0)
                        {
                            stopwatch.Restart();
                            SaveToDataBaseOneByOne(Ret.AnalysisData.Values, CurrectPageIndex,true);
                            stopwatch.Stop();
                            Console.WriteLine($"各个添加耗时{stopwatch.Elapsed.TotalSeconds}");
                        }
                        //假如从未开始过，则进入全部重新状态
                        else if (StatusNum == -1)
                        {
                            stopwatch.Restart();
                            SaveToDataBaseRange(Ret.AnalysisData.Values, CurrectPageIndex,true);
                            stopwatch.Stop();
                            Console.WriteLine($"集群添加耗时{stopwatch.Elapsed.TotalSeconds}");
                        }

                        SaveStatus();
                        var NextData = new ConcurrentDictionary<int, TorrentInfo>(Ret.NextDayData);
                        Ret.Dispose();
                        DownloadOldLoop(NextData, NextData.Values.ElementAt(0).Day);
                    }
                }
                catch (Exception e)
                {
                    ErrorDealWith(e, Download);
                }
            };
            Download.DownloadStringAsync(AddOneDay());
        }

        private void ErrorDealWith(Exception e, WebClientEx.WebClientEx download)
        {
            if (download.ErrorInfo == "Timeout")
            {
            }
            else if ((e as WebException).Status == WebExceptionStatus.UnknownError)
            {
            }

            CancelInfo.Cancel();
        }
    }

    internal class DownWork : WebPageGet
    {
        private readonly string DayOfToday;

        internal DownWork()
        {
            DownLoadOldPage();
            DayOfToday = DateTime.Now.ToString("yyyy-MM-dd");
            DownLoadNewPage();
        }

        private void DownLoadOldPage()
        {
            Task.Factory.StartNew(DownloadOldInit, Setting.CancelSign.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private void DownLoadNewPage()
        {
            Task.Factory.StartNew(() =>
                {
                    Console.WriteLine("开始获得新数据");
                    Thread.Sleep(new TimeSpan(0, 0, 60, 0, 0)); //每小时遍历一次吧
                    if (DateTime.Now.ToString("yyyy-MM-dd") != DayOfToday)
                        if (PageInDateStatus(DayOfToday) != 1)
                            DownloadNewInit(DayOfToday);
                    DownLoadNewPage();
                }, Setting.CancelSign.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }
    }
}