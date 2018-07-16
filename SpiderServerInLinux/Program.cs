using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;

namespace SpiderServerInLinux
{
    internal class Program
    {
        private static void Main(string[] args)
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

            Loger.Instance.LocalInfo($"启动");
            DataBaseCommand.Init();
            Loger.Instance.LocalInfo("数据库初始化完毕");
            Task.Factory.StartNew(() =>
            {
                Loger.Instance.LocalInfo("主线程启动");
                new DownLoop().DownLoopAsync();
            }, TaskCreationOptions.LongRunning);
            Console.CancelKeyPress += delegate { DataBaseCommand.SaveLastCountStatus(); };


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
    }
}