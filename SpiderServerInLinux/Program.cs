using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cowboy.WebSockets;
using HtmlAgilityPack;
using LiteDB;
using Shadowsocks.Controller;
using xNet;
using static xNet.HttpResponse;

namespace SpiderServerInLinux
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Setting._GlobalSet = new GlobalSet().Open();
            await Init();
            return await Setting.ShutdownResetEvent.Task.ConfigureAwait(false);
        }

        private static async Task Init()
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            AppDomain.CurrentDomain.ProcessExit += delegate
            {
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
            Task.Run(() => { DataBaseCommand.InitNyaaDataBase(); }),
            Task.Run(() => { DataBaseCommand.InitJavDataBase(); }),
            Task.Run(() =>
            {
                if (Setting._GlobalSet.SocksCheck)
                {
                    Setting.SSR = new ShadowsocksController();
                }
            }),
            Task.Run(() =>
            {
                Setting.server = new server();
            })).ContinueWith(obj
            => Loger.Instance.LocalInfo("数据库初始化完毕")).
            ContinueWith(obj =>
            {
                Setting.DownloadManage = new DownloadManage();
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