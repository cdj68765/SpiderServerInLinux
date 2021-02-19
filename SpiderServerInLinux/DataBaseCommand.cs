using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;

namespace SpiderServerInLinux
{
    internal class DataBaseCommand
    {
        internal static void InitDataBase()
        {
            void InitNyaaDataBase()
            {
                using (var db = new LiteDatabase(@"Nyaa.db"))
                {
                    if (!db.CollectionExists("NyaaDB"))
                    {
                        Loger.Instance.LocalInfo("正在创建Nyaa数据库");
                        var NyaaDB = db.GetCollection<NyaaInfo>("NyaaDB");
                        NyaaDB.EnsureIndex(x => x.Catagory);
                        NyaaDB.EnsureIndex(x => x.Date);
                        NyaaDB.EnsureIndex(x => x.id);
                        // NyaaDB.EnsureIndex(x => x.Title);
                        Loger.Instance.LocalInfo("创建Nyaa数据库成功");
                    }
                    else
                    {
                        Loger.Instance.LocalInfo("打开Nyaa数据库正常");
                    }
                }
            }
            void InitJavDataBase()
            {
                using (var db = new LiteDatabase(@"Jav.db"))
                {
                    if (!db.CollectionExists("JavDB"))
                    {
                        Loger.Instance.LocalInfo("创建JavDB数据库");
                        var NyaaDB = db.GetCollection<JavInfo>("JavDB");
                        NyaaDB.EnsureIndex(x => x.id);
                        NyaaDB.EnsureIndex(x => x.Date);
                        NyaaDB.EnsureIndex(x => x.Size);
                        var _Table = db.GetCollection("WebPage");
                        _Table.EnsureIndex("_id", true);
                        _Table.EnsureIndex("Status");
                        Loger.Instance.LocalInfo("创建JAV数据库成功");
                    }
                    else
                    {
                        Loger.Instance.LocalInfo("打开Jav数据库正常");
                    }
                }
            }
            void InitMiMiAiDataBase()
            {
                using (var db = new LiteDatabase(@"MiMi.db"))
                {
                    if (!db.CollectionExists("MiMiDB"))
                    {
                        Loger.Instance.LocalInfo("创建MiMiAi数据库");
                        var MiMiDB = db.GetCollection<MiMiAiData>("MiMiDB");
                        MiMiDB.EnsureIndex(x => x.Title);
                        MiMiDB.EnsureIndex(x => x.Date);
                        var _Table = db.GetCollection("WebPage");
                        _Table.EnsureIndex("_id", true);
                        _Table.EnsureIndex("Uri", true);
                        _Table.EnsureIndex("Status");
                        Loger.Instance.LocalInfo("创建MiMiAi数据库成功");
                    }
                    else
                    {
                        Loger.Instance.LocalInfo("打开MiMiAi数据库正常");
                    }
                }
            }
            void InitMiMiAiStoryDataBase()
            {
                using (var db = new LiteDatabase(@"MiMi.db"))
                {
                    if (!db.CollectionExists("MiMiStory"))
                    {
                        Loger.Instance.LocalInfo("创建MiMiAi小说数据库");
                        var MiMiDB = db.GetCollection<MiMiAiStory>("MiMiStory");
                        MiMiDB.EnsureIndex(x => x.Title);
                        MiMiDB.EnsureIndex(x => x.Uri);

                        Loger.Instance.LocalInfo("创建MiMiAi小说数据库成功");
                    }
                    else
                    {
                        Loger.Instance.LocalInfo("打开MiMiAi小说数据库正常");
                    }
                }
            }
            void InitT66yDataBase()
            {
                using (var db = new LiteDatabase(@"T66y.db"))
                {
                    //var db = Setting.T66yDB;
                    if (!db.CollectionExists("T66yData"))
                    {
                        Loger.Instance.LocalInfo("创建T66y数据库");
                        var T66yDB = db.GetCollection<T66yData>("T66yData");
                        T66yDB.EnsureIndex(x => x.Title);
                        T66yDB.EnsureIndex(x => x.Uri);
                        T66yDB.EnsureIndex(x => x.Date);
                        Loger.Instance.LocalInfo("创建T66y数据库成功");
                    }
                    else
                    {
                        Loger.Instance.LocalInfo("打开T66y数据库正常");
                    }
                    if (!db.CollectionExists("ImgData"))
                    {
                        var T66yDB = db.GetCollection<T66yImgData>("ImgData");
                        T66yDB.EnsureIndex(x => x.Status);
                        T66yDB.EnsureIndex(x => x.Hash);
                        T66yDB.EnsureIndex(x => x.id);
                        T66yDB.EnsureIndex(x => x.Date);
                    }
                }
            }
            void InitSISDataBase()
            {
                using (var db = new LiteDatabase(@"SIS.db"))
                {
                    if (!db.CollectionExists("SISData"))
                    {
                        Loger.Instance.LocalInfo("创建SIS数据库");
                        var SISDB = db.GetCollection<SISData>("SISData");
                        SISDB.EnsureIndex(x => x.Title);
                        SISDB.EnsureIndex(x => x.Uri);
                        SISDB.EnsureIndex(x => x.Date);
                        Loger.Instance.LocalInfo("创建SIS数据库成功");
                    }
                    else
                    {
                        Loger.Instance.LocalInfo("打开SIS数据库正常");
                    }
                    if (!db.CollectionExists("ImgData"))
                    {
                        var SISDB = db.GetCollection<SISImgData>("ImgData");
                        SISDB.EnsureIndex(x => x.Status);
                        SISDB.EnsureIndex(x => x.Hash);
                        SISDB.EnsureIndex(x => x.id);
                        SISDB.EnsureIndex(x => x.Date);
                    }
                }
            }
            InitNyaaDataBase();
            InitJavDataBase();
            InitMiMiAiDataBase();
            InitMiMiAiStoryDataBase();
            InitT66yDataBase();
            InitSISDataBase();
        }

        #region 从数据库读取

        internal static DateRecord GetNyaaDateInfo(string Date)
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                var DateRecord = db.GetCollection<DateRecord>("DateRecord");
                var FindData = DateRecord.FindOne(Dt => Dt._id == Date);
                if (FindData is DateRecord)
                {
                    return FindData;
                }
                return null;
            }
        }

        internal static bool GetWebInfoFromNyaa(string Date)
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                var DateRecord = db.GetCollection<DateRecord>("DateRecord");
                if (DateRecord.Exists(x => x._id == Date))
                {
                    if (DateRecord.FindOne(Dt => Dt._id == Date).Status)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        internal static int GetNyaaCheckPoint()
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                Stopwatch Time = new Stopwatch();
                Loger.Instance.WithTimeStart("数据查找中", Time);
                var DateRecord = db.GetCollection<NyaaInfo>("NyaaDB");
                var results = DateRecord.Find(Query.All()).OrderByDescending(x => x.Timestamp).Where(d => !d.Day.StartsWith("2019")).FirstOrDefault();
                // var results = DateRecord.FindOne(x => x.Timestamp == Setting._GlobalSet.NyaaCheckPoint);
                Loger.Instance.WithTimeStop($"数据查找完毕{results.Timestamp}", Time);
                return results.Timestamp;
            }
        }

        internal static bool GetNyaaCheckPoint(int ID)
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                var DateRecord = db.GetCollection<NyaaInfo>("NyaaDB");
                if (DateRecord.Exists(x => x.id == ID)) return true;
                return false;
            }
        }

        #endregion 从数据库读取

        #region 数据库查找

        internal static void GetDataFromDataBase()
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                var NyaaDB = db.GetCollection<NyaaInfo>("NyaaDB");
                //var FindAdress = NyaaDB.Find(x=>x.== "Address");
            }
        }

        internal static dynamic GetDataFromMiMi(string Code, Cowboy.WebSockets.AsyncWebSocketSession session = null, CancellationTokenSource Cancel = null, params string[] Data)
        {
            using (var db = new LiteDatabase(@"MiMi.db"))
            {
                switch (Code)
                {
                    case "TabletInfo":
                        {
                            // var _Table = db.GetCollection("WebPage");
                            // return _Table.FindOne(Query.And(Query.All("_id", Query.Descending), Query.EQ("Status", false)));
                            //return _Table.FindOne(x => x["Status"] == false);
                        }
                        break;

                    case "UnitInfo":
                        {
                            //var _Table = db.GetCollection<MiMiAiData>("MiMiDB");
                            // return _Table.FindOne(Query.And(Query.All("Date", Query.Descending), Query.EQ("Status", false)));
                        }
                        break;

                    case "CheckMiMiStoryLastPage":
                        {
                            var _Table = db.GetCollection<MiMiAiStory>("MiMiStory");
                            return _Table.Exists(X => X.id == 140503);
                        }
                        break;

                    case "GetNullStory":
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                try
                                {
                                    var _Table = db.GetCollection<MiMiAiStory>("MiMiStory");
                                    var Time = new Stopwatch();
                                    Time.Start();
                                    var Search = _Table.Find(x => string.IsNullOrEmpty(x.Story));
                                    Loger.Instance.ServerInfo("主机", $"搜索MiMiStory开始{Time.ElapsedMilliseconds}ms");
                                    // Loger.Instance.ServerInfo("数据库", $"搜索GetNullStory命令返回结果{Search.Count()}条");
                                    foreach (var item in Search)
                                    {
                                        await session.SendBinaryAsync(item.ToByte());
                                    }
                                    Loger.Instance.ServerInfo("主机", $"搜索MiMiStory完毕{Time.ElapsedMilliseconds}ms");
                                }
                                catch (Exception)
                                {
                                }
                            }, Cancel.Token);
                        }
                        break;

                    case "GetStory":
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                var _Table = db.GetCollection<MiMiAiStory>("MiMiStory");
                                var Time = new Stopwatch();
                                Time.Start();
                                var Search = _Table.Find(x => x.Story.Contains(Data[0]));
                                if (Data[0] == "*")
                                    Search = _Table.FindAll();
                                Loger.Instance.ServerInfo("主机", $"搜索MiMiStory开始{Time.ElapsedMilliseconds}ms");
                                // Loger.Instance.ServerInfo("数据库", $"搜索GetNullStory命令返回结果{Search.Count()}条");
                                foreach (var item in Search)
                                {
                                    await session.SendBinaryAsync(item.ToByte());
                                }
                                Loger.Instance.ServerInfo("主机", $"搜索MiMiStory完毕{Time.ElapsedMilliseconds}ms");
                            }, Cancel.Token);
                        }
                        break;
                }
            }
            return Task.CompletedTask;
        }

        internal static BsonDocument GetWebInfoFromMiMi(string Code)
        {
            using (var db = new LiteDatabase(@"MiMi.db"))
            {
                var _Table = db.GetCollection("WebPage");
                var F = _Table.Find(x => x["_id"] == Code);
            }
            return null;
        }

        internal static bool GetWebInfoFromJav(string Date)
        {
            using (var db = new LiteDatabase(@"Jav.db"))
            {
                var _Table = db.GetCollection("WebPage");
                if (_Table.Exists(x => x["_id"] == Date))
                {
                    var Ret = _Table.FindOne(X => X["_id"] == Date);
                    if (Ret["Status"] == "True")
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool GetOrSaveWebInfoFromJav(string Code)
        {
            using (var db = new LiteDatabase(@"Jav.db"))
            {
                var _Table = db.GetCollection("WebPage");
                if (_Table.Exists(x => x["_id"] == Code))
                {
                    var Ret = _Table.FindOne(X => X["_id"] == Code);
                    if (Ret["Status"] == "True")
                    {
                        return true;
                    }
                    else
                    {
                        Ret["Status"] = bool.TrueString;
                        _Table.Upsert(Ret);
                    }
                }
                else
                {
                    _Table.Upsert(new BsonDocument
                    {
                        ["_id"] = Code,
                        ["Status"] = bool.FalseString
                    });
                }
            }
            return false;
        }

        internal static dynamic GetDataFromT66y(string Code, string search = "", Cowboy.WebSockets.AsyncWebSocketSession session = null, CancellationTokenSource Cancel = null, params string[] Data)
        {
            // var T66yDB = db.GetCollection<T66yData>("T66yData"); var T66yDB = db.GetCollection<T66yImgData>("ImgData");
            //using (var db = new LiteDatabase(@"T66y.db"))
            using (var db = new LiteDatabase("Filename=T66y.db;Connection=Shared;ReadOnly=True"))
            {
                //var db = Setting.T66yDB;

                switch (Code)
                {
                    case "GetImgFromDate":
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                try
                                {
                                    Loger.Instance.ServerInfo("主机", $"搜索T66y图片{search}");
                                    var _Table = db.GetCollection<T66yImgData>("ImgData");
                                    var Time = new Stopwatch();
                                    Time.Start();
                                    var Search = _Table.Find(x => x.Date == search);
                                    foreach (var item in Search)
                                    {
                                        await session.SendBinaryAsync(item.ToByte());
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }, Cancel.Token);
                        }
                        break;

                    case "GetTotalImg":
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                try
                                {
                                    Loger.Instance.ServerInfo("主机", $"搜索T66y图片{Code}");
                                    var _Table = db.GetCollection<T66yImgData>("ImgData");
                                    var Time = new Stopwatch();
                                    Time.Start();
                                    var Search = _Table.FindAll();
                                    foreach (var item in Search)
                                    {
                                        await session.SendBinaryAsync(item.ToByte());
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }, Cancel.Token);
                        }
                        break;

                    case "GetImgFromId":
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                try
                                {
                                    Loger.Instance.ServerInfo("主机", $"搜索T66y图片{search}");
                                    var _Table = db.GetCollection<T66yImgData>("ImgData");
                                    var Time = new Stopwatch();
                                    Time.Start();
                                    var Search = _Table.Find(x => x.id == search);
                                    foreach (var item in Search)
                                    {
                                        await session.SendBinaryAsync(item.ToByte());
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }, Cancel.Token);
                        }
                        break;

                    case "GetDataFromDate":
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                Loger.Instance.ServerInfo("主机", $"搜索T66y数据{search}");
                                var _Table = db.GetCollection<T66yData>("T66yData");
                                var Time = new Stopwatch();
                                Time.Start();
                                var Search = _Table.Find(x => x.Date == search);
                                foreach (var item in Search)
                                {
                                    await session.SendBinaryAsync(item.ToByte());
                                }
                            }, Cancel.Token);
                        }
                        break;

                    case "GetDataFromID":
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                Loger.Instance.ServerInfo("主机", $"搜索T66y数据{search}");
                                var _Table = db.GetCollection<T66yData>("T66yData");
                                var Time = new Stopwatch();
                                Time.Start();
                                var Search = _Table.Find(x => x.id == int.Parse(search));
                                foreach (var item in Search)
                                {
                                    await session.SendBinaryAsync(item.ToByte());
                                }
                            }, Cancel.Token);
                        }
                        break;

                    case "GetDataFromName":
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                Loger.Instance.ServerInfo("主机", $"搜索T66y数据{search}");
                                var _Table = db.GetCollection<T66yData>("T66yData");
                                var Time = new Stopwatch();
                                Time.Start();
                                var Search = _Table.Find(x => x.Title.Contains(search));
                                foreach (var item in Search)
                                {
                                    await session.SendBinaryAsync(item.ToByte());
                                }
                            }, Cancel.Token);
                        }
                        break;
                }
            }
            return Task.CompletedTask;
        }

        internal static dynamic GetDataFromSIS(string Code, string search = "", Cowboy.WebSockets.AsyncWebSocketSession session = null, CancellationTokenSource Cancel = null, params string[] Data)
        {
            // var T66yDB = db.GetCollection<T66yData>("T66yData"); var T66yDB = db.GetCollection<T66yImgData>("ImgData");
            using (var db = new LiteDatabase(@"SIS.db"))
            {
                switch (Code)
                {
                    case "GetImgFromDate":
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                try
                                {
                                    Loger.Instance.ServerInfo("主机", $"搜索SIS图片{search}");
                                    var _Table = db.GetCollection<SISImgData>("ImgData");
                                    var Search = _Table.Find(x => x.Date == search);
                                    foreach (var item in Search)
                                    {
                                        await session.SendBinaryAsync(item.ToByte());
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }, Cancel.Token);
                        }
                        break;

                    case "GetTotalImg":
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                try
                                {
                                    Loger.Instance.ServerInfo("主机", $"搜索SIS图片{Code}");
                                    var _Table = db.GetCollection<SISImgData>("ImgData");
                                    var Search = _Table.FindAll();
                                    foreach (var item in Search)
                                    {
                                        await session.SendBinaryAsync(item.ToByte());
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }, Cancel.Token);
                        }
                        break;

                    case "GetImgFromId":
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                try
                                {
                                    Loger.Instance.ServerInfo("主机", $"搜索SIS图片{search}");
                                    var _Table = db.GetCollection<SISImgData>("ImgData");
                                    var Search = _Table.Find(x => x.id == search);
                                    foreach (var item in Search)
                                    {
                                        await session.SendBinaryAsync(item.ToByte());
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }, Cancel.Token);
                        }
                        break;

                    case "GetDataFromDate":
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                Loger.Instance.ServerInfo("主机", $"搜索SIS数据{search}");
                                var _Table = db.GetCollection<SISData>("SISData");
                                var Search = _Table.Find(x => x.Date == search);
                                foreach (var item in Search)
                                {
                                    await session.SendBinaryAsync(item.ToByte());
                                }
                            }, Cancel.Token);
                        }
                        break;

                    case "GetDataFromID":
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                Loger.Instance.ServerInfo("主机", $"搜索SIS数据{search}");
                                var _Table = db.GetCollection<SISData>("SISData");
                                var Search = _Table.Find(x => x.id == int.Parse(search));
                                foreach (var item in Search)
                                {
                                    await session.SendBinaryAsync(item.ToByte());
                                }
                            }, Cancel.Token);
                        }
                        break;

                    case "GetDataFromName":
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                Loger.Instance.ServerInfo("主机", $"搜索SIS数据{search}");
                                var _Table = db.GetCollection<SISData>("SISData");
                                var Search = _Table.Find(x => x.Title.Contains(search));
                                foreach (var item in Search)
                                {
                                    await session.SendBinaryAsync(item.ToByte());
                                }
                            }, Cancel.Token);
                        }
                        break;
                }
            }
            return Task.CompletedTask;
        }

        internal static dynamic GetDataFromT66y(string Code = "", string search = "")
        {
            //using (var db = new LiteDatabase(@"T66y.db"))
            using (var db = new LiteDatabase("Filename=T66y.db;Connection=Shared;ReadOnly=True"))
            {
                //var db = Setting.T66yDB;

                switch (Code)
                {
                    case "CheckT66yExists":
                        {
                            var T66yDb = db.GetCollection<T66yData>("T66yData");
                            return T66yDb.Exists(x => x.id == int.Parse(search));
                        }
                        break;

                    case "img":
                        {
                            var T66yImgDb = db.GetCollection<T66yImgData>("ImgData");
                            if (!string.IsNullOrEmpty(search))
                            {
                                if (T66yImgDb.Exists(x => x.id == search))
                                {
                                    return T66yImgDb.FindOne(x => x.id == search);
                                }
                                else
                                {
                                    return null;
                                }
                            }
                            else
                            {
                                if (T66yImgDb.Exists(x => x.Status == false))
                                {
                                    return T66yImgDb.Find(x => x.Status == false).ToList(); ;
                                    return T66yImgDb.Find(x => x.Status == false);
                                }
                                else
                                {
                                    return null;
                                }
                            }
                        }
                    default:
                        {
                            var T66yDb = db.GetCollection<T66yData>("T66yData");
                            return T66yDb.Find(x => x.Status == false);
                        }
                }
            }
        }

        internal static dynamic GetDataFromSIS(string Code = "", string search = "")
        {
            using (var db = new LiteDatabase(@"SIS.db"))
            {
                switch (Code)
                {
                    case "CheckSISExists":
                        {
                            var Db = db.GetCollection<SISData>("SISData");
                            return Db.Exists(x => x.id == int.Parse(search));
                        }
                        break;

                    case "img":
                        {
                            var ImgDb = db.GetCollection<T66yImgData>("ImgData");
                            if (!string.IsNullOrEmpty(search))
                            {
                                if (ImgDb.Exists(x => x.id == search))
                                {
                                    return ImgDb.FindOne(x => x.id == search);
                                }
                                else
                                {
                                    return null;
                                }
                            }
                            else
                            {
                                if (ImgDb.Exists(x => x.Status == false))
                                {
                                    return ImgDb.Find(x => x.Status == false);
                                }
                                else
                                {
                                    return null;
                                }
                            }
                        }
                    default:
                        {
                            var T66yDb = db.GetCollection<T66yData>("T66yData");
                            return T66yDb.Find(x => x.Status == false);
                        }
                }
            }
        }

        #endregion 数据库查找

        #region 保存到数据库

        internal static void SaveToDataBaseRange(ICollection<NyaaInfo> Data, int Page, bool Mode = false)
        {
            Stopwatch Time = new Stopwatch();
            Loger.Instance.WithTimeStart("数据库保存中", Time);
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                var NyaaDB = db.GetCollection<NyaaInfo>("NyaaDB");
                try
                {
                    NyaaDB.InsertBulk(Data);
                }
                catch (LiteException e)
                {
                    Loger.Instance.LocalInfo("集群添加失败，转入单独添加");
                    foreach (var VARIABLE in Data)
                    {
                        try
                        {
                            NyaaDB.Upsert(VARIABLE);
                        }
                        catch (LiteException ex)
                        {
                            Loger.Instance.LocalInfo($"单独添加失败失败原因{ex}");
                        }
                    }
                }

                db.GetCollection<DateRecord>("DateRecord")
                    .Upsert(new DateRecord { _id = Data.ElementAt(0).Day, Status = Mode, Page = Page });
            }

            Loger.Instance.WithTimeStop("数据库操作完毕", Time);
        }

        internal static void ChangeJavActress()
        {
            bool CompareChar(string c) => (char.Parse(c) >= 'A' && char.Parse(c) <= 'Z');
            using (var db = new LiteDatabase(@"Jav.db"))
            {
                var JavDB = db.GetCollection<JavInfo>("JavDB");
                {
                    Loger.Instance.ServerInfo("Jav", $"启动命名切换");
                    var DateNow = "";
                    Stopwatch TimeCount = new Stopwatch();
                    TimeCount.Start();
                    // foreach (var item in JavDB.FindAll())
                    foreach (var item in JavDB.Find(x => x.Date == "19-07-26"))
                    {
                        if (DateNow != item.Date)
                        {
                            if (!string.IsNullOrEmpty(DateNow))
                            {
                                Loger.Instance.ServerInfo("Jav", $"当前处理日期{DateNow},耗时{TimeCount.ElapsedMilliseconds / 1000}秒");
                                TimeCount.Restart();
                            }
                            DateNow = item.Date;
                        }
                        if (item.Actress.Length == 1 && item.Actress[0] != null)
                        {
                            var SaveS = new List<string>();
                            var TempS = new StringBuilder();
                            foreach (var item1 in item.Actress[0].Replace(" ", "").Select(x => x.ToString()))
                            {
                                if (CompareChar(item1))
                                {
                                    if (TempS.Length != 0)
                                        SaveS.Add(TempS.ToString());
                                    TempS.Clear();
                                    TempS.Append(item1);
                                }
                                else
                                {
                                    TempS.Append(item1);
                                }
                            }
                            SaveS.Add(TempS.ToString());
                            var SaveS2 = new List<string>();
                            TempS.Clear();
                            var Flag = false;//单个单词组合用
                            foreach (var item2 in SaveS)
                            {
                                if (item2.Length > 1)
                                {
                                    if (Flag)
                                    {
                                        SaveS2.Add(TempS.ToString());
                                        TempS.Clear();
                                        Flag = false;
                                    }
                                    if (TempS.Length == 0)
                                    {
                                        TempS.Append(item2);
                                    }
                                    else
                                    {
                                        TempS.Append($" {item2}");
                                        SaveS2.Add(TempS.ToString());
                                        TempS.Clear();
                                    }
                                }
                                else if (item2.Length == 1)
                                {
                                    TempS.Append(item2);
                                    Flag = true;
                                }
                            }
                            if (Flag && TempS.Length != 0)
                            {
                                SaveS2.Add(TempS.ToString());
                            }
                        }
                    }
                }
            }
        }

        internal static void SaveToDataBaseOneByOne(ICollection<NyaaInfo> Data, int Page, bool Mode = false)
        {
            Stopwatch Time = new Stopwatch();
            Loger.Instance.WithTimeStart("数据库保存中", Time);
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                db.GetCollection<DateRecord>("DateRecord")
                    .Upsert(new DateRecord { _id = Data.ElementAt(0).Day, Status = false });
                var NyaaDB = db.GetCollection<NyaaInfo>("NyaaDB");
                foreach (var VARIABLE in Data)
                {
                    try
                    {
                        NyaaDB.Upsert(VARIABLE);
                    }
                    catch (Exception e)
                    {
                        Loger.Instance.LocalInfo(e);
                    }

                    db.GetCollection<DateRecord>("DateRecord")
                        .Upsert(new DateRecord { _id = Data.ElementAt(0).Day, Status = Mode, Page = Page });
                }
            }
            Loger.Instance.WithTimeStop("数据库操作完毕", Time);
        }

        internal static bool SaveToJavDataBaseOneObject(JavInfo item2)
        {
            using (var db = new LiteDatabase(@"Jav.db"))
            {
                try
                {
                    var JavDB = db.GetCollection<JavInfo>("JavDB");
                    /*if (JavDB.Update(item2))
                    {
                        return true;
                    }
                    JavDB.Insert(item2);
                    return false;*/
                    return JavDB.Upsert(item2);
                }
                catch (Exception ex)
                {
                    Loger.Instance.LocalInfo($"Jav添加到数据库失败,失败原因{ex.Message}");
                }
                return false;
            }
        }

        internal static void SaveToJavDataBaseRange(ICollection<JavInfo> Collect)
        {
            using (var db = new LiteDatabase(@"Jav.db"))
            {
                try
                {
                    var JavDB = db.GetCollection<JavInfo>("JavDB");
                    JavDB.InsertBulk(Collect);
                }
                catch (Exception)
                {
                    SaveToJavDataBaseOneByOne(Collect);
                }
            }
        }

        internal static bool SaveToNyaaDataBaseRange(ICollection<NyaaInfo> Collect)
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                try
                {
                    var NyaaDB = db.GetCollection<NyaaInfo>("NyaaDB");
                    NyaaDB.InsertBulk(Collect);
                    return true;
                }
                catch (Exception)
                {
                }
            }
            return false;
        }

        internal static void SaveToNyaaDateInfo(DateRecord Date)
        {
            if (string.IsNullOrWhiteSpace(Date._id)) return;
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                try
                {
                    var NyaaDB = db.GetCollection<DateRecord>("DateRecord");
                    NyaaDB.Upsert(Date);
                }
                catch (Exception)
                {
                }
            }
        }

        internal static bool SaveToNyaaDataBaseOneObject(NyaaInfo item2, bool Mode = true)
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                try
                {
                    var NyaaDB = db.GetCollection<NyaaInfo>("NyaaDB");
                    if (Mode)
                    {
                        if (!NyaaDB.Update(item2))
                        {
                            NyaaDB.Insert(item2);
                            return true;
                        }
                    }
                    else
                    {
                        NyaaDB.Upsert(item2);
                    }
                    /* if (!NyaaDB.Exists(x => x.Timestamp == item2.Timestamp))
                     {
                         NyaaDB.Insert(item2);
                         return true;
                     }
                     else if (!NyaaDB.Exists(x => x.Url == item2.Url))
                     {
                         NyaaDB.Insert(item2);
                         return true;
                     }*/
                }
                catch (Exception)
                {
                }
                return false;
            }
        }

        internal static void SaveToJavDataBaseOneByOne(ICollection<JavInfo> Collect)
        {
            using (var db = new LiteDatabase(@"Jav.db"))
            {
                var JavDB = db.GetCollection<JavInfo>("JavDB");
                foreach (var item in Collect)
                {
                    try
                    {
                        JavDB.Upsert(item);
                    }
                    catch (Exception e)
                    {
                        Loger.Instance.LocalInfo(e);
                    }
                }
            }
        }

        internal static void SavePage(string Page)
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                db.GetCollection("WebPage")
                    .Upsert(new BsonDocument { ["_id"] = ObjectId.NewObjectId(), ["Page"] = Encoding.Unicode.GetBytes(Page) });
            }
        }

        internal static bool SaveToMiMiDataTablet(string[] tempData, bool UseSave = true)
        {
            using (var db = new LiteDatabase(@"MiMi.db"))
            {
                var _Table = db.GetCollection("WebPage");
                if (!_Table.Exists(X => X["Uri"] == tempData[0]))//以链接检查是否存在，不存在则进入
                {
                    if (UseSave)
                    {
                        _Table.Upsert(new BsonDocument { ["_id"] = DateTime.Parse(tempData[3]), ["Title"] = tempData[1], ["Uri"] = tempData[0], ["Status"] = bool.Parse(tempData[4]) });
                        db.Dispose();
                    }

                    return true;//返回True表示不存在 要添加
                }

                if (UseSave)
                {
                    _Table.Upsert(new BsonDocument { ["_id"] = DateTime.Parse(tempData[3]), ["Title"] = tempData[1], ["Uri"] = tempData[0], ["Status"] = bool.Parse(tempData[4]) });
                }
                var Date = DateTime.Parse(tempData[3]).ToString("yyyy-MM-dd");
                var MiMiDb = db.GetCollection<MiMiAiData>("MiMiDB");
                var Find = MiMiDb.FindOne(x => x.Date == Date);
                if (Find == null) return true;
                if (Find.InfoList.Count == 0) return true;
                return false;
                /*   else if (tempData.Length == 2)
                    {
                        var Ret = _Table.FindById(tempData[0]);
                        if (Ret != null)
                        {
                            Ret["Status"] = bool.Parse(tempData[1]);
                            _Table.Update(Ret);
                        }
                    }*/
            }
        }

        internal static void SaveToMiMiDataTablet(BsonDocument tempData)
        {
            using (var db = new LiteDatabase(@"MiMi.db"))
            {
                var _Table = db.GetCollection("WebPage");
                _Table.Upsert(tempData);
            }
        }

        internal static void SaveToMiMiDataUnit(ICollection<MiMiAiData> Data)
        {
            using (var db = new LiteDatabase(@"MiMi.db"))
            {
                var MiMiDb = db.GetCollection<MiMiAiData>("MiMiDB");
                try
                {
                    MiMiDb.InsertBulk(Data);
                }
                catch (LiteException e)
                {
                    Loger.Instance.LocalInfo("集群添加失败，转入单独添加");
                    foreach (var VARIABLE in Data)
                    {
                        try
                        {
                            MiMiDb.Upsert(VARIABLE);
                        }
                        catch (LiteException ex)
                        {
                            Loger.Instance.LocalInfo($"单独添加失败失败原因{ex.Message}");
                        }
                    }
                }
                db.Dispose();
            }
        }

        internal static bool SaveToMiMiStoryDataUnit(ICollection<MiMiAiStory> Data = null, MiMiAiStory UnitData = null)
        {
            using (var db = new LiteDatabase(@"MiMi.db"))
            {
                var MiMiDb = db.GetCollection<MiMiAiStory>("MiMiStory");
                if (UnitData != null)
                {
                    return MiMiDb.Upsert(UnitData);
                }
                else
                {
                    try
                    {
                        MiMiDb.InsertBulk(Data);
                        return true;
                    }
                    catch (LiteException e)
                    {
                        Loger.Instance.LocalInfo("集群添加失败，转入单独添加");
                        foreach (var VARIABLE in Data)
                        {
                            try
                            {
                                MiMiDb.Upsert(VARIABLE);
                            }
                            catch (LiteException ex)
                            {
                                Loger.Instance.LocalInfo($"单独添加失败失败原因{ex.Message}");
                            }
                        }
                        return false;
                    }
                }
            }
        }

        internal static bool T66yWriteIng = false;

        internal static bool SaveToT66yDataUnit(string Collection, ICollection<T66yData> Data = null, T66yData UnitData = null)
        {
            T66yWriteIng = true;
            var Fin = false;
            using (var db = new LiteDatabase(@"T66y.db"))
            {
                //var db = Setting.T66yDB;

                var T66yDb = db.GetCollection<T66yData>(Collection);
                if (UnitData != null)
                {
                    try
                    {
                        Fin = T66yDb.Upsert(UnitData);
                    }
                    catch (Exception ex)
                    {
                        Loger.Instance.LocalInfo($"T66y数据库添加失败{ex.Message}");

                        Fin = false;
                        // return T66yDb.Update(UnitData);
                    }
                }
                else
                {
                    try
                    {
                        T66yDb.InsertBulk(Data);
                        Fin = true;
                    }
                    catch (LiteException e)
                    {
                        Loger.Instance.LocalInfo("集群添加失败，转入单独添加");
                        foreach (var VARIABLE in Data)
                        {
                            try
                            {
                                Fin = T66yDb.Upsert(VARIABLE);
                            }
                            catch (LiteException ex)
                            {
                                Loger.Instance.LocalInfo($"单独添加失败失败原因{ex.Message}");
                            }
                        }
                        Fin = false;
                    }
                }
            }
            T66yWriteIng = false;
            return Fin;
        }

        internal static bool SaveToT66yDataUnit(ICollection<T66yImgData> Data = null, T66yImgData UnitData = null, bool Update = false)
        {
            T66yWriteIng = true;
            var Fin = false;

            using (var db = new LiteDatabase(@"T66y.db"))
            {
                //var db = Setting.T66yDB;
                var T66yDb = db.GetCollection<T66yImgData>("ImgData");
                if (UnitData != null)
                {
                    /* if (Update)
                     {
                         T66yDb.Delete(x => x.id == UnitData.id);
                         T66yDb.Insert(UnitData);
                         var FO = T66yDb.FindOne(x => x.id == UnitData.id);
                         db.Dispose();
                     }
                     else*/

                    Fin = T66yDb.Upsert(UnitData);
                }
                else
                {
                    try
                    {
                        if (Data == null)
                        {
                            return false;
                        }
                        T66yDb.InsertBulk(Data);
                        Fin = true;
                    }
                    catch (LiteException e)
                    {
                        Loger.Instance.LocalInfo("集群添加失败，转入单独添加");
                        foreach (var VARIABLE in Data)
                        {
                            try
                            {
                                Fin = T66yDb.Upsert(VARIABLE);
                            }
                            catch (LiteException ex)
                            {
                                Loger.Instance.LocalInfo($"单独添加失败失败原因{ex.Message}");
                            }
                        }
                        Fin = false;
                    }
                }
            }
            T66yWriteIng = false;
            return Fin;
        }

        internal static bool SaveToSISDataUnit(string Collection, dynamic UnitData = null)
        {
            var Ret = false;
            using (var db = new LiteDatabase(@"SIS.db"))
            {
                switch (Collection)
                {
                    case "SISData":
                        {
                            var STSDB = db.GetCollection<SISData>("SISData");
                            if (UnitData != null)
                            {
                                Ret = STSDB.Upsert(UnitData);
                            }
                        }
                        break;

                    case "img":
                        {
                            var STSDB = db.GetCollection<SISImgData>("ImgData");
                            Ret = STSDB.Upsert(UnitData);
                        }
                        break;
                }
            }
            return Ret;
        }

        internal static void SaveToMiMiDataUnit(MiMiAiData Data)
        {
            using (var db = new LiteDatabase(@"MiMi.db"))
            {
                var MiMiDb = db.GetCollection<MiMiAiData>("MiMiDB");
                try
                {
                    MiMiDb.Upsert(Data);
                }
                catch (LiteException ex)
                {
                    Loger.Instance.LocalInfo($"单独添加失败失败原因{ex.Message}");
                }
            }
        }

        internal static void SaveToMiMiDataErrorUnit(string[] ErrorInfo)
        {
            using (var db = new LiteDatabase(@"MiMi.db"))
            {
                var MiMiDb = db.GetCollection("Error");
                try
                {
                    MiMiDb.Upsert(new BsonDocument
                    {
                        ["_id"] = ObjectId.NewObjectId().CreationTime.ToString("MM-dd HH:mm:ss:ff"),
                        ["Date"] = ErrorInfo[0],
                        ["UnitIndex"] = ErrorInfo[1],
                        ["ListIndex"] = ErrorInfo[2],
                        ["Type"] = ErrorInfo[3],
                        ["Uri"] = ErrorInfo[4],
                        ["ErrorInfo"] = ErrorInfo[5],
                        ["Status"] = bool.Parse(ErrorInfo[6])
                    });
                }
                catch (LiteException ex)
                {
                    Loger.Instance.LocalInfo($"错误信息添加失败,失败原因{ex.Message}");
                }
            }
        }

        #endregion 保存到数据库
    }
}