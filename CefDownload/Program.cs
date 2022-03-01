using CefSharp;
using CefSharp.Handler;
using CefSharp.OffScreen;
using CefSharp.ResponseFilter;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CefDownload
{
    class Program
    {
        internal static readonly TaskCompletionSource<byte> ShutdownResetEvent = new TaskCompletionSource<byte>();

        private static async Task<int> Main(string[] args)
        {
            var settines = new CefSettings()
            {
                Locale = "zh-CN",
                AcceptLanguageList = "zh-CN",
                MultiThreadedMessageLoop = true,
                CachePath = Path.GetFullPath("Cache"),
                PersistSessionCookies = true
            };
            Cef.Initialize(settines);
            BrowserSettings browserSettings = new BrowserSettings()
            {
                FileAccessFromFileUrls = CefState.Enabled,
                UniversalAccessFromFileUrls = CefState.Enabled
            };

            var Browser = new ChromiumWebBrowser("https://www.baidu.com/index.php?tn=mswin_oem_dg")
            {
                RequestHandler = new _RequestHandler()
            };
            Browser.FrameLoadEnd += delegate
            {
                Console.WriteLine("Complete");
            };
            return await ShutdownResetEvent.Task.ConfigureAwait(false);

        }

        public class _RequestHandler : RequestHandler
        {
            internal Action<byte[]> jsonData;

            protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
            {
                return new ResourceHandler() { };
            }
        }
        internal class ResourceHandler : ResourceRequestHandler
        {
            private MemoryStream memoryStream = new MemoryStream();

            protected override IResponseFilter GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
            {
                return new StreamResponseFilter(memoryStream);
            }

            protected override void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
            {
                if (status == UrlRequestStatus.Success && receivedContentLength > 0)
                {
                    Console.WriteLine($"{request.Url}|{memoryStream.Length}");

                }
            }
        }
    }
}
