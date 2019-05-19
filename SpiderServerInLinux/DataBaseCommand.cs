using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace SpiderServerInLinux
{
    internal static class DataBaseCommand
    {
        internal static void InitNyaaDataBase()
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

        internal static void InitJavDataBase()
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
                    Loger.Instance.LocalInfo("创建JAV数据库成功");
                }
                else
                {
                    Loger.Instance.LocalInfo("打开Jav数据库正常");
                }
            }
        }

        internal static void InitMiMiAiDataBase()
        {
            using (var db = new LiteDatabase(@"MiMi.db"))
            {
                if (!db.CollectionExists("MiMiDB"))
                {
                    Loger.Instance.LocalInfo("创建MiMiAi数据库数据库");
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

        #region 从数据库读取

        internal static DateRecord GetDateInfo(string Date)
        {
            Stopwatch Time = new Stopwatch();
            Loger.Instance.WithTimeStart("数据库读取中", Time);
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                var DateRecord = db.GetCollection<DateRecord>("DateRecord");
                var FindData = DateRecord.FindOne(Dt => Convert.ToDateTime(Dt._id).ToString("yyyy-MM-dd") == Date);
                if (FindData is DateRecord)
                {
                    Loger.Instance.WithTimeStart($"从数据库返回{Date}数据", Time);
                    return FindData;
                }

                Loger.Instance.WithTimeStop($"未在数据库找到{Date}数据", Time);
                return null;
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

        internal static dynamic GetDataFromMiMi(string Code)
        {
            using (var db = new LiteDatabase(@"MiMi.db"))
            {
                switch (Code)
                {
                    case "TabletInfo":
                        {
                            var _Table = db.GetCollection("WebPage");
                            return _Table.FindOne(Query.And(Query.All("_id", Query.Descending), Query.EQ("Status", false)));
                            //return _Table.FindOne(x => x["Status"] == false);
                        }
                    case "UnitInfo":
                        {
                            var _Table = db.GetCollection<MiMiAiData>("MiMiDB");
                            return _Table.FindOne(Query.And(Query.All("Date", Query.Descending), Query.EQ("Status", false)));
                        }
                }
            }
            return null;
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
                    if (!JavDB.Exists(x => x.id == item2.id))
                    {
                        JavDB.Insert(item2);
                        return true;
                    }
                }
                catch (Exception)
                {
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

        internal static bool SaveToMiMiDataTablet(string[] tempData)
        {
            using (var db = new LiteDatabase(@"MiMi.db"))
            {
                var _Table = db.GetCollection("WebPage");
                if (!_Table.Exists(X => X["Uri"] == tempData[0]))
                {
                    _Table.Upsert(new BsonDocument { ["_id"] = DateTime.Parse(tempData[3]), ["Title"] = tempData[1], ["Uri"] = tempData[0], ["Status"] = bool.Parse(tempData[4]) });
                    return true;
                }
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
            }
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