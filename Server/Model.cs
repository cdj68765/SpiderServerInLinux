﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FileMode = System.IO.FileMode;

namespace SpiderServerInLinux
{
    internal static class Setting
    {
        internal static int LoopTime = 5000;
        internal static bool Platform = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64" ? true : false;
        internal static GlobalSet _GlobalSet;
        internal static BlockingCollection<NyaaInfo> WordProcess = new BlockingCollection<NyaaInfo>();
        internal static int Socks5Point;
        internal static string JavDownLoadNow;
        internal static string NyaaDownLoadNow;
        internal static string MiMiDownLoadNow;
        internal static server server;
        internal static readonly CancellationTokenSource CancelSign = new CancellationTokenSource();
        internal static readonly TaskCompletionSource<byte> ShutdownResetEvent = new TaskCompletionSource<byte>();
        internal static readonly string NyaaAddress = "https://sukebei.nyaa.si/view/";
        internal static string NyaaDay = "";//过去下载每条Nyaa用
        internal static string MiMiDay = "";//过去下载每条MiMi用
    }

    [Serializable]
    internal class GlobalSet
    {
        private string _NyaaAddress = "https://sukebei.nyaa.si/?p=";
        private string _JavAddress = "https://www.141jav.com/new?page=";
        private string _MiMiAiAddress = "http://www.mmfhd.com/forumdisplay.php?fid=55&page=";
        private string _ssr_url;
        private int _NyaaLastPageIndex = 0;
        private int _JavLastPageIndex = 0;
        private int _MiMiAiPageIndex = 0;
        private int _ConnectPoint = 2222;
        private long _totalUploadBytes = 0;
        private long _totalDownloadBytes = 0;
        private bool _SocksCheck = false;
        private bool _NyaaFin = false;
        private bool _JavFin = false;
        private bool _MiMiFin = false;
        private bool _AutoRun = false;
        internal string NyaaAddress { get { return _NyaaAddress; } set { _NyaaAddress = value; Save(); } }
        internal string JavAddress { get { return _JavAddress; } set { _JavAddress = value; Save(); } }
        internal string ssr_url { get { return _ssr_url; } set { _ssr_url = value; Save(); } }
        internal int NyaaLastPageIndex { get { return _NyaaLastPageIndex; } set { _NyaaLastPageIndex = value; Save(); } }
        internal int JavLastPageIndex { get { return _JavLastPageIndex; } set { _JavLastPageIndex = value; Save(); } }
        internal int ConnectPoint { get { return _ConnectPoint; } set { _ConnectPoint = value; Save(); } }
        internal bool SocksCheck { get { return _SocksCheck; } set { _SocksCheck = value; Save(); } }
        internal bool NyaaFin { get { return _NyaaFin; } set { _NyaaFin = value; Save(); } }
        internal bool JavFin { get { return _JavFin; } set { _JavFin = value; Save(); } }
        internal bool MiMiFin { get { return _MiMiFin; } set { _MiMiFin = value; Save(); } }
        internal bool AutoRun { get { return _AutoRun; } set { _AutoRun = value; Save(); } }
        internal string MiMiAiAddress { get { return _MiMiAiAddress; } set { _MiMiAiAddress = value; Save(); } }

        internal int MiMiAiPageIndex { get { return _MiMiAiPageIndex; } set { _MiMiAiPageIndex = value; Save(); } }
        internal long totalUploadBytes { get { return _totalUploadBytes; } set { _totalUploadBytes = value; Save(); } }
        internal long totalDownloadBytes { get { return _totalDownloadBytes; } set { _totalDownloadBytes = value; Save(); } }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (string.IsNullOrEmpty(_NyaaAddress)) _NyaaAddress = "https://sukebei.nyaa.si/?p=";
            if (string.IsNullOrEmpty(_JavAddress)) _JavAddress = "https://www.141jav.com/new?page=";
            if (string.IsNullOrEmpty(_MiMiAiAddress)) _MiMiAiAddress = "http://www.mmbutt.com/forumdisplay.php?fid=55&page=";
            if (string.IsNullOrEmpty(_ssr_url)) _ssr_url = "ssr://MjEwLjE1Mi4xMi44OTozNDExMjphdXRoX2NoYWluX2E6Y2hhY2hhMjAtaWV0ZjpodHRwX3NpbXBsZTpOalk0T0RjM05qVS8_b2Jmc3BhcmFtPSZwcm90b3BhcmFtPSZyZW1hcmtzPTVwZWw1cHlzSUdJeUlDMGdXLWlCbENfbnA3dGRJRWxFUTBZZzVMaWM1THFzJmdyb3VwPTQ0R1Q0NEd2NDRLTEx1aVFqT09CaUEmdWRwcG9ydD00NTg3NjcmdW90PTE1NzA0OTM2";
        }

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

        [NonSerialized]
        private bool SaveInUse = false;

        private GlobalSet Save()
        {
            if (SaveInUse == true) return this;
            SaveInUse = true;
            try
            {
                using (Stream stream = new FileStream("GlobalSet.dat", FileMode.OpenOrCreate))
                {
                    IFormatter Fileformatter = new BinaryFormatter();
                    Fileformatter.Serialize(stream, this);
                }
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