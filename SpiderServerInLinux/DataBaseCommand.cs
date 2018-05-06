using System;
using System.Collections.Generic;
using LiteDB;
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
                var FindAdress = SettingData.FindOne(id => id.Item == "Address");
                if (!(FindAdress is GlobalSet))
                {
                    setting.Address = "https://sukebei.nyaa.si/";
                    SettingData.Upsert(new GlobalSet() {Item = "Address", Value = setting.Address});
                    SettingData.Upsert(new GlobalSet() {Item = "LastCount", Value = "1"});

                    var DateRecord = db.GetCollection("DateRecord");

                    var customer = new BsonDocument() {["_id"] = "2017", ["Status"] = false};
                    DateRecord.EnsureIndex("_id");
                    // DateRecord.Insert(new BsonDocument() { ["_id"] = "2017", ["Status"] = false });

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

                setting.LastPageIndex = int.Parse(SettingData.FindOne(id => id.Item == "LastCount").Value);
            }
        }

        internal static void SaveToDataBaseFormList(ICollection<TorrentInfo> Data,string Day)
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                var NyaaDB = db.GetCollection<TorrentInfo>("NyaaDB");
                NyaaDB.InsertBulk(Data);
            }
        }

        internal static DateRecord GetDateInfo(string Date)
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                setting = new Setting();
                var DateRecord = db.GetCollection<DateRecord>("DateRecord");
                var FindData = DateRecord.FindOne(Dt => Convert.ToDateTime(Dt).ToShortDateString() == Date);
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
        internal static void SaveToDataOneByOne(ICollection<TorrentInfo> Data,string Day)
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {

                var NyaaDB = db.GetCollection<TorrentInfo>("NyaaDB");
                foreach (var VARIABLE in Data)
                {
                    NyaaDB.Upsert(VARIABLE.id, VARIABLE);
                }
                setting = new Setting();
                var SettingData = db.GetCollection<DateRecord>("DateRecord");
                var FindAdress = SettingData.FindOne(Dt => Convert.ToDateTime(Dt).ToShortDateString() == Day);
                if (FindAdress is DateRecord)
                {

                }
            }
        }
    }
}