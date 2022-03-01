using Cowboy.WebSockets;
using LiteDB;
using ServiceWire.NamedPipes;
using ServiceWire.TcpIp;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using WebpInter;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace WebPTest
{
    [Serializable]
    public class SendMessage : IDisposable
    {
        public int ReadFromSIS;
        public int ReadFromT66y;
        public int SendCount;
        public int SaveCount;
        public int PassCount;

        public List<CLientInfo> ClientCount = new List<CLientInfo>();
        public string CurrectDataBase;

        public List<string> Message = new List<string>();
        public int WaitSendCount;
        public int WaitSaveCount;
        public string Mem;

        public static SendMessage ToClass(byte[] data)
        {
            using var stream = new MemoryStream(data);
            IFormatter Fileformatter = new BinaryFormatter();
            Fileformatter.Binder = new UBinder();
            return Fileformatter.Deserialize(stream) as SendMessage;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClientCount.Clear();
                Message.Clear();
            }
        }

        ~SendMessage()
        {
            this.Dispose(false);
        }

        public class UBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.EndsWith("SendMessage"))
                {
                    return typeof(SendMessage);
                }
                if (typeName.EndsWith("CLientInfo"))
                {
                    return typeof(CLientInfo);
                }
                else
                    if (typeName.Contains("CLientInfo"))
                {
                    if (typeName.Contains("List"))
                        return typeof(List<CLientInfo>);
                    else
                        return typeof(Dictionary<string, CLientInfo>);
                }
                return (Assembly.GetExecutingAssembly()).GetType(typeName);
            }

            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                base.BindToName(serializedType, out assemblyName, out typeName);
            }
        }
    }

    [Serializable]
    public class CLientInfo
    {
        public int SendCount;
        public int GetCount;
        public long SendByte;
        public long GetByte;
        public string IP;
    }

    internal class Program
    {
        public enum ImageType
        {
            Unknown,
            JPEG,
            PNG,
            GIF,
            BMP,
            TIFF,
        }

        private static ImageType GetFileImageTypeFromHeader(byte[] headerBytes)
        {
            //JPEG:
            if (headerBytes[0] == 0xFF &&//FF D8
                headerBytes[1] == 0xD8 &&
                (
                 (headerBytes[6] == 0x4A &&//'JFIF'
                  headerBytes[7] == 0x46 &&
                  headerBytes[8] == 0x49 &&
                  headerBytes[9] == 0x46)
                  ||
                 (headerBytes[6] == 0x45 &&//'EXIF'
                  headerBytes[7] == 0x78 &&
                  headerBytes[8] == 0x69 &&
                  headerBytes[9] == 0x66)
                ) &&
                headerBytes[10] == 00)
            {
                return ImageType.JPEG;
            }
            //PNG
            if (headerBytes[0] == 0x89 && //89 50 4E 47 0D 0A 1A 0A
                headerBytes[1] == 0x50 &&
                headerBytes[2] == 0x4E &&
                headerBytes[3] == 0x47 &&
                headerBytes[4] == 0x0D &&
                headerBytes[5] == 0x0A &&
                headerBytes[6] == 0x1A &&
                headerBytes[7] == 0x0A)
            {
                return ImageType.PNG;
            }
            //GIF
            if (headerBytes[0] == 0x47 &&//'GIF'
                headerBytes[1] == 0x49 &&
                headerBytes[2] == 0x46)
            {
                return ImageType.GIF;
            }
            //BMP
            if (headerBytes[0] == 0x42 &&//42 4D
                headerBytes[1] == 0x4D)
            {
                return ImageType.BMP;
            }
            //TIFF
            if ((headerBytes[0] == 0x49 &&//49 49 2A 00
                 headerBytes[1] == 0x49 &&
                 headerBytes[2] == 0x2A &&
                 headerBytes[3] == 0x00)
                 ||
                (headerBytes[0] == 0x4D &&//4D 4D 00 2A
                 headerBytes[1] == 0x4D &&
                 headerBytes[2] == 0x00 &&
                 headerBytes[3] == 0x2A))
            {
                return ImageType.TIFF;
            }

            return ImageType.Unknown;
        }

        private static string HumanReadableFilesize(double size)
        {
            var units = new[] { "B", "KB", "MB", "GB", "TB", "PB" };
            double mod = 1024.0;
            var DoubleCount = new List<double>();
            while (size >= mod)
            {
                size /= mod;
                DoubleCount.Add(size);
            }
            var Ret = "";
            for (int j = DoubleCount.Count; j > 0; j--)
            {
                if (j == DoubleCount.Count)
                {
                    Ret += $"{Math.Floor(DoubleCount[j - 1])}{units[j]}";
                }
                else
                {
                    Ret += $"{Math.Floor(DoubleCount[j - 1] - (Math.Floor(DoubleCount[j]) * 1024))}{units[j]}";
                }
            }
            return Ret;
        }

        internal static readonly TaskCompletionSource<byte> ShutdownResetEvent = new TaskCompletionSource<byte>();
        //internal static readonly List<string> LocalInfoC = new List<string>();
        //internal static readonly List<string> Remote = new List<string>();

        private static BlockingCollection<WebpImage> WriteOpera = new BlockingCollection<WebpImage>(128);
        // private static Channel<WebpImage> WriteOpera = Channel.CreateBounded<WebpImage>(new BoundedChannelOptions(20) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });

        // private static BlockingCollection<string> WriteSend = new BlockingCollection<string>();
        private static BlockingCollection<string> WriteDel = new BlockingCollection<string>();

        private static BlockingCollection<WebpImage> save = new BlockingCollection<WebpImage>(1024);

        //internal static Channel<string> save = Channel.CreateBounded<string>(new BoundedChannelOptions(1024) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
        private static ConcurrentDictionary<int, Tuple<string, Stopwatch>> MirrorBody = new ConcurrentDictionary<int, Tuple<string, Stopwatch>>();

        private static string BaseUri = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64" ? @"Z:\publish\" : @"/media/sda1/publish/";

        //public class OriImage
        //{
        //    public string id { get; set; }
        //    public string From { get; set; }

        //    public string Date { get; set; }
        //    public string Hash { get; set; }
        //    public byte[] img { get; set; }
        //    public List<int> FromList { get; set; }
        //    public bool Status { get; set; }
        //}
        private static string CurrectBase = "";

        private static async Task<int> Main(string[] args)
        {
            //var SaveF = new BlockingCollection<byte[]>(10);
            //var read = File.Open(@$"G:\WDC WD10JPVX-00J 1A01.img", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            //long size = 1024 * 1024 * 100;

            //_ = Task.Factory.StartNew(async () =>
            //  {
            //      var filepath = @$"L:\WDC WD10JPVX-00J 1A01.img";
            //      if (File.Exists(filepath))
            //          File.Delete(filepath);
            //      var Write = File.Open(filepath, FileMode.CreateNew);

            //      foreach (var item in SaveF.GetConsumingEnumerable())
            //      {
            //          await Write.WriteAsync(item, 0, item.Length);
            //      }
            //      await Write.DisposeAsync();
            //  });
            //var first = new byte[1024];
            //var Writefirst = new List<byte>();
            //read.Read(first, 0, 1024);
            //for (int i = 0; i < first.Length - 248; i++)
            //{
            //    Writefirst.Add(first[i]);
            //}
            //SaveF.Add(Writefirst.ToArray());
            //long Posion = 1024;
            //do
            //{
            //    if (read.Length - Posion > size)
            //    {
            //        var Writing = new byte[size];
            //        await read.ReadAsync(Writing, 0, Writing.Length);
            //        SaveF.Add(Writing);
            //        Posion += size;
            //    }
            //    else
            //    {
            //        var Writing = new byte[(int)(read.Length - Posion) + 247];
            //        await read.ReadAsync(Writing, 0, Writing.Length);

            //        SaveF.Add(Writing);
            //        Posion += Writing.Length;
            //        break;
            //    }

            //    Console.WriteLine($"{Math.Round((double)read.Position / (double)read.Length, 4) * 100}");
            //}
            //while (true);
            //SaveF.CompleteAdding();
            //read.Dispose();
            //Console.WriteLine("完成");
            //Console.ReadLine();
            //using var db = new LiteDatabase(@$"Filename=E:\publish\SIS.db");
            //using var db2 = new LiteDatabase(@$"Filename=E:\publish\NSIS.db");
            //var cc = 0;
            //foreach (var item in db.GetCollectionNames())
            //{
            //    cc = 0;
            //    if (item == "SISData")
            //    {
            //        var T66yDB = db2.GetCollection<SISData>("SISData");
            //        T66yDB.EnsureIndex(x => x.Title);
            //        T66yDB.EnsureIndex(x => x.Uri);
            //        T66yDB.EnsureIndex(x => x.Date);
            //        T66yDB.EnsureIndex(x => x.Status);
            //    }
            //    if (item == "ImgData")
            //    {
            //        var T66yDB = db2.GetCollection<T66yImgData>("ImgData");
            //        T66yDB.EnsureIndex(x => x.Status);
            //        T66yDB.EnsureIndex(x => x.Hash);
            //        T66yDB.EnsureIndex(x => x.id);
            //        T66yDB.EnsureIndex(x => x.Date);
            //        continue;
            //    }
            //    var Temp = db2.GetCollection(item);
            //    foreach (var item2 in db.GetCollection(item).FindAll())
            //    {
            //        cc += 1;
            //        Temp.Upsert(item2);
            //        Console.WriteLine($"{item}|{cc}");
            //    }
            //}
            //db2.Dispose();
            //Console.WriteLine("完成");
            //Console.ReadLine();
            //         {
            //             var client = new TcpClient<IImage2Webp>(new TcpZkEndPoint("", "",
            //new IPEndPoint(IPAddress.Parse("192.168.2.95"), 2222), connectTimeOutMs: 2500));
            //             string Status = "start";
            //             while (true)
            //             {
            //                 try
            //                 {
            //                     var Ret = client.Proxy.Get3(Status);
            //                     CurrectBase = Ret.Item3;
            //                     System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            //                     if (Encoding.UTF8.GetString(md5.ComputeHash(Ret.Item1)) == Ret.Item2)
            //                     {
            //                         if (!File.Exists(Ret.Item3))
            //                             File.Create(Ret.Item3).Dispose();
            //                         FileStream fileStream = new FileStream(Ret.Item3, FileMode.Append, FileAccess.Write);
            //                         fileStream.Write(Ret.Item1, 0, Ret.Item1.Length);
            //                         fileStream.Dispose();
            //                         if (Ret.Item3 == "fin")
            //                         {
            //                             break;
            //                         }
            //                         Status = "continue";
            //                     }
            //                     else
            //                     {
            //                         Status = "redownload";
            //                         if (File.Exists(Ret.Item3))
            //                         {
            //                             do
            //                             {
            //                                 File.Delete(Ret.Item3);
            //                                 Thread.Sleep(1000);
            //                             } while (File.Exists(Ret.Item3));
            //                         }
            //                     }
            //                 }
            //                 catch (Exception)
            //                 {
            //                     if (File.Exists(CurrectBase))
            //                     {
            //                         do
            //                         {
            //                             File.Delete(CurrectBase);
            //                             Thread.Sleep(1000);
            //                         } while (File.Exists(CurrectBase));
            //                     }
            //                 }
            //             }
            //         }

            string ShowMessage = "";
            bool ShutDownSign = false;
            var Client_IP = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(x => x.ToString().StartsWith("192.168.2"));
            string ClientIP = string.Empty;
            if (Client_IP != null)
                ClientIP = Client_IP.ToString();
            else ClientIP = Dns.GetHostName();

            #region 遗留代码

            //{
            //    using var Tempdb = new LiteDatabase($@"Filename=C:\Users\cdj68\Desktop\2021-08-022.db;");
            //    Tempdb.BeginTrans();
            //    var CCC = 0;
            //back:
            //    try
            //    {
            //        {
            //            using var Tempd = new LiteDatabase(File.Open(@$"C:\Users\cdj68\Desktop\2021-08-02.db", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            //            var o1 = Tempd.GetCollection<WebpImage>("WebpData");
            //            var o2 = Tempdb.GetCollection<WebpImage>("WebpData");

            //            foreach (var item in o1.Find(Query.All(), CCC))
            //            {
            //                CCC += 1;

            //                o2.Upsert(item);
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex);
            //        Console.WriteLine(CCC);
            //        CCC += 1;
            //        goto back;
            //    }
            //    Tempdb.Commit();
            //    Tempdb.Dispose();
            //}
            //{
            //    using var Tempdb = new LiteDatabase($@"Filename=C:\Users\cdj68\Desktop\2021-08-022.db;");
            //    if (!Tempdb.CollectionExists("ImgData"))
            //    {
            //        var TempImg = Tempdb.GetCollection<OriImage>("ImgData");
            //        TempImg.EnsureIndex(x => x.id);
            //        TempImg.EnsureIndex(x => x.From);
            //        TempImg.EnsureIndex(x => x.Status);
            //        var TempWebp = Tempdb.GetCollection<WebpImage>("WebpData");
            //        TempWebp.EnsureIndex(x => x.id);
            //        TempWebp.EnsureIndex(x => x.From);
            //    }
            //    Tempdb.BeginTrans();
            //    var CCC = 0;
            //back:
            //    try
            //    {
            //        {
            //            using var SISWebdb = new LiteDatabase($@"Filename=E:\DataBaseFix\SIS.db;");

            //            var SISDB = SISWebdb.GetCollection<SISImgData>("ImgData");

            //            var o1 = Tempdb.GetCollection<OriImage>("ImgData");

            //            foreach (var item in SISDB.Find(x => x.Date == "2021-8-2"))
            //            {
            //                CCC += 1;
            //                var Date = string.Empty;
            //                if (DateTime.TryParse(item.Date, out DateTime date))
            //                    Date = date.ToString("yyyy-MM-dd");
            //                else
            //                    Date = "1970-01-01";
            //                var Add = new OriImage()
            //                {
            //                    Date = Date,
            //                    From = "SIS",
            //                    FromList = item.FromList,
            //                    Hash = item.Hash,
            //                    id = item.id,
            //                    img = item.img,
            //                    Status = false
            //                };
            //                o1.Upsert(Add);
            //            }
            //        }
            //        {
            //            using var SISWebdb = new LiteDatabase($@"Filename=E:\DataBaseFix\T66y.db;");

            //            var SISDB = SISWebdb.GetCollection<T66yImgData>("ImgData");

            //            var o1 = Tempdb.GetCollection<OriImage>("ImgData");

            //            foreach (var item in SISDB.Find(x => x.Date == "2021-08-02"))
            //            {
            //                CCC += 1;
            //                var Date = string.Empty;
            //                if (DateTime.TryParse(item.Date, out DateTime date))
            //                    Date = date.ToString("yyyy-MM-dd");
            //                else
            //                    Date = "1970-01-01";
            //                var Add = new OriImage()
            //                {
            //                    Date = Date,
            //                    From = "T66y",
            //                    FromList = item.FromList,
            //                    Hash = item.Hash,
            //                    id = item.id,
            //                    img = item.img,
            //                    Status = false
            //                };
            //                o1.Upsert(Add);
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex);
            //        Console.WriteLine(CCC);
            //        CCC += 1;
            //        goto back;
            //    }
            //    Tempdb.Commit();
            //    Tempdb.Dispose();
            //}

            //var ImgPath = $"{BaseUri}Image{Path.DirectorySeparatorChar}";

            //ConcurrentDictionary<string, int> ReTry = new ConcurrentDictionary<string, int>();
            //List<string> Pass = new List<string>();
            //if (File.Exists("Temp"))
            //{
            //    Pass.AddRange(File.ReadAllLines("Temp"));
            //}
            //var DirInfo = new DirectoryInfo(ImgPath).GetFiles().Select(x => new Tuple<string, long>(x.Name, x.Length)).ToArray();
            //var ReadCount = 0;
            //foreach (var Dir in DirInfo)
            //{
            //    ReadCount += 1;
            //    if (Pass.Exists(x => x == Dir.Item1)) continue;
            //    if (Dir.Item1.ToLower().Contains("log")) continue;

            //    var Tempdb2 = new LiteDatabase(File.Open(@$"{ImgPath}{Dir.Item1}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            //    var ForeachList = Tempdb2.GetCollection<OriImage>("ImgData").Find(x => !x.Status).Select(x => x.id).ToArray();
            //    var ForeachList2 = Tempdb2.GetCollection<WebpImage>("WebpData").FindAll().Select(x => x.id).ToArray();
            //    Tempdb2.Dispose();
            //    GC.Collect();

            //    //Tempdb2.Dispose();
            //    //file.Close();
            //    //file.Dispose();
            //    //foreach (var Image in Find)
            //    //{
            //    //    File.AppendAllLines(Dir.Item1, new string[] { Image });
            //    //}
            //    //GC.Collect();
            //    var R3 = 0;

            //Back:
            //    var R0 = 0;
            //    var R1 = 0;
            //    var R2 = 0;

            //    var Check = false;
            //    var TempFindDb = new LiteDatabase(@$"{ImgPath}{Dir.Item1}");
            //    TempFindDb.BeginTrans();
            //    foreach (var ID in ForeachList2)
            //    {
            //        R0 += 1;
            //        var WebpData = TempFindDb.GetCollection<WebpImage>("WebpData");
            //        var ImgData = TempFindDb.GetCollection<OriImage>("ImgData");
            //        var item = WebpData.FindById(ID);
            //        bool change = false;

            //        if (item.Type == null)
            //        {
            //            Check = true;
            //            var TempFind = ImgData.FindById(ID);
            //            if (TempFind != null)
            //            {
            //                try
            //                {
            //                    if (GetFileImageTypeFromHeader(TempFind.img) == ImageType.JPEG)
            //                        item.Type = "jpg";
            //                    else if (GetFileImageTypeFromHeader(TempFind.img) == ImageType.GIF)
            //                        item.Type = "gif";
            //                    else
            //                        item.Type = GetFileImageTypeFromHeader(TempFind.img).ToString();
            //                }
            //                catch (Exception)
            //                {
            //                    item.Type = "False";
            //                }

            //                change = true;
            //                R1 += 1;
            //            }
            //            else
            //            {
            //                if (item.From == "T66y")
            //                {
            //                    using var T66y = new LiteDatabase(File.Open(@$"{BaseUri}T66y.db", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            //                    var Find = T66y.GetCollection<T66yImgData>("ImgData").FindById(ID);
            //                    if (Find != null)
            //                    {
            //                        try
            //                        {
            //                            if (GetFileImageTypeFromHeader(Find.img) == ImageType.JPEG)
            //                                item.Type = "jpg";
            //                            else if (GetFileImageTypeFromHeader(Find.img) == ImageType.GIF)
            //                                item.Type = "gif";
            //                            else
            //                                item.Type = GetFileImageTypeFromHeader(Find.img).ToString();
            //                        }
            //                        catch (Exception)
            //                        {
            //                            item.Type = "False";
            //                        }

            //                        change = true;
            //                        R1 += 1;
            //                    }
            //                }
            //                else if (item.From == "SIS")
            //                {
            //                    using var T66y = new LiteDatabase(File.Open(@$"{BaseUri}SIS.db", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            //                    var Find = T66y.GetCollection<SISImgData>("ImgData").FindById(ID);
            //                    if (Find != null)
            //                    {
            //                        try
            //                        {
            //                            if (GetFileImageTypeFromHeader(Find.img) == ImageType.JPEG)
            //                                item.Type = "jpg";
            //                            else if (GetFileImageTypeFromHeader(Find.img) == ImageType.GIF)
            //                                item.Type = "gif";
            //                            else
            //                                item.Type = GetFileImageTypeFromHeader(Find.img).ToString();
            //                        }
            //                        catch (Exception)
            //                        {
            //                            item.Type = "False";
            //                        }

            //                        change = true;
            //                        R1 += 1;
            //                    }
            //                }
            //                else
            //                {
            //                    change = false;
            //                    R1 += 1;
            //                }
            //            }
            //        }
            //        try
            //        {
            //            if (GetFileImageTypeFromHeader(item.img) != ImageType.Unknown)
            //            {
            //                if (item.Status)
            //                {
            //                    Check = true;
            //                    item.Status = false;
            //                    change = true;
            //                    R2 += 1;
            //                }
            //            }
            //        }
            //        catch (Exception)
            //        {
            //            var OriImgData = ImgData.FindById(item.id);
            //            if (OriImgData != null)
            //            {
            //                OriImgData.Status = true;
            //                ImgData.Update(OriImgData);
            //                item.Status = false;
            //                if (OriImgData.img != null)
            //                {
            //                    item.img = OriImgData.img;
            //                    WebpData.Update(item);
            //                }
            //                else WebpData.Delete(item.id);
            //            }
            //            else WebpData.Delete(item.id);
            //        }

            //        if (change)
            //        {
            //            if (item != null)
            //            {
            //                if (WebpData.Exists(x => x.id == item.id))
            //                    WebpData.Update(item);
            //                else
            //                    WebpData.Upsert(item);
            //                var OriImgData = ImgData.FindById(item.id);
            //                if (OriImgData != null)
            //                {
            //                    OriImgData.Status = true;
            //                    ImgData.Update(OriImgData);
            //                }
            //            }
            //        }
            //        Console.WriteLine($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {R0} | {R1} | {R2} | {Dir.Item1}/{ReadCount}/{DirInfo.Length} | {R3}");
            //    }
            //    TempFindDb.Commit();
            //    TempFindDb.Dispose();
            //    if (Check)
            //    {
            //        R3 += 1;
            //        if (R3 > 2)
            //        {
            //            Console.WriteLine();
            //        }
            //        goto Back;
            //    }
            //    R3 = 0;
            //    File.AppendAllLines("Temp", new string[] { Dir.Item1 });
            //}

            //var ImagePath = $"{BaseUri}Image{Path.DirectorySeparatorChar}";
            //var ImagePath2 = $"{BaseUri}Image2{Path.DirectorySeparatorChar}";
            //var DirInfo = new DirectoryInfo(ImagePath).GetFiles().Select(x => new Tuple<string, long>(x.Name, x.Length)).ToList();
            //long Size = 0;
            //long size2 = DirInfo.Sum(x => x.Item2);
            //var Count = 0;
            //foreach (var Date in DirInfo)
            //{
            //    Size += Date.Item2;
            //    Count += 1;
            //    Console.WriteLine($"{Count}/{DirInfo.Count}    |       {HumanReadableFilesize(Size)}/{HumanReadableFilesize(size2)}");
            //    using var Tempdb = new LiteDatabase(File.Open(@$"{ImagePath}{Date.Item1}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            //    using var Tempdb2 = new LiteDB4.LiteDatabase($@"Filename={ImagePath2}{Date.Item1};");
            //    if (!Tempdb2.CollectionExists("ImgData"))
            //    {
            //        var TempImg = Tempdb2.GetCollection<OriImage>("ImgData");
            //        TempImg.EnsureIndex(x => x.id);
            //        TempImg.EnsureIndex(x => x.From);
            //        TempImg.EnsureIndex(x => x.Status);
            //        var TempWebp = Tempdb2.GetCollection<WebpImage>("WebpData");
            //        TempWebp.EnsureIndex(x => x.id);
            //        TempWebp.EnsureIndex(x => x.From);
            //    }
            //    {
            //        var TempImg1 = Tempdb.GetCollection<OriImage>("ImgData");
            //        var TempImg2 = Tempdb2.GetCollection<OriImage>("ImgData");
            //        foreach (var item in TempImg1.FindAll())
            //        {
            //            TempImg2.Upsert(item);
            //        }
            //    }
            //    {
            //        var TempImg1 = Tempdb.GetCollection<WebpImage>("WebpData");
            //        var TempImg2 = Tempdb2.GetCollection<WebpImage>("WebpData");
            //        foreach (var item in TempImg1.FindAll())
            //        {
            //            TempImg2.Upsert(item);
            //        }
            //    }
            //    Tempdb.Dispose();
            //    Tempdb2.Dispose();
            //}
            //Console.WriteLine("Fin");
            //Console.ReadLine();
            {
                //using var SISWeb = new LiteDatabase(@"SISWeb.db");
                //if (!SISWeb.CollectionExists("ImgData"))
                //{
                //    var Temp = SISWeb.GetCollection("SISWeb");
                //    Temp.EnsureIndex(x => x["Uri"]);
                //    Temp.EnsureIndex(x => x["Status"]);
                //}
                //using var T66yWeb = new LiteDatabase(@"T66yWeb.db");
                //if (!T66yWeb.CollectionExists("ImgData"))
                //{
                //    var Temp = SISWeb.GetCollection("SISWeb");
                //    Temp.EnsureIndex(x => x["Uri"]);
                //    Temp.EnsureIndex(x => x["Status"]);
                //}
            }
            //string BaseUri2 = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64" ? @"C:\Users\cdj68\Desktop\" : @"/media/sda1/publish/";
            //var SIS = new LiteDatabase(File.Open(@$"{BaseUri}SIS.db", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            //var Image = new LiteDatabase(File.Open(@$"{BaseUri}Image.db", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            //if (!Directory.Exists($"{BaseUri2}{Path.DirectorySeparatorChar}Image"))
            //{
            //    Directory.CreateDirectory($"{BaseUri2}{Path.DirectorySeparatorChar}Image");
            //}

            //var Count = 0;
            //var Count2 = 0;
            //var TotalSize = new FileInfo(@$"{BaseUri}SIS.db").Length;
            //var HM = HumanReadableFilesize(TotalSize);
            //long SizeCount = 0;
            //{
            //    void ReadOrAdd(SISImgData item)
            //    {
            //        if (item.img != null)
            //        {
            //            SizeCount += item.img.Length;
            //            if (item.img.Length > 1024)
            //            {
            //                var Date = string.Empty;
            //                if (DateTime.TryParse(item.Date, out DateTime date))
            //                    Date = date.ToString("yyyy-MM-dd");
            //                else
            //                    Date = "1970-01-01";
            //                using var Tempdb = new LiteDatabase($@"Filename={BaseUri2}{Path.DirectorySeparatorChar}Image{Path.DirectorySeparatorChar}{Date}.db;");
            //                if (!Tempdb.CollectionExists("ImgData"))
            //                {
            //                    var TempImg = Tempdb.GetCollection<OriImage>("ImgData");
            //                    TempImg.EnsureIndex(x => x.id);
            //                    TempImg.EnsureIndex(x => x.From);
            //                    TempImg.EnsureIndex(x => x.Status);
            //                    var TempWebp = Tempdb.GetCollection<WebpImage>("WebpData");
            //                    TempWebp.EnsureIndex(x => x.id);
            //                    TempWebp.EnsureIndex(x => x.From);
            //                }
            //                var Add = new OriImage()
            //                {
            //                    Date = Date,
            //                    From = "SIS",
            //                    FromList = item.FromList,
            //                    Hash = item.Hash,
            //                    id = item.id,
            //                    img = item.img,
            //                    Status = false
            //                };
            //                var Find = Image.GetCollection<ImgData>("ImgData").FindById(item.id);
            //                if (Find != null)
            //                {
            //                    if (Find.img != null)
            //                    {
            //                        Add.Status = true;
            //                        var Add2 = new WebpImage()
            //                        {
            //                            Date = Date,
            //                            From = "SIS",
            //                            Type = Find.Type,
            //                            FromList = item.FromList,
            //                            Hash = item.Hash,
            //                            id = item.id,
            //                            img = Find.img,
            //                            Status = true
            //                        };
            //                        var TempWebp = Tempdb.GetCollection<WebpImage>("WebpData");
            //                        try
            //                        {
            //                            TempWebp.Upsert(Add2);
            //                            Count2 += 1;
            //                        }
            //                        catch (Exception ex)
            //                        {
            //                            Console.WriteLine($"WebpData添加失败，等待处理{ex}");
            //                            Console.ReadLine();
            //                        }
            //                    }
            //                }
            //                var Temp = Tempdb.GetCollection<OriImage>("ImgData");
            //                try
            //                {
            //                    Temp.Upsert(Add);
            //                }
            //                catch (Exception ex)
            //                {
            //                    Console.WriteLine($"ImgData添加失败，等待处理{ex}");
            //                    Console.ReadLine();
            //                }
            //                Tempdb.Dispose();
            //            }
            //        }
            //        Count += 1;
            //        Console.WriteLine($"SIS:{Count}  ||   {Count2}   ||   {HumanReadableFilesize(SizeCount)}//{HM}   ||   {(SizeCount / TotalSize) * 100.0}%");
            //    }
            //    try
            //    {
            //        foreach (var item in SIS.GetCollection<SISImgData>("ImgData").FindAll())
            //        {
            //            ReadOrAdd(item);
            //        }
            //    }
            //    catch (Exception)
            //    {
            //    back:
            //        try
            //        {
            //            foreach (var item in SIS.GetCollection<SISImgData>("ImgData").Find(Query.All(), skip: Count))
            //            {
            //                ReadOrAdd(item);
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            Console.WriteLine(ex.Message);
            //            goto back;
            //        }
            //    }
            //}
            //Count = 0;
            //Count2 = 0;
            //SIS.Dispose();
            //TotalSize = new FileInfo(@$"{BaseUri}T66y.db").Length;
            //HM = HumanReadableFilesize(TotalSize);
            //SizeCount = 0;
            //using var T66y = new LiteDatabase(File.Open(@$"{BaseUri}T66y.db", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            //{
            //    void ReadOrAdd(T66yImgData item)
            //    {
            //        if (item.img != null)
            //        {
            //            SizeCount += item.img.Length;

            //            if (item.img.Length > 1024)
            //            {
            //                var Date = string.Empty;
            //                if (DateTime.TryParse(item.Date, out DateTime date))
            //                    Date = date.ToString("yyyy-MM-dd");
            //                else
            //                    Date = "1970-01-01";
            //                using var Tempdb = new LiteDatabase($@"Filename={BaseUri2}{Path.DirectorySeparatorChar}Image{Path.DirectorySeparatorChar}{Date}.db;Connection=Shared;");
            //                if (!Tempdb.CollectionExists("ImgData"))
            //                {
            //                    var TempImg = Tempdb.GetCollection<OriImage>("ImgData");
            //                    TempImg.EnsureIndex(x => x.id);
            //                    TempImg.EnsureIndex(x => x.From);
            //                    TempImg.EnsureIndex(x => x.Status);
            //                    var TempWebp = Tempdb.GetCollection<WebpImage>("WebpData");
            //                    TempWebp.EnsureIndex(x => x.id);
            //                    TempWebp.EnsureIndex(x => x.From);
            //                }
            //                var Add = new OriImage()
            //                {
            //                    Date = Date,
            //                    From = "T66y",
            //                    FromList = item.FromList,
            //                    Hash = item.Hash,
            //                    id = item.id,
            //                    img = item.img,
            //                    Status = false
            //                };
            //                var Find = Image.GetCollection<ImgData>("ImgData").FindById(item.id);
            //                if (Find != null)
            //                {
            //                    if (Find.img != null)
            //                    {
            //                        Add.Status = true;
            //                        var Add2 = new WebpImage()
            //                        {
            //                            Date = Date,
            //                            From = "T66y",
            //                            FromList = item.FromList,
            //                            Type = Find.Type,
            //                            Hash = item.Hash,
            //                            id = item.id,
            //                            img = Find.img,
            //                            Status = true
            //                        };
            //                        var TempWebp = Tempdb.GetCollection<WebpImage>("WebpData");
            //                        try
            //                        {
            //                            TempWebp.Upsert(Add2);
            //                            Count2 += 1;
            //                        }
            //                        catch (Exception ex)
            //                        {
            //                            Console.WriteLine($"WebpData添加失败，等待处理{ex}");
            //                            Console.ReadLine();
            //                        }
            //                    }
            //                }
            //                var Temp = Tempdb.GetCollection<OriImage>("ImgData");
            //                try
            //                {
            //                    Temp.Upsert(Add);
            //                }
            //                catch (Exception ex)
            //                {
            //                    Console.WriteLine($"ImgData添加失败，等待处理{ex}");
            //                    Console.ReadLine();
            //                }
            //                Tempdb.Dispose();
            //            }
            //        }
            //        Count += 1;
            //        Console.WriteLine($"T66y:{Count}  ||   {Count2}   ||   {HumanReadableFilesize(SizeCount)}//{HM}   ||   {(SizeCount / TotalSize) * 100.0}%");
            //    }
            //    {
            //        try
            //        {
            //            foreach (var item in T66y.GetCollection<T66yImgData>("ImgData").FindAll())
            //            {
            //                ReadOrAdd(item);
            //            }
            //        }
            //        catch (Exception)
            //        {
            //        back:
            //            try
            //            {
            //                foreach (var item in T66y.GetCollection<T66yImgData>("ImgData").Find(Query.All(), skip: Count))
            //                {
            //                    ReadOrAdd(item);
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                Console.WriteLine(ex.Message);
            //                goto back;
            //            }
            //        }
            //    }
            //}
            //T66y.Dispose();
            //Console.WriteLine("Fin");
            //Console.ReadLine();
            //using var db = new LiteDatabase($@"Filename=Z:\publish\Image.db;Connection=Direct;");
            //using var db2 = new LiteDatabase($@"Filename=Z:\publish\Image3.db;Connection=Direct;");

            //if (!db2.CollectionExists("ImgData"))
            //{
            //    var SIS = db2.GetCollection<ImgData>("ImgData");
            //    SIS.EnsureIndex(x => x.Date);
            //    SIS.EnsureIndex(x => x.Hash);
            //    SIS.EnsureIndex(x => x.id);
            //    SIS.EnsureIndex(x => x.Status);
            //    SIS.EnsureIndex(x => x.Type);
            //}
            //var Count = 0;
            //var Erro2 = 0;
            //var S1 = db.GetCollection<ImgData>("ImgData");
            //var S2 = db2.GetCollection<ImgData>("ImgData");
            //var Read = File.ReadAllText(@"Z:\publish\TempImage.txt");
            //var Red = Read.Split(new[] { "AAEAAAD" }, StringSplitOptions.None);
            //StringBuilder sb = new StringBuilder();
            //foreach (var item in Red)
            //{
            //    if (string.IsNullOrEmpty(item)) continue;
            //    Count += 1;
            //    try
            //    {
            //        sb.Append($"AAEAAAD{item}");
            //        var Find = ImgData.ToClass(Convert.FromBase64String(sb.ToString()));
            //        sb.Clear();
            //        if (!S1.Exists(x => x.id == Find.id))
            //        {
            //            S1.Upsert(Find);
            //        }
            //    }
            //    catch (Exception)
            //    {
            //        Erro2 += 1;
            //    }
            //    Console.WriteLine($"{Count}/{Red.Length}|{(Count / Red.Length) * 100}     |{Erro2}");
            //}
            //Console.WriteLine("Fin");
            //Console.ReadLine();
            //while (true)
            //{
            //    try
            //    {
            //        foreach (var item in S1.Find(Query.All(), skip: Count).Select(x => x.id))
            //        {
            //            var Find = S1.FindById(item);
            //            //S2.Upsert(Find);
            //            Count += 1;
            //            Console.WriteLine(Count);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //    }
            //}
            //Console.WriteLine("Fin");
            //Console.ReadLine();

            #endregion 遗留代码

            //while (!Debugger.IsAttached)
            //{
            //    Console.WriteLine("wait");
            //    Thread.Sleep(1000);
            //}
            //Debugger.Break();
            var LoopConnectIng = false;
            foreach (var item in new DirectoryInfo(@"./").GetFiles("*.jpg"))
            {
                File.Delete(item.FullName);
            }
            foreach (var item in new DirectoryInfo(@"./").GetFiles("*.webp"))
            {
                File.Delete(item.FullName);
            }
            SendMessage RemoteData = new SendMessage();
            AsyncWebSocketClient CurrectConnect = null;

            ShowMessage = ("程序开始运行");
            Stopwatch sw = Stopwatch.StartNew();

            long Long1 = 0;
            long Long11 = 0;

            long Long2 = 0;
            long Long22 = 0;

            long Long3 = 0;
            long Long4 = 0;
            long Long7 = 0;
            string Error = "";

            #region 显示用

            ThreadPool.QueueUserWorkItem(
                (obj) =>
                {
                    do
                    {
                        try
                        {
                            Console.Clear();
                            Console.WriteLine($"{ShowMessage}                                  {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB");

                            if (RemoteData != null)
                            {
                                Console.WriteLine($"图片原始:{HumanReadableFilesize(Long11)}                         等待发送数:{RemoteData.WaitSendCount}");
                                Console.WriteLine($"图片换成WEBP:{HumanReadableFilesize(Long22)}                     已经发送数:{RemoteData.SendCount}");
                                Console.WriteLine($"图片计数:{Long7}                              从SIS读取数:{RemoteData.ReadFromSIS}");
                                Console.WriteLine($"Gif原始:{HumanReadableFilesize(Long1)}                         从T66y读取数:{RemoteData.ReadFromT66y}");
                                Console.WriteLine($"Gif换成WEBP:{HumanReadableFilesize(Long2)}                      等待保存数:{RemoteData.WaitSaveCount}");
                                Console.WriteLine($"Gif计数:{Long4}                                已经保存数:{RemoteData.SaveCount}");
                                Console.WriteLine($"总共计数:{Long3}                              当前数据库:{RemoteData.CurrectDataBase}");
                                Console.WriteLine($"线程:{MirrorBody.Count(x => x.Value.Item2.IsRunning)}                                     {RemoteData.Mem}");
                                foreach (var item in RemoteData.ClientCount)
                                {
                                    Console.WriteLine($"当前接收到:{WriteOpera.Count}                                    客户端IP:{item.IP}");
                                    Console.WriteLine($"当前等待发回:{save.Count}                                  获得数据大小:{HumanReadableFilesize(item.SendByte)}");
                                    Console.WriteLine($"总等待:{sw.Elapsed:dd\\.hh\\:mm\\:ss}s                             获得数据数量:{item.SendCount}");
                                    Console.WriteLine($"                                                处理数据大小:{HumanReadableFilesize(item.GetByte)}");
                                    Console.WriteLine($"                                                处理数据数量:{item.GetCount}");
                                }
                            }
                            foreach (var item in MirrorBody)
                            {
                                if (item.Value.Item2.IsRunning)
                                    Console.WriteLine($"{item.Key}耗时   {item.Value.Item2.Elapsed.TotalSeconds}s   {item.Value.Item1}");
                                if (Console.CursorTop > Console.WindowHeight - 2) break;
                            }
                            if (RemoteData != null)
                            {
                                // RemoteData.Message.Reverse();
                                foreach (var item in RemoteData.Message)
                                {
                                    Console.WriteLine($"{item}");
                                    if (Console.CursorTop > Console.WindowHeight - 2) break;
                                }
                            }
                            Console.WriteLine(Error);
                        }
                        catch (Exception Ex)
                        {
                            Thread.Sleep(500);
                        }
                        Thread.Sleep(1000);
                    } while (true);
                });
            _ = Task.Factory.StartNew(async () =>
              {
              });
            AppDomain.CurrentDomain.ProcessExit += delegate
            {
                if (CurrectConnect != null && CurrectConnect.State == WebSocketState.Open)
                {
                    CurrectConnect.Close(WebSocketCloseCode.NormalClosure);
                    CurrectConnect.Abort();
                }
            };

            #endregion 显示用

            #region Old

            //None.OldMethod();
            // var Online = new Uri($"ws://127.0.0.1:2222/Webp");
            //System.Timers.Timer timer;
            //var client = new AsyncWebSocketClient(new Uri($"ws://192.168.2.162:2222/Webp"),
            //      (c, s) =>
            //      {
            //          try
            //          {
            //              RemoteData.Dispose();
            //              var SP = s.Split('$');
            //              RemoteData = SendMessage.ToClass(Convert.FromBase64String(SP[1]));
            //              var Get = OriImage.ToClass(Convert.FromBase64String(SP[0]));
            //              var TempAdd = new WebpImage()
            //              {
            //                  Date = Get.Date,
            //                  From = Get.From,
            //                  FromList = Get.FromList,
            //                  Hash = Get.Hash,
            //                  id = Get.id,
            //                  img = Get.img,
            //                  Status = true
            //              };
            //              WriteOpera.Writer.TryWrite(TempAdd);
            //              save.Add(Get.id);
            //          }
            //          catch (Exception ex)
            //          {
            //              ShowMessage = ex.Message;
            //          }
            //          return Task.CompletedTask;
            //      }, null,
            //     (a) =>
            //     {
            //         Task.Factory.StartNew(() =>
            //         {
            //             CurrectConnect = a;
            //             timer = new System.Timers.Timer() { Interval = 1000, Enabled = true, AutoReset = true };
            //             timer.Elapsed += delegate
            //             {
            //                 try
            //                 {
            //                     ShutDownSign = false;
            //                     if (CurrectConnect == null) CurrectConnect = a;
            //                     if (CurrectConnect.State != WebSocketState.Open)
            //                     {
            //                         CurrectConnect = null;
            //                         RemoteData.Dispose();
            //                         RemoteData = new SendMessage();
            //                         ShowMessage = ("连接断开");
            //                         if (!LoopConnectIng)
            //                         {
            //                             _ = Task.Factory.StartNew(() =>
            //                             {
            //                                 while (CurrectConnect == null)
            //                                 {
            //                                     LoopConnectIng = true;
            //                                     try
            //                                     {
            //                                         ShowMessage = ("等待连接");
            //                                         a.Connect();
            //                                         ShowMessage = ("连接成功");
            //                                         LoopConnectIng = false;
            //                                         break;
            //                                     }
            //                                     catch (Exception ex)
            //                                     {
            //                                         ShowMessage = ($"连接失败:{ex.Message}");
            //                                     }
            //                                 }
            //                             });
            //                         }
            //                         timer.Dispose();
            //                     }
            //                     else if (CurrectConnect.State == WebSocketState.Open)
            //                     {
            //                         var WaitAdd = WriteOpera.Reader.Count < 25 ? "true" : "false";
            //                         if (!save.Contains(WaitAdd))
            //                             save.Add(WaitAdd);
            //                     }
            //                 }
            //                 catch (Exception)
            //                 {
            //                 }
            //             };
            //         });
            //         return Task.CompletedTask;
            //     },
            //     async (c) =>
            //     {
            //         if (LoopConnectIng) return;
            //         while (true)
            //         {
            //             try
            //             {
            //                 LoopConnectIng = true;
            //                 ShowMessage = ("等待连接");
            //                 await c.Connect();
            //                 break;
            //             }
            //             catch (Exception ex)
            //             {
            //                 ShowMessage = ($"连接失败:{ex.Message}");
            //             }
            //             await Task.Delay(1000);
            //         }
            //         LoopConnectIng = false;
            //     }
            //     , null);
            //_ = Task.Factory.StartNew(async () =>
            //{
            //    if (LoopConnectIng) return;

            //    while (true)
            //    {
            //        try
            //        {
            //            LoopConnectIng = true;
            //            ShowMessage = ("等待连接");
            //            client.Connect().Wait();
            //            ShowMessage = ("连接成功");
            //            break;
            //        }
            //        catch (Exception ex)
            //        {
            //            ShowMessage = ($"连接失败:{ex.Message}");
            //        }
            //        await Task.Delay(1000);
            //    }
            //    LoopConnectIng = false;
            //});
            //_ = Task.Factory.StartNew(async () =>
            //{
            //    foreach (var item in save.GetConsumingEnumerable())
            //    {
            //        try
            //        {
            //            if (CurrectConnect != null && CurrectConnect.State == WebSocketState.Open)
            //            {
            //                await CurrectConnect.SendTextAsync(item);
            //            }
            //            else
            //            {
            //                save.Add(item);
            //            }
            //        }
            //        catch (Exception)
            //        {
            //            save.Add(item);
            //            ShowMessage = ($"保存失败次");
            //            await CurrectConnect.Close(WebSocketCloseCode.NormalClosure);
            //            CurrectConnect.Dispose();
            //            GC.Collect();
            //    }
            //});

            #endregion Old

            _ = Task.Factory.StartNew(() =>
            {
                foreach (var item in WriteDel)
                {
                    try
                    {
                        if (new DirectoryInfo(@"./").GetFiles().Length > 50)
                        {
                            foreach (var item2 in new DirectoryInfo(@"./").GetFiles("*.jpg"))
                            {
                                File.Delete(item2.FullName);
                            }
                            foreach (var item2 in new DirectoryInfo(@"./").GetFiles("*.webp"))
                            {
                                File.Delete(item2.FullName);
                            }
                        }
                        if (File.Exists(item))
                            File.Delete(item);
                    }
                    catch (Exception)
                    {
                    }
                    if (File.Exists(item))
                        WriteDel.Add(item);
                }
            });
            ConcurrentQueue<string> OperaIng = new ConcurrentQueue<string>();
            var gif2webp = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64" ? @"E:\新建文件夹\SpiderServerInLinux\DBTest\bin\Debug\gif2webp.exe" : "gif2webp";
            var cwebp = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64" ? @"E:\新建文件夹\SpiderServerInLinux\DBTest\bin\Debug\cwebp.exe" : "cwebp";
            Action<WebpImage, int> cb = async (item, co) =>
            {
                if (OperaIng.Contains(item.id))
                    return;
                else
                    OperaIng.Enqueue(item.id);
                var Check = false;
                long Long5 = 0;
                if (!MirrorBody.ContainsKey(co))
                {
                    Stopwatch sw = new Stopwatch();
                    MirrorBody.TryAdd(co, new Tuple<string, Stopwatch>($"{item.Date}   {item.id}", sw));
                }
                else
                {
                    MirrorBody[co] = new Tuple<string, Stopwatch>($"{item.Date}    {item.id}", MirrorBody[co].Item2);
                }
                MirrorBody[co].Item2.Restart();

                //await save.Writer.WriteAsync(new ImgData()
                //{
                //    Date = item.Date,
                //    FromList = item.FromList,
                //    Hash = item.Hash,
                //    id = item.id,
                //    img = item.img,
                //    Status = item.Status,
                //    Type = Check ? "jpg" : "gif"
                //});
                //item.Dispose();
                //GC.Collect();
                //return;
                Long3 += 1;
                {
                    var FileName = $@"{DateTime.Now.Ticks}{item.img.Length}.jpg";
                    File.WriteAllBytes(FileName, item.img);
                    Process p = new Process();
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    try
                    {
                        if (item.img[0] == 0x47 && item.img[1] == 0x49 && item.img[2] == 0x46)
                        {
                            p.StartInfo.FileName = gif2webp;
                            p.StartInfo.Arguments = $"{FileName} -o {FileName}.webp -mixed  -min_size -lossy -mt -m 4 -quiet";
                            Long4 += 1;
                        }
                        else
                        {
                            Check = true;
                            p.StartInfo.FileName = cwebp;
                            p.StartInfo.Arguments = $"{FileName} -o {FileName}.webp -jpeg_like  -m 4 -q 75 -noalpha -mt -quiet";
                            Long7 += 1;
                        }
                    tryagain:

                        p.Start();
                        p.WaitForExit();

                        if (File.Exists($"{FileName}.webp"))
                        {
                            var rr = File.ReadAllBytes($"{FileName}.webp");
                            if (!Check)
                            {
                                Long1 += item.img.Length;
                                Long2 += rr.Length;
                            }
                            else
                            {
                                Long11 += item.img.Length;
                                Long22 += rr.Length;
                            }
                            item.img = rr;
                            item.Type = Check ? "jpg" : "gif";
                            if (DateTime.TryParse(item.Date, out DateTime dateTime))
                            {
                                item.Date = dateTime.ToString("yyyy-MM-dd");
                            }
                            item.Status = true;
                            save.Add(item);
                            GC.Collect();
                        }
                        else
                        {
                            if (Long5 < 1)
                            {
                                Long5 += 1;
                                Console.WriteLine($"重试{Long5}次");
                                goto tryagain;
                            }
                            else
                            {
                                item.Status = false;
                                save.Add(item);
                                //File.AppendAllLines("Error.txt", new string[] { $"{FileName}|{p.StandardOutput.ReadToEnd()}" });
                            }
                        }
                        do
                        {
                            var DelCount = 0;
                            try
                            {
                                if (File.Exists($"{FileName}.webp"))
                                {
                                    File.Delete($"{FileName}.webp");
                                }
                                if (File.Exists($"{FileName}.webp"))
                                {
                                    await Task.Delay(1000);
                                    continue;
                                }
                                else
                                    break;
                            }
                            catch (Exception)
                            {
                                DelCount += 1;
                                await Task.Delay(1000);
                            }
                        } while (true);
                        do
                        {
                            var DelCount = 0;
                            try
                            {
                                if (File.Exists($"{FileName}"))
                                {
                                    File.Delete($"{FileName}");
                                }
                                if (File.Exists($"{FileName}"))
                                {
                                    await Task.Delay(1000);
                                    continue;
                                }
                                else
                                    break;
                            }
                            catch (Exception)
                            {
                                DelCount += 1;
                                await Task.Delay(1000);
                            }
                        } while (true);
                    }
                    catch (Exception ex)
                    {
                    }
                }
                Long5 = 0;
                MirrorBody[co].Item2.Stop();
                if (OperaIng.Contains(item.id))
                    OperaIng.TryDequeue(out string result);
            };
            for (int i = 0; i < 20; i++)
            {
                int j = i;
                _ = Task.Factory.StartNew(async () =>
                {
                    //do
                    //{
                    //    var item = await WriteOpera.Reader.ReadAsync();
                    //    cb(item, j);
                    //} while (true);
                    foreach (var item in WriteOpera.GetConsumingEnumerable())
                    {
                        cb(item, j);
                    }
                });
            }
            _ = Task.Factory.StartNew(async () =>
            {
                void DeleteLoop()
                {
                    if (File.Exists(CurrectBase))
                    {
                        do
                        {
                            File.Delete(CurrectBase);
                            Thread.Sleep(1000);
                        } while (File.Exists(CurrectBase));
                    }
                    if (File.Exists(@$"New-{CurrectBase}"))
                    {
                        do
                        {
                            File.Delete(@$"New-{CurrectBase}");
                            Thread.Sleep(1000);
                        } while (File.Exists(@$"New-{CurrectBase}"));
                    }
                }
                do
                {
                    try
                    {
                        var client = new TcpClient<IImage2Webp>(new TcpZkEndPoint("", "", new IPEndPoint(IPAddress.Parse("192.168.2.162"), 2222), connectTimeOutMs: 2500));
                        {
                            do
                            {
                                try
                                {
                                    var S2 = client.Proxy.Get2(ClientIP);
                                    if (S2 != null)
                                        RemoteData = SendMessage.ToClass(S2);
                                }
                                catch (global::System.Exception)
                                {
                                }

                                if (string.IsNullOrEmpty(CurrectBase))
                                {
                                    if (save.Count > 0)
                                    {
                                        foreach (var item in save.GetConsumingEnumerable())
                                        {
                                            try
                                            {
                                                client.Proxy.Set(ClientIP, "", item.Send());
                                                if (save.Count == 0) break;
                                            }
                                            catch (Exception ex)
                                            {
                                                Error = ex.Message;
                                                save.Add(item);
                                            }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(CurrectBase))
                                    {
                                        await Task.Delay(1000);
                                        continue;
                                    }
                                    if (WriteOpera.Count >= 20) continue;
                                    var GetFile = client.Proxy.Get3("start");
                                    if (GetFile != null)
                                    {
                                        CurrectBase = GetFile.Item2;
                                        DeleteLoop();
                                        if (!File.Exists(CurrectBase))
                                            File.Create(CurrectBase).Dispose();
                                        var Status = string.Empty;
                                        while (true)
                                        {
                                            try
                                            {
                                                try
                                                {
                                                    if (!client.IsConnected)
                                                    {
                                                        await Task.Delay(1000);
                                                        continue;
                                                    }
                                                    var S2 = client.Proxy.Get2(ClientIP);
                                                    if (S2 != null)
                                                        RemoteData = SendMessage.ToClass(S2);
                                                }
                                                catch (global::System.Exception)
                                                {
                                                }
                                                var Ret = client.Proxy.Get3(Status);
                                                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                                                if (Encoding.UTF8.GetString(md5.ComputeHash(Ret.Item1)) == Ret.Item2)
                                                {
                                                    if (!File.Exists(CurrectBase))
                                                        File.Create(CurrectBase).Dispose();
                                                    using FileStream fileStream = new FileStream(CurrectBase, FileMode.Append, FileAccess.Write);
                                                    fileStream.Write(Ret.Item1, 0, Ret.Item1.Length);
                                                    fileStream.Dispose();
                                                    if (Ret.Item3 == "fin")
                                                    {
                                                        break;
                                                    }
                                                    Status = "continue";
                                                }
                                                else
                                                {
                                                    Status = "redownload";
                                                    DeleteLoop();
                                                }
                                            }
                                            catch (Exception)
                                            {
                                                if (File.Exists(CurrectBase))
                                                {
                                                    do
                                                    {
                                                        File.Delete(CurrectBase);
                                                        Thread.Sleep(1000);
                                                    } while (File.Exists(CurrectBase));
                                                }
                                            }
                                        }

                                        _ = Task.Factory.StartNew(async () =>
                                          {
                                              ConcurrentDictionary<string, int> ReTry = new ConcurrentDictionary<string, int>();
                                              bool Writeing = false;

                                              void WriteToMessage(string info)
                                              {
                                                  Error = info;
                                              }

                                              do
                                              {
                                                  try
                                                  {
                                                      var ReadCount = 0;
                                                      {
                                                          ReadCount += 1;
                                                          try
                                                          {
                                                              WriteToMessage($"{CurrectBase} Start");
                                                              async void WritIng()
                                                              {
                                                                  while (Writeing)
                                                                  {
                                                                      await Task.Delay(100);
                                                                  }
                                                              }

                                                              var Skip = 0;
                                                              using var Tempdb2 = new LiteDatabase(@$"Filename={CurrectBase};");
                                                              using var Tempdb3 = new LiteDatabase(@$"Filename=New-{CurrectBase};");
                                                              try
                                                              {
                                                                  WritIng();

                                                                  if (!Tempdb3.CollectionExists("WebpData"))
                                                                  {
                                                                      var TempWebp = Tempdb3.GetCollection<WebpImage>("WebpData");
                                                                      TempWebp.EnsureIndex(x => x.id);
                                                                      TempWebp.EnsureIndex(x => x.From);
                                                                      TempWebp.EnsureIndex(x => x.Status);
                                                                  }
                                                                  {
                                                                      var C1 = Tempdb2.GetCollection<OriImage>("ImgData").Count();
                                                                      var C2 = Tempdb2.GetCollection<WebpImage>("WebpData").Count();
                                                                      if (C1 != 0)
                                                                      {
                                                                          if (C1 != C2)
                                                                          {
                                                                              WriteToMessage($"{CurrectBase} Start |  Mov ImgData WebpData");
                                                                          ForeachListB:
                                                                              try
                                                                              {
                                                                                  foreach (var item in Tempdb2.GetCollection<OriImage>("ImgData").Find(Query.All(), Skip).Select(x => x.id))
                                                                                  {
                                                                                      try
                                                                                      {
                                                                                          if (Tempdb2.CollectionExists("WebpData"))
                                                                                          {
                                                                                              if (!Tempdb2.GetCollection<WebpImage>("WebpData").Exists(item))
                                                                                              {
                                                                                                  Skip += 1;
                                                                                                  continue;
                                                                                              }
                                                                                          }
                                                                                      }
                                                                                      catch (global::System.Exception)
                                                                                      {
                                                                                      }

                                                                                      var Data = Tempdb2.GetCollection<OriImage>("ImgData").FindById(item);
                                                                                      if (Data.img != null)
                                                                                      {
                                                                                          var ImageT = GetFileImageTypeFromHeader(Data.img);
                                                                                          if (ImageT != ImageType.Unknown)
                                                                                          {
                                                                                              var Add = new WebpImage()
                                                                                              {
                                                                                                  Date = Data.Date,
                                                                                                  From = Data.From,
                                                                                                  FromList = Data.FromList,
                                                                                                  Hash = Data.Hash,
                                                                                                  id = Data.id,
                                                                                                  img = Data.img,
                                                                                                  Status = false,
                                                                                                  Type = ImageT.ToString()
                                                                                              };
                                                                                              Tempdb3.GetCollection<WebpImage>("WebpData").Upsert(Add);
                                                                                          }
                                                                                      }
                                                                                      Skip += 1;
                                                                                      WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {CurrectBase} | ImgData | {Skip}/{C1}");
                                                                                      GC.Collect();
                                                                                      WritIng();
                                                                                  }
                                                                              }
                                                                              catch (Exception ex)
                                                                              {
                                                                                  Skip += 1;
                                                                                  goto ForeachListB;
                                                                              }
                                                                          }
                                                                      }
                                                                      if (C2 != 0)
                                                                      {
                                                                          Skip = 0;
                                                                          foreach (var item in Tempdb2.GetCollection<WebpImage>("WebpData").Find(Query.All(), Skip))
                                                                          {
                                                                              Tempdb3.GetCollection<WebpImage>("WebpData").Upsert(item);
                                                                              Skip += 1;
                                                                              WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {CurrectBase} | WebpData | {Skip}/{C2}");
                                                                              GC.Collect();
                                                                              WritIng();
                                                                          }
                                                                      }
                                                                      WriteToMessage($"{CurrectBase} Rebuild Complete");
                                                                      //Tempdb3.Dispose();
                                                                      GC.Collect();
                                                                  }
                                                              }
                                                              catch (Exception)
                                                              {
                                                              }
                                                              GC.Collect();
                                                              Tempdb2.Dispose();
                                                              var Fin = false;
                                                              WriteToMessage($"{CurrectBase} Start | ReadWebpData");
                                                              var ForeachList2 = new List<string>();
                                                              Skip = 0;
                                                          ForeachListA:
                                                              try
                                                              {
                                                                  WritIng();
                                                                  //using var Tempdb2 = new LiteDatabase(File.Open(@$"New-{CurrectBase}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                                                                  foreach (var item in Tempdb3.GetCollection<WebpImage>("WebpData").Find(x => !x.Status, Skip).Select(x => x.id))
                                                                  {
                                                                      ForeachList2.Add(item);
                                                                      Skip += 1;
                                                                      GC.Collect();
                                                                      WritIng();
                                                                  }
                                                                  // Tempdb2.Dispose();
                                                                  GC.Collect();
                                                              }
                                                              catch (Exception)
                                                              {
                                                                  Skip += 1;
                                                                  goto ForeachListA;
                                                              }
                                                              GC.Collect();
                                                              WriteToMessage($"{CurrectBase} Start | ForeachList2");

                                                              var ForeachList2Count = 0;
                                                              var DBB = Tempdb3.GetCollection<WebpImage>("WebpData");
                                                              foreach (var find in ForeachList2)
                                                              {
                                                                  ForeachList2Count += 1;
                                                                  if (ReTry.ContainsKey(find))
                                                                  {
                                                                      ReTry[find] += 1;
                                                                      if (ReTry[find] > 2) continue;
                                                                  }
                                                                  else
                                                                      ReTry.TryAdd(find, 0);
                                                                  Fin = true;
                                                                  WritIng();

                                                                  var TempFind = DBB.FindById(find);
                                                                  {
                                                                      if (TempFind != null)
                                                                      {
                                                                          WriteOpera.Add(TempFind);
                                                                      }
                                                                  }
                                                                  WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {CurrectBase} | {ForeachList2Count}/{ForeachList2.Count}");
                                                                  if (save.Count != 0)
                                                                  {
                                                                      do
                                                                      {
                                                                          if (save.TryTake(out WebpImage ret))
                                                                          {
                                                                              try
                                                                              {
                                                                                  if (ret != null)
                                                                                  {
                                                                                      DBB.Upsert(ret);
                                                                                      WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {CurrectBase} | save {save.Count}");
                                                                                  }
                                                                              }
                                                                              catch (Exception ex)
                                                                              {
                                                                                  save.Add(ret);
                                                                                  Error = ex.Message;
                                                                              }
                                                                          }
                                                                      } while (save.Count != 0);
                                                                  }
                                                              }
                                                              do
                                                              {
                                                                  do
                                                                  {
                                                                      if (save.TryTake(out WebpImage ret))
                                                                      {
                                                                          try
                                                                          {
                                                                              if (ret != null)
                                                                              {
                                                                                  DBB.Upsert(ret);
                                                                                  WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {CurrectBase} | save {save.Count}");
                                                                              }
                                                                          }
                                                                          catch (Exception ex)
                                                                          {
                                                                              save.Add(ret);
                                                                              Error = ex.Message;
                                                                          }
                                                                      }
                                                                  } while (save.Count != 0);
                                                                  WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {CurrectBase} | Writing {WriteOpera.Count} | {save.Count}");
                                                                  await Task.Delay(1000);
                                                              } while (MirrorBody.Count(x => x.Value.Item2.IsRunning) != 0 || WriteOpera.Count != 0 || save.Count != 0);
                                                              GC.Collect();

                                                              if (!Fin)
                                                              {
                                                                  Tempdb3.Rebuild();
                                                                  Tempdb3.Dispose();
                                                                  int Posion = 0;
                                                                  var status = string.Empty;
                                                                  string Md5 = "";
                                                                  byte[] Writing = null;
                                                                  Byte[] ArrayPool = null;
                                                                  int RedownloadCount = 0;
                                                                  var md5 = System.Security.Cryptography.MD5.Create();
                                                                  var read = File.Open(@$"New-{CurrectBase}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                                                                  status = client.Proxy.Send(null, "start", "", read.Length);
                                                                  read.Close();
                                                                  read.Dispose();
                                                                  do
                                                                  {
                                                                      read = File.Open(@$"New-{CurrectBase}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                                                                      if (read.Length - Posion > 1024 * 1024 * 10)
                                                                      {
                                                                          Writing = new byte[1024 * 1024 * 10];
                                                                          read.Position = Posion;
                                                                          read.Read(Writing, 0, Writing.Length);
                                                                          Md5 = Encoding.UTF8.GetString(md5.ComputeHash(Writing));
                                                                          status = client.Proxy.Send(Writing, status, Md5, read.Length);
                                                                          if (status != "redownload")
                                                                          {
                                                                              Posion += 1024 * 1024 * 10;
                                                                              Writing = null;
                                                                              GC.Collect();
                                                                          }
                                                                      }
                                                                      else
                                                                      {
                                                                          RedownloadCount = 0;
                                                                          Writing = new byte[(int)(read.Length - Posion)];
                                                                          read.Position = Posion;
                                                                          read.Read(Writing, 0, Writing.Length);
                                                                          Md5 = Encoding.UTF8.GetString(md5.ComputeHash(Writing));
                                                                          status = client.Proxy.Send(Writing, "fin", Md5, read.Length);
                                                                          if (status != "redownload")
                                                                          {
                                                                              Posion += Writing.Length;
                                                                              Writing = null;
                                                                              GC.Collect();
                                                                          }
                                                                      }
                                                                      read.Close();
                                                                      read.Dispose();
                                                                  } while (status != "Fin");
                                                                  Writing = null;
                                                                  GC.Collect();
                                                                  read.Close();
                                                                  read.Dispose();
                                                                  DeleteLoop();
                                                                  CurrectBase = string.Empty;
                                                                  WriteToMessage($"{Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {CurrectBase}/{ReadCount}/完成");
                                                                  break;
                                                              }
                                                              Fin = false;
                                                              Skip = 0;
                                                              goto ForeachListA;
                                                          }
                                                          catch (Exception ex)
                                                          {
                                                          }
                                                      }
                                                  }
                                                  catch (Exception ex)
                                                  {
                                                      Console.WriteLine(ex);
                                                  }
                                              } while (true);
                                          });
                                    }
                                    var S = client.Proxy.Get(ClientIP);
                                    if (S == null)
                                    {
                                        await Task.Delay(1000);
                                        continue;
                                    }
                                    //var SP = S.Split('$');
                                    //RemoteData = SendMessage.ToClass(Convert.FromBase64String(SP[1]));
                                    WriteOpera.Add(WebpImage.ToClass(S));
                                }
                                await Task.Delay(100);
                            } while (true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Error = ex.Message;
                    }
                } while (true);
            });
            //Console.CancelKeyPress += async (o, e) =>
            //{
            //    if (ShutDownSign == true) ShutdownResetEvent.SetResult(1);
            //    e.Cancel = true;
            //    ShutDownSign = true;
            //    if (CurrectConnect != null)
            //    {
            //        try
            //        {
            //            save.Add("Close");
            //        }
            //        catch (Exception)
            //        {
            //        }
            //    }
            //};
            return await ShutdownResetEvent.Task.ConfigureAwait(false);

            #region 获取用旧版本

            //_ = Task.Factory.StartNew(async () =>
            //{
            //	var TryTime = 0;

            //	do
            //	{
            //		try
            //		{
            //			Long6 = 0;
            //			var db = new LiteDatabase(@"Filename=Z:\publish\SIS.db;Connection=Shared;ReadOnly=True");
            //			var SISDB = db.GetCollection<SISImgData>("ImgData");
            //			foreach (var id in SISDB.FindAll().Select(x => x.id))
            //			{
            //				try
            //				{
            //					if (!Read(id))
            //					{
            //						var item = SISDB.FindById(id);
            //						if (item.img != null && item.img.Length > 1024)
            //						{
            //							ts.TryAdd(new Tuple<SISImgData, bool>(item, true));
            //							TryTime = 0;
            //							// var ms = new MemoryStream(item.img);
            //							// Image img = Image.FromStream(ms);
            //							// img.Dispose();
            //							//ms.Dispose();
            //						}
            //					}
            //				}
            //				catch (Exception ex)
            //				{
            //					TryTime += 1;
            //					var Message = $"{DateTime.Now:HH:m:s}-{Long6}-尝试{TryTime}-SIS读取失败{ex}";
            //					File.AppendAllLines("Error.txt", new string[] { Message });
            //					Error = ($"SIS读取失败,{Message}");
            //				}
            //				Long6 += 1;
            //			}
            //		}
            //		finally
            //		{
            //			if (TryTime < 5)
            //			{
            //				await Task.Delay(1000 * 5);
            //			}
            //			else
            //			{
            //				TryTime = 0;
            //				Long6 += 1;
            //			}
            //		}
            //	} while (true);
            //});
            //_ = Task.Factory.StartNew(async () =>
            //{
            //	var TryTime = 0;

            //	do
            //	{
            //		try
            //		{
            //			Long8 = 0;
            //			var db = new LiteDatabase(@"Filename=Z:\publish\T66y.db;Connection=Shared;ReadOnly=True");
            //			var SISDB = db.GetCollection<T66yImgData>("ImgData");
            //			foreach (var id in SISDB.FindAll().Select(x => x.id))
            //			{
            //				try
            //				{
            //					if (!Read(id))
            //					{
            //						var item = SISDB.FindById(id);
            //						if (item.img != null && item.img.Length > 1024)
            //						{
            //							var Temp = new SISImgData()
            //							{
            //								Date = item.Date,
            //								FromList = item.FromList,
            //								Hash = item.Hash,
            //								id = item.id,
            //								img = item.img,
            //							};
            //							ts.TryAdd(new Tuple<SISImgData, bool>(Temp, false));
            //						}
            //					}
            //				}
            //				catch (Exception ex)
            //				{
            //					TryTime += 1;
            //					var Message = $"{DateTime.Now:dd:m:s}-{Long8}-尝试{TryTime}-T66y读取失败{ex}";
            //					File.AppendAllLines("Error.txt", new string[] { Message });
            //					Error = ($"{DateTime.Now:HH:m:s}T66y读取失败,{Message}");
            //				}
            //				Long8 += 1;
            //			}
            //		}
            //		finally
            //		{
            //			if (TryTime < 5)
            //			{
            //				await Task.Delay(1000 * 5);
            //			}
            //			else
            //			{
            //				TryTime = 0;
            //				Long8 += 1;
            //			}
            //		}
            //	} while (true);
            //});

            #endregion 获取用旧版本

            #region 写入用

            //_ = Task.Factory.StartNew(() =>
            //{
            //    //var SAVECount = 0;
            //    using var db = new LiteDatabase($@"Filename=Z:\\Image.db");
            //    foreach (var item in save.GetConsumingEnumerable())
            //    {
            //        var SISDB = db.GetCollection<ImgData>("ImgData");
            //        var SISDB2 = db.GetCollection("Data");

            //        try
            //        {
            //            SISDB2.Upsert(new BsonDocument() { { "id", item.id } });
            //            SISDB.Upsert(item);
            //            //SAVECount = 0;
            //        }
            //        catch (Exception ex)
            //        {
            //            //SAVECount += 1;
            //            File.WriteAllLines("Error.txt", new string[] { $"保存失败{ex}|{item.id}" });
            //            Error = ($"{DateTime.Now:HH:m:s}保存失败{ex}");
            //            // if (SAVECount == 5)
            //            continue;
            //        }
            //    }
            //});

            #endregion 写入用

            return await ShutdownResetEvent.Task.ConfigureAwait(false);
        }
    }

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
    }

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
        public bool Status { get; set; }
    }

    [Serializable]
    internal class SISImgData : T66yImgData
    {
        public new byte[] img
        {
            get { return base.img; }
            set
            {
                base.img = value;
            }
        }
    }

    [Serializable]
    internal class ImgData : IDisposable
    {
        public string id { get; set; }

        public string Date { get; set; }
        public string Hash { get; set; }

        public byte[] img { get; set; }
        public List<int> FromList { get; set; }
        public bool Status { get; set; }
        public string Type { get; set; }

        public byte[] Send()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter Formatter = new BinaryFormatter();
                Formatter.Serialize(stream, this);
                return stream.ToArray();
            }
        }

        public static ImgData ToClass(byte[] data)
        {
            using var stream = new MemoryStream(data);
            IFormatter Fileformatter = new BinaryFormatter();
            Fileformatter.Binder = new UBinder();
            return Fileformatter.Deserialize(stream) as ImgData;
        }

        public class UBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.EndsWith("ImgData"))
                {
                    return typeof(ImgData);
                }
                return (Assembly.GetExecutingAssembly()).GetType(typeName);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.img = null;
            }
        }

        ~ImgData()
        {
            this.Dispose(false);
        }
    }

    [Serializable]
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
    internal class OriImgData : SISImgData, IDisposable
    {
        public new bool Status;

        public static OriImgData ToClass(byte[] data)
        {
            using var stream = new MemoryStream(data);
            IFormatter Fileformatter = new BinaryFormatter();
            Fileformatter.Binder = new UBinder();
            return Fileformatter.Deserialize(stream) as OriImgData;
        }

        public class UBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.EndsWith("OriImgData"))
                {
                    return typeof(OriImgData);
                }
                return Assembly.GetExecutingAssembly().GetType(typeName);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.img = null;
            }
        }

        ~OriImgData()
        {
            this.Dispose(false);
        }
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

    internal class MiMiAiStory
    {
        public int id { get; set; }
        public string Uri { get; set; }
        public string Title { get; set; }
        public string Story { get; set; }
        public byte[] Data { get; set; }
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

    internal static class None
    {
        #region 旧代码

        //internal static void OldMethod()
        //{
        //    var Count = 10000;
        //    goto image;
        //    using (var db = new LiteDB4.LiteDatabase(@"E:\DataBaseFix\Image.db"))
        //    {
        //        using (var db2 = new LiteDatabase(@"E:\DataBaseFix\Image3.db"))
        //        {
        //            if (!db2.CollectionExists("NyaaDB"))
        //            {
        //                var NyaaDB = db2.GetCollection<NyaaInfo>("NyaaDB");
        //                NyaaDB.EnsureIndex(x => x.Catagory);
        //                NyaaDB.EnsureIndex(x => x.Date);
        //                NyaaDB.EnsureIndex(x => x.id);
        //                var _Table = db.GetCollection("WebPage");
        //                _Table.EnsureIndex("_id", true);
        //                _Table.EnsureIndex("Status");
        //            }
        //            foreach (var item in db.GetCollectionNames())
        //            {
        //                if (item == "NyaaDB")
        //                {
        //                re:
        //                    try
        //                    {
        //                        var T66yDB2 = db2.GetCollection<NyaaInfo>(item);
        //                        foreach (var item2 in db.GetCollection<NyaaInfo>(item).Find(LiteDB4.Query.All(), skip: Count))
        //                        {
        //                            Count += 1;
        //                            try
        //                            {
        //                                T66yDB2.Upsert(item2);
        //                                Console.WriteLine($"{item}-完成{Count}");
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                Console.WriteLine($"{item}-错误{Count}   {ex.Message}");
        //                                File.AppendAllLines("error.txt", new string[]
        //                                {
        //                                $"{item}-错误{Count}   {ex.Message}"
        //                                });
        //                            }
        //                        }
        //                    }
        //                    catch (Exception)
        //                    {
        //                        Count += 1;
        //                        Console.WriteLine($"错误跳过-{Count}");

        //                        goto re;
        //                    }
        //                }
        //                else if (item == "WebPage")
        //                {
        //                    Count = 0;
        //                //var T66yDB2 = db2.GetCollection(item);
        //                //foreach (var item2 in db.GetCollection(item).FindAll())
        //                //{
        //                //    Count += 1;
        //                //    try
        //                //    {
        //                //        var TempAdd = new BsonDocument();
        //                //        foreach (var item3 in item2)
        //                //        {
        //                //            TempAdd.Add(item3.Key, item3.Value.AsBinary);

        //                //        }
        //                //        T66yDB2.Update(TempAdd);
        //                //        Console.WriteLine($"{item}-完成{Count}");

        //                //    }
        //                //    catch (Exception ex)
        //                //    {
        //                //        Console.WriteLine($"错误{Count}   {ex.Message}");
        //                //        File.AppendAllLines("error.txt", new string[]{
        //                //            $"{item}-错误{Count}   {ex.Message}"});
        //                //    }

        //                //}
        //                //continue;
        //                re:
        //                    var T66yDB2 = db2.GetCollection(item);
        //                    try
        //                    {
        //                        foreach (var item2 in db.GetCollection(item).Find(LiteDB4.Query.All(), skip: Count))
        //                        {
        //                            Count += 1;
        //                            try
        //                            {
        //                                var TempAdd = new BsonDocument();
        //                                foreach (var item3 in item2)
        //                                {
        //                                    TempAdd.Add(item3.Key, item3.Value.AsBinary);
        //                                }
        //                                T66yDB2.Update(TempAdd);
        //                                Console.WriteLine($"{item}-完成{Count}");
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                Console.WriteLine($"错误{Count}   {ex.Message}");
        //                                File.AppendAllLines("error.txt", new string[]{
        //                                $"{item}-错误{Count}   {ex.Message}"});
        //                            }
        //                        }
        //                        //foreach (var item2 in db.GetCollection<MiMiAiStory>(item).FindAll().Select(x => x.id).Skip(Count))
        //                        //{
        //                        //    Count += 1;
        //                        //    try
        //                        //    {
        //                        //        T66yDB2.Update(db.GetCollection<MiMiAiStory>(item).FindById(item2));
        //                        //        Console.WriteLine($"{item}-完成{Count}-{item2}");

        //                        //    }
        //                        //    catch (Exception ex)
        //                        //    {
        //                        //        Console.WriteLine($"错误{Count}   {ex.Message}");
        //                        //        File.AppendAllLines("error.txt", new string[]{
        //                        //        $"{item}-错误{Count}   {ex.Message}"});
        //                        //    }

        //                        //}
        //                    }
        //                    catch (Exception)
        //                    {
        //                        Count += 1;
        //                        Console.WriteLine($"错误跳过-{Count}");

        //                        goto re;
        //                    }
        //                }
        //                else
        //                {
        //                    var T66yDB2 = db2.GetCollection(item);
        //                    foreach (var item2 in db.GetCollection(item).FindAll())
        //                    {
        //                        Count += 1;
        //                        try
        //                        {
        //                            var TempAdd = new BsonDocument();
        //                            foreach (var item3 in item2)
        //                            {
        //                                TempAdd.Add(item3.Key, item3.Value.ToString());
        //                            }
        //                            T66yDB2.Update(TempAdd);
        //                            Console.WriteLine($"{item}-完成{Count}");
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            Console.WriteLine($"错误{Count}   {ex.Message}");
        //                            File.AppendAllLines("error.txt", new string[]{
        //                                $"{item}-错误{Count}   {ex.Message}"});
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    Console.WriteLine("完成");
        //    Console.ReadLine();
        //    //using (var db = new LiteDatabase(@"z:/publish/Image.db"))
        //    //{
        //    //    if (!db.CollectionExists("ImgData"))
        //    //    {
        //    //        var T66yDB = db.GetCollection<SISImgData>("ImgData");
        //    //        T66yDB.EnsureIndex(x => x.Status);
        //    //        T66yDB.EnsureIndex(x => x.Hash);
        //    //        T66yDB.EnsureIndex(x => x.id);
        //    //        T66yDB.EnsureIndex(x => x.Date);
        //    //    }
        //    //}
        //    //   using (var db = new LiteDatabase(@"/media/publish/Image.db"))
        //    while (!Debugger.IsAttached)
        //    {
        //        Console.WriteLine("wait");
        //        Thread.Sleep(1000);
        //    }
        //    Debugger.Break();
        //image:
        //    using (var db = new LiteDatabase(@"E:\DataBaseFix\Image3.db"))
        //    {
        //        if (!db.CollectionExists("ImgData"))
        //        {
        //            var T66yDB = db.GetCollection<WebpImage>("ImgData");
        //            T66yDB.EnsureIndex(x => x.Status);
        //            T66yDB.EnsureIndex(x => x.Hash);
        //            T66yDB.EnsureIndex(x => x.id);
        //            T66yDB.EnsureIndex(x => x.Date);
        //        }
        //        if (!db.CollectionExists("Data"))
        //        {
        //            var T66yDB = db.GetCollection("Data");
        //            T66yDB.EnsureIndex("id", true);
        //        }
        //    }
        //    //    var Count= 168500;
        //    using (var db = new LiteDatabase(@"E:\DataBaseFix\Image3.db"))
        //    {
        //        //using var db2 = new LiteDatabase(@"E:\DataBaseFix\Image3.db");
        //        var T66yDB = db.GetCollection<WebpImage>("ImgData");
        //        var T66yDB3 = db.GetCollection("Data");

        //    //var T66yDB2 = db2.GetCollection<ImgData2>("ImgData");
        //    r:
        //        try
        //        {
        //            foreach (var itemid in T66yDB.FindAll().Select(x => x.id))
        //            {
        //                try
        //                {
        //                    Count += 1;
        //                    var item = T66yDB.FindById(itemid);
        //                    if (item.img.Length < 1000)
        //                    {
        //                        Console.WriteLine();
        //                        T66yDB.Delete(itemid);
        //                        T66yDB3.Delete(T66yDB3.FindOne(x => x["id"] == itemid));
        //                    }
        //                    //if (T66yDB3.Exists(x => x["id"] == itemid)) continue;
        //                    //var item = T66yDB.FindById(itemid);
        //                    //T66yDB2.Upsert(item);
        //                    //T66yDB3.Upsert(new BsonDocument() { { "id", item.id } });
        //                    //Console.WriteLine(Count);
        //                }
        //                catch (Exception e)
        //                {
        //                    Console.WriteLine($"重置错误{Count}       {e.Message}");
        //                }
        //            }
        //            //foreach (BsonDocument item3 in T66yDB3.Find(Query.All(), skip: Count))
        //            //{
        //            //    Count += 1;

        //            //    try
        //            //    {
        //            //        var item = T66yDB.FindById(item3["id"]);
        //            //        var Temp = new ImgData2();
        //            //        {
        //            //            item.TryGetValue("_id", out BsonValue value);
        //            //            Temp.id = value.AsString;
        //            //        }
        //            //        {
        //            //            item.TryGetValue("Status", out BsonValue value);
        //            //            Temp.Status = value.AsBoolean;
        //            //        }
        //            //        {
        //            //            item.TryGetValue("img", out BsonValue value);
        //            //            Temp.img = value.AsBinary;
        //            //        }
        //            //        {
        //            //            item.TryGetValue("_type", out BsonValue value);
        //            //            Temp.Type = value.AsString;
        //            //        }
        //            //        {
        //            //            item.TryGetValue("Hash", out BsonValue value);
        //            //            Temp.Hash = value.AsString;
        //            //        }
        //            //        {
        //            //            item.TryGetValue("Date", out BsonValue value);
        //            //            Temp.Date = value.AsString;
        //            //        }
        //            //        {
        //            //            item.TryGetValue("FromList", out BsonValue value);
        //            //            Temp.FromList = new List<int>();

        //            //            foreach (var item2 in value.AsArray)
        //            //            {
        //            //                Temp.FromList.Add(item2);
        //            //            }
        //            //        }

        //            //        T66yDB2.Upsert(Temp);
        //            //        T66yDB3.Upsert(item3);

        //            //        Console.WriteLine(Count);
        //            //    }
        //            //    catch (Exception ex)
        //            //    {
        //            //        Console.WriteLine($"错误{Count}       {ex.Message}");
        //            //    }
        //            //}
        //        }
        //        catch (Exception ex)
        //        {
        //            Count += 1;
        //            Console.WriteLine($"重置错误{Count}       {ex.Message}");
        //            goto r;
        //        }
        //    }
        //    Console.WriteLine("完成");
        //    Console.ReadLine();
        //    //  return await ShutdownResetEvent.Task.ConfigureAwait(false);
        //    //using (var db = new LiteDatabase(@"z:\publish\Image.db"))
        //    //{
        //    //    if (!db.CollectionExists("ImgData"))
        //    //    {
        //    //        var T66yDB = db.GetCollection<ImgData>("ImgData");
        //    //        T66yDB.EnsureIndex(x => x.Status);
        //    //        T66yDB.EnsureIndex(x => x.Hash); ;
        //    //        T66yDB.EnsureIndex(x => x.id);
        //    //        T66yDB.EnsureIndex(x => x.Date);
        //    //    }
        //    //    if (!db.CollectionExists("Data"))
        //    //    {
        //    //        var T66yDB = db.GetCollection("Data");
        //    //    }
        //    //}
        //}

        #endregion 旧代码
    }

    [Serializable]
    public class OriImage
    {
        public string id { get; set; }
        public string From { get; set; }

        public string Date { get; set; }
        public string Hash { get; set; }
        public byte[] img { get; set; }
        public List<int> FromList { get; set; }
        public bool Status { get; set; }

        public byte[] Send()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter Formatter = new BinaryFormatter();
                Formatter.Serialize(stream, this);
                return stream.ToArray();
            }
        }

        public static OriImage ToClass(byte[] data)
        {
            using var stream = new MemoryStream(data);
            IFormatter Fileformatter = new BinaryFormatter();
            Fileformatter.Binder = new UBinder();
            return Fileformatter.Deserialize(stream) as OriImage;
        }

        public class UBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.EndsWith("OriImage"))
                {
                    return typeof(OriImage);
                }
                return Assembly.GetExecutingAssembly().GetType(typeName);
            }
        }
    }
}