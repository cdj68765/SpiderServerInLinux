using Shadowsocks.Controller;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
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
        internal static string JavDownLoadNow;
        internal static string NyaaDownLoadNow;
        internal static ShadowsocksController SSR;
        internal static server server;
        internal static DownloadManage DownloadManage;
        internal static readonly CancellationTokenSource CancelSign = new CancellationTokenSource();
        internal static readonly TaskCompletionSource<byte> ShutdownResetEvent = new TaskCompletionSource<byte>();
        internal static readonly string NyaaAddress = "https://sukebei.nyaa.si/view/";
        internal static string NyaaDay = "";//过去下载每条Nyaa用

        internal static bool CheckOnline(bool ssr = false)
        {
            try
            {
                using (var request = new HttpRequest())
                {
                    Thread.Sleep(1000);
                    request.ConnectTimeout = 1000;
                    request.UserAgent = Http.ChromeUserAgent();
                    if (ssr) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
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
        private string _MiMiaiAddress = "http://www.mmbuff.com/forumdisplay.php?fid=55&page=";
        private string _ssr_url = "ssr://MTkzLjExMC4yMDMuMjI6MzQxMTI6YXV0aF9jaGFpbl9hOmFlcy0yNTYtY2ZiOmh0dHBfc2ltcGxlOk5qWTRPRGMzTmpVLz9vYmZzcGFyYW09JnByb3RvcGFyYW09JnJlbWFya3M9NmFhWjVyaXZJR1FnTFNCYjU1UzFMLWlCbENfbnA3dGRJRU5PTWl0T1ZGUSZncm91cD00NEdUNDRHdjQ0S0xMdWlRak9PQmlBJnVkcHBvcnQ9MCZ1b3Q9MA";
        private int _NyaaLastPageIndex = 0;
        private int _JavLastPageIndex = 0;
        private int _MiMiAiPageIndex = 0;
        private int _ConnectPoint = 2222;
        private bool _SocksCheck = false;
        private bool _NyaaFin = false;
        private bool _JavFin = false;
        private bool _MiMiAiCheck = false;
        internal string NyaaAddress { get { return _NyaaAddress; } set { _NyaaAddress = value; Save(); } }
        internal string JavAddress { get { return _JavAddress; } set { _JavAddress = value; Save(); } }
        internal string ssr_url { get { return _ssr_url; } set { _ssr_url = value; Save(); } }
        internal int NyaaLastPageIndex { get { return _NyaaLastPageIndex; } set { _NyaaLastPageIndex = value; Save(); } }
        internal int JavLastPageIndex { get { return _JavLastPageIndex; } set { _JavLastPageIndex = value; Save(); } }
        internal int ConnectPoint { get { return _ConnectPoint; } set { _ConnectPoint = value; Save(); } }
        internal bool SocksCheck { get { return _SocksCheck; } set { _SocksCheck = value; Save(); } }
        internal bool NyaaFin { get { return _NyaaFin; } set { _NyaaFin = value; Save(); } }
        internal bool JavFin { get { return _JavFin; } set { _JavFin = value; Save(); } }
        internal string MiMiaiAddress { get { return _MiMiaiAddress; } set { _MiMiaiAddress = value; Save(); } }
        internal int MiMiAiPageIndex { get { return _MiMiAiPageIndex; } set { _MiMiAiPageIndex = value; Save(); } }
        internal bool MiMiAiCheck { get { return _MiMiAiCheck; } set { _MiMiAiCheck = value; Save(); } }

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