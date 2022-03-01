using HtmlAgilityPack;
using LiteDB;
using ShadowsocksR.Controller;
using SocksSharp.Proxy;
using SocksSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xNet;
using static SpiderServerInLinux.Image2Webp;
using System.Net.Http;
using ServiceWire.TcpIp;
using System.Net;
using WebpInter;

//copy /y "$(TargetPath)" "Z:\publish\"
// netsh winsock reset
//copy / y $(TargetDir)$(SolutionName).dll "Y:\net5\"
//copy / y $(TargetDir)$(SolutionName).pdb "Y:\net5\"
//copy / y $(TargetDir)$(SolutionName).dll "Y:\net5v5\"
//copy / y $(TargetDir)$(SolutionName).pdb "Y:\net5v5\"
namespace SpiderServerInLinux
{
    internal class ImgData2
    {
        public string id { get; set; }

        public string Date { get; set; }
        public string Hash { get; set; }

        public byte[] img { get; set; }
        public List<int> FromList { get; set; }
        public bool Status { get; set; }
        public string Type { get; set; }
        public string From { get; set; }
    }

    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            //while (!Debugger.IsAttached)
            //{
            //    await Task.Delay(1000);
            //    Console.WriteLine("Wait");
            //}

            //{
            //    using var db = new LiteDatabase($"Filename=/media/sda1/publish/T66y.db");
            //    var ddd = db.GetCollection<T66yData>("T66yData");
            //    ddd.EnsureIndex(x => x.Status);
            //    var list = new List<T66yData>();
            //    foreach (var item in ddd.Find(x => x.Status == false))
            //    {
            //        list.Add(item);
            //    }
            //}
            //Console.WriteLine("完成");
            //Console.ReadLine();
            /*  var Proxy = new ProxyClientX("192.168.2.162", 1088, ProxyType.Socks5);
              var Proxy2 = new ProxyClientHandler<Socks5>(new ProxySettings
              {
                  Host = "192.168.2.162",
                  Port = 1088
              });*/
            /* var serviceCollection = new ServiceCollection();

             serviceCollection
                 .AddHttpClient("ProxiedClient")
                 .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                 {
                     Proxy = Proxy,
                     AutomaticDecompression = DecompressionMethods.GZip
                 });

             var services = serviceCollection.BuildServiceProvider();
             var httpClientFactory = services.GetService<IHttpClientFactory>();
             var client = httpClientFactory.CreateClient("ProxiedClient");
             client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.143 Safari/537.36 MicroMessenger/7.0.9.501 NetType/WIFI MiniProgramEnv/Windows WindowsWechat");
             client.Timeout = new TimeSpan(0, 2, 0);
             string _Response = await client.GetStringAsync("https://sukebei.nyaa.si/");

             HttpRequestMessage message = new HttpRequestMessage();
             //message.Headers.Add("Accept", "application/json");
             //message.Content = new System.Net.Http.StringContent("{\"user\":\"11\"}", System.Text.Encoding.UTF8, "application/json");
             message.Method = System.Net.Http.HttpMethod.Post;
             message.RequestUri = new Uri(client.BaseAddress.ToString());
             HttpResponseMessage response = client.SendAsync(message).Result;
             var result = response.Content.ReadAsStringAsync().Result;*/
            /*  var builder = Host.CreateDefaultBuilder(args);
              builder.ConfigureServices(x =>
              {
                  x.AddHttpClient("ProxiedClient").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                  {
                      AutomaticDecompression = DecompressionMethods.All
                  });
              });

              using (var scope = builder.Build().Services.CreateScope())
              {
                  var httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("ProxiedClient");
                  // httpClient.BaseAddress = new Uri("apiUrl");
                  httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.143 Safari/537.36 MicroMessenger/7.0.9.501 NetType/WIFI MiniProgramEnv/Windows WindowsWechat");
                  //httpClient.DefaultRequestHeaders.Add("Referer", "https://www.baidu.com");
                  string _Response = await httpClient.GetStringAsync("https://sukebei.nyaa.si/");
                  //File.WriteAllText("t.html", _Response);
                  httpClient.Dispose();
                  httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("ProxiedClient");
                  httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.143 Safari/537.36 MicroMessenger/7.0.9.501 NetType/WIFI MiniProgramEnv/Windows WindowsWechat");

                  //_Response = await httpClient.GetStringAsync("https://www.141jav.com/new");
                  httpClient.Dispose();
                  httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("ProxiedClient");
                  httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.143 Safari/537.36 MicroMessenger/7.0.9.501 NetType/WIFI MiniProgramEnv/Windows WindowsWechat");

                  // var _Response = await httpClient.GetStringAsync("http://www.mmfhd.com/index.php");
                  httpClient.Dispose();
                  httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("ProxiedClient");
                  httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.143 Safari/537.36 MicroMessenger/7.0.9.501 NetType/WIFI MiniProgramEnv/Windows WindowsWechat");

                  _Response = await httpClient.GetStringAsync("http://www.t66y.com/thread0806.php?fid=25");
                  httpClient.Dispose();
                  httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("ProxiedClient");
                  httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.143 Safari/537.36 MicroMessenger/7.0.9.501 NetType/WIFI MiniProgramEnv/Windows WindowsWechat");

                  _Response = await httpClient.GetStringAsync("http://www.t66y.com/htm_data/2105/25/4486614.html");
                  httpClient.Dispose();
                  httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("ProxiedClient");
                  httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.143 Safari/537.36 MicroMessenger/7.0.9.501 NetType/WIFI MiniProgramEnv/Windows WindowsWechat");

                  _Response = await httpClient.GetStringAsync("http://104.194.212.8/forum/forum-25-1.html");
                  httpClient.Dispose();
                  httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("ProxiedClient");
                  httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.143 Safari/537.36 MicroMessenger/7.0.9.501 NetType/WIFI MiniProgramEnv/Windows WindowsWechat");
              }
              Console.ReadKey();

              {
                  HttpClientHandler Handler = new HttpClientHandler { Proxy = Proxy };

                  HttpClient Client = new HttpClient(Handler);

                  try
                  {
                      string Response = await Client.GetStringAsync("https://sukebei.nyaa.si/");

                      Console.WriteLine(Response);
                  }
                  catch (Exception ex)
                  {
                      Console.WriteLine(ex);
                  }
                  finally
                  {
                      Handler.Dispose();
                      Client.Dispose();
                  }
              }*/
            Setting._GlobalSet = GlobalSet.Open();
            if (args.Length != 0 && !string.IsNullOrEmpty(args[0]))
            {
                Setting._GlobalSet.ssr_url = args[0].ToString();
                Setting._GlobalSet.SocksCheck = true;
            }

            Setting._GlobalSet.MiMiFin = true;
            Init();
            void Init()
            {
                AppDomain.CurrentDomain.ProcessExit += delegate
                {
                    Setting.DownloadManage?.Dispose();
                    // Console.Clear(); Console.WriteLine("程序退出");
                };
                AppDomain.CurrentDomain.UnhandledException += delegate
                {
                    //Console.Clear();
                    // Console.WriteLine("程序异常");
                };
                TaskScheduler.UnobservedTaskException += (s, e) =>
                {
                    // Loger.Instance.ServerInfo("主机", $"线程异常{e.Exception.StackTrace}");
                };
                Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", true);
                if (!Setting.Platform)
                {
                    var D = new DirectoryInfo(@"./");
                    var FileD = D.GetFiles("*.html");
                    var count = 0;
                    foreach (var item in FileD)
                    {
                        try
                        {
                            item.Delete();
                            count += 1;
                            Console.WriteLine($"{count}|{FileD.Length}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                        /*    if (int.TryParse(item.Name.Replace(".html", ""), out int res))
                            {
                            }*/
                    }
                }
            }
            /*var SSR = new ShadowsocksRController("ssr://MTUzLjEwMS41Ny4zNTo1ODQ1NDphdXRoX2FlczEyOF9zaGExOmNoYWNoYTIwLWlldGY6cGxhaW46VFdsNmRXaHZNVEF4TUROSVN3Lz9vYmZzcGFyYW09WWpkbU9UQXhOemc1TG0xcFkzSnZjMjltZEM1amIyMCZwcm90b3BhcmFtPU1UYzRPVG95WVRGMllWayZyZW1hcmtzPVV5M2x1TGpsdDU3b2dhX3BnSm90NXBlbDVweXM1cDJ4NUxxc0lFTm9iMjl3WVEmZ3JvdXA9NDRHQzQ0S0U0NEtCJnVkcHBvcnQ9NzIwOTA2JnVvdD0xMTUwOTg1Ng");

            var request = new HttpRequest()
            {
                UserAgent = Http.ChromeUserAgent(),
                ConnectTimeout = 5000,
                KeepAliveTimeout = 10000,
                ReadWriteTimeout = 10000
            };

            request.Proxy = Socks5ProxyClient.Parse($"localhost:{SSR.SocksPort}");
            HttpResponse response = request.Get("https://bulink.xyz/api/subscribe/?token=nfcnx&sub_type=vmess");
            var RetS = response.ToString();*/
            /* var RetS = "dm1lc3M6Ly9leUoySWpvZ0lqSWlMQ0FpY0hNaU9pQWlYSFUyTm1ZMFhIVTJOV0l3WEhVMFpUaGxPakEwTFRFeElERTJPakF3SUMwZ1lua2dRblZNYVc1ckxuaDVlaUlzSUNKaFpHUWlPaUFpWEhVMFpqZG1YSFUzTlRJNFhIVTFNalJrWEhVNFltSXdYSFUxWmprM1hIVTJObVkwWEhVMk5XSXdYSFU0WW1FeVhIVTVOakExSWl3Z0luQnZjblFpT2lBaU1DSXNJQ0pwWkNJNklDSTJZVE5pWTJNd09DMDVZemMzTFRSak1ESXRPRFEwWWkwMFlUWTVOR00wWmpKbVpXRWlMQ0FpWVdsa0lqb2dJakFpTENBaWJtVjBJam9nSW5SamNDSXNJQ0owZVhCbElqb2dJbTV2Ym1VaUxDQWlhRzl6ZENJNklDSWlMQ0FpY0dGMGFDSTZJQ0lpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaU1qTXVNakkxTGpFMk5TNHlORElpTENBaWRpSTZJQ0l5SWl3Z0luQnpJam9nSW1kcGRHaDFZaTVqYjIwdlpuSmxaV1p4SUMwZ1hIVTNaamhsWEhVMU5tWmtYSFUxTW1Fd1hIVTFNakk1WEhVM09UaG1YSFUxWXpOalhIVTBaVGxoWEhVMVpHUmxYSFUyWkRGaVhIVTJOelE1WEhVM04yWTJYSFUxWlRBeVEyOXdaWEpoZEdsdmJpQkRiMnh2WTNScGIyNWNkVFkxTnpCY2RUWXpObVZjZFRSbE1tUmNkVFZtWXpNZ01TSXNJQ0p3YjNKMElqb2dORFF6TENBaWFXUWlPaUFpTXpSbU9HUmtNbVV0TlRWbU1DMDBaRGsxTFdKbVpHRXROekUwWmpVd1lqWmpNR1V4SWl3Z0ltRnBaQ0k2SUNJMk5DSXNJQ0p1WlhRaU9pQWlkM01pTENBaWRIbHdaU0k2SUNJaUxDQWlhRzl6ZENJNklDSWlMQ0FpY0dGMGFDSTZJQ0l2Y0dGMGFDOHhOakF6TVRjek5ESTFNRFlpTENBaWRHeHpJam9nSW5Sc2N5SjkKdm1lc3M6Ly9leUpoWkdRaU9pQWlNak11TWpJMExqTXhMakl3TWlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVGRtT0dWY2RUVTJabVJjZFRVeVlUQmNkVFV5TWpsY2RUYzVPR1pjZFRWak0yTmNkVFJsT1dGY2RUVmtaR1ZjZFRaa01XSmNkVFkzTkRsY2RUYzNaalpjZFRWbE1ESkRiM0JsY21GMGFXOXVJRU52Ykc5amRHbHZibHgxTmpVM01GeDFOak0yWlZ4MU5HVXlaRngxTldaak15QXlJaXdnSW5CdmNuUWlPaUEwTkRNc0lDSnBaQ0k2SUNJeU9HRTROR0kyT1MxbE1URTRMVFEzWVRBdE9UZzJPUzFtTXprM1ltTmpPV1ppWW1RaUxDQWlZV2xrSWpvZ0lqWTBJaXdnSW01bGRDSTZJQ0ozY3lJc0lDSjBlWEJsSWpvZ0lpSXNJQ0pvYjNOMElqb2dJaUlzSUNKd1lYUm9Jam9nSWk5d1lYUm9MekUyTURNeE56TTBNalV3TmlJc0lDSjBiSE1pT2lBaWRHeHpJbjA9CnZtZXNzOi8vZXlKaFpHUWlPaUFpTWpNdU1qSTFMakl4TXk0eU5ESWlMQ0FpZGlJNklDSXlJaXdnSW5Ceklqb2dJbWRwZEdoMVlpNWpiMjB2Wm5KbFpXWnhJQzBnWEhVM1pqaGxYSFUxTm1aa1hIVTFNbUV3WEhVMU1qSTVYSFUzT1RobVhIVTFZek5qWEhVMFpUbGhYSFUxWkdSbFhIVTJaREZpWEhVMk56UTVYSFUzTjJZMlhIVTFaVEF5UTJWeVlVNWxkSGR2Y210elhIVTJOVGN3WEhVMk16WmxYSFUwWlRKa1hIVTFabU16SURNaUxDQWljRzl5ZENJNklEUTBNeXdnSW1sa0lqb2dJakV4WXpjd00yRTRMV1l6WldJdE5HSXpZUzFpWXpsaUxUSTFNemxqTm1Gak5qYzVOaUlzSUNKaGFXUWlPaUFpTmpRaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5SNWNHVWlPaUFpSWl3Z0ltaHZjM1FpT2lBaUlpd2dJbkJoZEdnaU9pQWlMM0JoZEdndk1UWXdNekUzTXpReU5UQTJJaXdnSW5Sc2N5STZJQ0owYkhNaWZRPT0Kdm1lc3M6Ly9leUpoWkdRaU9pQWlNVEk0TGpFMExqRTFNeTQwTWlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVGRtT0dWY2RUVTJabVJjZFRVeVlUQmNkVFV5TWpsY2RUYzVPR1pjZFRWak0yTmNkVFJsT1dGY2RUVmtaR1ZjZFRaa01XSmNkVFkzTkRsY2RUYzNaalphWlc1c1lYbGxjbHgxTmpVM01GeDFOak0yWlZ4MU5HVXlaRngxTldaak15QTBJaXdnSW5CdmNuUWlPaUF4TXpjeU15d2dJbWxrSWpvZ0ltRTVNRFU1TjJNeExXSmhZak10TkRJeE55MWhaRFptTFRBNE16ZzJOelZqT0RZek15SXNJQ0poYVdRaU9pQWlNU0lzSUNKdVpYUWlPaUFpZDNNaUxDQWlkSGx3WlNJNklDSWlMQ0FpYUc5emRDSTZJQ0oxYzJFeUxXNXZaR1V1TXpNMk5uUmxjM1F1WTI5dElpd2dJbkJoZEdnaU9pQWlMM0poZVNJc0lDSjBiSE1pT2lBaWRHeHpJbjA9CnZtZXNzOi8vZXlKaFpHUWlPaUFpTWpNdU1qSTBMakUyTkM0eE1EQWlMQ0FpZGlJNklDSXlJaXdnSW5Ceklqb2dJbWRwZEdoMVlpNWpiMjB2Wm5KbFpXWnhJQzBnWEhVM1pqaGxYSFUxTm1aa1hIVTFNbUV3WEhVMU1qSTVYSFUzT1RobVhIVTFZek5qWEhVMFpUbGhYSFUxWkdSbFhIVTJaREZpWEhVMk56UTVYSFUzTjJZMlhIVTFaVEF5UTI5d1pYSmhkR2x2YmlCRGIyeHZZM1JwYjI1Y2RUWTFOekJjZFRZek5tVmNkVFJsTW1SY2RUVm1Zek1nTlNJc0lDSndiM0owSWpvZ05EUXpMQ0FpYVdRaU9pQWlaREkxWVRZMU9ETXROVEpqWVMwME9UWm1MVGczWVdVdFpqSXlOelZpTTJJd1pHUmtJaXdnSW1GcFpDSTZJQ0kyTkNJc0lDSnVaWFFpT2lBaWQzTWlMQ0FpZEhsd1pTSTZJQ0lpTENBaWFHOXpkQ0k2SUNJaUxDQWljR0YwYUNJNklDSXZjR0YwYUM4eE5qQXpNVGN6TkRJMU1EWWlMQ0FpZEd4eklqb2dJblJzY3lKOQp2bWVzczovL2V5SjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFpqWkRWY2RUVTJabVJQVmtnZ1UwRlRJRFlpTENBaVlXUmtJam9nSWpreExqRXpOQzR5TXpndU1UZzJJaXdnSW5CdmNuUWlPaUFpTkRReklpd2dJbWxrSWpvZ0ltRTVPREl4TmpCbExUQTJaR1F0TkRKallTMDVOakl4TFdJM01USTNaRE0zTmpaak5TSXNJQ0poYVdRaU9pQWlOalFpTENBaWJtVjBJam9nSW5keklpd2dJblI1Y0dVaU9pQWlibTl1WlNJc0lDSm9iM04wSWpvZ0ltRndjSE11YVhScExtZHZkaTVsWnlJc0lDSndZWFJvSWpvZ0lpOXpjMmhyYVhRaUxDQWlkR3h6SWpvZ0luUnNjeUlzSUNKemJta2lPaUFpSW4wPQp2bWVzczovL2V5SmhaR1FpT2lBaWFuQXdNaTVwYm1ScGFHOXRaUzUwYXlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFkxWlRWY2RUWTNNbU5jZFRVNU1qZGNkVGsyTW1GY2RUVmxPV05jZFRVNU1qZGNkVGsyTW1GTmFXTnliM052Wm5SY2RUWTFOekJjZFRZek5tVmNkVFJsTW1SY2RUVm1Zek1nTnlJc0lDSndiM0owSWpvZ01qQXdNakFzSUNKcFpDSTZJQ0kwWVRsalpEQmhOaTB4WmpZeUxUUTVOV010WWpZMVppMDBNV1V3TkRZNU9HSmtaV01pTENBaVlXbGtJam9nSWpFaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5SNWNHVWlPaUFpSWl3Z0ltaHZjM1FpT2lBaUlpd2dJbkJoZEdnaU9pQWlMeUlzSUNKMGJITWlPaUFpSW4wPQp2bWVzczovL2V5SmhaR1FpT2lBaWFHRnJkWEpsYVRFdWRITjFkSE4xTG5SdmNDSXNJQ0oySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRZMVpUVmNkVFkzTW1OY2RUUmxNV05jZFRSbFlXTkVhV2RwZEdGc0lGWk5JRGdpTENBaWNHOXlkQ0k2SURRME15d2dJbWxrSWpvZ0lqVmxNemcxWW1VNExXRTBOVEV0TkdReU1TMWhNbVZtTFdZeU5EQXhNR1ZqWlRZek55SXNJQ0poYVdRaU9pQWlNQ0lzSUNKdVpYUWlPaUFpZDNNaUxDQWlkSGx3WlNJNklDSWlMQ0FpYUc5emRDSTZJQ0pvWVd0MWNtVnBNUzUwYzNWMGMzVXVkRzl3SWl3Z0luQmhkR2dpT2lBaUwzUnpkWFJ6ZFNJc0lDSjBiSE1pT2lBaWRHeHpJbjA9CnZtZXNzOi8vZXlKMklqb2dJaklpTENBaWNITWlPaUFpWjJsMGFIVmlMbU52YlM5bWNtVmxabkVnTFNCY2RUZG1PR1ZjZFRVMlptUmNkVFV4T0RWY2RUVXpOR1ZjZFRobVltVmNkVFZrWkdWY2RUWXlZemxjZFRZMVlXWmNkVGRsWmpSY2RUVXlZVEJjZFRZMVlXWkNkWGxXVFNBNUlpd2dJbUZrWkNJNklDSjJNaTB3T0M1emMzSnpkV0l1YjI1bElpd2dJbkJ2Y25RaU9pQWlNVFV6SWl3Z0ltbGtJam9nSWpaaVlqazVNREV5TFRVeE5qa3ROREExWWkwNU5EUXpMVEExWkRnM056Tm1ZekV5TUNJc0lDSmhhV1FpT2lBaU5qUWlMQ0FpYm1WMElqb2dJbmR6SWl3Z0luUjVjR1VpT2lBaWJtOXVaU0lzSUNKb2IzTjBJam9nSW00ME5taHROVEkzTnpNdWJHRnZkMkZ1ZUdsaGJtY3VZMjl0SWl3Z0luQmhkR2dpT2lBaUlpd2dJblJzY3lJNklDSWlMQ0FpYzI1cElqb2dJaUo5CnZtZXNzOi8vZXlKaFpHUWlPaUFpU2xBd01TNHhMbFpPTkVVeExrNVBSRVV1TWpNemVYVnVMbWx1SWl3Z0ltRnBaQ0k2SURJc0lDSm9iM04wSWpvZ0lrcFFNREV1TVM1V1RqUkZNUzVPVDBSRkxqSXpNM2wxYmk1cGJpSXNJQ0pwWkNJNklDSXdNMkpqT0RaaFpDMDNPR1kyTFRNeU16Y3RZbUZsT0MxbE9HRmtNRE5rTkRJd056RWlMQ0FpYm1WMElqb2dJbmR6SWl3Z0luQmhkR2dpT2lBaUx6SXpNM2wxYmk5Mk1uSmhlU0lzSUNKd2IzSjBJam9nTWpNekxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFkxWlRWY2RUWTNNbU5jZFRSbE1XTmNkVFJsWVdOQmJXRjZiMjVjZFRZMU56QmNkVFl6Tm1WY2RUUmxNbVJjZFRWbVl6TWdNVEFpTENBaWRHeHpJam9nSW01dmJtVWlMQ0FpZEhsd1pTSTZJQ0p1YjI1bElpd2dJbllpT2lBeWZRPT0Kdm1lc3M6Ly9leUoySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRkbU9HVmNkVFUyWm1SRGJHOTFaRVpzWVhKbFhIVTFNVFpqWEhVMU0yWTRRMFJPWEhVNE1qZ3lYSFUzTUdJNUlERXhJaXdnSW1Ga1pDSTZJQ0ptY21WbExtMXBiR0ZuY205d1pYUnpMblJsWTJnaUxDQWljRzl5ZENJNklDSTRNQ0lzSUNKcFpDSTZJQ0ptWlRSaU1EbGtOaTA0WVdZMkxUTXpZVEl0T0RNME1DMWtNelV6T1dZNFl6aGhOR01pTENBaVlXbGtJam9nSWpBaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5SNWNHVWlPaUFpYm05dVpTSXNJQ0pvYjNOMElqb2dJaUlzSUNKd1lYUm9Jam9nSWk5eVlYa2lMQ0FpZEd4eklqb2dJaUlzSUNKemJta2lPaUFpSW4wPQp2bWVzczovL2V5SmhaR1FpT2lBaVNFc3dNUzR4TGxCTU9GVkZMazVQUkVVdU1qTXplWFZ1TG1sdUlpd2dJbUZwWkNJNklESXNJQ0pvYjNOMElqb2dJa2hMTURFdU1TNVFURGhWUlM1T1QwUkZMakl6TTNsMWJpNXBiaUlzSUNKcFpDSTZJQ0l3TTJKak9EWmhaQzAzT0dZMkxUTXlNemN0WW1GbE9DMWxPR0ZrTUROa05ESXdOekVpTENBaWJtVjBJam9nSW5keklpd2dJbkJoZEdnaU9pQWlMekl6TTNsMWJpOTJNbkpoZVNJc0lDSndiM0owSWpvZ01qTXpMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRrNU9UbGNkVFpsTW1aQmJXRjZiMjVjZFRZMU56QmNkVFl6Tm1WY2RUUmxNbVJjZFRWbVl6TWdNVElpTENBaWRHeHpJam9nSW01dmJtVWlMQ0FpZEhsd1pTSTZJQ0p1YjI1bElpd2dJbllpT2lBeWZRPT0Kdm1lc3M6Ly9leUoySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRVelpqQmNkVFpsTjJWY2RUYzNNREZjZFRZMVlqQmNkVFV6TVRkY2RUVmxNREpjZFRSbE1tUmNkVFV6TkdWY2RUYzFNelZjZFRSbVpURWdNVE1pTENBaVlXUmtJam9nSW5SM0xtWXdNUzV3WVc5d1lXOWpiRzkxWkM1amVXOTFJaXdnSW5CdmNuUWlPaUFpTXpNd05pSXNJQ0pwWkNJNklDSXhOalk1TUdGa015MWlNak5qTFROa05HUXRZalJoTUMwek56a3daVFU0WmpsaVpURWlMQ0FpWVdsa0lqb2dJaklpTENBaWJtVjBJam9nSW5SamNDSXNJQ0owZVhCbElqb2dJbTV2Ym1VaUxDQWlhRzl6ZENJNklDSWlMQ0FpY0dGMGFDSTZJQ0l2SWl3Z0luUnNjeUk2SUNJaWZRPT0Kc3M6Ly9ZMmhoWTJoaE1qQXRhV1YwWmkxd2IyeDVNVE13TlRwemVVTnBTbXd6Ym1JNFQwUUBzcy51cy5zc2htYXgubmV0OjU3NDc4I2dpdGh1Yi5jb20vZnJlZWZxJTIwLSUyMCVFNyVCRSU4RSVFNSU5QiVCRCVFNSVCQyU5NyVFNSU5MCU4OSVFNSVCMCVCQyVFNCVCQSU5QSVFNSVCNyU5RSVFNiU5NiU4NyVFNyU4OSVCOSVFNSVCMSVCMSVFNSU4NiU5QyVFNSU5QyVCQU9WSCVFNiU5NSVCMCVFNiU4RCVBRSVFNCVCOCVBRCVFNSVCRiU4MyUyMDE0CnNzOi8vWVdWekxUSTFOaTFuWTIwNlIzUXlPVlJTVDJReGJYQllAYmdwNC5nZW1nZW1zLm5ldDoxNzE5NCNnaXRodWIuY29tL2ZyZWVmcSUyMC0lMjAlRTUlQjklQkYlRTQlQjglOUMlRTclOUMlODElRTUlQjklQkYlRTUlQjclOUUlRTUlQjglODIlRTclQTclQkIlRTUlOEElQTglMjAxNQp0cm9qYW46Ly9zc3JzdWJAdDA4LnNzcnN1Yi5vbmU6NDQzI2dpdGh1Yi5jb20vZnJlZWZxJTIwLSUyMCVFNyVCRSU4RSVFNSU5QiVCRCVFNSU4NiU4NSVFNSU4RCU4RSVFOCVCRSVCRSVFNSVCNyU5RSVFNiU4QiU4OSVFNiU5NiVBRiVFNyVCQiVCNCVFNSU4QSVBMCVFNiU5NiVBRkJ1eVZNJTIwMTYKdm1lc3M6Ly9leUpoWkdRaU9pQWlkakl0TURrdWMzTnljM1ZpTG05dVpTSXNJQ0oySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRkbU9HVmNkVFUyWm1SY2RUZGxZbVJjZFRkbFlUWmNkVFZrWkdWY2RUZGxZbVJjZFRkbFlUWkNkWGxXVFNBeE55SXNJQ0p3YjNKMElqb2dNVFV6TENBaWFXUWlPaUFpTm1KaU9Ua3dNVEl0TlRFMk9TMDBNRFZpTFRrME5ETXRNRFZrT0RjM00yWmpNVEl3SWl3Z0ltRnBaQ0k2SUNJMk5DSXNJQ0p1WlhRaU9pQWlkM01pTENBaWRIbHdaU0k2SUNJaUxDQWlhRzl6ZENJNklDSnVORFpvYlRVeU56Y3pMbXhoYjNkaGJuaHBZVzVuTG1OdmJTSXNJQ0p3WVhSb0lqb2dJaThpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaVlXZ3RZM1V3TVM1b1lXOTVaUzVqWmlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFZpT0RsY2RUVm1ZbVJjZFRjM01ERmNkVGd3TlRSY2RUa3dNV0VnTVRnaUxDQWljRzl5ZENJNklEVXdNRE15TENBaWFXUWlPaUFpTkdFNVkyUXdZVFl0TVdZMk1pMDBPVFZqTFdJMk5XWXROREZsTURRMk9UaGlaR1ZqSWl3Z0ltRnBaQ0k2SUNJeUlpd2dJbTVsZENJNklDSjNjeUlzSUNKMGVYQmxJam9nSWlJc0lDSm9iM04wSWpvZ0lpSXNJQ0p3WVhSb0lqb2dJaThpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaVlYVjBieTVtY21WbGRqSXVkRzl3SWl3Z0ltWnBiR1VpT2lBaUlpd2dJbWxrSWpvZ0lqSTROamt5TTJNeUxUTXlZelF0TkRJNE5pMDVOR1JtTFdNeFlqWmxaR1EzTVRZNFpDSXNJQ0p1WlhRaU9pQWlkR053SWl3Z0ltaHZjM1FpT2lBaUlpd2dJbkJ2Y25RaU9pQWlNVFk0TXpjaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFkxWlRWY2RUWTNNbU5jZFRSbE1XTmNkVFJsWVdOTWFXNXZaR1ZjZFRZMU56QmNkVFl6Tm1WY2RUUmxNbVJjZFRWbVl6TWdNVGtpTENBaWRHeHpJam9nSWlJc0lDSjJJam9nTWl3Z0ltRnBaQ0k2SURFc0lDSjBlWEJsSWpvZ0ltNXZibVVpZlE9PQp2bWVzczovL2V5SmhaR1FpT2lBaVkzTXRZM1V3TVM1b1lXOTVaUzVqWmlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFpsTlRaY2RUVXpOVGRjZFRjM01ERmNkVGd3TlRSY2RUa3dNV0VnTWpBaUxDQWljRzl5ZENJNklESXdNREEzTENBaWFXUWlPaUFpTkdFNVkyUXdZVFl0TVdZMk1pMDBPVFZqTFdJMk5XWXROREZsTURRMk9UaGlaR1ZqSWl3Z0ltRnBaQ0k2SUNJeUlpd2dJbTVsZENJNklDSjNjeUlzSUNKMGVYQmxJam9nSWlJc0lDSm9iM04wSWpvZ0lpSXNJQ0p3WVhSb0lqb2dJaThpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaWFHNHRZMjB3TVM1b1lXOTVaUzVqWmlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFpqWWpOY2RUVXpOVGRjZFRjM01ERmNkVFprTVdKY2RUazJNek5jZFRWbE1ESmNkVGM1Wm1KY2RUVXlZVGdnTWpFaUxDQWljRzl5ZENJNklERXhPREEzTENBaWFXUWlPaUFpTkdFNVkyUXdZVFl0TVdZMk1pMDBPVFZqTFdJMk5XWXROREZsTURRMk9UaGlaR1ZqSWl3Z0ltRnBaQ0k2SUNJeUlpd2dJbTVsZENJNklDSjNjeUlzSUNKMGVYQmxJam9nSWlJc0lDSm9iM04wSWpvZ0lpSXNJQ0p3WVhSb0lqb2dJaThpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaWFuQXdNUzVwYm1ScGFHOXRaUzUwYXlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVGRtT0dWY2RUVTJabVJjZFRVeVlUQmNkVFV5TWpsY2RUYzVPR1pjZFRWak0yTmNkVFJsT1dGY2RUVmtaR1ZjZFRVM01qTmNkVFV4TkdKY2RUWXlZemxjZFRZeVl6bE5hV055YjNOdlpuUmNkVFV4Tm1OY2RUVXpaamdnTWpJaUxDQWljRzl5ZENJNklESXdNREl3TENBaWFXUWlPaUFpTkdFNVkyUXdZVFl0TVdZMk1pMDBPVFZqTFdJMk5XWXROREZsTURRMk9UaGlaR1ZqSWl3Z0ltRnBaQ0k2SUNJeElpd2dJbTVsZENJNklDSjNjeUlzSUNKMGVYQmxJam9nSWlJc0lDSm9iM04wSWpvZ0lpSXNJQ0p3WVhSb0lqb2dJaThpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaVVsVXdNeTR4TGpjd00wZzVMazVQUkVVdU1qTXplWFZ1TG1sdUlpd2dJbUZwWkNJNklESXNJQ0pvYjNOMElqb2dJbEpWTURNdU1TNDNNRE5JT1M1T1QwUkZMakl6TTNsMWJpNXBiaUlzSUNKcFpDSTZJQ0l3TTJKak9EWmhaQzAzT0dZMkxUTXlNemN0WW1GbE9DMWxPR0ZrTUROa05ESXdOekVpTENBaWJtVjBJam9nSW5keklpd2dJbkJoZEdnaU9pQWlMekl6TTNsMWJpOTJNbkpoZVNJc0lDSndiM0owSWpvZ01qTXpMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRSbVl6UmNkVGRtTlRkY2RUWTFZV1pjZFRVMU9EQmNkVFZqTnpGS2RYTjBTRzl6ZENBeU15SXNJQ0owYkhNaU9pQWlibTl1WlNJc0lDSjBlWEJsSWpvZ0ltNXZibVVpTENBaWRpSTZJREo5CnZtZXNzOi8vZXlKaFpHUWlPaUFpTVRnMUxqSTBNeTQxTnk0eE16SWlMQ0FpZGlJNklDSXlJaXdnSW5Ceklqb2dJbWRwZEdoMVlpNWpiMjB2Wm5KbFpXWnhJQzBnWEhVM1pqaGxYSFUxTm1aa1hIVTFNbUV3WEhVMU1qSTVYSFUzT1RobVhIVTFZek5qWEhVMFpUbGhYSFUxWkdSbFhIVTJaREZpWEhVMk56UTVYSFUzTjJZMlVITjVZMmg2WEhVMk5UY3dYSFUyTXpabFhIVTBaVEprWEhVMVptTXpJREkwSWl3Z0luQnZjblFpT2lBME5ETXNJQ0pwWkNJNklDSm1aR0k0Tm1OaVl5MWpOVE0zTFRSaE9USXRPVGN4Tnkwd1pEUmlZVEkyWVdJNVpXRWlMQ0FpWVdsa0lqb2dJaklpTENBaWJtVjBJam9nSW5keklpd2dJblI1Y0dVaU9pQWlJaXdnSW1odmMzUWlPaUFpWkdRdU1UazVNekF4TG5oNWVpSXNJQ0p3WVhSb0lqb2dJaTlrWkdVM1ptRTBMeUlzSUNKMGJITWlPaUFpZEd4ekluMD0Kdm1lc3M6Ly9leUpoWkdRaU9pQWlkakl0TURjdWMzTnljM1ZpTG05dVpTSXNJQ0oySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRSbVl6UmNkVGRtTlRkY2RUWTFZV1pjZFRZMVlqQmNkVGc1TjJaY2RUUm1NbVpjZFRVeU1qbGNkVFJsT1dGS2RYTjBTRzl6ZENBeU5TSXNJQ0p3YjNKMElqb2dNVFV6TENBaWFXUWlPaUFpTm1KaU9Ua3dNVEl0TlRFMk9TMDBNRFZpTFRrME5ETXRNRFZrT0RjM00yWmpNVEl3SWl3Z0ltRnBaQ0k2SUNJMk5DSXNJQ0p1WlhRaU9pQWlkM01pTENBaWRIbHdaU0k2SUNJaUxDQWlhRzl6ZENJNklDSnVORFpvYlRVeU56Y3pMbXhoYjNkaGJuaHBZVzVuTG1OdmJTSXNJQ0p3WVhSb0lqb2dJaThpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaVl5MTFjek11YjI5NFl5NWpZeUlzSUNKMklqb2dJaklpTENBaWNITWlPaUFpWjJsMGFIVmlMbU52YlM5bWNtVmxabkVnTFNCY2RUZG1PR1ZjZFRVMlptUkRiRzkxWkVac1lYSmxYSFU0TWpneVhIVTNNR0k1SURJMklpd2dJbkJ2Y25RaU9pQTBORE1zSUNKcFpDSTZJQ0prWWpWa01XRmhNeTA1TURoaUxUUTBaREV0WW1Vd1lTMDBaVFpoT0dRMFpUUmpaR0VpTENBaVlXbGtJam9nSWpZMElpd2dJbTVsZENJNklDSjNjeUlzSUNKMGVYQmxJam9nSWlJc0lDSm9iM04wSWpvZ0ltTXRkWE16TG05dmVHTXVZMk1pTENBaWNHRjBhQ0k2SUNJdmFtb2lMQ0FpZEd4eklqb2dJblJzY3lKOQp2bWVzczovL2V5SmhaR1FpT2lBaWFHc3dNUzVwYm1ScGFHOXRaUzUwYXlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVGs1T1RsY2RUWmxNbVpOYVdOeWIzTnZablJjZFRZMU56QmNkVFl6Tm1WY2RUUmxNbVJjZFRWbVl6TWdNamNpTENBaWNHOXlkQ0k2SURJd01ERXpMQ0FpYVdRaU9pQWlOR0U1WTJRd1lUWXRNV1kyTWkwME9UVmpMV0kyTldZdE5ERmxNRFEyT1RoaVpHVmpJaXdnSW1GcFpDSTZJQ0l4SWl3Z0ltNWxkQ0k2SUNKM2N5SXNJQ0owZVhCbElqb2dJaUlzSUNKb2IzTjBJam9nSWlJc0lDSndZWFJvSWpvZ0lpOGlMQ0FpZEd4eklqb2dJaUo5CnZtZXNzOi8vZXlKaFpHUWlPaUFpWm5WcmVXOHVjM05tWmpZMk5pNTNiM0pyWlhKekxtUmxkaUlzSUNKMklqb2dJaklpTENBaWNITWlPaUFpWjJsMGFIVmlMbU52YlM5bWNtVmxabkVnTFNCY2RUZG1PR1ZjZFRVMlptUkRiRzkxWkVac1lYSmxYSFUxTVRaalhIVTFNMlk0UTBST1hIVTRNamd5WEhVM01HSTVJREk0SWl3Z0luQnZjblFpT2lBME5ETXNJQ0pwWkNJNklDSmhaRGd3TmpRNE55MHlaREkyTFRRMk16WXRPVGhpTmkxaFlqZzFZMk00TlRJeFpqY2lMQ0FpWVdsa0lqb2dJalkwSWl3Z0ltNWxkQ0k2SUNKM2N5SXNJQ0owZVhCbElqb2dJaUlzSUNKb2IzTjBJam9nSWlJc0lDSndZWFJvSWpvZ0lpOGlMQ0FpZEd4eklqb2dJblJzY3lKOQp2bWVzczovL2V5SmhaR1FpT2lBaVVsVXdNUzR4TGpSVlJEWXdMazVQUkVVdU1qTXplWFZ1TG1sdUlpd2dJbllpT2lBaU1pSXNJQ0p3Y3lJNklDSm5hWFJvZFdJdVkyOXRMMlp5WldWbWNTQXRJRngxTkdaak5GeDFOMlkxTjF4MU5qVmhabHgxTmpWaU1GeDFPRGszWmx4MU5HWXlabHgxTlRJeU9WeDFOR1U1WVdwMWMzUm9iM04wSURJNUlpd2dJbkJ2Y25RaU9pQXlNek1zSUNKcFpDSTZJQ0l3TTJKak9EWmhaQzAzT0dZMkxUTXlNemN0WW1GbE9DMWxPR0ZrTUROa05ESXdOekVpTENBaVlXbGtJam9nSWpJaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5SNWNHVWlPaUFpSWl3Z0ltaHZjM1FpT2lBaVVsVXdNUzR4TGpSVlJEWXdMazVQUkVVdU1qTXplWFZ1TG1sdUlpd2dJbkJoZEdnaU9pQWlMekl6TTNsMWJpOTJNbkpoZVNJc0lDSjBiSE1pT2lBaUluMD0Kdm1lc3M6Ly9leUpoWkdRaU9pQWlVMGN3TVM0eExrdEZWMWhETGs1UFJFVXVNak16ZVhWdUxtbHVJaXdnSW1GcFpDSTZJRElzSUNKb2IzTjBJam9nSWxOSE1ERXVNUzVMUlZkWVF5NU9UMFJGTGpJek0zbDFiaTVwYmlJc0lDSnBaQ0k2SUNJd00ySmpPRFpoWkMwM09HWTJMVE15TXpjdFltRmxPQzFsT0dGa01ETmtOREl3TnpFaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5CaGRHZ2lPaUFpTHpJek0zbDFiaTkyTW5KaGVTSXNJQ0p3YjNKMElqb2dNak16TENBaWNITWlPaUFpWjJsMGFIVmlMbU52YlM5bWNtVmxabkVnTFNCY2RUWTFZakJjZFRVeVlUQmNkVFUzTmpGQmJXRjZiMjVjZFRZMU56QmNkVFl6Tm1WY2RUUmxNbVJjZFRWbVl6TWdNekFpTENBaWRHeHpJam9nSW01dmJtVWlMQ0FpZEhsd1pTSTZJQ0p1YjI1bElpd2dJbllpT2lBeWZRPT0Kdm1lc3M6Ly9leUpoWkdRaU9pQWlWVk13TVM0eExqVXdNMVJHTGs1UFJFVXVNak16ZVhWdUxtbHVJaXdnSW1GcFpDSTZJRElzSUNKb2IzTjBJam9nSWxWVE1ERXVNUzQxTUROVVJpNU9UMFJGTGpJek0zbDFiaTVwYmlJc0lDSnBaQ0k2SUNJd00ySmpPRFpoWkMwM09HWTJMVE15TXpjdFltRmxPQzFsT0dGa01ETmtOREl3TnpFaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5CaGRHZ2lPaUFpTHpJek0zbDFiaTkyTW5KaGVTSXNJQ0p3YjNKMElqb2dNak16TENBaWNITWlPaUFpWjJsMGFIVmlMbU52YlM5bWNtVmxabkVnTFNCY2RUZG1PR1ZjZFRVMlptUmNkVFV5WVRCY2RUVXlNamxjZFRjNU9HWmNkVFZqTTJOY2RUUmxPV0ZjZFRWa1pHVmNkVFprTVdKY2RUWTNORGxjZFRjM1pqWkJiV0Y2YjI1Y2RUWTFOekJjZFRZek5tVmNkVFJsTW1SY2RUVm1Zek1nTXpFaUxDQWlkR3h6SWpvZ0ltNXZibVVpTENBaWRIbHdaU0k2SUNKdWIyNWxJaXdnSW5ZaU9pQXlmUT09CnZtZXNzOi8vZXlKaFpHUWlPaUFpUzFJd01TNHhMazlKTUVsWUxrNVBSRVV1TWpNemVYVnVMbWx1SWl3Z0ltRnBaQ0k2SURJc0lDSm9iM04wSWpvZ0lrdFNNREV1TVM1UFNUQkpXQzVPVDBSRkxqSXpNM2wxYmk1cGJpSXNJQ0pwWkNJNklDSXdNMkpqT0RaaFpDMDNPR1kyTFRNeU16Y3RZbUZsT0MxbE9HRmtNRE5rTkRJd056RWlMQ0FpYm1WMElqb2dJbmR6SWl3Z0luQmhkR2dpT2lBaUx6SXpNM2wxYmk5Mk1uSmhlU0lzSUNKd2IzSjBJam9nTWpNekxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVGszWlRsY2RUVTJabVJjZFRrNU9UWmNkVFZqTVRSQmJXRjZiMjVjZFRZMU56QmNkVFl6Tm1WY2RUUmxNbVJjZFRWbVl6TWdNeklpTENBaWRHeHpJam9nSW01dmJtVWlMQ0FpZEhsd1pTSTZJQ0p1YjI1bElpd2dJbllpT2lBeWZRPT0Kdm1lc3M6Ly9leUpoWkdRaU9pQWlZV2d0WTNVd01TNW9ZVzk1WlM1alppSXNJQ0oySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRWaU9EbGNkVFZtWW1SY2RUYzNNREZjZFRnd05UUmNkVGt3TVdFZ016TWlMQ0FpY0c5eWRDSTZJRFV3TURRd0xDQWlhV1FpT2lBaU5HRTVZMlF3WVRZdE1XWTJNaTAwT1RWakxXSTJOV1l0TkRGbE1EUTJPVGhpWkdWaklpd2dJbUZwWkNJNklDSXlJaXdnSW01bGRDSTZJQ0ozY3lJc0lDSjBlWEJsSWpvZ0lpSXNJQ0pvYjNOMElqb2dJaUlzSUNKd1lYUm9Jam9nSWk4aUxDQWlkR3h6SWpvZ0lpSjkKdm1lc3M6Ly9leUpoWkdRaU9pQWlZMnd1Ykc5dlozTnZiUzU0ZVhvaUxDQWlkaUk2SUNJeUlpd2dJbkJ6SWpvZ0ltZHBkR2gxWWk1amIyMHZabkpsWldaeElDMGdYSFUzWmpobFhIVTFObVprWEhVMU1tRXdYSFUxTWpJNVhIVTNPVGhtWEhVMVl6TmpYSFUwWlRsaFhIVTFaR1JsWEhVMlpERmlYSFUyTnpRNVhIVTNOMlkyVFZWTVZFRkRUMDFjZFRZMU56QmNkVFl6Tm1WY2RUUmxNbVJjZFRWbVl6TWdNelFpTENBaWNHOXlkQ0k2SURRME15d2dJbWxrSWpvZ0ltRXhPRFkzTW1NMUxUQTFOVEl0TkdObVlTMDRNek5oTFRSaFl6RmhOak01T1daaFlpSXNJQ0poYVdRaU9pQWlOQ0lzSUNKdVpYUWlPaUFpZDNNaUxDQWlkSGx3WlNJNklDSWlMQ0FpYUc5emRDSTZJQ0lpTENBaWNHRjBhQ0k2SUNJdmRpSXNJQ0owYkhNaU9pQWlkR3h6SW4wPQp0cm9qYW46Ly9mYXN0c3NoLmNvbUBzZzEudmxlc3MuY286NDQzI2dpdGh1Yi5jb20vZnJlZWZxJTIwLSUyMCVFNiU5NiVCMCVFNSU4QSVBMCVFNSU5RCVBMU5ld01lZGlhJUU2JTk1JUIwJUU2JThEJUFFJUU0JUI4JUFEJUU1JUJGJTgzJTIwMzUKdm1lc3M6Ly9leUpoWkdRaU9pQWlVbFV3TWk0eExrSlZSRTVCTGs1UFJFVXVNak16ZVhWdUxtbHVJaXdnSW1GcFpDSTZJRElzSUNKb2IzTjBJam9nSWxKVk1ESXVNUzVDVlVST1FTNU9UMFJGTGpJek0zbDFiaTVwYmlJc0lDSnBaQ0k2SUNJd00ySmpPRFpoWkMwM09HWTJMVE15TXpjdFltRmxPQzFsT0dGa01ETmtOREl3TnpFaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5CaGRHZ2lPaUFpTHpJek0zbDFiaTkyTW5KaGVTSXNJQ0p3YjNKMElqb2dNak16TENBaWNITWlPaUFpWjJsMGFIVmlMbU52YlM5bWNtVmxabkVnTFNCY2RUUm1ZelJjZFRkbU5UZGNkVFkxWVdaY2RUZ3pZV0pjZFRZMVlXWmNkVGM1WkRGS2RYTjBTRzl6ZENBek5pSXNJQ0owYkhNaU9pQWlibTl1WlNJc0lDSjBlWEJsSWpvZ0ltNXZibVVpTENBaWRpSTZJREo5CnZtZXNzOi8vZXlKaFpHUWlPaUFpZEhkd2NtODJNREkxTG1GNmVtbGpieTV6Y0dGalpTSXNJQ0oySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRWbE4yWmNkVFJsTVdOY2RUYzNNREZjZFRWbE4yWmNkVFZrWkdWY2RUVmxNREpjZFRjNVptSmNkVFV5WVRnZ016Y2lMQ0FpY0c5eWRDSTZJREV4TlRVMExDQWlhV1FpT2lBaU9HSmtaREk1TWpVdE56SXhPQzB6TVRSaUxUazROMkV0WkdOaVlqUTBaREE1T0RVeUlpd2dJbUZwWkNJNklDSXlJaXdnSW01bGRDSTZJQ0ozY3lJc0lDSjBlWEJsSWpvZ0lpSXNJQ0pvYjNOMElqb2dJblIzY0hKdk5qQXlOQzVoZW5wcFkyOHVjSGNpTENBaWNHRjBhQ0k2SUNJdmRtbGtaVzhpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaWRqSXRNRFl1YzNOeWMzVmlMbTl1WlNJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFJtWXpSY2RUZG1OVGRjZFRZMVlXWmNkVFkxWWpCY2RUZzVOMlpjZFRSbU1tWmNkVFV5TWpsY2RUUmxPV0ZxZFhOMGFHOXpkQ0F6T0NJc0lDSndiM0owSWpvZ01UVXpMQ0FpYVdRaU9pQWlObUppT1Rrd01USXROVEUyT1MwME1EVmlMVGswTkRNdE1EVmtPRGMzTTJaak1USXdJaXdnSW1GcFpDSTZJQ0kyTkNJc0lDSnVaWFFpT2lBaWQzTWlMQ0FpZEhsd1pTSTZJQ0lpTENBaWFHOXpkQ0k2SUNKdU5EWm9iVFV5TnpjekxteGhiM2RoYm5ocFlXNW5MbU52YlNJc0lDSndZWFJvSWpvZ0lpOGlMQ0FpZEd4eklqb2dJaUo5CnZtZXNzOi8vZXlKaFpHUWlPaUFpZW1odmJtZDZhSFZoYm1kaGVtaHJMbmhwWVc5aVlXbDVkVzR1YldVaUxDQWlkaUk2SUNJeUlpd2dJbkJ6SWpvZ0ltZHBkR2gxWWk1amIyMHZabkpsWldaeElDMGdYSFUzWmpobFhIVTFObVprWEhVMU1tRXdYSFUxTWpJNVhIVTNPVGhtWEhVMVl6TmpYSFUwWlRsaFhIVTFaR1JsWEhVMU56SXpYSFUxTVRSaVhIVTJNbU01WEhVMk1tTTVUV2xqY205emIyWjBYSFUxTVRaalhIVTFNMlk0SURNNUlpd2dJbkJ2Y25RaU9pQTRNalFzSUNKcFpDSTZJQ0l4TkRJMVpEbGlaUzFoWVRrM0xUTmlOek10WVRaak1TMWhNR05qWldVMFkyVmhNak1pTENBaVlXbGtJam9nSWpJaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5SNWNHVWlPaUFpSWl3Z0ltaHZjM1FpT2lBaVpXeHplSGhpYkhrdWVHbGhiMkpoYVhsMWJpNXRaU0lzSUNKd1lYUm9Jam9nSWk5b2JITWlMQ0FpZEd4eklqb2dJaUo5CnZtZXNzOi8vZXlKaFpHUWlPaUFpUTBFd01TNHhMbEk1UVU5TExrNVBSRVV1TWpNemVYVnVMbWx1SWl3Z0ltRnBaQ0k2SURJc0lDSm9iM04wSWpvZ0lrTkJNREV1TVM1U09VRlBTeTVPVDBSRkxqSXpNM2wxYmk1cGJpSXNJQ0pwWkNJNklDSXdNMkpqT0RaaFpDMDNPR1kyTFRNeU16Y3RZbUZsT0MxbE9HRmtNRE5rTkRJd056RWlMQ0FpYm1WMElqb2dJbmR6SWl3Z0luQmhkR2dpT2lBaUx6SXpNM2wxYmk5Mk1uSmhlU0lzSUNKd2IzSjBJam9nTWpNekxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFV5WVRCY2RUWXlabVpjZFRVNU1qZGNkVGxpTkRGY2RUVXpNVGRjZFRVeE5HSmNkVGMzTURGY2RUZzBPVGxjZFRjeU56bGNkVFV5TWpsY2RUVmpNVFJCYldGNmIyNWNkVFkxTnpCY2RUWXpObVZjZFRSbE1tUmNkVFZtWXpNZ05EQWlMQ0FpZEd4eklqb2dJbTV2Ym1VaUxDQWlkSGx3WlNJNklDSnViMjVsSWl3Z0luWWlPaUF5ZlE9PQp2bWVzczovL2V5SmhaR1FpT2lBaVpHUXVNVGs1TXpBeExuaDVlaUlzSUNKMklqb2dJaklpTENBaWNITWlPaUFpWjJsMGFIVmlMbU52YlM5bWNtVmxabkVnTFNCY2RUZG1PR1ZjZFRVMlptUmNkVFV5WVRCY2RUVXlNamxjZFRjNU9HWmNkVFZqTTJOY2RUUmxPV0ZjZFRWa1pHVmNkVFprTVdKY2RUWTNORGxjZFRjM1pqWlFjM2xqYUhwY2RUWTFOekJjZFRZek5tVmNkVFJsTW1SY2RUVm1Zek1nTkRFaUxDQWljRzl5ZENJNklEUTBNeXdnSW1sa0lqb2dJbVprWWpnMlkySmpMV00xTXpjdE5HRTVNaTA1TnpFM0xUQmtOR0poTWpaaFlqbGxZU0lzSUNKaGFXUWlPaUFpTWlJc0lDSnVaWFFpT2lBaWQzTWlMQ0FpZEhsd1pTSTZJQ0lpTENBaWFHOXpkQ0k2SUNKa1pDNHhPVGt6TURFdWVIbDZJaXdnSW5CaGRHZ2lPaUFpTDJSa1pUZG1ZVFF2SWl3Z0luUnNjeUk2SUNKMGJITWlmUT09CnZtZXNzOi8vZXlKaFpHUWlPaUFpTVRBMExqRTJMakU0TWk0eE5TSXNJQ0oySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRkbU9HVmNkVFUyWm1SRGJHOTFaRVpzWVhKbFhIVTFNVFpqWEhVMU0yWTRRMFJPWEhVNE1qZ3lYSFUzTUdJNUlEUXlJaXdnSW5CdmNuUWlPaUEwTkRNc0lDSnBaQ0k2SUNKa1lqVmtNV0ZoTXkwNU1EaGlMVFEwWkRFdFltVXdZUzAwWlRaaE9HUTBaVFJqWkdFaUxDQWlZV2xrSWpvZ0lqWTBJaXdnSW01bGRDSTZJQ0ozY3lJc0lDSjBlWEJsSWpvZ0lpSXNJQ0pvYjNOMElqb2dJbU10Y25VekxtOXZlR011WTJNaUxDQWljR0YwYUNJNklDSXZhbW9pTENBaWRHeHpJam9nSW5Sc2N5SjkKdHJvamFuOi8vc3Nyc3ViQHQxMS5zc3JzdWIub25lOjQ0MyNnaXRodWIuY29tL2ZyZWVmcSUyMC0lMjAlRTQlQkYlODQlRTclQkQlOTclRTYlOTYlQUYlMjAlMjA0Mwp0cm9qYW46Ly9zc3JzdWJAdDA5LnNzcnN1Yi5vbmU6NDQzI2dpdGh1Yi5jb20vZnJlZWZxJTIwLSUyMCVFNyVCRSU4RSVFNSU5QiVCRCVFNyVCQSVCRCVFNyVCQSVBNiVFNSVCNyU5RSVFNyVCQSVCRCVFNyVCQSVBNkJ1eVZNJTIwNDQKdm1lc3M6Ly9leUpoWkdRaU9pQWlkM2QzTG1ScFoybDBZV3h2WTJWaGJpNWpiMjBpTENBaWRpSTZJQ0l5SWl3Z0luQnpJam9nSW1kcGRHaDFZaTVqYjIwdlpuSmxaV1p4SUMwZ1hIVTNaamhsWEhVMU5tWmtRMnh2ZFdSR2JHRnlaVngxTlRFMlkxeDFOVE5tT0VORVRseDFPREk0TWx4MU56QmlPU0EwTlNJc0lDSndiM0owSWpvZ05EUXpMQ0FpYVdRaU9pQWlaR0kxWkRGaFlUTXRPVEE0WWkwME5HUXhMV0psTUdFdE5HVTJZVGhrTkdVMFkyUmhJaXdnSW1GcFpDSTZJQ0kyTkNJc0lDSnVaWFFpT2lBaWQzTWlMQ0FpZEhsd1pTSTZJQ0lpTENBaWFHOXpkQ0k2SUNKakxYSjFNeTV2YjNoakxtTmpJaXdnSW5CaGRHZ2lPaUFpTDJwcUlpd2dJblJzY3lJNklDSjBiSE1pZlE9PQp2bWVzczovL2V5SmhaR1FpT2lBaWVtaHZibWQ2YUhWaGJtZGhlbWhyTG5ocFlXOWlZV2w1ZFc0dWJXVWlMQ0FpZGlJNklDSXlJaXdnSW5Ceklqb2dJbWRwZEdoMVlpNWpiMjB2Wm5KbFpXWnhJQzBnWEhVM1pqaGxYSFUxTm1aa1hIVTFNbUV3WEhVMU1qSTVYSFUzT1RobVhIVTFZek5qWEhVMFpUbGhYSFUxWkdSbFhIVTFOekl6WEhVMU1UUmlYSFUyTW1NNVhIVTJNbU01VFdsamNtOXpiMlowWEhVMU1UWmpYSFUxTTJZNElEUTJJaXdnSW5CdmNuUWlPaUE0TWpRc0lDSnBaQ0k2SUNJelpqVTNaalU0TVMwNE1XRXpMVE0wWlRBdFlqZ3hZaTFsTkRZd01qTXdOall5WVRZaUxDQWlZV2xrSWpvZ0lqSWlMQ0FpYm1WMElqb2dJbmR6SWl3Z0luUjVjR1VpT2lBaUlpd2dJbWh2YzNRaU9pQWlaV3h6ZUhoaWJIa3VlR2xoYjJKaGFYbDFiaTV0WlNJc0lDSndZWFJvSWpvZ0lpOW9iSE1pTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaVkyUnVaR1V1YVhKMFpYbDZMblJ2WkdGNUlpd2dJbllpT2lBaU1pSXNJQ0p3Y3lJNklDSm5hWFJvZFdJdVkyOXRMMlp5WldWbWNTQXRJRngxTjJZNFpWeDFOVFptWkVOc2IzVmtSbXhoY21WY2RUVXhObU5jZFRVelpqaERSRTVjZFRneU9ESmNkVGN3WWprZ05EY2lMQ0FpY0c5eWRDSTZJRFEwTXl3Z0ltbGtJam9nSWpOaU5XVXlOVGhsTFRoak5XVXRORFZrTXkxaU4yUXlMVEF5WXpobU5XWmpNR0ppTWlJc0lDSmhhV1FpT2lBaU5qUWlMQ0FpYm1WMElqb2dJbmR6SWl3Z0luUjVjR1VpT2lBaUlpd2dJbWh2YzNRaU9pQWlZMlJ1WkdVdWFYSjBaWGw2TG5SdlpHRjVJaXdnSW5CaGRHZ2lPaUFpTHlJc0lDSjBiSE1pT2lBaWRHeHpJbjA9CnZtZXNzOi8vZXlKaFpHUWlPaUFpUjBJd01TNHhMbGxZVUVWYUxrNVBSRVV1TWpNemVYVnVMbWx1SWl3Z0ltRnBaQ0k2SURJc0lDSm9iM04wSWpvZ0lrZENNREV1TVM1WldGQkZXaTVPVDBSRkxqSXpNM2wxYmk1cGJpSXNJQ0pwWkNJNklDSXdNMkpqT0RaaFpDMDNPR1kyTFRNeU16Y3RZbUZsT0MxbE9HRmtNRE5rTkRJd056RWlMQ0FpYm1WMElqb2dJbmR6SWl3Z0luQmhkR2dpT2lBaUx6SXpNM2wxYmk5Mk1uSmhlU0lzSUNKd2IzSjBJam9nTWpNekxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVGRtT0dWY2RUVTJabVJjZFRsbFltSmNkVGMzTURGY2RUYzBNRFpjZFRWa1pUVmNkVFZpTmpaY2RUazJOaklnTkRnaUxDQWlkR3h6SWpvZ0ltNXZibVVpTENBaWRIbHdaU0k2SUNKdWIyNWxJaXdnSW5ZaU9pQXlmUT09CnZtZXNzOi8vZXlKaFpHUWlPaUFpWXkxeWRUTXViMjk0WXk1all5SXNJQ0oySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRkbU9HVmNkVFUyWm1SRGJHOTFaRVpzWVhKbFhIVTFNVFpqWEhVMU0yWTRRMFJPWEhVNE1qZ3lYSFUzTUdJNUlEUTVJaXdnSW5CdmNuUWlPaUEwTkRNc0lDSnBaQ0k2SUNKa1lqVmtNV0ZoTXkwNU1EaGlMVFEwWkRFdFltVXdZUzAwWlRaaE9HUTBaVFJqWkdFaUxDQWlZV2xrSWpvZ0lqWTBJaXdnSW01bGRDSTZJQ0ozY3lJc0lDSjBlWEJsSWpvZ0lpSXNJQ0pvYjNOMElqb2dJbU10Y25VekxtOXZlR011WTJNaUxDQWljR0YwYUNJNklDSXZhbW9pTENBaWRHeHpJam9nSW5Sc2N5SjkKCnZtZXNzOi8vZXlKd2IzSjBJam9nTUN3Z0ltRnBaQ0k2SURBc0lDSnBaQ0k2SUNJMllUTmlZMk13T0MwNVl6YzNMVFJqTURJdE9EUTBZaTAwWVRZNU5HTTBaakptWldFaUxDQWljSE1pT2lBaUxTQmNkVFJsWlRWY2RUUmxNR0pjZFRneU9ESmNkVGN3WWpsY2RUUmxNMkZDZFV4cGJtdGNkVGd4WldGY2RUVmxabUVnWEhVNU5qVXdYSFUyTnpBNFhIVTJaRFF4WEhVNU1XTm1OVWNnWEhVMU1UUmtYSFU0WkRNNVhIVTRaRFEwWEhVMlpUa3dYSFU0WW1ZM1hIVTFOREE0WEhVM05EQTJYSFUwWmpkbVhIVTNOVEk0SUMwaUxDQWlZV1JrSWpvZ0lseDFOR1ZsTlZ4MU5HVXdZbHgxTlRFMFpGeDFPR1F6T1Z4MU9ESTRNbHgxTnpCaU9WeDFPR0poTVZ4MU5tUTBNVngxT1RGalppSXNJQ0owYkhNaU9pQWlibTl1WlNJc0lDSjJJam9nSWpJaUxDQWlibVYwSWpvZ0luUmpjQ0lzSUNKb2IzTjBJam9nSWlJc0lDSndZWFJvSWpvZ0lpSXNJQ0owZVhCbElqb2dJbTV2Ym1VaWZRPT0Kdm1lc3M6Ly9leUp3YjNKMElqb2dORFF6TENBaVlXbGtJam9nTUN3Z0ltbGtJam9nSW1KbE1qUmxPR1EyTFdGbE1tUXRORGMzT1MwNVlUa3pMVEl5T0RjMFpXVm1ZalE0TnlJc0lDSndjeUk2SUNKaWRXeHBibXNnWEhVM1pqVXhYSFUxTTJOaVhIVTFNakEyWEhVMFpXRmlYSFUzWldKbVhIVTRaR1ZtSURFd0lGeDFOemxtWWx4MU5USmhPQ0lzSUNKaFpHUWlPaUFpYm1veUxtSjFiR2x1YXk1NGVYb3VabTlpZW5NdVkyOXRJaXdnSW5Sc2N5STZJQ0p1YjI1bElpd2dJbllpT2lBaU1pSXNJQ0p1WlhRaU9pQWlkR053SWl3Z0ltaHZjM1FpT2lBaUlpd2dJbkJoZEdnaU9pQWlJaXdnSW5SNWNHVWlPaUFpYm05dVpTSjkKdm1lc3M6Ly9leUp3YjNKMElqb2dORFF6TENBaVlXbGtJam9nTUN3Z0ltbGtJam9nSW1KbE1qUmxPR1EyTFdGbE1tUXRORGMzT1MwNVlUa3pMVEl5T0RjMFpXVm1ZalE0TnlJc0lDSndjeUk2SUNKaWRXeHBibXNnWEhVM1pqVXhYSFUxTTJOaVhIVTFNakEyWEhVMFpXRmlYSFUzWldKbVhIVTRaR1ZtSURFeElGeDFOR1V3T1Z4MU4yWTFNU0lzSUNKaFpHUWlPaUFpYkdFeU1TNWlkV3hwYm1zdWVIbDZMbVp2WW5wekxtTnZiU0lzSUNKMGJITWlPaUFpYm05dVpTSXNJQ0oySWpvZ0lqSWlMQ0FpYm1WMElqb2dJblJqY0NJc0lDSm9iM04wSWpvZ0lpSXNJQ0p3WVhSb0lqb2dJaUlzSUNKMGVYQmxJam9nSW01dmJtVWlmUT09CnZtZXNzOi8vZXlKd2IzSjBJam9nTkRRekxDQWlZV2xrSWpvZ01Dd2dJbWxrSWpvZ0ltSmxNalJsT0dRMkxXRmxNbVF0TkRjM09TMDVZVGt6TFRJeU9EYzBaV1ZtWWpRNE55SXNJQ0p3Y3lJNklDSmlkV3hwYm1zZ1hIVTNaalV4WEhVMU0yTmlYSFUxTWpBMlhIVTBaV0ZpWEhVM1pXSm1YSFU0WkdWbUlERXlJRngxTkdVd09WeDFOMlkxTVNBd0xqa2lMQ0FpWVdSa0lqb2dJbXhoTWprdVluVnNhVzVyTG5oNWVpNW1iMko2Y3k1amIyMGlMQ0FpZEd4eklqb2dJbTV2Ym1VaUxDQWlkaUk2SUNJeUlpd2dJbTVsZENJNklDSjBZM0FpTENBaWFHOXpkQ0k2SUNJaUxDQWljR0YwYUNJNklDSWlMQ0FpZEhsd1pTSTZJQ0p1YjI1bEluMD0Kdm1lc3M6Ly9leUp3YjNKMElqb2dORFF6TENBaVlXbGtJam9nTUN3Z0ltbGtJam9nSW1KbE1qUmxPR1EyTFdGbE1tUXRORGMzT1MwNVlUa3pMVEl5T0RjMFpXVm1ZalE0TnlJc0lDSndjeUk2SUNKaWRXeHBibXNnWEhVM1pqVXhYSFUxTTJOaVhIVTFNakEyWEhVMFpXRmlYSFUzWldKbVhIVTRaR1ZtSURJZ1hIVTBaVEE1WEhVM1pqVXhJREF1TlZ4MU5UQXdaQ0lzSUNKaFpHUWlPaUFpYkdFeUxtSjFiR2x1YXk1NGVYb3VabTlpZW5NdVkyOXRJaXdnSW5Sc2N5STZJQ0p1YjI1bElpd2dJbllpT2lBaU1pSXNJQ0p1WlhRaU9pQWlkR053SWl3Z0ltaHZjM1FpT2lBaUlpd2dJbkJoZEdnaU9pQWlJaXdnSW5SNWNHVWlPaUFpYm05dVpTSjkKdm1lc3M6Ly9leUp3YjNKMElqb2dORFF6TENBaVlXbGtJam9nTUN3Z0ltbGtJam9nSW1KbE1qUmxPR1EyTFdGbE1tUXRORGMzT1MwNVlUa3pMVEl5T0RjMFpXVm1ZalE0TnlJc0lDSndjeUk2SUNKaWRXeHBibXNnWEhVM1pqVXhYSFUxTTJOaVhIVTFNakEyWEhVMFpXRmlYSFUzWldKbVhIVTRaR1ZtSURVZ1hIVTBaVEE1WEhVM1pqVXhJaXdnSW1Ga1pDSTZJQ0pzWVRRdVluVnNhVzVyTG5oNWVpNW1iMko2Y3k1amIyMGlMQ0FpZEd4eklqb2dJbTV2Ym1VaUxDQWlkaUk2SUNJeUlpd2dJbTVsZENJNklDSjBZM0FpTENBaWFHOXpkQ0k2SUNJaUxDQWljR0YwYUNJNklDSWlMQ0FpZEhsd1pTSTZJQ0p1YjI1bEluMD0Kdm1lc3M6Ly9leUp3YjNKMElqb2dORFF6TENBaVlXbGtJam9nTUN3Z0ltbGtJam9nSW1KbE1qUmxPR1EyTFdGbE1tUXRORGMzT1MwNVlUa3pMVEl5T0RjMFpXVm1ZalE0TnlJc0lDSndjeUk2SUNKaWRXeHBibXNnWEhVM1pqVXhYSFUxTTJOaVhIVTFNakEyWEhVMFpXRmlYSFUzWldKbVhIVTRaR1ZtSURZZ1hIVTNPV1ppWEhVMU1tRTRJaXdnSW1Ga1pDSTZJQ0p1YWpFdVluVnNhVzVyTG5oNWVpNW1iMko2Y3k1amIyMGlMQ0FpZEd4eklqb2dJbTV2Ym1VaUxDQWlkaUk2SUNJeUlpd2dJbTVsZENJNklDSjBZM0FpTENBaWFHOXpkQ0k2SUNJaUxDQWljR0YwYUNJNklDSWlMQ0FpZEhsd1pTSTZJQ0p1YjI1bEluMD0Kdm1lc3M6Ly9leUp3YjNKMElqb2dORFF6TENBaVlXbGtJam9nTUN3Z0ltbGtJam9nSW1KbE1qUmxPR1EyTFdGbE1tUXRORGMzT1MwNVlUa3pMVEl5T0RjMFpXVm1ZalE0TnlJc0lDSndjeUk2SUNKaWRXeHBibXNnWEhVM1pqVXhYSFUxTTJOaVhIVTFNakEyWEhVMFpXRmlYSFUzWldKbVhIVTRaR1ZtSURjZ1hIVTBaVEE1WEhVM1pqVXhJaXdnSW1Ga1pDSTZJQ0pzWVRFd0xtSjFiR2x1YXk1NGVYb3VabTlpZW5NdVkyOXRJaXdnSW5Sc2N5STZJQ0p1YjI1bElpd2dJbllpT2lBaU1pSXNJQ0p1WlhRaU9pQWlkR053SWl3Z0ltaHZjM1FpT2lBaUlpd2dJbkJoZEdnaU9pQWlJaXdnSW5SNWNHVWlPaUFpYm05dVpTSjkKdm1lc3M6Ly9leUp3YjNKMElqb2dORFF6TENBaVlXbGtJam9nTUN3Z0ltbGtJam9nSW1KbE1qUmxPR1EyTFdGbE1tUXRORGMzT1MwNVlUa3pMVEl5T0RjMFpXVm1ZalE0TnlJc0lDSndjeUk2SUNKaWRXeHBibXNnWEhVM1pqVXhYSFUxTTJOaVhIVTFNakEyWEhVMFpXRmlYSFUzWldKbVhIVTRaR1ZtSURnZ1hIVTBaVEE1WEhVM1pqVXhJaXdnSW1Ga1pDSTZJQ0pzWVRFeExtSjFiR2x1YXk1NGVYb3VabTlpZW5NdVkyOXRJaXdnSW5Sc2N5STZJQ0p1YjI1bElpd2dJbllpT2lBaU1pSXNJQ0p1WlhRaU9pQWlkR053SWl3Z0ltaHZjM1FpT2lBaUlpd2dJbkJoZEdnaU9pQWlJaXdnSW5SNWNHVWlPaUFpYm05dVpTSjkK";
             var result = Encoding.UTF8.GetString(Convert.FromBase64String(RetS));
             foreach (var item in result.Split(Environment.NewLine.ToCharArray()))
             {
                 if (item.StartsWith("ss://"))
                 {
                     Console.WriteLine();
                 }
             }*/

            #region PPV

            {
                //HandleMainPage(File.ReadAllText(@"D:\141PPV\1 - 141PPV.com - Free Uncensored JAV Torrents.html"));
                void InitPPVDataBase()
                {
                    using (var db = new LiteDatabase(@$"Jav.db"))
                    {
                        if (!db.CollectionExists("PPVDB"))
                        {
                            Loger.Instance.LocalInfo("创建PPVDB数据库");
                            var PPVDB = db.GetCollection<JavInfo>("PPVDB");
                            PPVDB.EnsureIndex(x => x.id);
                            PPVDB.EnsureIndex(x => x.Date);
                            PPVDB.EnsureIndex(x => x.Size);
                            Loger.Instance.LocalInfo("创建PPV数据库成功");
                        }
                        else
                        {
                            Loger.Instance.LocalInfo("打开PPV数据库正常");
                        }
                    }
                }
                IEnumerable<JavInfo> HandleMainPage(string html)
                {
                    var HtmlDoc = new HtmlDocument();
                    HtmlDoc.LoadHtml(html);
                    foreach (var CardPage in HtmlDoc.DocumentNode.SelectNodes("/html[1]/body[1]/div[1]/div"))
                    {
                        var TempPPVInfo = new JavInfo();
                        try
                        {
                            if (CardPage.Attributes["class"].Value != "card mb-3") continue;
                            TempPPVInfo.ImgUrl = CardPage.SelectSingleNode("div[1]/div[1]/div[1]/img[1]").Attributes["src"].Value;
                            TempPPVInfo.id = CardPage.SelectSingleNode("div[1]/div[1]/div[2]/div[1]/h5[1]/a[1]").InnerText.Replace("\n", "").Replace(" ", "");
                            //TempPPVInfo.Describe = CardPage.SelectSingleNode("div[1]/div[1]/div[2]/div[1]/h5[1]/a[1]").Attributes["href"].Value;
                            TempPPVInfo.Size = CardPage.SelectSingleNode("div[1]/div[1]/div[2]/div[1]/h5[1]/span[1]").InnerText;
                            DateTime.TryParse(CardPage.SelectSingleNode("div[1]/div[1]/div[2]/div[1]/p[1]/a[1]").InnerText, out DateTime Date);
                            TempPPVInfo.Date = Date.ToString("yyyy-MM-dd");
                            TempPPVInfo.Describe = CardPage.SelectSingleNode("div[1]/div[1]/div[2]/div[1]/p[2]").InnerText.Replace("\n", "").Replace(" ", "");
                            TempPPVInfo.Magnet = CardPage.SelectSingleNode("div[1]/div[1]/div[2]/div[1]/a[1]").Attributes["href"].Value;
                        }
                        catch (Exception EX)
                        {
                            HtmlDoc.Save($"{DateTime.Now:mm:dd}.html");
                            Loger.Instance.LocalInfo($"PPV页面解析错误");
                        }
                        yield return TempPPVInfo;
                    }
                    yield break;
                }
            }

            #endregion PPV

            //DataBaseCommand.BaseUri = @"D:\";

            //var SSR = new Shadowsocks.Controller.ShadowsocksController("https://bulink.xyz/api/subscribe/?token=nfcnx&sub_type=vmess");
            // HandleT66yPage(File.ReadAllText(@"Z:\publish\3940263.html", Encoding.GetEncoding(936)));
            /* while (!Debugger.IsAttached)
             {
                 Console.WriteLine("wait");
                 Thread.Sleep(1000);
             }
             Debugger.Break();*/
            // Setting.DownloadManage = new DownloadManage();
            /*  using (var db = new LiteDatabase(@"Z:\publish\SIS.db"))
              {
                  using var db2 = new LiteDatabase(@"SIS.db");
                  if (!db2.CollectionExists("SISData"))
                  {
                      var SISDB = db2.GetCollection<SISData>("SISData");
                      SISDB.EnsureIndex(x => x.Title);
                      SISDB.EnsureIndex(x => x.Uri);
                      SISDB.EnsureIndex(x => x.Date);
                  }

                  if (!db2.CollectionExists("ImgData"))
                  {
                      var SISDB = db2.GetCollection<SISImgData>("ImgData");
                      SISDB.EnsureIndex(x => x.Status);
                      SISDB.EnsureIndex(x => x.Hash);
                      SISDB.EnsureIndex(x => x.id);
                      SISDB.EnsureIndex(x => x.Date);
                  }
                  foreach (var CollectionNames in db.GetCollectionNames())
                  {
                      var Skip = 0;
                      do
                      {
                          if (CollectionNames == "ImgData")
                          {
                              var T66yDB = db.GetCollection<SISImgData>(CollectionNames);
                              var T66yDB2 = db2.GetCollection<SISImgData>(CollectionNames);
                              try
                              {
                                  foreach (var item in T66yDB.Find(Query.All(), Skip))
                                  {
                                      try
                                      {
                                          //if (!T66yDB2.Exists(x => x["id"] == item["id"]))
                                          T66yDB2.Upsert(item);
                                      }
                                      catch (Exception)
                                      {
                                      }
                                      Skip += 1;
                                  }
                                  break;
                              }
                              catch (Exception)
                              {
                                  Skip += 1;
                                  Console.WriteLine(Skip);
                              }
                          }
                          else
                          {
                              var T66yDB = db.GetCollection<SISData>(CollectionNames);
                              var T66yDB2 = db2.GetCollection<SISData>(CollectionNames);

                              try
                              {
                                  foreach (var item in T66yDB.Find(Query.All(), Skip))
                                  {
                                      try
                                      {
                                          //if (!T66yDB2.Exists(x => x["id"] == item["id"]))
                                          T66yDB2.Upsert(item);
                                      }
                                      catch (Exception)
                                      {
                                      }
                                      Skip += 1;
                                  }
                                  break;
                              }
                              catch (Exception)
                              {
                                  Skip += 1;
                                  Console.WriteLine(Skip);
                              }
                          }
                      } while (true);
                  }
              }*/
            //https://www.sis001.com/forum/viewthread.php?tid=10863690
            /*using (var request = new HttpRequest()
            {
                UserAgent = Http.ChromeUserAgent(),
                ConnectTimeout = 20000,
                CharacterSet = Encoding.GetEncoding("GBK")
            })
            {
                request.Proxy = Socks5ProxyClient.Parse($"192.168.2.162:1088");
                HttpResponse response = request.Get("https://www.sis001.com/forum/viewthread.php?tid=10863690");
                var Save = response.ToString();
                File.WriteAllBytes("10863690.html", response.Ret);
            }*/

            //LiteDB.Engine.LiteEngine.Upgrade(@"Z:\publish\GlobalSet.db");
            //LiteDatabase db = new LiteDatabase(@"Z:\publish\T66y.db");
            // LiteDatabase db = new LiteDatabase(@"Z:\publish\Nyaa.db");
            // Console.WriteLine();
            // GetOtherT6yyPage();
            //var Download = DownloadGooglePage(4044141);//.Replace("&raquo;", "").Replace("&nbsp;", "").Replace("&copy;", "").Replace("/r", "").Replace("/t", "").Replace("/n", "").Replace("&amp;", "&"); ;
            //AnalyGooglePage(File.ReadAllText("4044141.html"));
            //var RetUrl = AnalyGooglePage(File.ReadAllText("4164642.html"));
            //var RetHtml = client.GetStringAsync($"{ AnalyGooglePage(DownloadGooglePage(PageCount))}").Result;
            //var _Table = db.GetCollection<T66yImgData>("ImgData");
            //var fo = _Table.FindOne(Query.EQ("id", "http://img200.imagexport.com/th/25143/1i1g8gafv3yr.jpg"));
            //var fo = _Table.Find(x => x["Date"] == "2020-09-03").Count();
            //var fo = _Table.Find(x => x.Date == "2020-09-03").Count();
            //var aa = new LiteDB.BsonMapper();
            // var FFF = aa.ToObject<T66yImgData>(fo);
            //Console.WriteLine();
            // HandleT66yPage();
            //HandleSISPage(new SISData(), File.ReadAllText("Text.html"));

            IEnumerable<string[]> AnalySISMainPage(string Page)
            {
                var HtmlDoc = new HtmlDocument();

                HtmlDoc.LoadHtml(Page);
                HtmlNodeCollection htmlNodes = null;
                htmlNodes = HtmlDoc.DocumentNode.SelectNodes("/html/body/div[4]/div[1]/div[7]/form/table[4]/tbody");
                if (htmlNodes == null)
                    htmlNodes = HtmlDoc.DocumentNode.SelectNodes("/html/body/div[4]/div[1]/div[7]/form/table[2]/tbody");
                foreach (var item in htmlNodes)
                {
                    var TempData = new string[5];
                    try
                    {
                        if (item.Id.StartsWith("normalthread_"))
                        {
                            var id = item.Id.Replace("normalthread_", "");
                            var temp = HtmlNode.CreateNode(item.OuterHtml);
                            var TypeName = temp.SelectSingleNode("/tbody[1]/tr[1]/th[1]/em[1]").InnerText;
                            var Title = temp.SelectSingleNode("/tbody[1]/tr[1]/th[1]/span[1]").InnerText;
                            var uri = temp.SelectSingleNode("/tbody[1]/tr[1]/th[1]/span[1]/a[1]").Attributes["href"].Value;
                            var Date = temp.SelectSingleNode("/tbody[1]/tr[1]/td[3]/em[1]").InnerText;
                            TempData = new string[]
                            {
                                uri,//地址
                                Title,//标题
                                Date,//日期
                                id,//编号
                                TypeName,//类型
                            };
                        }
                    }
                    catch (Exception)
                    {
                        Loger.Instance.LocalInfo($"SIS001页面解析错误");
                        File.AppendAllLines("SIS001.txt", new string[] { item.OuterHtml });
                    }
                    yield return TempData;
                }
                yield break;
            }
            //void HandleSISPage(SISData temp, string HTMLDATA)
            //{
            //    temp.MainList = new List<string>();
            //    var ImageCount = 0;
            //    var HtmlDoc = new HtmlDocument();
            //    HtmlDoc.LoadHtml(HTMLDATA);
            //    var Title = HtmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[4]/div[1]/form[1]/div[1]/table[1]/tr[1]/td[2]/div[3]/h2");
            //    foreach (var item in HtmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[4]/div[1]/form[1]/div[1]/table[1]/tr[1]/td[2]/div[3]/div[3]").ChildNodes)
            //    {
            //        switch (item.Name)
            //        {
            //            case "#text":
            //                var StringTemp = item.InnerHtml.Replace("\r\n", "");
            //                if (!string.IsNullOrEmpty(StringTemp))
            //                    temp.MainList.Add(item.InnerHtml.Replace("\r\n", ""));
            //                break;

            //            case "img":
            //                ImageCount += 1;
            //                //temp.MainList.Add(item.Attributes["src"].Value);
            //                var ImgUri = item.Attributes["src"].Value;
            //                break;

            //            case "font":
            //                FontInPut(temp, item.ChildNodes);
            //                break;

            //            default:
            //                break;
            //        }
            //    }
            //    var HASH = HtmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[4]/div[1]/form[1]/div[1]/table[1]/tr[1]/td[2]/div[3]/div[4]/dl[1]/dt[1]/a[1]").Attributes["href"].Value;
            //    var DownloadUrl = HtmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[4]/div[1]/form[1]/div[1]/table[1]/tr[1]/td[2]/div[3]/div[4]/dl[1]/dt[1]/a[2]").Attributes["href"].Value;
            //    temp.MainList.Add(HASH);
            //    temp.MainList.Add(DownloadUrl);
            //    void FontInPut(SISData temp, HtmlNodeCollection childNodes)
            //    {
            //        foreach (var item2 in childNodes)
            //        {
            //            switch (item2.Name)
            //            {
            //                case "#text":
            //                    var StringTemp = item2.InnerHtml.Replace("\r\n", "");
            //                    if (!string.IsNullOrEmpty(StringTemp))
            //                        temp.MainList.Add(item2.InnerHtml.Replace("\r\n", ""));
            //                    break;

            //                case "font":
            //                    FontInPut(temp, item2.ChildNodes);
            //                    break;

            //                default:
            //                    break;
            //            }
            //        }
            //    }
            //}
            //HandleT66yPage(File.ReadAllText("1.htm", Encoding.GetEncoding("gbk")));
            void HandleT66yPage(string Html)
            {
                var TempData = new T66yData() { HtmlData = Html };
                var ImgList = new List<T66yImgData>();
                var TempList = new List<string>();
                var Quote = new List<string>();
                var _HtmlDoc = new HtmlDocument();
                _HtmlDoc.LoadHtml(Html);
                //_HtmlDoc.Save($"{DownloadPage}.html", Encoding.GetEncoding("gbk"));
                try
                {
                    var ParentNode = _HtmlDoc.DocumentNode.SelectSingleNode("//div[@class='tiptop']").ParentNode;
                    TempData.Title = ParentNode.SelectSingleNode("//h4").InnerHtml;
                    var MainPage = ParentNode.SelectSingleNode("//div[4]");
                    var Type = ParentNode.SelectSingleNode("//*[@id='main']/div[1]/table[1]/tr[1]/td[1]/b[1]/a[2]");
                    if (Type.InnerHtml != "國產原創區")
                    {
                        return;
                    }
                    var Time = ParentNode.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tbody[1]/tr[2]/th[1]/div[1]");//.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", "");
                    if (Time == null)
                        Time = ParentNode.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tr[2]/th[1]/div[1]");//.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", "");
                    if (Time.ChildNodes.Count < 2)
                        Time = ParentNode.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tr[2]/th[1]/div[3]");
                    if (Time != null)
                        if (DateTime.TryParse(Time.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", ""), out DateTime dateTime))
                            TempData.Date = dateTime.ToString("yyyy-MM-dd");
                    var Start = false;
                    foreach (var item in MainPage.ChildNodes)
                    {
                        if (Start)
                        {
                            if (item.Name == "blockquote")
                            {
                                if (item.ChildNodes.Count != 0)
                                {
                                    SpildChild(item.ChildNodes, ref Quote);
                                }
                            }
                            else if (!string.IsNullOrWhiteSpace(item.InnerHtml)) // !item.InnerHtml.Contains("&nbsp"))
                            {
                                if (item.ChildNodes.Count != 0)
                                    SpildChild(item.ChildNodes, ref TempList);
                                else
                                    TempList.Add(item.InnerHtml);
                            }
                            else if (item.Name == "img")
                            {
                                var img = item.Attributes["ess-data"].Value.Replace(".th.jpg", ".jpg");
                                TempList.Add(img);
                                ImgList.Add(new T66yImgData() { id = img });
                            }
                        }
                        else if (FindFirst(item.InnerHtml, TempData.Title))
                        {
                            Start = true;
                            if (item.ChildNodes.Count != 0)
                                SpildChild(item.ChildNodes, ref TempList);
                            else
                                TempList.Add(item.InnerHtml);
                        }
                    }

                    if (TempList.Count == 0)
                    {
                    }
                    TempList.RemoveAt(TempList.Count - 1);//删除 赞
                    for (int i = TempList.Count - 1; i > 0; i--)
                    {
                        if (TempList[i].ToLower().Contains("quote"))
                            TempList.RemoveAt(i);
                        else if (TempList[i].ToLower().Contains("nbsp"))
                        {
                            var RepS = TempList[i].ToLower().Replace("nbsp", "");
                            if (RepS.Length < 2)
                                TempList.RemoveAt(i);
                            else
                                TempList[i] = RepS;
                        }
                    }
                    var findrmdown = false;
                    if (!findrmdown)
                    {
                        foreach (var item3 in TempList)
                        {
                            if (item3.Contains("rmdown"))
                            {
                                findrmdown = true;
                            }
                        }
                    }
                    if (!findrmdown)
                    {
                        TempData.HtmlData = string.Empty;
                        Loger.Instance.LocalInfo($"{TempData.Title}页面未找到下载地址");
                    }
                    TempData.MainList = new List<string>(TempList);
                    TempData.QuoteList = new List<string>(Quote);
                    TempData.Status = true;
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (!_HtmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[2]/div[2]/table[1]/tr[2]/th[1]/center[1]/div[1]").InnerHtml.StartsWith("您沒有登錄或者您沒有權限訪問此頁面"))
                        {
                            Loger.Instance.LocalInfo($"T66y页面解析错误{ex.Message}");
                            _HtmlDoc.Save($"{TempData.id}.html", Encoding.GetEncoding("gbk"));
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                finally
                {
                    ImgList.Clear();
                }
                void SpildChild(HtmlNodeCollection item, ref List<string> sL)
                {
                    foreach (var item2 in item)
                    {
                        if (item2.ChildNodes.Count == 0)
                        {
                            if (!string.IsNullOrWhiteSpace(item2.InnerHtml) && !item2.InnerHtml.Contains("&nbsp"))
                                sL.Add(item2.InnerHtml);
                            if (item2.Name == "img")
                            {
                                var img = item2.Attributes["ess-data"].Value.Replace(".th.jpg", ".jpg");
                                sL.Add(img);
                            }
                        }
                        else
                        {
                            SpildChild(item2.ChildNodes, ref sL);
                        }
                    }
                }
                bool FindFirst(string src, string Title)
                {
                    var SearchChar = new HashSet<string>() { "名称", "名稱", "rmdown", Title };
                    foreach (var item in SearchChar)
                    {
                        if (src.Contains(item))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            void HandleT66yPageTest(string Data)
            {
                var ImgList = new List<T66yImgData>();
                var TempList = new List<string>();
                var Quote = new List<string>();
                var _HtmlDoc = new HtmlDocument();
                //_HtmlDoc.LoadHtml(Data);
                // _HtmlDoc.Load(File.OpenRead($"{PageCount}.html"), Encoding.GetEncoding("gbk"));
                //_HtmlDoc.Save($"{PageCount}.html", Encoding.GetEncoding("gbk"));

                //_HtmlDoc.Load("4177015.html");
                //_HtmlDoc.Load("4164637.html", Encoding.GetEncoding("gbk"));
                _HtmlDoc.LoadHtml(Data);
                try
                {
                    //_HtmlDoc.Save($"Html{count++}.html", Encoding.GetEncoding("gbk"));
                    var ParentNode = _HtmlDoc.DocumentNode.SelectSingleNode("//div[@class='tiptop']").ParentNode;
                    var Name = ParentNode.SelectSingleNode("//h4");
                    var MainNode = ParentNode.SelectSingleNode("//div[4]");

                    var Type = ParentNode.SelectSingleNode("//*[@id='main']/div[1]/table[1]/tr[1]/td[1]/b[1]/a[2]");
                    var Time = ParentNode.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tbody[1]/tr[2]/th[1]/div[1]");//.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", "");
                    if (Time == null)
                        Time = ParentNode.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tr[2]/th[1]/div[1]");//.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", "");
                    if (Time.ChildNodes.Count < 2)
                        Time = ParentNode.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tr[2]/th[1]/div[3]");
                    if (Time != null)
                        if (DateTime.TryParse(Time.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", ""), out DateTime dateTime))
                        {
                            string Dat = dateTime.ToString("yyyy-MM-dd");
                        }

                    /*var Time = ParentNode.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tbody[1]/tr[2]/th[1]/div[1]");//.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", "");
                    if (Time == null)
                        Time = ParentNode.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tr[2]/th[1]/div[1]");//.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", "");
                    if (Time.ChildNodes.Count < 2)
                        Time = ParentNode.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tr[2]/th[1]/div[3]");
                    var STime = Time.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", "");*/
                    // var Date = DateTime.Parse(Time);
                    /*foreach (var item in CN.SelectNodes("//*[@id='main']/div"))
                    {
                        Console.WriteLine();
                    }*/
                    var Start = false;
                    foreach (var item in MainNode.ChildNodes)
                    {
                        if (Start)
                        {
                            if (item.Name == "blockquote")//TempList.Last().Contains("引用") || TempList.Last().Contains("Quote"))
                            {
                                if (item.ChildNodes.Count != 0)
                                {
                                    SpildChild(item.ChildNodes, ref Quote);
                                }
                            }
                            else if (!string.IsNullOrWhiteSpace(item.InnerHtml)) // !item.InnerHtml.Contains("&nbsp"))
                            {
                                if (item.ChildNodes.Count != 0)
                                    SpildChild(item.ChildNodes, ref TempList);
                                else
                                    TempList.Add(item.InnerHtml);
                            }
                            else if (item.Name == "img")
                            {
                                var img = item.Attributes["ess-data"].Value.Replace(".th.jpg", ".jpg");
                                //SaveImg(img);
                                TempList.Add(img);
                            }
                        }
                        else if (FindFirst(item.InnerHtml, Name.InnerHtml))
                        {
                            Start = true;
                            if (item.ChildNodes.Count != 0)
                                SpildChild(item.ChildNodes, ref TempList);
                            else
                                TempList.Add(item.InnerHtml);
                        }
                    }
                    if (TempList.Count == 0)
                    {
                    }
                    TempList.RemoveAt(TempList.Count - 1);//删除 赞
                    for (int i = TempList.Count - 1; i > 0; i--)
                    {
                        if (TempList[i].ToLower().Contains("quote") || TempList[i].ToLower().Contains("nbsp"))
                            TempList.RemoveAt(i);
                    }
                    var findrmdown = false;
                    if (!findrmdown)
                    {
                        foreach (var item3 in TempList)
                        {
                            if (item3.Contains("rmdown"))
                            {
                                findrmdown = true;
                            }
                        }
                    }
                    if (!findrmdown)
                    {
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (_HtmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[2]/div[2]/table[1]/tr[2]/th[1]/center[1]/div[1]").InnerHtml.StartsWith("您沒有登錄或者您沒有權限訪問此頁面"))
                        {
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                void SpildChild(HtmlNodeCollection item, ref List<string> sL)
                {
                    foreach (var item2 in item)
                    {
                        if (item2.ChildNodes.Count == 0)
                        {
                            if (!string.IsNullOrWhiteSpace(item2.InnerHtml) && !item2.InnerHtml.Contains("&nbsp"))
                                sL.Add(item2.InnerHtml);
                            if (item2.Name == "img")
                            {
                                var img = item2.Attributes["ess-data"].Value.Replace(".th.jpg", ".jpg");
                                sL.Add(img);
                                SaveImg(img);
                            }
                        }
                        else
                        {
                            SpildChild(item2.ChildNodes, ref sL);
                        }
                    }
                }
                bool FindFirst(string src, string Title)
                {
                    var SearchChar = new HashSet<string>() { "名称", "名稱", "rmdown", Title };
                    foreach (var item in SearchChar)
                    {
                        if (src.Contains(item))
                        {
                            return true;
                        }
                    }
                    return false;
                }
                void SaveImg(string img)
                {
                    T66yImgData SearchImg = DataBaseCommand.GetDataFromT66y("img", img);
                    if (SearchImg != null)
                    {
                        if (!SearchImg.Status)
                        {
                        }
                    }
                    else
                    {
                    }
                }
            }
            void HandleJav()
            {
                var HtmlDoc = new HtmlDocument();

                HtmlDoc.Load(@"Y:\net5\错误4cc6a8ae-c9ed-4143-a22d-8dc4c96c978a.html");
                var TempData = new JavInfo();

                foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[1]/div"))
                {
                    try
                    {
                        TempData = new JavInfo();
                        var temp = HtmlNode.CreateNode(item.OuterHtml);
                        TempData.ImgUrl = temp.SelectSingleNode("div/div/div[1]/img").Attributes["src"].Value;
                        //TempData.ImgUrlError = temp.SelectSingleNode("div/div/div[1]/img").Attributes["onerror"].Value.Split('\'')[1];
                        //TempData.id = temp.SelectSingleNode("div/div/div[2]/div/h5/a").InnerText.Replace("\n", "");
                        TempData.id = temp.SelectSingleNode("div/div/div[2]/div/h5/a").Attributes["href"].Value.Replace(@"/torrent/", "");
                        TempData.Size = temp.SelectSingleNode("div/div/div[2]/div/h5/span").InnerText;
                        var Date = temp.SelectSingleNode("div/div/div[2]/div/p[1]/a").Attributes["href"].Value.Substring(1);
                        if (Date.StartsWith("date"))
                        {
                            Date = Date.Replace("date/", "");
                        }
                        TempData.Date = $"{DateTime.Parse(Date):yy-MM-dd}";
                        var tags = new List<string>();
                        try
                        {
                            foreach (var Tags in temp.SelectNodes(@"//div[@class='tags']/a"))
                            {
                                tags.Add(Tags.InnerText.Replace("\n", "").Replace("\r", ""));
                            }
                        }
                        catch (NullReferenceException) { }
                        catch (Exception)
                        {
                        }
                        TempData.Tags = tags.ToArray();
                        TempData.Describe = temp.SelectSingleNode("div/div/div[2]/div/p[2]").InnerText;
                        //TempData.Describe = temp.SelectSingleNode("div/div/div[2]/div/p[2]").InnerText.ReplaceEntities().Replace("\n", "");
                        var Actress = new List<string>();
                        try
                        {
                            foreach (var Tags in temp.SelectNodes(@"//div[@class='panel']/a"))
                            //foreach (var Tags in temp.SelectNodes(@"div/div/div[2]/div/div[2]"))
                            {
                                Actress.Add(Tags.InnerText.Replace("\n", ""));
                            }
                        }
                        catch (NullReferenceException) { }
                        catch (Exception)
                        {
                        }
                        TempData.Actress = Actress.ToArray();
                        TempData.Magnet = temp.SelectSingleNode("div/div/div[2]/div/a[1]").Attributes["href"].Value;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            await InitCoreAsync().ConfigureAwait(false);
            return await Setting.ShutdownResetEvent.Task.ConfigureAwait(false);

            {
                var HtmlDoc = new HtmlDocument();
                HtmlDoc.Load(@"C:\Users\cdj68\Desktop\无标题.html");
                try
                {
                    foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[1]/div"))
                    {
                        var TempData = new JavInfo();
                        var temp = HtmlNode.CreateNode(item.OuterHtml);
                        TempData.ImgUrl = temp.SelectSingleNode("div/div/div[1]/img").Attributes["src"].Value;
                        TempData.ImgUrlError = temp.SelectSingleNode("div/div/div[1]/img").Attributes["onerror"].Value.Split('\'')[1];
                        //TempData.id = temp.SelectSingleNode("div/div/div[2]/div/h5/a").InnerText.Replace("\n", "");
                        TempData.id = temp.SelectSingleNode("div/div/div[2]/div/h5/a").Attributes["href"].Value.Replace(@"/torrent/", "");
                        TempData.Size = temp.SelectSingleNode("div/div/div[2]/div/h5/span").InnerText;
                        TempData.Date = $"{DateTime.Parse(temp.SelectSingleNode("div/div/div[2]/div/p[1]/a").Attributes["href"].Value.Substring(1)):yy-MM-dd}";
                        var tags = new List<string>();
                        try
                        {
                            foreach (var Tags in temp.SelectNodes(@"//div[@class='tags']/a"))
                            {
                                tags.Add(Tags.InnerText.Replace("\n", "").Replace("\r", ""));
                            }
                        }
                        catch (Exception)
                        {
                        }
                        TempData.Tags = tags.ToArray();
                        TempData.Describe = temp.SelectSingleNode("div/div/div[2]/div/p[2]").InnerText.ReplaceEntities().Replace("\n", ""); ;
                        var Actress = new List<string>();
                        try
                        {
                            foreach (var Tags in temp.SelectNodes(@"//div[@class='panel']/a"))
                            //foreach (var Tags in temp.SelectNodes(@"div/div/div[2]/div/div[2]/a"))
                            {
                                Actress.Add(Tags.InnerText.Replace("\n", ""));
                            }
                        }
                        catch (System.NullReferenceException e)
                        {
                        }
                        catch (Exception e)
                        {
                        }
                        TempData.Actress = Actress.ToArray();
                        TempData.Magnet = temp.SelectSingleNode("div/div/div[2]/div/a[1]").Attributes["href"].Value;
                    }
                }
                catch (Exception e)
                {
                }
            }
            // HtmlDoc.LoadHtml(Encoding.GetEncoding("GBK").GetString(File.ReadAllBytes("MiMiS")));
            /*try
            {
                var HtmlDoc = new HtmlDocument();
                HtmlDoc.Load("MiMiL");
                foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/center/form/div[1]/div/table"))
                {
                    var TempData = new string[]
                    {
                    item.SelectSingleNode("tr/td[1]/a").Attributes["href"].Value,
                    item.SelectSingleNode("tr/td[3]/a[1]").InnerText,
                    item.SelectSingleNode("tr/td[4]/a").InnerText,
                    item.SelectSingleNode("tr/td[4]/span").InnerText,
                    bool.FalseString
                    };
                    if (TempData[2] == "mimi")
                    {
                        if (TempData[1].Contains("BT合集"))
                        {
                            //DataBaseCommand.SaveToMiMiDataTablet(TempData);
                            DataBaseCommand.SaveToMiMiDataTablet(new[] { TempData[3], bool.TrueString });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }*/
            /* ProxySettings ProxySettings = new ProxySettings { Host = "127.0.0.1", Port = 7070 };
             var client = new HttpClient(new ProxyClientHandler<Socks5>(ProxySettings));

             client.Timeout = new TimeSpan(0, 1, 0);
             var HtmlDoc = new HtmlDocument();
             HtmlDoc.Load(new FileStream("Html", System.IO.FileMode.Open), Encoding.GetEncoding("GBK"));
             foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[2]/div[2]/table[1]/tbody[1]/tr"))
             {
                 if (string.IsNullOrEmpty(item.InnerHtml)) continue;
                 if (item.InnerLength < 50)
                     continue;
                 if (item.Attributes["class"].Value == "tr3 t_one tac" && item.SelectSingleNode("td[1]").InnerHtml.Contains(".::"))
                 {
                     var temp = HtmlNode.CreateNode(item.OuterHtml);
                     var Url = temp.SelectSingleNode("td[2]/h3/a").Attributes["href"].Value;
                     var HtmlTempData = Get(ref Url);
                     var TempData = new string[]
                     {
                         Url,//地址
                         temp.SelectSingleNode("td[2]/h3/a").InnerHtml,//标题
                         temp.SelectSingleNode("td[3]/div/span").Attributes["title"].Value.Split(' ')[2],//日期
                         HtmlTempData.Item1,//编号
                         HtmlTempData.Item2,//内容
                     };
                 }
             }
             Tuple<string, string> Get(ref string Url)
             {
                 if (Url.StartsWith("htm"))
                 {
                     var UrlS = Url.Replace("htm_data", "").Replace(".html", "").Split('/');
                     return new Tuple<string, string>($"{UrlS[1]}{UrlS[2]}{UrlS[3]}", client.GetStringAsync($"http://t66y.com/{Url}").Result);
                 }
                 else
                 {
                     // Url = $"htm_data/{year}/{Month}/{Url.Split('=')[1]}.html"; var RetHtml = client.GetStringAsync($"http://t66y.com/{Url}").Result;
                     var TempDoc = new HtmlDocument();
                     TempDoc.Load(client.GetStringAsync($"http://t66y.com/{Url}").Result);
                     Url = TempDoc.DocumentNode.SelectSingleNode("/html/body/center/div/a[2]").Attributes["href"].Value;
                     return Get(ref Url);
                     //File.WriteAllText("Html2", RetHtml);
                 }
             }*/
            //http://www.mmfhd.com/forumdisplay.php?fid=55&page=
            //http://www.mmbuff.com/forumdisplay.php?fid=55&page=
            /* using (var request = new HttpRequest()
              {
                  UserAgent = Http.ChromeUserAgent(),
                  ConnectTimeout = 20000,
                  CharacterSet = Encoding.GetEncoding("GBK")
              })
              {
                  //request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:7070");
                  HttpResponse response = request.Get("http://www.mmbuff.com/viewthread.php?tid=1203210");
                  var Save = response.ToString();
                  File.WriteAllText("MiMiC", Save);
              }
                              var HtmlDoc = new HtmlDocument();
                  HtmlDoc.LoadHtml(Encoding.GetEncoding("GBK").GetString(response.Ret));
                  foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/center/form/div[1]/div/table"))
                  {
                      var CT = item.SelectSingleNode("tr/td[1]/a").Attributes["href"].Value;
                      CT = item.SelectSingleNode("tr/td[3]/a[1]").InnerText;
                      CT = item.SelectSingleNode("tr/td[4]/a").InnerText;
                      CT = item.SelectSingleNode("tr/td[4]/span").InnerText;
                  }
               */

            Setting._GlobalSet = GlobalSet.Open();
            if (args.Length != 0 && !string.IsNullOrEmpty(args[0]))
            {
                Setting._GlobalSet.ssr_url = args[0].ToString();
                Setting._GlobalSet.SocksCheck = true;
            }
            Setting._GlobalSet.MiMiFin = true;
            return await Setting.ShutdownResetEvent.Task.ConfigureAwait(false);
        }

        private static void DebugJav()
        {
            var HtmlDoc = new HtmlDocument();

            HtmlDoc.LoadHtml(File.ReadAllText("Error.html"));
            try
            {
                foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[1]/div"))
                {
                    var TempData = new JavInfo();
                    var temp = HtmlNode.CreateNode(item.OuterHtml);
                    TempData.ImgUrl = temp.SelectSingleNode("div/div/div[1]/img").Attributes["src"].Value;
                    TempData.ImgUrlError = temp.SelectSingleNode("div/div/div[1]/img").Attributes["onerror"].Value.Split('\'')[1];
                    //TempData.id = temp.SelectSingleNode("div/div/div[2]/div/h5/a").InnerText.Replace("\n", "");
                    TempData.id = temp.SelectSingleNode("div/div/div[2]/div/h5/a").Attributes["href"].Value.Replace(@"/torrent/", "");
                    TempData.Size = temp.SelectSingleNode("div/div/div[2]/div/h5/span").InnerText;
                    TempData.Date = $"{DateTime.Parse(temp.SelectSingleNode("div/div/div[2]/div/p[1]/a").Attributes["href"].Value.Substring(1)):yy-MM-dd}";
                    var tags = new List<string>();
                    try
                    {
                        foreach (var Tags in temp.SelectNodes(@"//div[@class='tags']/a"))
                        {
                            tags.Add(Tags.InnerText.Replace("\n", "").Replace("\r", ""));
                        }
                    }
                    catch (NullReferenceException) { }
                    catch (Exception)
                    {
                    }
                    TempData.Tags = tags.ToArray();
                    TempData.Describe = temp.SelectSingleNode("div/div/div[2]/div/p[2]").InnerText.ReplaceEntities().Replace("\n", ""); ;
                    var Actress = new List<string>();
                    try
                    {
                        foreach (var Tags in temp.SelectNodes(@"//div[@class='panel']/a"))
                        //foreach (var Tags in temp.SelectNodes(@"div/div/div[2]/div/div[2]"))
                        {
                            Actress.Add(Tags.InnerText.Replace("\n", ""));
                        }
                    }
                    catch (NullReferenceException) { }
                    catch (Exception)
                    {
                        Loger.Instance.LocalInfo($"Jav人名解析失败");
                    }
                    TempData.Actress = Actress.ToArray();
                    TempData.Magnet = temp.SelectSingleNode("div/div/div[2]/div/a[1]").Attributes["href"].Value;
                }
            }
            catch (Exception)
            {
            }
        }

        private static async Task InitCoreAsync()
        {
            await Task.WhenAll(
            Task.Run(() => DataBaseCommand.InitDataBase()),
            Task.Run(() =>
            {
                try
                {
                    Setting.SSR = new ShadowsocksRController();
                    Setting.Socks5Point = Setting.SSR.SocksPort;
                    Setting.SSR.CheckOnline();
                    Setting.NyaaSSR = new ShadowsocksRController(Setting._GlobalSet.ssr4Nyaa);
                    Setting.NyaaSocks5Point = 1088;// Setting.NyaaSSR.SocksPort;
                }
                catch (Exception ex)
                {
                    Loger.Instance.LocalInfo($"{ex.Message}");
                }
                finally
                {
                    //ThreadPool.QueueUserWorkItem(x =>
                    //{
                    //    Setting.NyaaSocks5Point = 1088;
                    //    /*if (!Setting.NyaaSSR.CheckOnline(@"https://sukebei.nyaa.si/", Setting.NyaaSocks5Point))
                    //        Setting.NyaaSocks5Point = Setting.NyaaSSR.SocksPort;*/
                    //});
                }
            }),
            Task.Run(() => Setting.server = new server()),
            Task.Run(() =>
            {
                var tcphost = new TcpHost(new IPEndPoint(IPAddress.Any, 2222), null, null, null)
                {
                    UseCompression = false,
                    CompressionThreshold = 131072
                };
                tcphost.AddService<IImage2Webp>(new Image2Webp());
                tcphost.Open();
                Loger.Instance.ServerInfo("Webp", $"Webp监听启动");
            })
            ).ContinueWith(obj
            => Loger.Instance.LocalInfo("数据库初始化完毕")).
            ContinueWith(obj =>
            {
                /*if (Setting.CheckOnline(Setting._GlobalSet.SocksCheck))
                {
                    Loger.Instance.LocalInfo("网络连接正常，正在加载下载进程");
                }
                else
                {
                    Loger.Instance.LocalInfo("外网访问失败，等待操作");
                }*/
                /* while (true)
                 {
                     Thread.Sleep(100);
                     Loger.Instance.LocalInfo("Text");
                     Loger.Instance.ServerInfo("", "Text");
                 }*/
                //Task.Run(() => DataBaseCommand.ChangeJavActress());
                if (Setting._GlobalSet.AutoRun) Setting.DownloadManage = new DownloadManage(); else Loger.Instance.LocalInfo("自动运行关闭，等待命令");

                /*var _controller = new ShadowsocksRController();
                _controller.Start();
                using (var request = new HttpRequest())
                {
                    request.UserAgent = Http.ChromeUserAgent();
                    request.Proxy = Socks5ProxyClient.Parse("127.0.0.1:7071");
                    HttpResponse response = request.Get(@"sukebei.nyaa.si");
                    Console.WriteLine(response.ToString());
                    */
                /*    request
                        // Parameters URL-address.
                        .AddUrlParam("data1", "value1")
                        .AddUrlParam("data2", "value2")

                        // Parameters 'x-www-form-urlencoded'.
                        .AddParam("data1", "value1")
                        .AddParam("data2", "value2")
                        .AddParam("data2", "value2")

                        // Multipart data.
                        .AddField("data1", "value1")
                        .AddFile("game_code", @"C:\orion.zip")

                        // HTTP-header.
                        .AddHeader("X-Apocalypse", "21.12.12");

                    // These parameters are sent in this request.
                    request.Post("/").None();

                    // But in this request they will be gone.
                    request.Post("/").None();*/
                //}

                /*  Task.Factory.StartNew(() =>
                  {
                      while (!Setting.ShutdownResetEvent.Task.IsCompleted)
                      {
                          Thread.Sleep(5000);
                          Setting.StatusByte = Setting._GlobalSet.Send();
                      }
                  }, TaskCreationOptions.LongRunning);*/
                //Task.Factory.StartNew(() => new DownNyaaLoop().DownLoopAsync(), TaskCreationOptions.LongRunning);
                //Task.Factory.StartNew(() => { }, TaskCreationOptions.LongRunning);
            });
        }

        //private static readonly Stopwatch Time = new Stopwatch();

        #region MyRegion

        private static void Main2(string[] args)
        {
            /*  var DownloadCollect = new BlockingCollection<TorrentInfo2>();
              var DateRecordC = new BlockingCollection<DateRecord>();
              var CounT = 0;
              Task.Factory.StartNew(() =>
              {
                  using (var db = new LiteDatabase(@"Nyaa.db"))
                  {
                      var DateRecord = db.GetCollection<TorrentInfo>("NyaaDB");
                      CounT = DateRecord.Count();
                      Parallel.ForEach(DateRecord.FindAll(), VARIABLE =>
                      {
                          if (!string.IsNullOrEmpty(VARIABLE.Torrent))
                          {
                              DownloadCollect.TryAdd(new TorrentInfo2()
                              {
                                  Catagory = VARIABLE.Catagory,
                                  Timestamp = VARIABLE.id,
                                  Class = VARIABLE.Class,
                                  Title = VARIABLE.Title,
                                  Torrent = VARIABLE.Torrent,
                                  Magnet = VARIABLE.Magnet,
                                  Size = VARIABLE.Size,
                                  Date = VARIABLE.Date,
                                  Up = VARIABLE.Up,
                                  Leeches = VARIABLE.Leeches,
                                  Complete = VARIABLE.Complete
                              });
                          }
                      });
                      DownloadCollect.CompleteAdding();
                      var Record = db.GetCollection<DateRecord>("DateRecord");

                      Parallel.ForEach(Record.FindAll(), VARIABLE => { DateRecordC.TryAdd(VARIABLE); });
                      DateRecordC.CompleteAdding();
                      CounT = Record.Count();
                  }
              });
              var NowC = 0;
              Task.Factory.StartNew(() =>
              {
                  using (var db = new LiteDatabase(@"Nyaa2.db"))
                  {
                      var SettingData = db.GetCollection<GlobalSet>("Setting");
                      SettingData.Upsert(new GlobalSet {_id = "Address", Value = "https://sukebei.nyaa.si/"});
                      SettingData.Upsert(new GlobalSet {_id = "LastCount", Value = "2790"});
                      var DateRecord = db.GetCollection<DateRecord>("DateRecord");
                      DateRecord.EnsureIndex(X => X._id);
                      var NyaaDB = db.GetCollection<TorrentInfo2>("NyaaDB");
                      NyaaDB.EnsureIndex(x => x.Catagory);
                      NyaaDB.EnsureIndex(x => x.Date);
                      NyaaDB.EnsureIndex(x => x.id);

                      Parallel.ForEach(DownloadCollect.GetConsumingEnumerable(),
                          VARIABLE =>
                          {
                              NyaaDB.Insert(VARIABLE);
                              Interlocked.Increment(ref NowC);
                          });
                      NowC = 0;
                      Parallel.ForEach(DateRecordC.GetConsumingEnumerable(),
                          VARIABLE =>
                          {
                              DateRecord.Insert(VARIABLE);
                              Interlocked.Increment(ref NowC);
                          });
                  }
              });

              while (true)
              {
                  Console.SetCursorPosition(0, 0);
                  Console.Write("                                  ");
                  Console.SetCursorPosition(0, 0);
                  Console.Write($"{NowC}/{CounT}");
                  Thread.Sleep(500);
              }

              */

            /*   do
               {
                      var ST = string.Empty;

                   for (int i = 0; i < new Random().Next(10, 30); i++)
                   {
                       ST += $"启动{i}";
                   }
                   Loger.Instance.LocalInfo(ST);
                   Thread.Sleep(1);
               } while (true);*/

            /* Loger.Instance.LocalInfo($"启动");
             DataBaseCommand.Init();
             Loger.Instance.LocalInfo("数据库初始化完毕");
             Task.Factory.StartNew(() =>
             {
                 Loger.Instance.LocalInfo("主线程启动");
                 new DownLoop().DownLoopAsync();
             }, TaskCreationOptions.LongRunning);
             Console.CancelKeyPress += delegate { DataBaseCommand.SaveLastCountStatus(); };*/

            //new DownWork();
            /*   var Web= new  WebClient();
                  try
                  {
                      Web.DownloadString("https://sukebei.nyaa.si/?p=1");
                  }
                  catch (Exception ex)
                  {
                      while (ex != null)
                      {
                          Console.WriteLine(ex.Message);
                          ex = ex.InnerException;
                      }
                  }*/

            // Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd"));

            // new DownWork();
            /*  Task.Factory.StartNew(() =>
              {
                  Loger.Instance.Info("开始获取网页数据");
                 new DownloadHelp(Setting.LastPageIndex);
              }, Setting.CancelSign.Token);
              Task.Factory.StartNew(() =>
              {
                  Loger.Instance.Info("启动网页数据分析和保存");
                  //管道剩余待处理数据
                  //剩余处理时间
                  //处理完毕且管道已经清空
                  foreach (var item in DownloadHelp.DownloadCollect.GetConsumingEnumerable())
                  {
                  }
              }, Setting.CancelSign.Token);*/

            //test
            //new WebPageGet(@"https://sukebei.nyaa.si/?p=500000");
            //GetDataFromDataBase();
            // var ret = new HandlerHtml(File.ReadAllText("save.txt"));
            // SaveToDataBaseFormList(ret.AnalysisData.Values);
            //SaveToDataBaseOneByOne(ret.AnalysisData.Values);
            //var TCPCmd = TCPCommand.Init(1000);
            //TCPCmd.StartListener();
        }

        #endregion MyRegion
    }
}