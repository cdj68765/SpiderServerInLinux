﻿using System;
using System.Collections.Concurrent;
using System.Threading;

namespace SpiderServerInLinux
{
    internal class GlobalSet
    {
        public string _id { get; set; }
        public string Value { get; set; }
    }

    internal static class Setting
    {
        internal static readonly CancellationTokenSource CancelSign = new CancellationTokenSource();
        internal static string Address;
        internal static int LastPageIndex;
        internal static BlockingCollection<TorrentInfo> WordProcess = new BlockingCollection<TorrentInfo>();
    }

   /* internal class TorrentInfo
    {
        public int id { get; set; }
        public string Class { get; set; }
        public string Catagory { get; set; }
        public string Title { get; set; }
        public string Torrent { get; set; }
        public string Magnet { get; set; }
        public string Size { get; set; }
        public string Day => Convert.ToDateTime(Date).ToString("yyyy-MM-dd");
        public string Date { get; set; }
        public string Up { get; set; }
        public string Leeches { get; set; }
        public string Complete { get; set; }
    }*/

    internal class TorrentInfo
    {
        public int id => int.Parse(Url.Replace(@"/view/", "").Replace("#comments",""));
        public int Timestamp { get; set; }
        public string Url { get; set; }
        public string Class { get; set; }
        public string Catagory { get; set; }
        public string Title { get; set; }
        public string Torrent { get; set; }
        public string Magnet { get; set; }
        public string Size { get; set; }
        public string Day => Convert.ToDateTime(Date).ToString("yyyy-MM-dd");
        public string Date { get; set; }
        public string Up { get; set; }
        public string Leeches { get; set; }
        public string Complete { get; set; }
    }

    internal class DateRecord
    {
        public string _id { get; set; }
        public bool Status { get; set; }
        public int Page { get; set; }
    }
}