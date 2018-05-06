using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using static SpiderServerInLinux.Setting;
using static SpiderServerInLinux.DataBaseCommand;

namespace SpiderServerInLinux
{
    internal class HandlerHtml
    {
        public ConcurrentDictionary<int, TorrentInfo> AnalysisData = null;
        public bool AddFin = true;
        public HandlerHtml(string result, ConcurrentDictionary<int, TorrentInfo> PreData = null,string Day=null)
        {
            if (PreData != null)
                AnalysisData = PreData;
            else
                AnalysisData = new ConcurrentDictionary<int, TorrentInfo>();

            if (result != "")
            {
                var HtmlDoc = new HtmlDocument();
                HtmlDoc.LoadHtml(result);
                foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[1]/div[2]/table/tbody/tr"))
                {
                    TorrentInfo TempData = new TorrentInfo();
                    var temp = HtmlNode.CreateNode(item.OuterHtml);
                    TempData.Class = item.Attributes["class"].Value;
                    TempData.Catagory = temp.SelectSingleNode("td[1]/a").Attributes["title"]
                        .Value;
                    TempData.Title = temp.SelectSingleNode("td[2]/a").Attributes["title"]
                        .Value;
                    TempData.Torrent = temp.SelectSingleNode("td[3]/a[1]").Attributes["href"].Value;
                    if (TempData.Torrent.StartsWith("magnet"))
                    {
                        TempData.Magnet = TempData.Torrent;
                        TempData.Torrent = "";
                    }
                    else
                    {
                        TempData.Magnet = temp.SelectSingleNode("td[3]/a[2]").Attributes["href"].Value;
                    }

                    TempData.Size = temp.SelectSingleNode("td[4]").InnerText;

                    TempData.id = int.Parse(temp.SelectSingleNode("td[5]").Attributes["data-timestamp"].Value);
                    TempData.Date = temp.SelectSingleNode("td[5]").InnerText;
                    TempData.Up = temp.SelectSingleNode("td[6]").InnerText;
                    TempData.Leeches = temp.SelectSingleNode("td[7]").InnerText;
                    TempData.Complete = temp.SelectSingleNode("td[8]").InnerText;
                    //用来判断是否下载完毕一整天的数据
                    if (Day!=null)
                    {
                        if (TempData.Day != Day)
                        {
                            AddFin = true;
                            break;
                        }
                    }
                    AnalysisData.AddOrUpdate(TempData.id, TempData, (key, Value) => TempData);
                }
                //SaveToDataBaseFormList(new List<TorrentInfo>(AnalysisData.Values));
            }

        }
    }
}