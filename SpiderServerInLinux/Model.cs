using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LiteDB;

namespace SpiderServerInLinux
{
   internal class GlobalSet
    {
        public string _id { get; set; }
        public string Value { get; set; }

    }
    internal class Setting
    {
        internal static readonly CancellationTokenSource CancelSign;
        internal static Setting setting;
        internal string Address;
        internal int LastPageIndex;
        internal BlockingCollection<TorrentInfo> WordProcess = new BlockingCollection<TorrentInfo>();
    }
    internal class TorrentInfo
    {
        public int id { get; set; }
        public string Class { get; set; }
        public string Catagory { get; set; }
        public string Title { get; set; }
        public string Torrent { get; set; }
        public string Magnet { get; set; }
        public string Size { get; set; }
        public string Day => Convert.ToDateTime(Date).ToLongDateString();
        public string Date { get; set; }
        public string Up { get; set; }
        public string Leeches { get; set; }
        public string Complete { get; set; }
    }
    internal  class DateRecord
    {
        public String _id { get; set; }
        public bool Status { get; set; }
        public int Page { get; set; }
    }

}
