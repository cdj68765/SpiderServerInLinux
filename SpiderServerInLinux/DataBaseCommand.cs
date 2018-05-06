using System;
using System.Collections.Generic;
using LiteDB;
using System.Linq;
using static SpiderServerInLinux.Setting;

namespace SpiderServerInLinux
{
    internal class DataBaseCommand
    {
        internal static void Init()
        {
            // 打开数据库 (如果不存在自动创建) 
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                setting = new Setting();
                var SettingData = db.GetCollection<GlobalSet>("Setting");
                var FindAdress = SettingData.FindOne(id => id._id == "Address");
                if (!(FindAdress is GlobalSet))
                {
                    setting.Address = "https://sukebei.nyaa.si/";
                    SettingData.Upsert(new GlobalSet() { _id = "Address", Value = setting.Address });
                    SettingData.Upsert(new GlobalSet() { _id = "LastCount", Value = "1" });
                    var DateRecord = db.GetCollection<DateRecord>("DateRecord");
                    DateRecord.EnsureIndex(X => X._id);

                    var NyaaDB = db.GetCollection<TorrentInfo>("NyaaDB");
                    NyaaDB.EnsureIndex(x => x.Catagory);
                    NyaaDB.EnsureIndex(x => x.Date);
                    NyaaDB.EnsureIndex(x => x.id);
                    NyaaDB.EnsureIndex(x => x.Title);
                }
                else
                {
                    setting.Address = FindAdress.Value;
                }
                setting.LastPageIndex = int.Parse(SettingData.FindOne(id => id._id == "LastCount").Value);
            }
        }

        internal static void SaveToDataBaseRange(ICollection<TorrentInfo> Data ,int Page,bool Mode=false)
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                var NyaaDB = db.GetCollection<TorrentInfo>("NyaaDB");
                NyaaDB.InsertBulk(Data);
                db.GetCollection<DateRecord>("DateRecord").Upsert(new DateRecord() { _id = Data.ElementAt(0).Day, Status = Mode,Page=Page });
            }
        }

        internal static DateRecord GetDateInfo(string Date)
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                setting = new Setting();
                var DateRecord = db.GetCollection<DateRecord>("DateRecord");
                var FindData = DateRecord.FindOne(Dt => Convert.ToDateTime(Dt._id).ToShortDateString() == Date);
                if (FindData is DateRecord)
                {
                    return FindData;
                }
                return null;
            }
        }

        internal static void GetDataFormDataBase()
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                var NyaaDB = db.GetCollection<TorrentInfo>("NyaaDB");
                var FindAdress = NyaaDB.FindAll();
            }
        }
        internal static void SaveToDataBaseOneByOne(ICollection<TorrentInfo> Data,int Page,bool Mode=false)
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                db.GetCollection<DateRecord>("DateRecord").Upsert(new DateRecord() { _id = Data.ElementAt(0).Day, Status = false });
                var NyaaDB = db.GetCollection<TorrentInfo>("NyaaDB");
                foreach (var VARIABLE in Data)
                {
                    NyaaDB.Upsert(VARIABLE);
                }
                db.GetCollection<DateRecord>("DateRecord").Upsert(new DateRecord() { _id = Data.ElementAt(0).Day, Status = Mode ,Page=Page});
            }
        }
    }
}