using Shadowsocks.Controller;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using xNet;

namespace SpiderServerInLinux
{
    internal static class Setting
    {
        internal static int LoopTime = 5000;
        internal static bool Platform = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64" ? true : false;
        internal static GlobalSet _GlobalSet;
        internal static BlockingCollection<NyaaInfo> WordProcess = new BlockingCollection<NyaaInfo>();
        internal static ShowInControl ShowInfo;
        internal static int Socks5Point;
        internal static ShadowsocksController SSR;
        internal static server server;
        internal static DownloadManage DownloadManage;
        internal static readonly CancellationTokenSource CancelSign = new CancellationTokenSource();
        internal static readonly TaskCompletionSource<byte> ShutdownResetEvent = new TaskCompletionSource<byte>();

        internal static bool CheckOnline(bool ssr = false)
        {
            try
            {
                using (var request = new HttpRequest())
                {
                    Thread.Sleep(1000);
                    request.ConnectTimeout = 1000;
                    request.UserAgent = Http.ChromeUserAgent();
                    if (ssr) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Socks5Point}");
                    HttpResponse response = request.Get(@"google.co.jp");
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
                        SSR = new ShadowsocksController();
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
                    SSR.Stop();
                    SSR = null;
                    GC.Collect();
                }
            }
            return false;
        }
    }

    [Serializable]
    internal class GlobalSet
    {
        private string _NyaaAddress = "https://sukebei.nyaa.si/?p=";
        private string _JavAddress = "https://www.141jav.com/new?page=";
        private string _ssr_url = "ssr://MTkzLjExMC4yMDMuMjI6MzQxMTI6YXV0aF9jaGFpbl9hOmFlcy0yNTYtY2ZiOmh0dHBfc2ltcGxlOk5qWTRPRGMzTmpVLz9vYmZzcGFyYW09JnByb3RvcGFyYW09JnJlbWFya3M9NmFhWjVyaXZJR1FnTFNCYjU1UzFMLWlCbENfbnA3dGRJRU5PTWl0T1ZGUSZncm91cD00NEdUNDRHdjQ0S0xMdWlRak9PQmlBJnVkcHBvcnQ9MCZ1b3Q9MA";
        private int _NyaaLastPageIndex = 0;
        private int _JavLastPageIndex = 0;
        private int _ConnectPoint = 2222;
        private bool _SocksCheck = false;
        private bool _NyaaFin = false;
        private bool _JavFin = false;
        internal string NyaaAddress { get { return _NyaaAddress; } set { _NyaaAddress = value; Save(); } }
        internal string JavAddress { get { return _JavAddress; } set { _JavAddress = value; Save(); } }
        internal string ssr_url { get { return _ssr_url; } set { _ssr_url = value; Save(); } }
        internal int NyaaLastPageIndex { get { return _NyaaLastPageIndex; } set { _NyaaLastPageIndex = value; Save(); } }
        internal int JavLastPageIndex { get { return _JavLastPageIndex; } set { _JavLastPageIndex = value; Save(); } }
        internal int ConnectPoint { get { return _ConnectPoint; } set { _ConnectPoint = value; Save(); } }
        internal bool SocksCheck { get { return _SocksCheck; } set { _SocksCheck = value; Save(); } }
        internal bool NyaaFin { get { return _SocksCheck; } set { _NyaaFin = value; Save(); } }
        internal bool JavFin { get { return _SocksCheck; } set { _JavFin = value; Save(); } }

        internal GlobalSet()
        {
        }

        internal GlobalSet Open()
        {
            if (File.Exists("GlobalSet.dat"))
            {
                Loger.Instance.LocalInfo($"找到配置文件，路径{new FileInfo("GlobalSet.dat").FullName}");
                using (Stream stream = new FileStream("GlobalSet.dat", FileMode.Open))
                {
                    IFormatter Formatter = new BinaryFormatter();
                    Formatter.Binder = new UBinder();
                    return Formatter.Deserialize(stream) as GlobalSet;
                }
            }
            else
            {
                Loger.Instance.LocalInfo($"未找到配置文件，正在新建");
            }
            return new GlobalSet().Save();
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

        private GlobalSet Save()
        {
            using (Stream stream = new FileStream("GlobalSet.dat", FileMode.OpenOrCreate))
            {
                IFormatter Fileformatter = new BinaryFormatter();
                Fileformatter.Serialize(stream, this);
            }
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
}