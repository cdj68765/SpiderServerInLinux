using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SpiderServerInLinux
{
    internal class DownloadHelp
    {
        internal readonly BlockingCollection<Tuple<int, string>> DownloadCollect;

        internal int DownloadMaxPage = 10;
        internal CancellationTokenSource CancelSign = new CancellationTokenSource();
        //private readonly Stopwatch Time = new Stopwatch();
        private readonly WebClientEx.WebClientEx WebClient = new WebClientEx.WebClientEx();
        private int CurrectPageIndex;

        internal DownloadHelp()
        {

            DownloadCollect = new BlockingCollection<Tuple<int, string>>(DownloadMaxPage);
            CancelSign = new CancellationTokenSource();
        }

        internal async Task DownloadPageLoop(int StartPage)
        {
            CurrectPageIndex = StartPage;
            Loger.Instance.LocalInfo($"循环下载模式启动");
            do
            {
                 DownloadWork();
                var time = new Random().Next(1000, 10000);
                for (var i = time; i > 0; i -= 1000)
                {
                    Loger.Instance.WaitTime(i / 1000);
                    await Task.Delay(1000);
                }

                Interlocked.Increment(ref CurrectPageIndex);
            } while (!CancelSign.IsCancellationRequested);
        }

        readonly WebClientEx.WebClientEx WebClientEX = new WebClientEx.WebClientEx();
        private  void DownloadWork()
        {
            if (!CancelSign.IsCancellationRequested&& !DownloadCollect.IsAddingCompleted)
            {
                try
                {
                    Stopwatch Time = new Stopwatch();
                    Loger.Instance.WithTimeStart($"下载网页page={CurrectPageIndex}", Time);
                    DownloadCollect.TryAdd(new Tuple<int, string>(CurrectPageIndex,
                        WebClientEX.DownloadStringTaskAsync(new Uri($"{Setting.Address}?p={CurrectPageIndex}"))
                            .Result));
                    Loger.Instance.WithTimeStop("下载网页完毕", Time);
                }
                catch (Exception ex)
                {
                    if (DownloadCollect.IsCompleted)
                    {
                        Loger.Instance.Error($"管道状态已经完成");
                        return;
                    }

                    if (ex.Message.IndexOf("Object", StringComparison.Ordinal)!=-1)
                    {
                        Loger.Instance.Error($"Object错误，开始重试");
                        Thread.Sleep(10000);
                        DownloadWork();
                    }

                    if (WebClient.ErrorInfo == "Timeout")
                    {
                        Loger.Instance.Error($"访问超时");
                    }
                    else if ((ex as WebException).Status == WebExceptionStatus.UnknownError)
                    {
                        Loger.Instance.Error($"推测下载完毕");
                        DownloadCollect.CompleteAdding();
                        CancelSign.Cancel();
                    }
                    else
                    {
                        Loger.Instance.Error($"发生错误，错误信息{ex}");
                        var T = new Task(() =>
                        {
                            var time = new Random().Next(10000, 100000);
                            for (var i = time; i > 0; i -= 1000)
                            {
                                Loger.Instance.WaitTime(i / 1000);
                                Thread.Sleep(1000);
                            }
                        });
                        T.Start();
                        T.ContinueWith(obj => { DownloadWork(); });
                    }
                }
            }
            else if (CancelSign.IsCancellationRequested)
            {
                Loger.Instance.Error("下载终止");
                DownloadCollect.CompleteAdding();
            }
        }
    }
}