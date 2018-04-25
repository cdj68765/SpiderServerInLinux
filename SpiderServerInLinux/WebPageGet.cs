using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SpiderServerInLinux
{
    internal class WebPageGet
    {
        readonly WebClient WebClient = new WebClientEx.WebClientEx();
        readonly CancellationTokenSource CancelInfo = new CancellationTokenSource();

        public void GetPage(string Path = "")
        {
            Task.Factory.StartNew(() =>
                {
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
                            if (WebClientEx.WebClientEx.ErrorInfo == "Timeout")
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


        async void DownloadControl()
        {
            var TheFirstRet = new HandlerHtml(await WebClient.DownloadStringTaskAsync(
                new Uri($"{Setting.setting.Address}?p={Setting.setting.LastPage}"))).AnalysisData;
            /*思路设想
             1.先获取第一次数据，然后判断数据库内该时间段是否已经完成//分析获得数据第一个和最后一个的时间
             2.未完成的情况下，开始不断获取任务，获取一天的时间//获取阶段使用单线程分析
             3.检测每次获得的数据时间，如果出现差别就暂停任务，重置数据后进行新一轮下载
             */
            
        }
    }
}