using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using HtmlAgilityPack;

namespace SpiderServerInLinux
{
    internal class HandlerHtml : IDisposable
    {
        public bool AddFin;
        int pageCount = 0;
        private string DateOfNow = string.Empty;
        public ConcurrentDictionary<int, TorrentInfo> AnalysisData;
        public ConcurrentDictionary<int, TorrentInfo> NextDayData;
        private static readonly Stopwatch Time = new Stopwatch();
        public HandlerHtml(string result, ConcurrentDictionary<int, TorrentInfo> PreData = null, string Day = null)
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
                    var TempData = new TorrentInfo();
                    var temp = HtmlNode.CreateNode(item.OuterHtml);
                    TempData.Class = item.Attributes["class"].Value;
                    TempData.Catagory = temp.SelectSingleNode("td[1]/a").Attributes["title"]
                        .Value;
                    TempData.Title = temp.SelectSingleNode("td[2]/a").Attributes["title"]
                        .Value;
                    TempData.Url = temp.SelectSingleNode("td[2]/a").Attributes["href"]
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

                    TempData.Timestamp = int.Parse(temp.SelectSingleNode("td[5]").Attributes["data-timestamp"].Value);
                    TempData.Date = temp.SelectSingleNode("td[5]").InnerText;
                    TempData.Up = temp.SelectSingleNode("td[6]").InnerText;
                    TempData.Leeches = temp.SelectSingleNode("td[7]").InnerText;
                    TempData.Complete = temp.SelectSingleNode("td[8]").InnerText;
                    //用来判断是否下载完毕一整天的数据
                    if (Day != null)
                    {
                        if (AddFin)
                        {
                            NextDayData.AddOrUpdate(TempData.id, TempData, (key, Value) => TempData);
                            continue;
                        }

                        if (TempData.Day != Day)
                        {
                            NextDayData = new ConcurrentDictionary<int, TorrentInfo>();
                            NextDayData.AddOrUpdate(TempData.id, TempData, (key, Value) => TempData);
                            AddFin = true;
                            continue;
                        }
                    }

                    AnalysisData.AddOrUpdate(TempData.id, TempData, (key, Value) => TempData);
                }

                //SaveToDataBaseFormList(new List<TorrentInfo>(AnalysisData.Values));
            }
        }
        internal readonly BlockingCollection<Tuple<Tuple<string,int>, System.Collections.Generic.ICollection<TorrentInfo>>> DataCollect=new BlockingCollection<Tuple<Tuple<string, int>, System.Collections.Generic.ICollection<TorrentInfo>>>();

        public void HandlerToHtml(string result)
        {
            if (AnalysisData == null) AnalysisData = new ConcurrentDictionary<int, TorrentInfo>();
            if (result != "")
            {
                Loger.Instance.DateInfo(DateOfNow);
                Interlocked.Increment(ref pageCount);
                var HtmlDoc = new HtmlDocument();
                HtmlDoc.LoadHtml(result);
                foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[1]/div[2]/table/tbody/tr"))
                {
                    var TempData = new TorrentInfo();
                    var temp = HtmlNode.CreateNode(item.OuterHtml);
                    TempData.Class = item.Attributes["class"].Value;
                    TempData.Catagory = temp.SelectSingleNode("td[1]/a").Attributes["title"]
                        .Value;
                    TempData.Title = temp.SelectSingleNode("td[2]/a").Attributes["title"]
                        .Value;
                    TempData.Url = temp.SelectSingleNode("td[2]/a").Attributes["href"]
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

                    TempData.Timestamp = int.Parse(temp.SelectSingleNode("td[5]").Attributes["data-timestamp"].Value);
                    TempData.Date = temp.SelectSingleNode("td[5]").InnerText;
                    TempData.Up = temp.SelectSingleNode("td[6]").InnerText;
                    TempData.Leeches = temp.SelectSingleNode("td[7]").InnerText;
                    TempData.Complete = temp.SelectSingleNode("td[8]").InnerText;
                    if (string.IsNullOrEmpty(DateOfNow) || AddFin)
                    {
                        if (!AddFin) Loger.Instance.WithTimeStart($"开始获取{TempData.Day}数据", Time);
                        DateOfNow = TempData.Day;
                        AddFin = false;
                    }

                    if (DateOfNow != TempData.Day)
                    {
                        DataCollect.TryAdd(
                            new Tuple<Tuple<string, int>, System.Collections.Generic.ICollection<TorrentInfo>>(
                                new Tuple<string, int>(DateOfNow, pageCount), AnalysisData.Values));
                        pageCount = 0;
                        AnalysisData.Clear();
                        AddFin = true;
                        Loger.Instance.WithTimeRestart($"结束获取{TempData.Day}数据", Time);
                    }


                    AnalysisData.AddOrUpdate(TempData.id, TempData, (key, Value) => TempData);
                }

                if (DateOfNow == "2000-01-01")
                {
                    DataBaseCommand.SaveToDataBaseRange(AnalysisData.Values, pageCount, false);
                    AnalysisData.Clear();
                }
            }
        }


        public HandlerHtml()
        {
        }

        public void Dispose()
        {
            AnalysisData = null;
            NextDayData = null;
            GC.Collect();
        }
    }
}