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

        public WebPageGet()
        {
            Task.Factory.StartNew(() =>
                {
                    WebClient.DownloadStringCompleted += (Sender, Object) => { new HandlerHtml(Object.Result); };
                    WebClient.DownloadStringAsync(new Uri(Setting.setting.Address));

                }, CancelInfo.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }
    }
}