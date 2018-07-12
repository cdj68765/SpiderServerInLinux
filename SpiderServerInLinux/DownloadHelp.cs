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
        private readonly Stopwatch Time = new Stopwatch();
        private readonly WebClientEx.WebClientEx WebClient = new WebClientEx.WebClientEx();
        private int CurrectPageIndex;

        internal DownloadHelp()
        {
            WebClient = new WebClientEx.WebClientEx();
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

        private async void  DownloadWork()
        {
            if (DownloadCollect.Count <= DownloadMaxPage && !CancelSign.IsCancellationRequested)
            {
                try
                {
                  //  Loger.Instance.WithTimeStart($"下载网页page={CurrectPageIndex}", Time);
                    DownloadCollect.TryAdd(new Tuple<int, string>(CurrectPageIndex,
                        await WebClient.DownloadStringTaskAsync(new Uri($"{Setting.Address}?p={CurrectPageIndex}"))));
                   // Loger.Instance.WithTimeStop("下载网页完毕", Time);
                }
                catch (Exception ex)
                {
                    if (WebClient.ErrorInfo == "Timeout")
                    {
                        Loger.Instance.Error($"访问超时");
                    }
                    else if ((ex as WebException).Status == WebExceptionStatus.UnknownError)
                    {
                        Loger.Instance.Error($"推测下载完毕");
                        CancelSign.Cancel();
                    }

                    Loger.Instance.Error($"发生错误，错误信息{ex}");

                    var T = new Task(() =>
                    {
                        var time = new Random().Next(10000, 60000);
                        for (var i = time; i > 0; i -= 1000)
                        {
                            Loger.Instance.WaitTime(i / 1000);
                            Thread.Sleep(1000);
                        }
                    });
                    T.Start();
                    await T.ContinueWith(obj => { DownloadWork(); });
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