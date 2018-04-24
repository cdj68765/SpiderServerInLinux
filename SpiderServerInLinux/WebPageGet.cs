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

        public WebPageGet(string Path="")
        {
            if (Path != "")
            {
                Setting.setting.Address = Path;
              File.WriteAllText("5000.txt", WebClient.DownloadString(new Uri(Path)));
                return;
            }
            Task.Factory.StartNew(() =>
                {
                    WebClient.DownloadStringCompleted += (Sender, Object) =>
                    {
                        try
                        {
                            new HandlerHtml(Object.Result);
                        }
                        catch (Exception e)
                        {
                            if (WebClientEx.WebClientEx.ErrorInfo == "Timeout")
                            {

                            }
                        }
                   
                    };
                    WebClient.DownloadStringAsync(new Uri(Setting.setting.Address));

                }, CancelInfo.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }
    }
}