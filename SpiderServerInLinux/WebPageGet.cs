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
        Stopwatch Time = new Stopwatch();
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
            Loger.Instance.LocalInfo("新数据下载初始化");
            var WebClient = new WebClientEx.WebClientEx();
            var TheFirstRet = new HandlerHtml(await WebClient.DownloadStringTaskAsync(AddOneDay()), null,
                DateTime.Now.ToString("yyyy-MM-dd"));
            var FirstDay = TheFirstRet.AnalysisData.Values.ElementAt(0).Day;
            var LastDay = TheFirstRet.AnalysisData.Values.ElementAt(TheFirstRet.AnalysisData.Count - 1).Day;

            if (FirstDay == LastDay)
            {
                if (FirstDay == Date)
                {
                    Loger.Instance.LocalInfo("数据循环下载");
                    DownloadNewLoop(TheFirstRet.AnalysisData, Date);
                }
                else
                {
                    Loger.Instance.LocalInfo("下载初始化");
                    DownloadNewInit(Date);
                }
            }
            else
            {
                if (LastDay == Date)
                {
                    Loger.Instance.LocalInfo("数据循环下载");
                    DownloadNewLoop(TheFirstRet.NextDayData, Date);
                }
                else
                {
                    Loger.Instance.LocalInfo("下载初始化");
                    DownloadNewInit(Date);
                }
            }
        }

        void DownloadNewLoop(ConcurrentDictionary<int, TorrentInfo> theFirstRet, string Day)
        {
            var Download = new WebClientEx.WebClientEx();
            Download.DownloadStringCompleted += (Sender, Object) =>
            {
                try
                {
                    Loger.Instance.WithTimeRestart($"下载完毕", Time);
                    var Ret = new HandlerHtml(Object.Result, theFirstRet, Day);
                    Loger.Instance.WithTimeRestart($"分析数据", Time);
                    if (!Ret.AddFin)
                    {
                        Download.DownloadStringAsync(AddOneDay());
                    }
                    else
                    {
                        var StatusNum = PageInDateStatus(Day);
                        if (StatusNum == 0)
                        {
                            SaveToDataBaseOneByOne(Ret.AnalysisData.Values, CurrectPageIndex,true);
                           
                        }
                        else if (StatusNum == -1)
                        {
                            SaveToDataBaseRange(Ret.AnalysisData.Values, CurrectPageIndex,true);
                        }

                        SaveStatus();
                        if (PageInDateStatus(Ret.NextDayData.Values.ElementAt(0).Day) != -1)
                        {
                            Loger.Instance.WithTimeRestart($"开始集群添加", Time);
                            var NextData = new ConcurrentDictionary<int, TorrentInfo>(Ret.NextDayData);
                            Ret.Dispose();
                            Loger.Instance.WithTimeRestart($"集群添加完毕，开始新一轮循环", Time);
                            DownloadNewLoop(NextData, NextData.Values.ElementAt(0).Day);
                         
                        }
                    }
                }
                catch (Exception e)
                {
                    ErrorDealWith(e, Download);
                }
            };
           
            var DownloadPage = AddOneDay();
            Loger.Instance.WithTimeStart($"下载页面:{DownloadPage}", Time);
            Download.DownloadStringAsync(DownloadPage);
        }

        internal async void DownloadOldInit()
        {
            Loger.Instance.LocalInfo("开始获得旧数据");
            /*思路设想
             1.先获取第一次数据，然后判断数据库内该时间段是否已经完成//分析获得数据第一个和最后一个的时间
             2.未完成的情况下，开始不断获取任务，获取一天的时间//获取阶段使用单线程分析
             3.检测每次获得的数据时间，如果出现差别就暂停任务，重置数据后进行新一轮下载
             */
            //首先获得一波页面数据分析
            var WebClient = new WebClientEx.WebClientEx();
            //  CurrectPageIndex = Setting.LastPageIndex;
            Loger.Instance.LocalInfo("开始第一次获取初始化");
            var TheFirstRet = new HandlerHtml(await WebClient.DownloadStringTaskAsync(
                new Uri($"{Setting.Address}?p={CurrectPageIndex}")));
            //于数据库交流，获得数据库时间状态
            //首先判断首尾是否相同 用来判断同一页是否有日期交替

            var FirstDay = TheFirstRet.AnalysisData.Values.ElementAt(0).Day;
            var LastDay = TheFirstRet.AnalysisData.Values.ElementAt(TheFirstRet.AnalysisData.Count - 1).Day;
            if (FirstDay == LastDay)
            {
                Loger.Instance.WithTimeStart("查询数据库时间差",Time);
                if (PageInDateStatus(FirstDay) == 1)
                {
                    Loger.Instance.WithTimeStop("查询完毕，当前页为第一天", Time);
                    AddOneDay();
                    DownloadOldInit();
                }
                else
                {
                    Loger.Instance.WithTimeStop("查询完毕，当前页非第一天", Time);
                    DownloadOldLoop(TheFirstRet.AnalysisData, LastDay);
                }
            }
            else
            {
                Loger.Instance.WithTimeStart("查询完毕当前页非第一天，再次查询", Time);
                if (PageInDateStatus(FirstDay) == 1)
                {
                    if (PageInDateStatus(LastDay) == 1)
                    {
                        Loger.Instance.WithTimeStop("查询完毕当前页为第一天", Time);
                        AddOneDay();
                        DownloadOldInit();
                    }
                    else
                    {
                        Loger.Instance.WithTimeStop("查询完毕，当前页非第一天", Time);
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
                    Loger.Instance.WithTimeStop("查询完毕", Time);
                    DownloadOldLoop(TheFirstRet.AnalysisData, FirstDay);
                }
            }
        }

        private void DownloadOldLoop(ConcurrentDictionary<int, TorrentInfo> theFirstRet, string Day)
        {
            try
            {
                var Download = new WebClientEx.WebClientEx();
                Download.DownloadStringCompleted += (Sender, Object) =>
                {
                    try
                    {
                        Loger.Instance.PageInfo(CurrectPageIndex);
                        Loger.Instance.WithTimeRestart($"下载完毕", Time);
                        var Ret = new HandlerHtml(Object.Result, theFirstRet, Day);
                        Loger.Instance.WithTimeRestart($"分析数据", Time);
                        //检查是否遍历到了下一天
                        if (!Ret.AddFin)
                        {
                            //是的话当前页+1，并下载
                            var DownloadTemp = AddOneDay();
                            Loger.Instance.LocalInfo($"当前页增加为{DownloadTemp}，继续下载");
                            Download.DownloadStringAsync(DownloadTemp);
                        }
                        else
                        {
                            Loger.Instance.WithTimeRestart($"查询页面时间", Time);
                            var StatusNum = PageInDateStatus(Day);
                            Loger.Instance.WithTimeRestart($"查询页面完毕", Time);
                            //假如未完成，从第一条开始进入获取状态
                            if (StatusNum == 0)
                            {
                                Loger.Instance.WithTimeRestart($"遍历添加到列", Time);
                                SaveToDataBaseOneByOne(Ret.AnalysisData.Values, CurrectPageIndex, true);
                                Loger.Instance.WithTimeRestart($"添加完毕", Time);
                            }
                            //假如从未开始过，则进入全部重新状态
                            else if (StatusNum == -1)
                            {
                                Loger.Instance.WithTimeRestart($"全部添加到列", Time);
                                SaveToDataBaseRange(Ret.AnalysisData.Values, CurrectPageIndex, true);
                                Loger.Instance.WithTimeRestart($"添加完毕", Time);
                            }

                            SaveStatus();
                            var NextData = new ConcurrentDictionary<int, TorrentInfo>(Ret.NextDayData);
                            Ret.Dispose();
                            Loger.Instance.WithTimeStop($"进行新一轮添加", Time);
                            DownloadOldLoop(NextData, NextData.Values.ElementAt(0).Day);
                        }
                    }
                    catch (Exception e)
                    {
                        Loger.Instance.LocalInfo(e.ToString());
                        //ErrorDealWith(e, Download);
                    }
                };
                var DownloadPage = AddOneDay();
                Loger.Instance.WithTimeStart($"下载页面:{DownloadPage}", Time);
                int time = new Random().Next(1000, 5000);
                for (int i = time; i > 0; i -= 500)
                {
                    Loger.Instance.WaitTime(i / 1000);
                    Thread.Sleep(500);
                }
                Download.DownloadStringAsync(DownloadPage);
            }
            catch (Exception e)
            {

            }

        }

        private void ErrorDealWith(Exception e, WebClientEx.WebClientEx download)
        {
            if (download.ErrorInfo == "Timeout")
            {
            }
            else if ((e as WebException).Status == WebExceptionStatus.UnknownError)
            {
            }

           // CancelInfo.Cancel();
        }
    }

    internal class DownWork : WebPageGet
    {
        private  string DayOfToday="";

        internal DownWork()
        {
            DownLoadOldPage();
          //  DownLoadNewPage();
        }

        private void DownLoadOldPage() => Task.Factory.StartNew(DownloadOldInit, Setting.CancelSign.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        private void DownLoadNewPage() => Task.Factory.StartNew(() =>
                                            {
                                                Loger.Instance.LocalInfo("开始获得新数据");
                                                Thread.Sleep(new TimeSpan(0, 0, 60, 0, 0)); //每小时遍历一次吧
                                                if (DateTime.Now.ToString("yyyy-MM-dd") != DayOfToday)
                                                    if (PageInDateStatus(DayOfToday) != 1)
                                                        DownloadNewInit(DayOfToday);
                                                DownLoadNewPage();
                                            }, Setting.CancelSign.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
    }
}