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
                }
                else
                {
                    setting.Address = FindAdress.Value;
                }

                setting.LastPageIndex = int.Parse(SettingData.FindOne(id => id.Item == "LastCount").Value);
            }
        }

        internal static void SaveToDataBaseFormList(List<TorrentInfo> Data)
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                var SettingData = db.GetCollection<TorrentInfo>("NyaaDB");
                SettingData.InsertBulk(Data);
            }
        }

        internal static DateRecord GetDateInfo(string Date)
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                setting = new Setting();
                var SettingData = db.GetCollection<DateRecord>("DateRecord");
                var FindAdress = SettingData.FindOne(Dt => Convert.ToDateTime(Dt).ToShortDateString() == Date);
                if (FindAdress is DateRecord)
                {
                    return FindAdress;
                }

                return null;
            }
        }

        internal static void GetDataFormDataBase()
        {
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                var SettingData = db.GetCollection<TorrentInfo>("NyaaDB");
                var FindAdress = SettingData.FindAll();
            }
        }
    }
}