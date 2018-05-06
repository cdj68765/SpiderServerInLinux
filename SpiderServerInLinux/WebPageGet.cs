using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static SpiderServerInLinux.DataBaseCommand;

namespace SpiderServerInLinux
{
    internal class WebPageGet
    {
         int CurrectPageIndex;
       
        readonly CancellationTokenSource CancelInfo = new CancellationTokenSource();

        internal WebPageGet()
        {
            //阐述目前遇到的问题，首先启动下载，但是我并不知道下载数据是否在数据库上已经存在，因此需要以日期为单位检索
            //终上所述，检索每一页的首位和尾位日期，检索首位在数据库内的状态，如果首位和尾位的日期不同，检索尾位在数据库内的状态
            //如果不存在，则新建线程开始以天为单位的记录
            //由于网页数据是向后更新的，因此如果找到存在数据，但是未完成状态的，那么就以首位为起始，找到不一样的为止，为了防止万一，使用Dic建立索引
        }

        Uri AddOneDay()
        {
            //无论什么情况下，下载完一次就PageIndex自增
            Interlocked.Increment(ref CurrectPageIndex);
            if (CurrectPageIndex > Setting.setting.LastPageIndex)
            {
                Setting.setting.LastPageIndex = CurrectPageIndex;
            }
            return new Uri($"{Setting.setting.Address}?p={CurrectPageIndex}");
        }
        public void GetPage(string Path = "")
        {
            Task.Factory.StartNew(() =>
                {
                    WebClientEx.WebClientEx WebClient = new WebClientEx.WebClientEx();
                    WebClient.DownloadStringCompleted += (Sender, Object) =>
                    {
                        try
                        {
                            //Setting.setting.WordProcess.Add(new HandlerHtml(Object.Result).AnyData);
                            /* Interlocked.Increment(ref Setting.setting.LastPage);
                             WebClient.DownloadStringAsync(
                                 new Uri($"{Setting.setting.Address}?p={Setting.setting.LastPage}"));*/
                        }
                        catch (Exception e)
                        {
                            if (WebClient.ErrorInfo == "Timeout")
                            {

                            }
                            else if ((e as WebException).Status == WebExceptionStatus.UnknownError)
                            {

                            }
                        }

                    };
                }, CancelInfo.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            // WebClient.DownloadStringAsync(new Uri($"{Setting.setting.Address}?p={Setting.setting.LastPage}"));

        }


        public async void DownloadInit(bool StartNew = false)
        {
            /*思路设想
             1.先获取第一次数据，然后判断数据库内该时间段是否已经完成//分析获得数据第一个和最后一个的时间
             2.未完成的情况下，开始不断获取任务，获取一天的时间//获取阶段使用单线程分析
             3.检测每次获得的数据时间，如果出现差别就暂停任务，重置数据后进行新一轮下载
             */
            //首先获得一波页面数据分析
            var WebClient = new WebClientEx.WebClientEx();
            CurrectPageIndex = !StartNew ? Setting.setting.LastPageIndex : 1;
            var TheFirstRet = new HandlerHtml(await WebClient.DownloadStringTaskAsync(
                new Uri($"{Setting.setting.Address}?p={CurrectPageIndex}")));

            //于数据库交流，获得数据库时间状态
            //首先判断首尾是否相同 用来判断同一页是否有日期交替
            if (!StartNew)
            {
                var FirstDay = TheFirstRet.AnalysisData.Values.ElementAt(0).Day;
                var FirstDayStatus = PageInDateStatus(FirstDay);
                if (!TheFirstRet.AddFin)
                {
                    //假如已经存在，且已经完成，则向后追溯
                    if (FirstDayStatus == 1)
                    {
                        AddOneDay();
                        DownloadInit();
                    }
                    else
                    {
                        DownloadLoop(TheFirstRet.AnalysisData, FirstDay);
                    }
                }
                else
                {
                    //如果不相同则分别判断首位和尾位的状态
                    //由于数据是不断更新的，所以即便是未完成状态，也不需要往前追溯
                    //对于首尾差异，仅判断首的状态
                    var LastDay = TheFirstRet.NextDayData.Values.ElementAt(0).Day;
                    var LastDayStatus = PageInDateStatus(LastDay);
                    //对于已经完成的，则从上至下判断差异时间，从差异时间开始获取
                    if (FirstDayStatus == 1)
                    {
                        if (LastDayStatus == 1)
                        {
                            AddOneDay();
                            DownloadInit();
                        }
                        else
                        {
                            DownloadLoop(TheFirstRet.NextDayData, LastDay);
                        }
                    }
                    //假如未完成，从当前开始进入获取状态
                    else
                    {
                        DownloadLoop(TheFirstRet.AnalysisData, FirstDay);
                    }
                }
            }
        }
        //判断从数据库返回的历史日期状态 -1代表没有创建过，0代表未完成，1代表已经完成
        int PageInDateStatus(string Date)
        {
            var Status = GetDateInfo(Date);
            if (Status == null) return -1;
            return Status.Status == false ? 0 : 1;
        }
        private void DownloadLoop(ConcurrentDictionary<int, TorrentInfo> theFirstRet, string Day)
        {
            Task.Factory.StartNew(() =>
            {
                var Download = new WebClientEx.WebClientEx();
                Download.DownloadStringCompleted += (Sender, Object) =>
                {
                    try
                    {
                        var Ret = new HandlerHtml(Object.Result, theFirstRet, Day);
                        //检查是否遍历到了下一天
                        if (!Ret.AddFin)
                        {
                            //是的话当前页+1，并下载
                            Download.DownloadStringAsync(AddOneDay());
                        }
                        else
                        {
                            var StatusNum = PageInDateStatus(Day);
                            //假如未完成，从第一条开始进入获取状态
                            if (StatusNum == 0)
                            {
                                SaveToDataBaseOneByOne(Ret.AnalysisData.Values, CurrectPageIndex, true);
                            }
                            //假如从未开始过，则进入全部重新状态
                            else if (StatusNum == -1)
                            {
                                SaveToDataBaseRange(Ret.AnalysisData.Values, CurrectPageIndex, true);
                            }
                            AddOneDay();
                            var NextData = new ConcurrentDictionary<int, TorrentInfo>(Ret.NextDayData);
                            Ret.Dispose();
                            DownloadLoop(NextData, NextData.Values.ElementAt(0).Day);
                        }
                    }
                    catch (Exception e)
                    {
                        ErrorDealWith(e, Download);
                    }
                };
                Download.DownloadStringAsync(AddOneDay());
            }, CancelInfo.Token, TaskCreationOptions.None,
           TaskScheduler.Default);
        }

      /*  private void StartOneByOneAddLoop(ConcurrentDictionary<int, TorrentInfo> theFirstRet,string Day)
        {
            Task.Factory.StartNew(() =>
            {
                var Download = new WebClientEx.WebClientEx();
                Download.DownloadStringCompleted += (Sender, Object) =>
                {
                    try
                    {
                        var Ret = new HandlerHtml(Object.Result,theFirstRet, Day);
                        //检查是否遍历到了下一天
                        if (!Ret.AddFin)
                        {
                            //是的话当前页+1，并下载
                            Download.DownloadStringAsync(AddOneDay());
                        }
                        else
                        {
                            var FirstRetList = new List<TorrentInfo>(Ret.AnalysisData.Values);
                        }
                        //Setting.setting.WordProcess.Add(new HandlerHtml(Object.Result).AnyData);
                        /* Interlocked.Increment(ref Setting.setting.LastPage);
                         WebClient.DownloadStringAsync(
                             new Uri($"{Setting.setting.Address}?p={Setting.setting.LastPage}"));*/
                 //   }
                 /*   catch (Exception e)
                    {
                        ErrorDealWith(e, Download);
                    }
                };
                Download.DownloadStringAsync(AddOneDay());
            }, CancelInfo.Token, TaskCreationOptions.None,
           TaskScheduler.Default);

        }*/

        private void ErrorDealWith(Exception e, WebClientEx.WebClientEx download)
        {
            if (download.ErrorInfo == "Timeout")
            {

            }
            else if ((e as WebException).Status == WebExceptionStatus.UnknownError)
            {

            }
        }
    }
}