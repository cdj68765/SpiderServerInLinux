using System;
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
                var FindAdress = SettingData.FindOne(id => id.Item == "Adress");
                if(!(FindAdress is GlobalSet))
                {
                    setting.Address = "https://sukebei.nyaa.si/";
                    SettingData.Upsert(new GlobalSet() { Item = "Adress", Value = setting.Address });
                    SettingData.Upsert(new GlobalSet() { Item = "LastCount", Value = "1" });
                }
                else
                {
                    setting.Address = FindAdress.Value;
                }
                setting.LastPage=int.Parse(SettingData.FindOne(id => id.Item == "LastCount").Value);

            }
        }
    }
}