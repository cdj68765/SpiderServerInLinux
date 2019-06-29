using HtmlAgilityPack;
using Shadowsocks.Controller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xNet;

namespace SpiderServerInLinux
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
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

            Setting._GlobalSet = new GlobalSet().Open();
            Setting._GlobalSet.ssr_url = "ssr://aGt0Lm5vcy5vb286MzM4ODY6YXV0aF9hZXMxMjhfc2hhMTpjaGFjaGEyMC1pZXRmOnBsYWluOk1tRXhkbUZaLz9vYmZzcGFyYW09JnByb3RvcGFyYW09JnJlbWFya3M9NUxpdDVaeUw2YWFaNXJpdkxlbW1tZWE0ci1tYnUtaW9pZyZncm91cD00NEdmNDRHazQ0RzI0NEdo";
            await Init();
            return await Setting.ShutdownResetEvent.Task.ConfigureAwait(false);
        }

        private static async Task Init()
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            AppDomain.CurrentDomain.ProcessExit += delegate
            {
                Setting.DownloadManage.Dispose();
                Console.Clear();
                Console.WriteLine("程序退出");
            };
            AppDomain.CurrentDomain.UnhandledException += delegate
            {
                Console.Clear();
                Console.WriteLine("程序异常");
            };
            TaskScheduler.UnobservedTaskException += delegate
            {
                Console.Clear();
                Console.WriteLine("线程异常");
            };
            await InitCoreAsync().ConfigureAwait(false);
        }

        private static async Task InitCoreAsync()
        {
            await Task.WhenAll(
            Task.Run(() => DataBaseCommand.InitNyaaDataBase()),
            Task.Run(() => DataBaseCommand.InitJavDataBase()),
            Task.Run(() => DataBaseCommand.InitMiMiAiDataBase()),
            Task.Run(() =>
            {
                if (Setting._GlobalSet.SocksCheck) Setting.SSR = new ShadowsocksController();
            }),
            Task.Run(() => Setting.server = new server())).ContinueWith(obj
            => Loger.Instance.LocalInfo("数据库初始化完毕")).
            ContinueWith(obj =>
            {
                if (Setting._GlobalSet.AutoRun)
                    Setting.DownloadManage = new DownloadManage();
                else Loger.Instance.LocalInfo("自动运行关闭，等待命令");
                /*var _controller = new ShadowsocksController();
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
            var TCPCmd = TCPCommand.Init(1000);
            TCPCmd.StartListener();
        }

        #endregion MyRegion
    }
}