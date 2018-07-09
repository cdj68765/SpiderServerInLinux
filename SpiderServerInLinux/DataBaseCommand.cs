using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LiteDB;
namespace SpiderServerInLinux
{
    internal static class DataBaseCommand
    {
        static readonly Stopwatch Time = new Stopwatch();
        internal static void Init()
        {
           
            Loger.Instance.WithTimeStart("创建或者打开数据库", Time);
            // 打开数据库 (如果不存在自动创建) 
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                Loger.Instance.WithTimeRestart("创建或者打开完毕",Time);
                var SettingData = db.GetCollection<GlobalSet>("Setting");
                var FindAdress = SettingData.FindOne(id => id._id == "Address");
                Loger.Instance.WithTimeRestart("查找历史信息", Time);
                if (!(FindAdress is GlobalSet))
                {
                    Loger.Instance.WithTimeRestart("查找失败，正在创建", Time);
                    Setting.Address = "https://sukebei.nyaa.si/";
                    SettingData.Upsert(new GlobalSet {_id = "Address", Value = Setting.Address});
                    Loger.Instance.WithTimeRestart("插入地址信息", Time);
                    SettingData.Upsert(new GlobalSet {_id = "LastCount", Value = "" + "1"});
                    Loger.Instance.WithTimeRestart("插入枚举数", Time);
                    var DateRecord = db.GetCollection<DateRecord>("DateRecord");
                    DateRecord.EnsureIndex(X => X._id);
                    Loger.Instance.WithTimeRestart("插入基础信息到数据库", Time);
                    var NyaaDB = db.GetCollection<TorrentInfo>("NyaaDB");
                    NyaaDB.EnsureIndex(x => x.Catagory);
                    NyaaDB.EnsureIndex(x => x.Date);
                    NyaaDB.EnsureIndex(x => x.id);
                    NyaaDB.EnsureIndex(x => x.Title);
                    Loger.Instance.WithTimeRestart("插入网站基础信息到数据库", Time);
                    Loger.Instance.WithTimeStop("创建成功", Time);
                }
                else
                {
                    Loger.Instance.WithTimeStop("查找成功", Time);
                    Setting.Address = FindAdress.Value;
                }

                Setting.LastPageIndex = int.Parse(SettingData.FindOne(id => id._id == "LastCount").Value);
            }
        }

        #region 从数据库读取

        internal static DateRecord GetDateInfo(string Date)
        {
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

        #endregion

        #region 数据库查找

        internal static void GetDataFromDataBase()
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                var NyaaDB = db.GetCollection<TorrentInfo>("NyaaDB");
                //var FindAdress = NyaaDB.Find(x=>x.== "Address");
            }
        }

        #endregion

        #region 保存到数据库

        internal static void SaveToDataBaseRange(ICollection<TorrentInfo> Data, int Page, bool Mode = false)
        {
            Loger.Instance.WithTimeStart("数据库保存中", Time);
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                var NyaaDB = db.GetCollection<TorrentInfo>("NyaaDB");
                NyaaDB.InsertBulk(Data);
                db.GetCollection<DateRecord>("DateRecord")
                    .Upsert(new DateRecord {_id = Data.ElementAt(0).Day, Status = Mode, Page = Page});
            }
            Loger.Instance.WithTimeStop("数据库完毕", Time);
        }

        internal static void SaveToDataBaseOneByOne(ICollection<TorrentInfo> Data, int Page, bool Mode = false)
        {
            Loger.Instance.WithTimeStart("数据库保存中", Time);
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                db.GetCollection<DateRecord>("DateRecord")
                    .Upsert(new DateRecord {_id = Data.ElementAt(0).Day, Status = false});
                var NyaaDB = db.GetCollection<TorrentInfo>("NyaaDB");
                foreach (var VARIABLE in Data) NyaaDB.Upsert(VARIABLE);
                db.GetCollection<DateRecord>("DateRecord")
                    .Upsert(new DateRecord {_id = Data.ElementAt(0).Day, Status = Mode, Page = Page});
            }
            Loger.Instance.WithTimeStop("数据库完毕", Time);
        }

        internal static void SaveStatus()
        {
            Loger.Instance.WithTimeStart("数据库保存中", Time);
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                db.GetCollection<GlobalSet>("Setting")
                    .Upsert(new GlobalSet {_id = "LastCount", Value = Setting.LastPageIndex.ToString()});
            }
            Loger.Instance.WithTimeStop("数据库完毕", Time);
        }

        #endregion
    }
}