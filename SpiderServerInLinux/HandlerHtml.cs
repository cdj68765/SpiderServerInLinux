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
        public HandlerHtml(string result)
        {
            if (result != "")
            {
                var HtmlDoc = new HtmlAgilityPack.HtmlDocument();
                HtmlDoc.LoadHtml(result);
                foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[1]/div[2]/table/tbody/tr"))
                {
                    var temp = HtmlNode.CreateNode(item.OuterHtml);
                    var ItemClass = item.Attributes["class"].Value;
                    var ImgClass = temp.SelectSingleNode("td[1]/a").Attributes["title"]
                        .Value;
                    var Title = temp.SelectSingleNode("td[2]/a").Attributes["title"]
                        .Value;
                    var Torrent = temp.SelectSingleNode("td[3]/a[1]").Attributes["href"].Value;
                    var Magnet = "";
                    if (Torrent.StartsWith("magnet"))
                    {
                        Magnet = Torrent;
                        Torrent = "";
                    }
                    else
                    {
                        Magnet = temp.SelectSingleNode("td[3]/a[2]").Attributes["href"].Value;
                    }

                    var Size = temp.SelectSingleNode("td[4]").InnerText;
                    var TimeStamp = temp.SelectSingleNode("td[5]").Attributes["data-timestamp"].Value;
                    var Time = temp.SelectSingleNode("td[5]").InnerText;
                    var UP = temp.SelectSingleNode("td[6]").InnerText;
                    var Down = temp.SelectSingleNode("td[7]").InnerText;
                    var Completed = temp.SelectSingleNode("td[8]").InnerText;
                }

            }
        }
    }
}