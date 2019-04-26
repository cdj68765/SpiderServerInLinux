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
            Loger.Instance.LocalInfo("创建或者打开Nyaa数据库");
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                if (!db.CollectionExists("NyaaDB"))
                {
                    Loger.Instance.LocalInfo("正在创建Nyaa表");
                    var DateRecord = db.GetCollection<DateRecord>("DateRecord");
                    DateRecord.EnsureIndex(X => X._id);
                    var NyaaDB = db.GetCollection<NyaaInfo>("NyaaDB");
                    NyaaDB.EnsureIndex(x => x.Catagory);
                    NyaaDB.EnsureIndex(x => x.Date);
                    NyaaDB.EnsureIndex(x => x.id);
                    // NyaaDB.EnsureIndex(x => x.Title);
                    Loger.Instance.LocalInfo("创建Nyaa表成功");
                }
                else
                {
                    Loger.Instance.LocalInfo("查找Nyaa表成功");
                }
            }
        }

        internal static void InitJavDataBase()
        {
            Loger.Instance.LocalInfo("创建或者打开Jav数据库");
            using (var db = new LiteDatabase(@"Jav.db"))
            {
                if (!db.CollectionExists("JavDB"))
                {
                    var DateRecord = db.GetCollection<DateRecord>("DateRecord");
                    DateRecord.EnsureIndex(X => X._id);
                    Loger.Instance.LocalInfo("插入基础信息到数据库");
                    var NyaaDB = db.GetCollection<JavInfo>("JavDB");
                    NyaaDB.EnsureIndex(x => x.id);
                    NyaaDB.EnsureIndex(x => x.Date);
                    NyaaDB.EnsureIndex(x => x.Size);
                    Loger.Instance.LocalInfo("创建JAV数据库成功");
                }
                else
                {
                    Loger.Instance.LocalInfo("查找Jav表成功");
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
                    .Insert(new BsonDocument { ["_id"] = ObjectId.NewObjectId(), ["Page"] = Encoding.Unicode.GetBytes(Page) });
            }
        }

        #endregion 保存到数据库
    }
}