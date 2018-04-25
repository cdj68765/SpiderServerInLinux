using System;
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

namespace SpiderServerInLinux
{
    internal class HandlerHtml
    {
        public readonly List<TorrentInfo> AnalysisData = new List<TorrentInfo>();

        public HandlerHtml(string result)
        {
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
                    var Magnet = "";
                    if (TempData.Torrent.StartsWith("magnet"))
                    {
                        Magnet = TempData.Torrent;
                        TempData.Torrent = "";
                    }
                    else
                    {
                        TempData.Magnet = temp.SelectSingleNode("td[3]/a[2]").Attributes["href"].Value;
                    }

                    TempData.Size = temp.SelectSingleNode("td[4]").InnerText;
                    TempData.TimeStamp = int.Parse(temp.SelectSingleNode("td[5]").Attributes["data-timestamp"].Value);
                    TempData.Date = Convert.ToDateTime(temp.SelectSingleNode("td[5]").InnerText);
                    TempData.Up = temp.SelectSingleNode("td[6]").InnerText;
                    TempData.Leeches = temp.SelectSingleNode("td[7]").InnerText;
                    TempData.Complete = temp.SelectSingleNode("td[8]").InnerText;
                    AnalysisData.Add(TempData);
                }

            }

        }
    }
}