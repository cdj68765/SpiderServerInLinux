using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace SpiderServerInLinux
{

    internal class DownloadHelp
    {
        internal  static BlockingCollection<Tuple<int,string>> DownloadCollect;
        readonly WebClientEx.WebClientEx WebClient = new WebClientEx.WebClientEx();
        internal int DownloadMaxPage = 10;
        int CurrectPageIndex = 0;
        internal DownloadHelp(int DownloadPage)
        {
            CurrectPageIndex = DownloadPage;
            DownloadCollect = new BlockingCollection<Tuple<int, string>> (boundedCapacity: 1);
            DownloadWork();
        }
        readonly Stopwatch Time = new Stopwatch();
        async void DownloadWork()
        {
            if (DownloadCollect.Count <= DownloadMaxPage && !Setting.CancelSign.IsCancellationRequested)
            {
                try
                {
                    Loger.Instance.WithTimeStart($"下载网页{CurrectPageIndex}", Time);
                    DownloadCollect.TryAdd(new Tuple<int, string>( CurrectPageIndex,await WebClient.DownloadStringTaskAsync(new Uri($"{Setting.Address}?p={CurrectPageIndex}"))));
                    Loger.Instance.WithTimeStop("下载网页完毕", Time);
                }
                catch (Exception ex)
                {

                    Loger.Instance.Error($"发生错误，错误信息{ex}");
                }
            }
            else if (Setting.CancelSign.IsCancellationRequested)
            {
                Loger.Instance.Error("下载终止");
                DownloadCollect.CompleteAdding();
            }
        }
    }
}
