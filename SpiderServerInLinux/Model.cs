using LiteDB;
using ShadowsocksR.Controller;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using xNet;

namespace SpiderServerInLinux
{
    internal static class Setting
    {
        //internal static LiteDatabase T66yDB = new LiteDatabase(@"T66y.db");
        internal static int LoopTime = 5000;

        internal static bool Platform = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64" ? true : false;
        internal static GlobalSet _GlobalSet;
        internal static Stack<string> LocalInfoC = new Stack<string>();
        internal static Stack<string> Remote = new Stack<string>();
        internal static BlockingCollection<NyaaInfo> WordProcess = new BlockingCollection<NyaaInfo>();

        //internal static ShowInControl ShowInfo;
        internal static int Socks5Point;

        internal static int NyaaSocks5Point;

        internal static string JavDownLoadNow;
        internal static string NyaaDownLoadNow;
        internal static string MiMiDownLoadNow;
        internal static string MiMiAiStoryDownLoadNow;
        internal static string SISDownLoadNow;
        internal static string T66yDownLoadNow;
        internal static string T66yDownLoadNowOther;
        internal static string T66yDownLoadOldOther;
        internal static BlockingCollection<WebpImage> SaveImgOpera = new BlockingCollection<WebpImage>(5);

        internal static bool SISDownloadIng = false;
        internal static bool T66yDownloadIng = false;

        internal static ShadowsocksRController SSR;
        internal static ShadowsocksRController NyaaSSR;

        internal static server server;
        internal static DownloadManage DownloadManage;
        internal static readonly CancellationTokenSource CancelSign = new CancellationTokenSource();
        internal static readonly TaskCompletionSource<byte> ShutdownResetEvent = new TaskCompletionSource<byte>();
        internal static readonly string NyaaAddress = "https://sukebei.nyaa.si/view/";
        internal static string NyaaDay = "";//过去下载每条Nyaa用
        internal static string MiMiDay = "";//过去下载每条MiMi用
        internal static int JavPageCount = 0;
        internal static int SISPageCount = 1;
        internal static int HttpConnectCount = 1;

        internal static DataCount DataCount = new DataCount();

        internal static bool CheckOnline(bool ssr = false)
        {
            return true;
            try
            {
                using (var request = new HttpRequest())
                {
                    Thread.Sleep(1000);
                    request.ConnectTimeout = 1000;
                    request.UserAgent = Http.ChromeUserAgent();
                    if (ssr) request.Proxy = Socks5ProxyClient.Parse($"192.168.2.116:{Setting.Socks5Point}");
                    HttpResponse response = request.Get(@"https://www.google.co.jp/");
                    //HttpResponse response = request.Get(@"https://www.141jav.com/new");
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        //File.WriteAllText("Page.heml", response.ToString());
                        //Thread.Sleep(1000);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                if (!_GlobalSet.SocksCheck)
                {
                    if (SSR == null)
                    {
                        SSR = new ShadowsocksRController();
                    }
                    Loger.Instance.Error("连接失败，尝试使用SSR连接");
                    if (!ssr)
                    {
                        if (CheckOnline(true))
                        {
                            Loger.Instance.Error("使用SSR连接成功");
                            _GlobalSet.SocksCheck = true;
                            return true;
                        }
                    }
                    SSR = null;
                    GC.Collect();
                }
            }
            return false;
        }
    }

    [Serializable]
    internal class OnlineOpera : IDisposable
    {
        internal string NyaaAddress = Setting._GlobalSet.NyaaAddress;
        internal string JavAddress = Setting._GlobalSet.JavAddress;
        internal string MiMiAiAddress = Setting._GlobalSet.MiMiAiAddress;

        internal string ssr4Nyaa = Setting._GlobalSet.ssr4Nyaa;
        internal string ssr_url = Setting._GlobalSet.ssr_url;
        internal int ConnectPoint = Setting._GlobalSet.ConnectPoint;

        internal long TotalUploadBytes = Setting._GlobalSet.totalUploadBytes;
        internal long TotalDownloadBytes = Setting._GlobalSet.totalDownloadBytes;

        internal List<string> LocalInfo = Setting.LocalInfoC.ToList();
        internal List<string> RemoteInfo = Setting.Remote.ToList();

        internal bool NyaaSocksCheck = Setting._GlobalSet.NyaaSocksCheck;
        internal bool SocksCheck = Setting._GlobalSet.SocksCheck;
        internal bool OnlyList = true;
        internal bool AutoRun = Setting._GlobalSet.AutoRun;

        internal string MiMiInterval = Setting.DownloadManage != null ? Setting.DownloadManage.MiMiSpan.ElapsedMilliseconds == 0 ? $"{Setting.MiMiDownLoadNow},{Setting.MiMiDay}" : Setting.DownloadManage.GetMiMiNewDataTimer.Interval.ToString() : string.Empty;
        internal string JavInterval = Setting.DownloadManage != null ? Setting.DownloadManage.JavSpan.ElapsedMilliseconds == 0 ? Setting.JavDownLoadNow : Setting.DownloadManage.GetJavNewDataTimer.Interval.ToString() : string.Empty;
        internal string NyaaInterval = Setting.DownloadManage != null ? Setting.DownloadManage.NyaaSpan.ElapsedMilliseconds == 0 ? Setting.NyaaDownLoadNow : Setting.DownloadManage.GetNyaaNewDataTimer.Interval.ToString() : string.Empty;
        internal string MiMiStoryInterval = Setting.DownloadManage != null ? Setting.DownloadManage.MiMiStorySpan.ElapsedMilliseconds == 0 ? Setting.MiMiAiStoryDownLoadNow : Setting.DownloadManage.GetMiMiAiStoryDataTimer.Interval.ToString() : string.Empty;
        internal string T66yInterval = Setting.DownloadManage != null ? Setting.DownloadManage.GetT66ySpan.ElapsedMilliseconds == 0 ? Setting.T66yDownLoadNow : Setting.DownloadManage.GetT66yDataTimer.Interval.ToString() : string.Empty;
        internal string T66yOtherMessage = Setting.DownloadManage != null ? Setting.T66yDownLoadNowOther : string.Empty;
        internal string T66yOtherOldMessage = Setting.DownloadManage != null ? Setting.T66yDownLoadOldOther : string.Empty;
        internal string SisInterval = Setting.DownloadManage != null ? Setting.SISDownLoadNow : string.Empty;
        internal int SisIndex = Setting.HttpConnectCount;

        internal string Memory = $"内存使用量:{Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB";

        internal TimeSpan MiMiSpan = Setting.DownloadManage != null ? Setting.DownloadManage.MiMiSpan.Elapsed : TimeSpan.Zero;
        internal TimeSpan JavSpan = Setting.DownloadManage != null ? Setting.DownloadManage.JavSpan.Elapsed : TimeSpan.Zero;
        internal TimeSpan NyaaSpan = Setting.DownloadManage != null ? Setting.DownloadManage.NyaaSpan.Elapsed : TimeSpan.Zero;
        internal TimeSpan MiMiStorySpan = Setting.DownloadManage != null ? Setting.DownloadManage.MiMiStorySpan.Elapsed : TimeSpan.Zero;
        internal TimeSpan GetT66ySpan = Setting.DownloadManage != null ? Setting.DownloadManage.GetT66ySpan.Elapsed : TimeSpan.Zero;

        internal int NyaaSSRPoint = Setting.NyaaSSR != null ? Setting.NyaaSSR.SocksPort : 0;
        internal int SSRPoint = Setting.SSR != null ? Setting.SSR.SocksPort : 0;
        internal int SocksPoint = Setting.Socks5Point;
        internal int NyaaSocksPoint = Setting.NyaaSocks5Point;
        internal DataCount DataCount = Setting.DataCount;

        /*
                public string NyaaAddress
                {
                    get
                    {
                        _NyaaAddress = Setting._GlobalSet.NyaaAddress;
                        return _NyaaAddress;
                    }
                    set { Setting._GlobalSet.NyaaAddress = value; }
                }

                public string JavAddress
                {
                    get
                    {
                        _JavAddress = Setting._GlobalSet.JavAddress;

                        return _JavAddress;
                    }
                    set { Setting._GlobalSet.JavAddress = value; }
                }
        */
        /* public string MiMiAiAddress { get { return Setting._GlobalSet.MiMiAiAddress; } set { Setting._GlobalSet.MiMiAiAddress = value; } }
         public string ssr_url { get { return Setting._GlobalSet.ssr_url; } set { Setting._GlobalSet.ssr_url = value; } }
         public int ConnectPoint { get { return Setting._GlobalSet.ConnectPoint; } set { Setting._GlobalSet.ConnectPoint = value; } }
         public long TotalUploadBytes { get { return Setting._GlobalSet.totalUploadBytes; } }
         public long TotalDownloadBytes { get { return Setting._GlobalSet.totalDownloadBytes; } }
         public List<string> LocalInfo { get { return Setting.LocalInfoC.ToList(); } }
         public List<string> RemoteInfo { get { return Setting.LocalInfoC.ToList(); } }*/

        public static byte[] Send(bool First = false)
        {
            using var OnlineOpera = new OnlineOpera() { OnlyList = First ? false : true };
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter Formatter = new BinaryFormatter();
                Formatter.Serialize(stream, OnlineOpera);
                return stream.ToArray();
            }
        }

        internal static Task ChangeSet(OnlineOpera onlineOpera)
        {
            return Task.Run(() =>
            {
                if (onlineOpera.NyaaAddress != Setting._GlobalSet.NyaaAddress)
                {
                    Setting._GlobalSet.NyaaAddress = onlineOpera.NyaaAddress;
                }
                if (onlineOpera.JavAddress != Setting._GlobalSet.JavAddress)
                {
                    Setting._GlobalSet.JavAddress = onlineOpera.JavAddress;
                }
                if (onlineOpera.MiMiAiAddress != Setting._GlobalSet.MiMiAiAddress)
                {
                    Setting._GlobalSet.MiMiAiAddress = onlineOpera.MiMiAiAddress;
                }
                if (onlineOpera.SocksCheck != Setting._GlobalSet.SocksCheck)
                {
                    Setting._GlobalSet.SocksCheck = onlineOpera.SocksCheck;
                    Loger.Instance.ServerInfo($"主机", $"更改当前代理状态为{Setting._GlobalSet.SocksCheck }");
                }
                if (onlineOpera.SocksPoint != Setting.Socks5Point)
                {
                    Setting.Socks5Point = onlineOpera.SocksPoint;
                    Loger.Instance.ServerInfo($"主机", $"代理端口更改为{Setting.Socks5Point}完毕");
                }
                if (onlineOpera.NyaaSocksCheck != Setting._GlobalSet.NyaaSocksCheck)
                {
                    Setting._GlobalSet.NyaaSocksCheck = onlineOpera.NyaaSocksCheck;
                    Loger.Instance.ServerInfo($"主机", $"Nyaa使用代理{onlineOpera.NyaaSocksCheck}");
                }
                if (onlineOpera.ssr_url != Setting._GlobalSet.ssr_url)
                {
                    var TestSSR = new ShadowsocksRController(onlineOpera.ssr_url);
                    if (TestSSR.CheckOnline())
                    {
                        if (Setting.Socks5Point == Setting.SSR.SocksPort)
                            Setting.Socks5Point = TestSSR.SocksPort;
                        if (Setting.SSR != null) Setting.SSR.Stop();
                        Setting.SSR = null;
                        Setting.SSR = TestSSR;
                        Setting._GlobalSet.ssr_url = onlineOpera.ssr_url;
                        GC.Collect();
                    }
                    else
                    {
                        Loger.Instance.ServerInfo("SSR", "新增SSR外网访问失败，不替换连接");
                    }
                }
                if (onlineOpera.NyaaSocksPoint != Setting.NyaaSocks5Point)
                {
                    Setting.NyaaSocks5Point = onlineOpera.NyaaSocksPoint;
                    Loger.Instance.ServerInfo($"主机", $"Nyaa使用代理端口{onlineOpera.NyaaSocksPoint}");
                }
                if (onlineOpera.ssr4Nyaa != Setting._GlobalSet.ssr4Nyaa)
                {
                    var TestSSR = new ShadowsocksRController(onlineOpera.ssr4Nyaa);
                    if (TestSSR.CheckOnline(@"https://sukebei.nyaa.si/"))
                    {
                        if (Setting.NyaaSocks5Point == Setting.NyaaSSR.SocksPort)
                        {
                            Setting.NyaaSocks5Point = TestSSR.SocksPort;
                        }
                        if (Setting.NyaaSSR != null) Setting.NyaaSSR.Stop();
                        Setting.NyaaSSR = null;
                        Setting.NyaaSSR = TestSSR;
                        Setting._GlobalSet.ssr4Nyaa = onlineOpera.ssr4Nyaa;
                        GC.Collect();
                    }
                }
                if (onlineOpera.AutoRun != Setting._GlobalSet.AutoRun)
                {
                    Setting._GlobalSet.AutoRun = onlineOpera.AutoRun;
                    Loger.Instance.ServerInfo($"主机", $"自动运行模式更改为{Setting._GlobalSet.AutoRun}");
                }
                if (onlineOpera.SisIndex != Setting._GlobalSet.SISPageIndex)
                {
                    if (onlineOpera.SisIndex != 0)
                    {
                        Setting.SISPageCount = onlineOpera.SisIndex;
                        Loger.Instance.ServerInfo($"主机", $"SIS下载页面更改为{Setting.SISPageCount}");
                    }
                }
                return Task.CompletedTask;
            });
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // called via myClass.Dispose(). OK to use any private object references
                }
                // Release unmanaged resources. Set large fields to null.
                disposed = true;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        ~OnlineOpera() // the finalizer
        {
            Dispose(false);
        }

        public class UBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.EndsWith("OnlineOpera"))
                {
                    return typeof(OnlineOpera);
                }
                if (typeName.EndsWith("DataCount"))
                {
                    return typeof(DataCount);
                }
                return (Assembly.GetExecutingAssembly()).GetType(typeName);
            }
        }
    }

    [Serializable]
    internal class DataCount
    {
        internal int D0;
        internal int D1;
        internal int D2;
        internal int D3;
        internal int T66y;
    }

    [Serializable]
    internal class GlobalSet
    {
        private string _NyaaAddress = "https://sukebei.nyaa.si/?p=";
        private string _JavAddress = "https://www.141jav.com/new?page=";
        private string _MiMiAiAddress = "http://www.mmroad.com/forumdisplay.php?fid=55&page=";
        private string _T66yAddress = "http://t66y.com/thread0806.php?fid=25&search=&page=";

        private Dictionary<string, int> _SSRList = new Dictionary<string, int>();
        private string _ssr_url = "";
        private string _ssr4Nyaa = "";

        private int _NyaaLastPageIndex = 0;
        private int _JavLastPageIndex = 0;
        private int _MiMiAiPageIndex = 0;
        private int _MiMiAiStoryPageIndex = 0;
        private int _T66yPageIndex = 0;
        private int _SISPageIndex = 0;
        private int _SISSkip = 0;

        private int _ConnectPoint = 2222;
        private long _totalUploadBytes = 0;
        private long _totalDownloadBytes = 0;
        private bool _SocksCheck = false;
        private bool _NyaaSocksCheck = false;

        private bool _NyaaFin = false;
        private bool _JavFin = false;
        private bool _MiMiFin = false;
        private bool _AutoRun = false;

        internal string NyaaAddress
        { get { return _NyaaAddress; } set { _NyaaAddress = value; Save(); } }

        internal string JavAddress
        { get { return _JavAddress; } set { _JavAddress = value; Save(); } }

        internal string T66yAddress
        { get { return _T66yAddress; } set { _T66yAddress = value; Save(); } }

        internal string MiMiAiAddress
        { get { return _MiMiAiAddress; } set { _MiMiAiAddress = value; Save(); } }

        internal Dictionary<string, int> SSRList => _SSRList;

        internal string ssr_url
        { get { return _ssr_url; } set { _ssr_url = value; Save(); } }

        internal string ssr4Nyaa
        { get { return _ssr4Nyaa; } set { _ssr4Nyaa = value; Save(); } }

        internal int SISPageIndex
        { get { return _SISPageIndex; } set { _SISPageIndex = value; Save(); } }

        internal int SISSkip
        { get { return _SISSkip; } set { _SISSkip = value; Save(); } }

        internal int NyaaLastPageIndex
        { get { return _NyaaLastPageIndex; } set { _NyaaLastPageIndex = value; Save(); } }

        internal int JavLastPageIndex
        { get { return _JavLastPageIndex; } set { _JavLastPageIndex = value; Save(); } }

        internal int ConnectPoint
        { get { return _ConnectPoint; } set { _ConnectPoint = value; Save(); } }

        internal int MiMiAiPageIndex
        { get { return _MiMiAiPageIndex; } set { _MiMiAiPageIndex = value; Save(); } }

        internal int MiMiAiStoryPageIndex
        { get { return _MiMiAiStoryPageIndex; } set { _MiMiAiStoryPageIndex = value; Save(); } }

        internal int T66yPageIndex
        { get { return _T66yPageIndex; } set { _T66yPageIndex = value; Save(); } }

        internal bool SocksCheck
        { get { return _SocksCheck; } set { _SocksCheck = value; Save(); } }

        internal bool NyaaSocksCheck
        { get { return _NyaaSocksCheck; } set { _NyaaSocksCheck = value; Save(); } }

        internal bool NyaaFin
        { get { return _NyaaFin; } set { _NyaaFin = value; Save(); } }

        internal bool JavFin
        { get { return _JavFin; } set { _JavFin = value; Save(); } }

        internal bool MiMiFin
        { get { return _MiMiFin; } set { _MiMiFin = value; Save(); } }

        internal bool AutoRun
        { get { return _AutoRun; } set { _AutoRun = value; Save(); } }

        internal long totalUploadBytes
        { get { return _totalUploadBytes; } set { _totalUploadBytes = value; Save(); } }

        internal long totalDownloadBytes
        { get { return _totalDownloadBytes; } set { _totalDownloadBytes = value; Save(); } }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            /*if (string.IsNullOrEmpty(_NyaaAddress)) _NyaaAddress = "https://sukebei.nyaa.si/?p=";
            if (string.IsNullOrEmpty(_JavAddress)) _JavAddress = "https://www.141jav.com/new?page=";
            if (string.IsNullOrEmpty(_MiMiAiAddress)) _MiMiAiAddress = "http://www.mmbutt.com/forumdisplay.php?fid=55&page=";
            if (string.IsNullOrEmpty(_ssr_url)) _ssr_url = "";*/
            if (string.IsNullOrEmpty(_T66yAddress)) _T66yAddress = "http://t66y.com/thread0806.php?fid=25&search=&page=";
            if (string.IsNullOrEmpty(_ssr_url)) _ssr_url = "ssr://MTUzLjEwMS41Ny4zNTo1ODQ1NDphdXRoX2FlczEyOF9zaGExOmNoYWNoYTIwLWlldGY6cGxhaW46VFdsNmRXaHZNVEF4TUROSVN3Lz9vYmZzcGFyYW09WWpkbU9UQXhOemc1TG0xcFkzSnZjMjltZEM1amIyMCZwcm90b3BhcmFtPU1UYzRPVG95WVRGMllWayZyZW1hcmtzPVV5M2x1TGpsdDU3b2dhX3BnSm90NXBlbDVweXM1cDJ4NUxxc0lFTm9iMjl3WVEmZ3JvdXA9NDRHQzQ0S0U0NEtCJnVkcHBvcnQ9NzIwOTA2JnVvdD0xMTUwOTg1Ng";
            if (_SSRList == null) _SSRList = new Dictionary<string, int>();
        }

        internal GlobalSet()
        {
        }

        internal static GlobalSet Open()
        {
            var LibAddress = $"{new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName}{Path.DirectorySeparatorChar}GlobalSet.db";
            using var db = new LiteDatabase(LibAddress);
            if (!db.CollectionExists("GlobalSet"))
            {
                Loger.Instance.LocalInfo($"未找到配置文件，正在新建");
                var globalSet = new GlobalSet();
                using var stream = new MemoryStream();
                IFormatter Fileformatter = new BinaryFormatter();
                Fileformatter.Serialize(stream, globalSet);
                db.GetCollection("GlobalSet").Upsert(new BsonDocument
                {
                    ["_id"] = 0,
                    ["Data"] = stream.ToArray()
                });
                return globalSet;
            }
            else
            {
                Loger.Instance.LocalInfo($"正在加载配置文件");
                var Date = db.GetCollection("GlobalSet").FindById(0)["Data"].AsBinary;
                using var stream = new MemoryStream(Date);
                IFormatter Formatter = new BinaryFormatter();
                Formatter.Binder = new UBinder();
                var ret = Formatter.Deserialize(stream) as GlobalSet;
                return ret;
            }
            /* if (File.Exists("GlobalSet.dat"))
             {
                 using (var stream = new FileStream("GlobalSet.dat", System.IO.FileMode.OpenOrCreate))
                 {
                     IFormatter Formatter = new BinaryFormatter();
                     Formatter.Binder = new UBinder();
                     var Ret = Formatter.Deserialize(stream) as GlobalSet;
                     Loger.Instance.ServerInfo("主机", $"{Ret.ConnectPoint}");
                     Loger.Instance.ServerInfo("主机", $"{Ret.MiMiAiAddress}");
                     return Ret;
                 }
             }
             else
             {
                 Loger.Instance.LocalInfo($"未找到配置文件，正在新建");
             }*/
            return new GlobalSet();
        }

        public void Open(byte[] Data)
        {
            using (Stream stream = new MemoryStream(Data))
            {
                IFormatter Formatter = new BinaryFormatter();
                Formatter.Binder = new UBinder();
                var Temp = (GlobalSet)Formatter.Deserialize(stream);
                Temp.JavLastPageIndex = Setting._GlobalSet.JavLastPageIndex;
                Temp.NyaaLastPageIndex = Setting._GlobalSet.NyaaLastPageIndex;
                Setting._GlobalSet = Temp;
                Setting._GlobalSet.Save();
            }
        }

        [NonSerialized]
        private bool SaveInUse = false;

        private GlobalSet Save()
        {
            if (SaveInUse == true) return this;
            SaveInUse = true;
            try
            {
                /*using (Stream stream = new FileStream("GlobalSet.dat", System.IO.FileMode.OpenOrCreate))
                {
                    IFormatter Fileformatter = new BinaryFormatter();
                    Fileformatter.Serialize(stream, this);
                }*/
                using var db = new LiteDatabase($"{new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName}/GlobalSet.db");

                using var stream = new MemoryStream();
                IFormatter Fileformatter = new BinaryFormatter();
                Fileformatter.Serialize(stream, this);
                db.GetCollection("GlobalSet").Upsert(new BsonDocument
                {
                    ["_id"] = 0,
                    ["Data"] = stream.ToArray()
                });
            }
            catch (Exception)
            {
            }
            SaveInUse = false;
            return this;
        }

        public byte[] Send()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                IFormatter Fileformatter = new BinaryFormatter();
                Fileformatter.Serialize(stream, Setting._GlobalSet);
                return stream.ToArray();
            }
        }

        public class UBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.EndsWith("GlobalSet"))
                {
                    return typeof(GlobalSet);
                }
                return (Assembly.GetExecutingAssembly()).GetType(typeName);
            }
        }
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

    internal class NyaaInfo
    {
        public int id => int.Parse(Url.Replace(@"/view/", "").Replace("#comments", ""));
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

    internal class JavInfo
    {
        public string id { get; set; }
        public string Magnet { get; set; }
        public string ImgUrl { get; set; }
        public string ImgUrlError { get; set; }
        public byte[] Image { get; set; }
        public string Describe { get; set; }
        public string Size { get; set; }
        public string Date { get; set; }
        public string[] Tags { get; set; }
        public string[] Actress { get; set; }
    }

    internal class MiMiAiData
    {
        public int id { get; set; }
        public string Title { get; set; }
        public string Date { get; set; }
        public int Index { get; set; }
        public List<BasicData> InfoList { get; set; }
        public bool Status { get; set; }

        internal class BasicData
        {
            public string Type { get; set; }
            public string info { get; set; }
            public byte[] Data { get; set; }
        }
    }

    [Serializable]
    internal class T66yData
    {
        public int id { get; set; }
        public string Title { get; set; }
        public string Uri { get; set; }
        public string Date { get; set; }
        public string HtmlData { get; set; }
        public List<string> MainList { get; set; }
        public List<string> QuoteList { get; set; }
        public bool Status { get; set; }

        internal byte[] ToByte()
        {
            using var stream = new MemoryStream();
            IFormatter Fileformatter = new BinaryFormatter();
            Fileformatter.Serialize(stream, this);
            return stream.ToArray();
        }

        public static T66yData ToClass(byte[] data)
        {
            using var stream = new MemoryStream(data);
            IFormatter Fileformatter = new BinaryFormatter();
            Fileformatter.Binder = new UBinder();
            return Fileformatter.Deserialize(stream) as T66yData;
        }

        public class UBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.EndsWith("T66yData"))
                {
                    return typeof(T66yData);
                }
                return (Assembly.GetExecutingAssembly()).GetType(typeName);
            }
        }
    }

    [Serializable]
    internal class SISData : T66yData
    {
        public string Type { get; set; }
    }

    [Serializable]
    internal class T66yImgData
    {
        public string id { get; set; }
        public string Date { get; set; }
        public string Hash { get; set; }

        public byte[] img { get; set; }
        public List<int> FromList { get; set; }
        public bool Status => img != null && (img.Length > 1024 || Hash == "Fail");

        internal byte[] ToByte()
        {
            using var stream = new MemoryStream();
            IFormatter Fileformatter = new BinaryFormatter();
            Fileformatter.Serialize(stream, this);
            return stream.ToArray();
        }
    }

    [Serializable]
    internal class SISImgData : T66yImgData
    {
        public new byte[] img
        {
            get { return base.img; }
            set
            {
                if (value != null)
                {
                    try
                    {
                        if (value.Length > 1024)
                        {
                            //using (var mem = new MemoryStream(value))
                            {
                                base.img = value;
                                Hash = Convert.ToBase64String(new MD5CryptoServiceProvider().ComputeHash(base.img));
                                /*Image img = Image.FromStream(mem);
                                if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Gif))
                                {
                                    base.img = value;
                                    Hash = Convert.ToBase64String(new MD5CryptoServiceProvider().ComputeHash(base.img));
                                }
                                else
                                {
                                    base.img = Setting.DownloadManage.Compress(img, 75).ToArray();
                                    Hash = Convert.ToBase64String(new MD5CryptoServiceProvider().ComputeHash(base.img));
                                }*/
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //base.img = value;
                        //Hash = Convert.ToBase64String(new MD5CryptoServiceProvider().ComputeHash(base.img));
                        /* try
                         {
                             File.WriteAllBytes($"{new Uri(id).Segments.Last()}", base.img);
                         }
                         catch (Exception)
                         {
                         }*/
                    }
                }
            }
        }
    }

    public class OriImage
    {
        public string id { get; set; }
        public string From { get; set; }

        public string Date { get; set; }
        public string Hash { get; set; }
        public byte[] img { get; set; }
        public List<int> FromList { get; set; }
        public bool Status { get; set; }
    }

    internal class WebpImage
    {
        public string id { get; set; }

        public string Date { get; set; }
        public string Hash { get; set; }

        public byte[] img { get; set; }
        public List<int> FromList { get; set; }
        public bool Status { get; set; }
        public string Type { get; set; }
        public string From { get; set; }

        public byte[] Send()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter Formatter = new BinaryFormatter();
                Formatter.Serialize(stream, this);
                return stream.ToArray();
            }
        }

        public static WebpImage ToClass(byte[] data)
        {
            using var stream = new MemoryStream(data);
            IFormatter Fileformatter = new BinaryFormatter();
            Fileformatter.Binder = new UBinder();
            return Fileformatter.Deserialize(stream) as WebpImage;
        }

        public class UBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.EndsWith("WebpImage"))
                {
                    return typeof(WebpImage);
                }
                return Assembly.GetExecutingAssembly().GetType(typeName);
            }
        }
    }

    [Serializable]
    internal class MiMiAiStory
    {
        public int id { get; set; }
        public string Uri { get; set; }
        public string Title { get; set; }
        public string Story { get; set; }
        public byte[] Data { get; set; }

        public byte[] ToByte()
        {
            using var stream = new MemoryStream();
            IFormatter Fileformatter = new BinaryFormatter();
            Fileformatter.Serialize(stream, this);
            return stream.ToArray();
        }

        public static MiMiAiStory ToClass(byte[] data)
        {
            using var stream = new MemoryStream(data);
            IFormatter Fileformatter = new BinaryFormatter();
            Fileformatter.Binder = new UBinder();
            return Fileformatter.Deserialize(stream) as MiMiAiStory;
        }

        public class UBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.EndsWith("MiMiAiStory"))
                {
                    return typeof(MiMiAiStory);
                }
                return (Assembly.GetExecutingAssembly()).GetType(typeName);
            }
        }
    }

    public class MiMiAiNetData
    {
        public List<ByteInfo> BtList = new List<ByteInfo>();
        public List<ByteInfo> ImgList = new List<ByteInfo>();
        public List<string[]> InfoList = new List<string[]>();
        private readonly Regex GetCodeRegex = new Regex(@"[a-zA-Z0-9//-]+", RegexOptions.Compiled);
        private string Title;
        private string SubTitle;
        private string OriTitle;
        private string GetCode;
        private string[] Actress;
        private string studio;
        private string series;
        private string[] Category;
        private DateTime date;
        private string magnet;
        private string Duration;
        private string Size;
        private string Star;
        private string Age;
        private string _3D;
        private string Height;

        private Dictionary<string, string> Match = new Dictionary<string, string>()
        {
            {"番号","GetCode"},
             {"商品番号","GetCode"},
            {"女優","Actress"},
            {"出演","Actress"},
             //{"主演女優","Actress"},
                {"主演","Actress"},
              {"名前","Actress"},
            {"スタジオ","studio"},
            {"シリーズ","series"},
            //{"カテゴリ一覧","Category"},
            {"カテゴリ","Category"},
            {"タグ","Category"},
              {"タイプ","Category"},
            {"発売日","date"},
            {"販売日","date"},
            {"配信日","date"},
            {"公開日","date"},
            {"特征","magnet"},
            {"驗證編號","magnet"},
            {"驗證全碼","magnet"},
            {"種子代碼","magnet"},
            {"店長推薦作品","SubTitle"},
            {"タイトル","SubTitle"},
            {"収録時間","Duration"},
            {"再生時間","Duration"},
             {"動画","Duration"},
            {"ユーザー評価","Star"},
            {"3サイズ","_3D"},
             {"サイズ","_3D"},
            {"年齢","Age"},
             { "身長","Height"}
        };

        internal void ReadBt(string innerText)
        {
            InfoList.Add(new[] { "bt", innerText });
            BtList.Add(new ByteInfo
            {
                OriInfo = innerText
                // Byteinfo = net.GetByte(new Uri(innerText)), Byteinfo = net.GetByte(new Uri("http://www7.2kdown.com/link.php?ref=2E6cs8MBHc")),
            });
        }

        internal void ReadInfo(string innerText)
        {
            try
            {
                innerText = innerText.Replace("\r\n", "").Replace("&nbsp;", "");
                if (innerText.StartsWith(" ")) innerText = innerText.Remove(0, 1);
                if (!string.IsNullOrEmpty(innerText))
                {
                    InfoList.Add(new[] { "text", innerText });
                }
                if (string.IsNullOrEmpty(Title) && innerText != "\r\n")
                {
                    Title = innerText;
                    return;
                }

                var _Match = Match.SingleOrDefault(VARIABLE => innerText.StartsWith(VARIABLE.Key));
                switch (_Match.Value)
                {
                    case "GetCode":
                        GetCode = GetCodeRegex.Match(innerText).Value;
                        break;

                    case "Actress":
                        SplitString(_Match.Key, ref innerText);
                        if (!string.IsNullOrEmpty(innerText))
                            Actress = innerText.Split(new char[] { '，', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        break;

                    case "Category":
                        SplitString(_Match.Key, ref innerText);
                        if (innerText.Contains(':'))
                        {
                            var Temp = innerText.Split(":")[1];
                            if (!string.IsNullOrEmpty(Temp))
                            {
                                innerText = Temp;
                            }
                        }
                        else if (innerText.Contains('：'))
                        {
                            var Temp = innerText.Split("：")[1];
                            if (!string.IsNullOrEmpty(Temp))
                            {
                                innerText = Temp;
                            }
                        }
                        Category = innerText.Split(new char[] { ',', ' ', '?' }, StringSplitOptions.RemoveEmptyEntries);
                        break;

                    case "SubTitle":
                        if (SubTitle == null)
                        {
                            SubTitle = innerText;
                        }
                        SplitString(_Match.Key, ref innerText);
                        OriTitle = innerText;
                        break;

                    case "date":
                        try
                        {
                            SplitString(_Match.Key, ref innerText);
                            if (!DateTime.TryParse(GetCodeRegex.Match(innerText).Value, out date))
                            {
                                if (!DateTime.TryParse(innerText, out date))
                                {
                                    Console.WriteLine();
                                }
                            }

                            break;
                        }
                        catch (Exception)
                        {
                        }
                        break;

                    case "magnet":
                        SplitString(_Match.Key, ref innerText);
                        magnet = $"magnet:?xt=urn:btih:{innerText.Replace(" ", "")}";
                        break;

                    default:
                        if (!string.IsNullOrWhiteSpace(innerText))
                        {
                            if (!string.IsNullOrEmpty(_Match.Value))
                            {
                                var Field = this.GetType().GetField(_Match.Value, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                                if (Field != null)
                                {
                                    SplitString(_Match.Key, ref innerText);
                                    if (!string.IsNullOrEmpty(innerText))
                                    {
                                        Field.SetValue(this, innerText);
                                        break;
                                    }
                                }
                            }
                            if (innerText.ToUpper().EndsWith("G") || innerText.ToUpper().EndsWith("MB"))
                            {
                                Size = innerText;
                                break;
                            }
                            else if (innerText.StartsWith("Link URL:") || innerText.StartsWith("写真") || innerText.StartsWith("形式") || innerText.StartsWith("L ストリーミング"))
                            {
                                break;
                            }
                            else if (string.IsNullOrEmpty(SubTitle) && innerText != "\r\n")
                            {
                                SubTitle = innerText;
                                break;
                            }
                            else
                            {
                                _Match = Match.SingleOrDefault(VARIABLE => InfoList[InfoList.Count - 2][1].Replace(":", "").Replace("：", "").StartsWith(VARIABLE.Key));
                                if (!string.IsNullOrEmpty(_Match.Key))
                                {
                                    switch (_Match.Value)
                                    {
                                        case "date":
                                            {
                                                if (!DateTime.TryParse(GetCodeRegex.Match(innerText).Value, out date))
                                                {
                                                    if (!DateTime.TryParse(innerText, out date))
                                                    {
                                                        Console.WriteLine();
                                                    }
                                                }
                                            }
                                            break;

                                        case "Actress":
                                            if (!string.IsNullOrEmpty(innerText))
                                                Actress = innerText.Split(new char[] { '，', ',' }, StringSplitOptions.RemoveEmptyEntries);
                                            break;

                                        default:
                                            {
                                                var Field = this.GetType().GetField(_Match.Value, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                                                if (Field != null)
                                                {
                                                    if (string.IsNullOrEmpty(Field.GetValue(this) as string))
                                                    {
                                                        if (!string.IsNullOrEmpty(innerText))
                                                        {
                                                            Field.SetValue(this, innerText);
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine();
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine();
                                }
                            }
                            Console.WriteLine();
                        }
                        //Debug.WriteLine($"{innerText}");
                        break;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static void SplitString(string Temp, ref String Text)
        {
            try
            {
                Text = Text.Replace(Temp, "").Remove(0, 2);
            }
            catch (Exception)
            {
                Text = "";
            }

            return;
            if (Text.Contains(":"))
            {
                Text = Text.Split(':')[1];
            }
            else if (Text.Contains("："))
            {
                Text = Text.Split('：')[1];
            }
            else if (Text.Contains(" "))
            {
                Text = Text.Split(' ')[1];
            }
            else
            {
                //Debug.WriteLine($"{Text}");
            }
        }

        internal void ReadImg(string innerText)
        {
            InfoList.Add(new[] { "img", innerText });
            ImgList.Add(new ByteInfo
            {
                OriInfo = innerText,
                //Byteinfo = net.GetByteDirect(innerText)
                //Byteinfo = net.GetByteDirect("http://img588.net/images/2017/09/14/4.th.jpg"),
            });
        }

        public class ByteInfo
        {
            public byte[] _Byteinfo;

            public string OriInfo { get; internal set; }
            public string Status { get; set; }

            public Tuple<string, byte[]> Byteinfo
            {
                set
                {
                    if (value.Item1 == null)
                    {
                        Status = "Null";
                    }
                    else if (value.Item1 == "")
                    {
                        Status = "GetOk";
                        _Byteinfo = value.Item2;
                    }
                    else
                    {
                        Status = value.Item1;
                    }
                }
            }
        }
    }
}