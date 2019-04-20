using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SpiderServerInLinux
{
    internal class DownNyaaLoop
    {
        private readonly DownloadHelp DoF = new DownloadHelp();

        internal void DownLoopAsync()
        {
            //DownNewDayAsync();
            Setting._GlobalSet.NyaaLastPageIndex = 12766;
            DownAsync();
            var PageHandler = new HandlerHtml();
            Task.Factory.StartNew(() =>
            {
                foreach (var Item in DoF.DownloadCollect.GetConsumingEnumerable())
                {
                    Setting._GlobalSet.NyaaLastPageIndex = Item.Item1;
                    DataBaseCommand.SavePage(Item.Item2);
                    if (PageHandler.HandlerToHtml(Item.Item2) != 75)
                    {
                        Loger.Instance.LocalInfo("当前获得条目小于75条,检查是否完成获取");
                        DoF.DownloadCollect.CompleteAdding();
                        DoF.CancelSign.Cancel();
                    }
                }
            }, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(() =>
            {
                foreach (var Item in PageHandler.DataCollect.GetConsumingEnumerable())
                {
                    var Status = PageInDateStatus(Item.Item1.Item1);
                    if (Status == -1)
                    {
                        DataBaseCommand.SaveToDataBaseRange(Item.Item2, Item.Item1.Item2, true);
                    }
                    else if (Status == 0)
                    {
                        DataBaseCommand.SaveToDataBaseOneByOne(Item.Item2, Item.Item1.Item2, true);
                    }
                    else
                    {
                        Loger.Instance.LocalInfo($"发现{Item.Item1.Item1}已经存在");
                        DataBaseCommand.SaveToDataBaseRange(Item.Item2, Item.Item1.Item2, true);
                        //return true;
                    }
                }

                int PageInDateStatus(string Date)
                {
                    var Status = DataBaseCommand.GetDateInfo(Date);
                    if (Status == null)
                    {
                        return -1;
                    }

                    if (Status.Status)
                    {
                        return 1;
                    }

                    return 0;
                }
            }, TaskCreationOptions.LongRunning);
        }

        private void DownNewDayAsync()
        {
            DownloadHelp NDoF = new DownloadHelp();
            var PageHandler = new HandlerHtml();
            var Timer = new System.Timers.Timer();
            Timer.AutoReset = true;
            Timer.Interval = 10000;
            Timer.Elapsed += delegate { };
            Task.Factory.StartNew(async () => { await NDoF.DownloadPageLoop(0); }, TaskCreationOptions.LongRunning);
            foreach (var Item in NDoF.DownloadCollect.GetConsumingEnumerable())
            {
                Setting._GlobalSet.NyaaLastPageIndex = Item.Item1;
                PageHandler.HandlerToHtml(Item.Item2);
            }
        }

        private async void DownAsync()
        {
            await DoF.DownloadPageLoop(Setting._GlobalSet.NyaaLastPageIndex);
        }

        internal class DownloadHelp
        {
            internal readonly BlockingCollection<Tuple<int, string>> DownloadCollect;

            internal int DownloadMaxPage = 10;
            internal readonly CancellationTokenSource CancelSign;

            //private readonly Stopwatch Time = new Stopwatch();
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
                    try
                    {
                        DownloadWork();
                        var time = new Random().Next(1000, 10000);
                        for (var i = time; i > 0; i -= 1000)
                        {
                            Loger.Instance.WaitTime(i / 1000);
                            await Task.Delay(1000);
                        }
                        Interlocked.Increment(ref CurrectPageIndex);
                    }
                    catch (Exception e)
                    {
                        var time = new Random().Next(10000, 100000);
                        for (var i = time; i > 0; i -= 1000)
                        {
                            Loger.Instance.WaitTime(i / 1000);
                            Thread.Sleep(1000);
                        }
                        Loger.Instance.Error(e);
                    }
                } while (!CancelSign.IsCancellationRequested);
            }

            private void DownloadWork()
            {
                if (!CancelSign.IsCancellationRequested && !DownloadCollect.IsAddingCompleted)
                {
                    try
                    {
                        Stopwatch Time = new Stopwatch();
                        WebClientEx.WebClientEx WebClientEX = new WebClientEx.WebClientEx();
                        Loger.Instance.WithTimeStart($"下载网页page={CurrectPageIndex}", Time);
                        DownloadCollect.TryAdd(new Tuple<int, string>(CurrectPageIndex,
                            WebClientEX.DownloadStringTaskAsync(new Uri($"{Setting._GlobalSet.NyaaAddress}?p={CurrectPageIndex}"))
                                .Result));
                        Loger.Instance.WithTimeStop("下载网页完毕", Time);
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (DownloadCollect.IsCompleted)
                        {
                            Loger.Instance.Error($"管道状态已经完成");
                            CancelSign.Cancel();
                        }

                        /*    if (ex.Message.IndexOf("Object", StringComparison.Ordinal) != -1)
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
                            }*/
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
}