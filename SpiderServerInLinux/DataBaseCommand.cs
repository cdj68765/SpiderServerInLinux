using System;
using LiteDB;

namespace SpiderServerInLinux
{
    internal class DataBaseCommand
    {
        public class Setting
        {
            public string Url { get; set; }
        }
        internal static DataBaseCommand Init()
        {
            // 打开数据库 (如果不存在自动创建)
            using (var db = new LiteDatabase(@"Nyaa.db"))
            {
                var col = db.GetCollection<string>("Setting");
                col.Upsert(1,"pc");
                var results = col.Find(x => x.StartsWith("Jo"));
                col.EnsureIndex("Name");
                // 现在，搜索你的文档
                var customer = col.Find(x=>x.StartsWith("pc"));
            }
            return new DataBaseCommand();
        }
    }
}