using Cowboy.WebSockets;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using LiteDB;
using System.Net;
using ServiceWire.TcpIp;
using WebpInter;
using System.Data.SqlTypes;
using System.Text;
using System.Runtime;
using ServiceWire.NamedPipes;

TaskCompletionSource<byte> ShutdownResetEvent = new TaskCompletionSource<byte>();

var tcphost = new TcpHost(new IPEndPoint(IPAddress.Any, 2222), null, null, null)
{
    UseCompression = false,
    CompressionThreshold = 131072
};
tcphost.AddService<IImage2Webp>(new Image2Webp());
tcphost.Open();
//await Task.Factory.StartNew(() =>
//{
//    var ModuleCatalog = new AsyncWebSocketServerModuleCatalog();
//    var _server = new AsyncWebSocketServer(2222, ModuleCatalog);
//    ModuleCatalog.RegisterModule(new Image2Webp());
//    _server.Listen();
//});
return await ShutdownResetEvent.Task.ConfigureAwait(false);

internal class Image2Webp : IImage2Webp
{
    private static byte[] WriteSendMessage = null;
    private static ConcurrentDictionary<string, SessionsCheck> _sessionsCheck = new ConcurrentDictionary<string, SessionsCheck>();
    private static SendMessage sendMessage = new SendMessage();
    private static ConcurrentQueue<string> MessageList = new ConcurrentQueue<string>();
    private static ConcurrentDictionary<string, AsyncWebSocketSession> _sessions = new ConcurrentDictionary<string, AsyncWebSocketSession>();
    private static BlockingCollection<WebpImage> WriteSend = new BlockingCollection<WebpImage>(20);
    private static BlockingCollection<WebpImage> WriteSave = new BlockingCollection<WebpImage>();
    private string BaseUri = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64" ? @"Z:\publish\" : @"/media/sda1/publish/";
    private string BaseUri2 = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64" ? @"Z:\publish\NewImage\" : @"/media/sda1/publish/NewImage/";
    private string SendMessageByte = $"{new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName}{Path.DirectorySeparatorChar}SendMessageByte.dat";
    private bool Writeing = false;
    private string WritingSendBase = string.Empty;
    private string ImgPath = $"{(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64" ? @"C:\Users\cdj68\Desktop\" : @"/media/sda1/publish/")}Image{Path.DirectorySeparatorChar}";

    private void WriteToMessage(string s)
    {
        if (!MessageList.Contains(s))
        {
            Console.WriteLine(s);
            MessageList.Enqueue(s);
        }
        if (MessageList.Count > 20)
            MessageList.TryDequeue(out string ss);
        WriteSendMessage = sendMessage.Send();
        GC.Collect();
    }

    public enum ImageType
    {
        Unknown,
        JPEG,
        PNG,
        GIF,
        BMP,
        TIFF,
    }

    public Image2Webp()
    {
        bool SISWebdbCheck = false;
        bool T66yWebdbCheck = false;

        //while (!Debugger.IsAttached)
        //{
        //    Console.WriteLine("wait");
        //    Thread.Sleep(1000);
        //}
        //Debugger.Break();

        //Console.Clear();
        Console.WriteLine("运行开始");
        {
            //using var db = new LiteDB4.LiteDatabase($@"Filename={DataBaseCommand.BaseUri}{DataBase};Connection=Direct;");
            //if (!db.CollectionExists("ImgData"))
            //{
            //    var SIS = db.GetCollection<ImgData>("ImgData");
            //    SIS.EnsureIndex(x => x.Date);
            //    SIS.EnsureIndex(x => x.Hash);
            //    SIS.EnsureIndex(x => x.id, true);
            //    SIS.EnsureIndex(x => x.Status);
            //    SIS.EnsureIndex(x => x.Type);
            //    var SIS2 = db.GetCollection("Data");
            //    SIS2.EnsureIndex("id", true);
            //}
            if (File.Exists(SendMessageByte))
            {
                try
                {
                    sendMessage = SendMessage.ToClass(File.ReadAllBytes(SendMessageByte));
                }
                catch (Exception)
                {
                    sendMessage = new SendMessage();
                }
                sendMessage.ClientCount = new List<CLientInfo>();
            }
        }

        ImageType GetFileImageTypeFromHeader(byte[] headerBytes)
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

        #region 数据库读取

        Task.Factory.StartNew(async () =>
        {
            //while (!Debugger.IsAttached)
            //{
            //    Console.WriteLine("wait");
            //    Thread.Sleep(1000);
            //}
            //Debugger.Break();
            if (sendMessage.HisOperaDataBase == null)
                sendMessage.HisOperaDataBase = new Dictionary<string, long>();
            do
            {
                try
                {
                    ConcurrentDictionary<string, int> ReTry = new ConcurrentDictionary<string, int>();

                    var DirInfo = new DirectoryInfo(ImgPath).GetFiles().Select(x => new Tuple<string, long>(x.Name, x.Length)).ToArray();
                    var ReadCount = 0;
                    foreach (var Dir in DirInfo)
                    {
                        ReadCount += 1;
                        try
                        {
                            if (Dir.Item1.ToLower().Contains("log")) continue;
                            if (sendMessage.HisOperaDataBase.ContainsKey(Dir.Item1))
                            {
                                if (sendMessage.HisOperaDataBase[Dir.Item1] == Dir.Item2)
                                    continue;
                                else if (sendMessage.HisOperaDataBase[Dir.Item1] > 0 && sendMessage.HisOperaDataBase[Dir.Item1] < 2)
                                {
                                    Console.WriteLine($"{Dir.Item1}重试{sendMessage.HisOperaDataBase[Dir.Item1]}次，跳过");

                                    sendMessage.HisOperaDataBase[Dir.Item1] = Dir.Item2;
                                    await File.WriteAllBytesAsync(SendMessageByte, sendMessage.Send());
                                    continue;
                                }
                            }
                            sendMessage.CurrectDataBase = $"{Dir.Item1} | {ReadCount}/{DirInfo.Length}";
                            WriteToMessage($"{sendMessage.CurrectDataBase} Start");

                            #region MyRegion

                            //File.Copy($"{BaseUri}{Path.DirectorySeparatorChar}Image{Path.DirectorySeparatorChar}{CurrectDataBase}", $"{BaseUri2}{Path.DirectorySeparatorChar}{CurrectDataBase}");
                            //    var Tempdb = new LiteDB4.LiteDatabase(File.Open(@$"{BaseUri}{Path.DirectorySeparatorChar}Image{Path.DirectorySeparatorChar}{Dir.Item1}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                            //var Tempdb = new LiteDB4.LiteDatabase($@"Filename={BaseUri2}{Path.DirectorySeparatorChar}{CurrectDataBase};");
                            //var Count = 0;
                            //foreach (var Item in Tempdb.GetCollection<OriImage>("WebpData").FindAll())
                            //{
                            //    try
                            //    {
                            //        using var Image = new LiteDB4.LiteDatabase(File.Open(@$"{BaseUri}Image.db", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                            //        var Type = Image.GetCollection<ImgData>("ImgData").FindById(Item.id).Type;
                            //        Item.Status = Type == "jpg" ? true : false;
                            //        Tempdb.GetCollection<OriImage>("WebpData").Update(Item);
                            //        Count += 1;
                            //        Console.WriteLine($"{CurrectDataBase}   处理     ||      {Count}"); Image.Dispose();
                            //        GC.Collect();
                            //    }
                            //    catch (Exception)
                            //    {
                            //    }
                            //}
                            //Console.WriteLine($"{CurrectDataBase}处理完毕");
                            //Tempdb.Dispose();
                            //GC.Collect();
                            //Console.Clear();
                            //FileStream fs = null;
                            //try
                            //{
                            //    fs = new FileStream(@$"{ImgPath}{Dir.Item1}", FileMode.Open, FileAccess.Read, FileShare.None);
                            //}
                            //catch (Exception)
                            //{
                            //    continue;
                            //}
                            //finally
                            //{
                            //    if (fs != null)
                            //        fs.Close();
                            //}
                            //if (File.Exists(Dir.Item1))
                            //    File.Delete(Dir.Item1);
                            //  Tempdb2 = new LiteDB4.LiteDatabase(@$"Filename={ImgPath}{Dir.Item1}");

                            #endregion MyRegion

                            async void WritIng()
                            {
                                while (Writeing || WriteSave.Count > 30)
                                {
                                    await Task.Delay(100);
                                }
                            }
                            // Console.WriteLine($"{sendMessage.CurrectDataBase} Start | ReadImgData");
                            // WriteToMessage($"{sendMessage.CurrectDataBase} Start | ReadImgData");

                            // var ForeachList = new List<string>();
                            var Skip = 0;
                            try
                            {
                                //WritIng();
                                //WritingSendBase = Dir.Item1;
                                //WriteToMessage($"{Dir.Item1} |  WaitSendDataBase");

                                //while (WritingSendBase == Dir.Item1)
                                //{
                                //    await Task.Delay(1000);
                                //}
                                //WriteToMessage($"{Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {Dir.Item1}/{ReadCount}/{DirInfo.Length}完成");

                                //if (!sendMessage.HisOperaDataBase.ContainsKey(Dir.Item1))
                                //    sendMessage.HisOperaDataBase.Add(Dir.Item1, new FileInfo($"{ImgPath}{Dir.Item1}").Length);
                                //else
                                //    sendMessage.HisOperaDataBase[Dir.Item1] = new FileInfo($"{ImgPath}{Dir.Item1}").Length;
                                //await File.WriteAllBytesAsync(SendMessageByte, sendMessage.Send());
                                //GC.Collect();
                                //continue;
                                using var Tempdb2 = new LiteDatabase(@$"Filename={ImgPath}{Dir.Item1};");
                                WriteToMessage($"{sendMessage.CurrectDataBase} Start | EnsureIndex");
                                Tempdb2.GetCollection<WebpImage>("WebpData").EnsureIndex(x => x.Status);
                                if (Tempdb2.CollectionExists("ImgData"))
                                {
                                    WritingSendBase = Dir.Item1;
                                    Tempdb2.Dispose();
                                    while (WritingSendBase == Dir.Item1)
                                    {
                                        Console.WriteLine($"{Dir.Item1} |  WaitSendDataBase");
                                        await Task.Delay(1000);
                                    }
                                    var C1 = Tempdb2.GetCollection<OriImage>("ImgData").Count();
                                    if (C1 != 0)
                                    {
                                        if (C1 != Tempdb2.GetCollection<WebpImage>("WebpData").Count())
                                        {
                                            WriteToMessage($"{Dir.Item1} Start |  Mov ImgData WebpData");
                                        ForeachListB:
                                            try
                                            {
                                                foreach (var item in Tempdb2.GetCollection<OriImage>("ImgData").Find(Query.All(), Skip).Select(x => x.id))
                                                {
                                                    if (!Tempdb2.GetCollection<WebpImage>("WebpData").Exists(item))
                                                    {
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
                                                                Tempdb2.GetCollection<WebpImage>("WebpData").Upsert(Add);
                                                            }
                                                        }
                                                    }
                                                    Skip += 1;
                                                    Tempdb2.GetCollection<OriImage>("ImgData").Delete(item);
                                                    WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {sendMessage.CurrectDataBase} | {Skip}/{C1} | {ReadCount}/{DirInfo.Length}");
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
                                    WriteToMessage($"{sendMessage.CurrectDataBase} Start | Rebuild");
                                    Tempdb2.DropCollection("ImgData");
                                    Tempdb2.Rebuild();
                                    Tempdb2.Dispose();
                                    GC.Collect();
                                }
                            }
                            catch (Exception)
                            {
                            }
                            GC.Collect();

                            //Tempdb2.Dispose();
                            //file.Close();
                            //file.Dispose();
                            //foreach (var Image in Find)
                            //{
                            //    File.AppendAllLines(Dir.Item1, new string[] { Image });
                            //}
                            //GC.Collect();

                            #region Old

                            //var R1 = 0;
                            //var R2 = 0;
                            //foreach (var ID in ForeachList2)
                            //{
                            //    while (Writeing)
                            //    {
                            //        await Task.Delay(100);
                            //    }
                            //    using var TempFindDb = new LiteDatabase(File.Open(@$"{ImgPath}{sendMessage.CurrectDataBase}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                            //    var WebpData = TempFindDb.GetCollection<WebpImage>("WebpData");
                            //    var ImgData = TempFindDb.GetCollection<OriImage>("ImgData");
                            //    var item = WebpData.FindById(ID);
                            //    bool change = false;

                            //    if (item.Type == null)
                            //    {
                            //        var TempFind = ImgData.FindById(ID);
                            //        if (TempFind != null)
                            //        {
                            //            if (GetFileImageTypeFromHeader(TempFind.img) == ImageType.JPEG)
                            //                item.Type = "jpg";
                            //            else if (GetFileImageTypeFromHeader(TempFind.img) == ImageType.GIF)
                            //                item.Type = "gif";
                            //            else
                            //                item.Type = GetFileImageTypeFromHeader(TempFind.img).ToString();
                            //            change = true;
                            //            R1 += 1;
                            //        }
                            //    }
                            //    if (GetFileImageTypeFromHeader(item.img) != ImageType.Unknown)
                            //    {
                            //        item.Status = false;
                            //        change = true;
                            //        R2 += 1;
                            //    }
                            //    TempFindDb.Dispose();
                            //    if (change)
                            //    {
                            //        WriteSave.Add(item);
                            //    }
                            //    Console.WriteLine($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {R1} |  {R2}   |   {Dir.Item1}/{ReadCount}/{DirInfo.Length}");
                            //}

                            #endregion Old

                            var Fin = false;
                            WriteToMessage($"{sendMessage.CurrectDataBase} Start | ReadWebpData");
                            var ForeachList2 = new List<string>();
                            Skip = 0;
                        ForeachListA:
                            try
                            {
                                WritIng();
                                using var Tempdb2 = new LiteDatabase(File.Open(@$"{ImgPath}{Dir.Item1}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                                foreach (var item in Tempdb2.GetCollection<WebpImage>("WebpData").Find(x => !x.Status, Skip).Select(x => x.id))
                                {
                                    ForeachList2.Add(item);
                                    Skip += 1;
                                    GC.Collect();
                                    WritIng();
                                }
                                Tempdb2.Dispose();
                                GC.Collect();
                            }
                            catch (Exception)
                            {
                                Skip += 1;
                                goto ForeachListA;
                            }
                            GC.Collect();
                            WriteToMessage($"{sendMessage.CurrectDataBase} Start | ForeachList2");

                            var ForeachList2Count = 0;
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
                                using var TempFindDb = new LiteDatabase(File.Open(@$"{ImgPath}{Dir.Item1}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                                var DBB = TempFindDb.GetCollection<WebpImage>("WebpData");
                                var TempFind = DBB.FindById(find);
                                TempFindDb.Dispose();
                                GC.Collect();
                                {
                                    if (TempFind != null)
                                    {
                                        //var Add = new OriImage()
                                        //{
                                        //    Date = TempFind.Date,
                                        //    From = TempFind.From,
                                        //    FromList = TempFind.FromList,
                                        //    Hash = TempFind.Hash,
                                        //    id = TempFind.id,
                                        //    img = TempFind.img,
                                        //    Status = false
                                        //};
                                        if (TempFind.From == "SIS") sendMessage.ReadFromSIS += 1;
                                        else sendMessage.ReadFromT66y += 1;
                                        WriteSend.Add(TempFind);
                                    }
                                }
                                var Temps = $" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {sendMessage.CurrectDataBase} | {ForeachList2Count}/{ForeachList2.Count} | {ReadCount}/{DirInfo.Length}";
                                WriteToMessage(Temps);
                                // TempFindDb.Dispose();
                                //file2.Close();
                                //file2.Dispose();
                            }

                            #region ForeachList

                            //Console.WriteLine($"{sendMessage.CurrectDataBase} Start | ForeachList");
                            //WriteToMessage($"{sendMessage.CurrectDataBase} Start | ForeachList");
                            //var ForeachListCount = 0;
                            //foreach (var find in ForeachList)
                            //{
                            //    ForeachListCount += 1;
                            //    if (ReTry.ContainsKey(find))
                            //    {
                            //        ReTry[find] += 1;
                            //        if (ReTry[find] > 2) continue;
                            //    }
                            //    else
                            //        ReTry.TryAdd(find, 0);
                            //    Fin = true;
                            //    Reading = true;
                            //    WritIng();
                            //    using var TempFindDb = new LiteDatabase(File.Open(@$"{ImgPath}{Dir.Item1}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                            //    var DBB = TempFindDb.GetCollection<OriImage>("ImgData");
                            //    var TempFind = DBB.FindById(find);
                            //    TempFindDb.Dispose();
                            //    GC.Collect();
                            //    Reading = false;

                            //    //if (GetFileImageTypeFromHeader(TempFind.img) == ImageType.Unknown)
                            //    {
                            //        //    TempFind.img = new byte[0];
                            //        //    TempFind.Status = true;
                            //        //    DBB.Update(TempFind);
                            //        //}
                            //        //else
                            //        //{
                            //        if (TempFind.From == "SIS") sendMessage.ReadFromSIS += 1;
                            //        else sendMessage.ReadFromT66y += 1;
                            //        WriteSend.Add(TempFind);
                            //    }
                            //    var Temps = $" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {sendMessage.CurrectDataBase} | {ForeachListCount}/{ForeachList.Count} | {ReadCount}/{DirInfo.Length}";
                            //    WriteToMessage(Temps);
                            //    Console.WriteLine(Temps);
                            //    // TempFindDb.Dispose();
                            //    //file2.Close();
                            //    //file2.Dispose();
                            //}
                            //Tempdb2.Dispose();
                            //if (File.Exists(Dir.Item1))
                            //    File.Delete(Dir.Item1);

                            #endregion ForeachList

                            if (!Fin)
                            {
                                WriteToMessage($"{Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {Dir.Item1}/{ReadCount}/{DirInfo.Length}完成");

                                if (!sendMessage.HisOperaDataBase.ContainsKey(Dir.Item1))
                                    sendMessage.HisOperaDataBase.Add(Dir.Item1, new FileInfo($"{ImgPath}{Dir.Item1}").Length);
                                else
                                    sendMessage.HisOperaDataBase[Dir.Item1] = new FileInfo($"{ImgPath}{Dir.Item1}").Length;
                                await File.WriteAllBytesAsync(SendMessageByte, sendMessage.Send());
                                GC.Collect();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            if (!sendMessage.HisOperaDataBase.ContainsKey(Dir.Item1))
                                sendMessage.HisOperaDataBase.Add(Dir.Item1, 0);
                            else
                                sendMessage.HisOperaDataBase[Dir.Item1] += 1;
                        }
                    }
                    WriteToMessage("无可处理数据，等待24小时");
                    await Task.Delay(1000 * 60 * 60 * 12);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            } while (true);
        });

        #endregion 数据库读取

        #region 数据库读取旧

        //async Task WriteToSISWeb(BsonDocument item)
        //{
        //    var ErrorCount = 0;
        //    while (SISWebdbCheck)
        //    {
        //        await Task.Delay(1000);
        //    }
        //back:
        //    try
        //    {
        //        using var SISWebdb = new LiteDB4.LiteDatabase($@"Filename={BaseUri}SISWeb.db;");
        //        SISWebdbCheck = true;
        //        var Img_Data = SISWebdb.GetCollection("ImgData");
        //        Img_Data.Update(item);
        //        SISWebdb.Dispose();
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorCount += 1;
        //        var Message = $"SISWeb写入失败:{DateTime.Now:HH:m:s}-{ex}-计数{ErrorCount}次";
        //        WriteToMessage($"{Message}");
        //        await Task.Delay(1000);
        //        if (ErrorCount < 60 * 30)
        //            goto back;
        //    }
        //    finally
        //    {
        //        SISWebdbCheck = false;
        //    }
        //}
        //async Task WriteToT66yWeb(BsonDocument item)
        //{
        //    var ErrorCount = 0;
        //    while (T66yWebdbCheck)
        //    {
        //        await Task.Delay(1000);
        //    }
        //back:
        //    try
        //    {
        //        using var T66yWebdb = new LiteDB4.LiteDatabase($@"Filename={BaseUri}T66yWeb.db;");
        //        T66yWebdbCheck = true;
        //        var Img_Data = T66yWebdb.GetCollection("ImgData");
        //        Img_Data.Update(item);
        //        T66yWebdb.Dispose();
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorCount += 1;
        //        var Message = $"T66yWeb写入失败:{DateTime.Now:HH:m:s}-{ex}-计数{ErrorCount}次";
        //        WriteToMessage($"{Message}");
        //        await Task.Delay(1000);
        //        if (ErrorCount < 60 * 30)
        //            goto back;
        //    }
        //    finally
        //    {
        //        T66yWebdbCheck = false;
        //    }
        //}

        //_ = Task.Factory.StartNew(async () =>
        //{
        //    int WaitTime = 100;
        //    var FinCheck = false;
        //    do
        //    {
        //        if (_sessionsCheck.Count != 0 && WriteSend.Count < 5 && WriteSave.Count < 20)
        //        {
        //            FinCheck = false;
        //            WaitTime = 1000;
        //            while (SISWebdbCheck)
        //            {
        //                await Task.Delay(100);
        //            }
        //            SISWebdbCheck = true;
        //            using var SISWebdb = new LiteDB4.LiteDatabase(File.Open($@"{BaseUri}SISWeb.db", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        //            using var db = new LiteDB4.LiteDatabase(File.Open($@"{BaseUri}SIS.db", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        //            try
        //            {
        //                var Img_Data = SISWebdb.GetCollection("ImgData");
        //                var SISDB = db.GetCollection<SISImgData>("ImgData");
        //                foreach (var item in Img_Data.Find(Query.EQ("Status", "false")))
        //                {
        //                    FinCheck = true;
        //                    sendMessage.ReadFromSIS += 1;
        //                    var SisData = SISDB.FindById(item["Uri"]);
        //                    if (SisData.img != null && SisData.img.Length > 1024)
        //                    {
        //                        var WriteSendDate = new ImgData()
        //                        {
        //                            Date = SisData.Date,
        //                            FromList = SisData.FromList,
        //                            Hash = SisData.Hash,
        //                            id = SisData.id,
        //                            img = SisData.img,
        //                            Status = true,
        //                            Type = ""
        //                        };
        //                        WriteSend.Add(WriteSendDate);
        //                        if (WriteSend.Count > 25) break;
        //                    }
        //                    else
        //                    {
        //                        item["Status"] = "True";
        //                        await WriteToSISWeb(item);
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                var Message = $"SIS读取失败:{DateTime.Now:HH:m:s}-{ sendMessage.ReadFromSIS }-{ex}";
        //                File.AppendAllLines("Error.txt", new string[] { Message });
        //                WriteToMessage($"{Message}");
        //            }
        //            finally
        //            {
        //                if (!FinCheck)
        //                {
        //                    WaitTime = 1000 * 60 * 60 * 24;
        //                    WriteToMessage($"SIS读取完毕，等待24小时");
        //                    Console.WriteLine($"SIS读取完毕，等待24小时");
        //                }
        //                GC.Collect();
        //                SISWebdbCheck = false;
        //            }
        //        }

        //        await Task.Delay(WaitTime);
        //    } while (true);
        //});
        //_ = Task.Factory.StartNew(async () =>
        //{
        //    int WaitTime = 100;
        //    var FinCheck = false;
        //    ConcurrentDictionary<string, int> pairs = new ConcurrentDictionary<string, int>();
        //    do
        //    {
        //        //while (Setting.T66yDownloadIng)
        //        //{
        //        //    await Task.Delay(1000);
        //        //    SecondCount += 1;
        //        //    WriteToMessage($"T66y发送等待:{SecondCount}秒");
        //        //}
        //        if (_sessionsCheck.Count != 0 && WriteSend.Count < 5 && WriteSave.Count < 20)
        //        {
        //            FinCheck = false;
        //            WaitTime = 1000;
        //            while (T66yWebdbCheck)
        //            {
        //                await Task.Delay(100);
        //            }
        //            T66yWebdbCheck = true;
        //            using var T66yWebdb = new LiteDB4.LiteDatabase(File.Open($@"{BaseUri}T66yWeb.db", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        //            using var db = new LiteDB4.LiteDatabase(File.Open($@"{BaseUri}T66y.db", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        //            try
        //            {
        //                var Img_Data = T66yWebdb.GetCollection("ImgData");
        //                var T66yDB = db.GetCollection<T66yImgData>("ImgData");
        //                foreach (var item in Img_Data.Find(Query.EQ("Status", "false")))
        //                {
        //                    if (pairs.ContainsKey(item["Uri"]))
        //                    {
        //                        pairs[item["Uri"]] += 1;
        //                        WriteToMessage($"{item["Uri"]}重复{pairs[item["Uri"]]}");
        //                        if (pairs[item["Uri"]] > 2) continue;
        //                    }
        //                    else
        //                        pairs.TryAdd(item["Uri"], 0);
        //                    FinCheck = true;
        //                    if (WriteSend.FirstOrDefault(x => x.id == item["Uri"]) != null) continue;
        //                    sendMessage.ReadFromT66y += 1;
        //                    var T66yData = T66yDB.FindById(item["Uri"]);
        //                    if (T66yData.img != null && T66yData.img.Length > 1024)
        //                    {
        //                        var WriteSendDate = new ImgData()
        //                        {
        //                            Date = T66yData.Date,
        //                            FromList = T66yData.FromList,
        //                            Hash = T66yData.Hash,
        //                            id = T66yData.id,
        //                            img = T66yData.img,
        //                            Status = false,
        //                            Type = ""
        //                        };
        //                        WriteSend.Add(WriteSendDate);
        //                        if (WriteSend.Count > 25) break;
        //                    }
        //                    else
        //                    {
        //                        item["Status"] = "True";
        //                        await WriteToT66yWeb(item);
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                var Message = $"T66y读取失败:{DateTime.Now:HH:m:s}-{ sendMessage.ReadFromT66y }-{ex}";
        //                File.AppendAllLines("Error.txt", new string[] { Message });
        //                WriteToMessage($"{Message}");
        //            }
        //            finally
        //            {
        //                if (!FinCheck)
        //                {
        //                    WaitTime = 1000 * 60 * 60 * 24;
        //                    WriteToMessage($"T66y读取完毕，等待24小时");
        //                    Console.WriteLine($"T66y读取完毕，等待24小时");
        //                }
        //                GC.Collect();
        //                T66yWebdbCheck = false;
        //            }
        //        }

        //        await Task.Delay(WaitTime);
        //    } while (true);
        //});

        #endregion 数据库读取旧

        #region 发送数据

        _ = Task.Factory.StartNew(async () =>
        {
            return;
            do
            {
                try
                {
                    if (_sessions.Count != 0)
                    {
                        foreach (var session in _sessions)
                        {
                            var IpAddress = session.Value.RemoteEndPoint.Address.ToString();
                            if (!_sessionsCheck.ContainsKey(IpAddress))
                                _sessionsCheck.TryAdd(IpAddress, new SessionsCheck() { MaxCheck = true });

                            if (_sessionsCheck[IpAddress].MaxCheck)
                            {
                                if (WriteSend.TryTake(out WebpImage SendItem))
                                {
                                    sendMessage.SendCount += 1;
                                    //await session.Value.SendBinaryAsync(SendItem.Send());
                                    _sessionsCheck[IpAddress].IdCheck = SendItem.id;
                                    _sessionsCheck[IpAddress].IdCheckFlag = false;
                                    int TryCountOne = 0;
                                    int TryCountTwo = 0;
                                    var S1 = Convert.ToBase64String(SendItem.Send());
                                    var S2 = Convert.ToBase64String(sendMessage.Send());
                                    await session.Value.SendTextAsync($"{S1}${S2}");
                                    while (!_sessionsCheck[IpAddress].IdCheckFlag)
                                    {
                                        await Task.Delay(10);
                                        TryCountOne += 1;
                                        if (TryCountOne == 1000)
                                        {
                                            TryCountOne = 0;
                                            TryCountTwo += 1;
                                        }
                                        if (TryCountTwo == 5) break;
                                    }
                                    if (TryCountTwo == 5)
                                    {
                                        WriteToMessage($"发送超时:{SendItem.id}");
                                        session.Value.Close(WebSocketCloseCode.NormalClosure).Wait();
                                        session.Value.Shutdown();
                                        session.Value.Dispose();
                                        _sessions.TryRemove(session.Key, out AsyncWebSocketSession throwAway);
                                        _sessionsCheck.TryRemove(IpAddress, out SessionsCheck check);
                                        //sendMessage.ClientCount.Remove(x=>x.);
                                        break;
                                    }
                                    //Interlocked.Increment(ref sendMessage.ClientCount[IpAddress].SendCount);
                                    //Interlocked.Add(ref sendMessage.ClientCount[IpAddress].SendByte, SendItem.img.Length);
                                    WriteToMessage($"正常发送数:{sendMessage.SendCount }");
                                }
                            }
                        }
                    }
                    else
                    {
                        await Task.Delay(1000);
                        WriteToMessage($"等待发送，总计数:{sendMessage.SendCount }");
                    }
                }
                catch (Exception ex)
                {
                    WriteToMessage($"发送错误{ex.Message}");
                }
            } while (true);
        });

        #endregion 发送数据

        #region 发送服务器信息

        //_ = Task.Factory.StartNew(async () =>
        //{
        //    do
        //    {
        //        try
        //        {
        //            await Task.Delay(1000);

        //            if (_sessions.Count != 0)
        //            {
        //                var SendData = sendMessage.Send();
        //                var SendBase = Convert.ToBase64String(SendData);
        //                await File.WriteAllBytesAsync(SendMessageByte, SendData);
        //                SendData = null;
        //                foreach (var session in _sessions)
        //                {
        //                    await session.Value.SendTextAsync(SendBase);
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //        }
        //    } while (true);
        //});

        #endregion 发送服务器信息

        #region 保存到数据库

        _ = Task.Factory.StartNew(() =>
        {
            do
            {
                try
                {
                    if (!WriteSave.TryTake(out WebpImage item)) continue;
                    //BackCheck:
                    //    FileStream fs = null;
                    //    try
                    //    {
                    //        fs = new FileStream(@$"{ImgPath}{sendMessage.CurrectDataBase}", FileMode.Open, FileAccess.Read, FileShare.None);
                    //    }
                    //    catch (Exception)
                    //    {
                    //        await Task.Delay(500);
                    //        goto BackCheck;
                    //    }
                    //    finally
                    //    {
                    //        if (fs != null)
                    //            fs.Close();
                    //    }
                    //    using var Tempdb = new LiteDB4.LiteDatabase($@"Filename={ImgPath}{sendMessage.CurrectDataBase};");
                    using var Tempdb = new LiteDatabase(@$"Filename={ImgPath}{item.Date}.db;");
                    try
                    {
                        //while (!Debugger.IsAttached)
                        //{
                        //    Console.WriteLine("wait");
                        //    Thread.Sleep(1000);
                        //}
                        //Debugger.Break();
                        //using var Tempdb = new LiteDatabase(File.Open(@$"{ImgPath}{sendMessage.CurrectDataBase}", FileMode.Open));
                        Writeing = true;
                        //while (Reading)
                        //{
                        //    await Task.Delay(100);
                        //}

                        var WebpData = Tempdb.GetCollection<WebpImage>("WebpData");
                        if (item != null)
                        {
                            if (WebpData.Exists(x => x.id == item.id))
                                WebpData.Update(item);
                            else
                                WebpData.Upsert(item);
                            sendMessage.SaveCount += 1;
                            var ImgData = Tempdb.GetCollection<OriImage>("ImgData");
                            var OriImgData = ImgData.FindById(item.id);
                            if (OriImgData != null)
                            {
                                OriImgData.Status = true;
                                ImgData.Update(OriImgData);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //Tempdb.Dispose();
                        //Tempdb = new LiteDatabase($@"Filename={ImgPath}{sendMessage.CurrectDataBase};");

                        File.WriteAllLines("Error.txt", new string[] { $"修改{ImgPath}{item.Date}.db数据库信息失败{ex.Message}|{item.id}" });
                        WriteToMessage($"{DateTime.Now:HH:m:s}保存失败{ex}");
                        try
                        {
                            File.AppendAllLines($"{BaseUri}TempImage.txt", new string[] { Convert.ToBase64String(item.Send()) });
                        }
                        catch (Exception)
                        {
                        }
                    }
                    finally
                    {
                        Tempdb.Dispose();
                        GC.Collect();
                        Writeing = false;
                        //Tempdb.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    File.WriteAllLines("Error.txt", new string[] { $"{DateTime.Now:HH:m:s}保存失败{ex}" });
                    WriteToMessage($"{DateTime.Now:HH:m:s}保存失败{ex}");
                    Writeing = false;
                }
            } while (true);
        });

        #region 遗留代码

        //_ = Task.Factory.StartNew(async () =>
        //{
        //    var DataBase = "Image.db";
        //    do
        //    {
        //        try
        //        {
        //            if (!WriteSave.TryTake(out ImgData item)) continue;
        //            using var db = new LiteDB4.LiteDatabase($@"Filename={BaseUri}{DataBase};");
        //            var SISDB = db.GetCollection<ImgData>("ImgData");
        //            try
        //            {
        //                if (item.img != null)
        //                {
        //                    if (!SISDB.Exists(x => x.id == item.id))
        //                    {
        //                        SISDB.Upsert(item);
        //                        sendMessage.SaveCount += 1;
        //                    }
        //                    else
        //                    {
        //                        sendMessage.PassCount += 1;
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                File.WriteAllLines("Error.txt", new string[] { $"保存失败{ex}|{item.id}" });
        //                WriteToMessage($"{DateTime.Now:HH:m:s}保存失败{ex}");
        //                File.AppendAllLines($"{BaseUri}TempImage.txt", new string[] { Convert.ToBase64String(item.Send()) });
        //            }
        //            finally
        //            {
        //                GC.Collect();
        //            }

        //            if (item.Status)
        //            {
        //                while (SISWebdbCheck)
        //                {
        //                    await Task.Delay(100);
        //                }
        //                SISWebdbCheck = true;
        //                using var SISWebdb = new LiteDB4.LiteDatabase($@"Filename={BaseUri}SISWeb.db;");
        //                try
        //                {
        //                    var Img_Data = SISWebdb.GetCollection("ImgData");
        //                    var FindOne = Img_Data.FindOne(x => x["Uri"] == item.id);
        //                    if (FindOne != null)
        //                    {
        //                        Img_Data.Delete(FindOne);
        //                    }
        //                    Img_Data.Upsert(new BsonDocument() { { "Uri", item.id }, { "Status", "True" } });
        //                }
        //                catch (Exception ex)
        //                {
        //                    File.WriteAllLines("Error.txt", new string[] { $"修改SISWebd信息失败{ex}|{item.id}" });
        //                    WriteToMessage($"{DateTime.Now:HH:m:s}保存失败{ex}");
        //                }

        //                SISWebdbCheck = false;
        //            }
        //            else
        //            {
        //                while (T66yWebdbCheck)
        //                {
        //                    await Task.Delay(100);
        //                }
        //                T66yWebdbCheck = true;
        //                using var T66yWebdb = new LiteDB4.LiteDatabase($@"Filename={BaseUri}T66yWeb.db;");
        //                try
        //                {
        //                    var Img_Data = T66yWebdb.GetCollection("ImgData");
        //                    var FindOne = Img_Data.FindOne(x => x["Uri"] == item.id);
        //                    if (FindOne != null)
        //                    {
        //                        Img_Data.Delete(FindOne);
        //                    }
        //                    Img_Data.Upsert(new BsonDocument() { { "Uri", item.id }, { "Status", "True" } });
        //                }
        //                catch (Exception ex)
        //                {
        //                    File.WriteAllLines("Error.txt", new string[] { $"修改T66yWebd信息失败{ex}|{item.id}" });
        //                    WriteToMessage($"{DateTime.Now:HH:m:s}保存失败{ex}");
        //                }
        //                T66yWebdbCheck = false;
        //            }
        //        }
        //        catch (Exception)
        //        {
        //        }
        //    } while (true);
        //});

        #endregion 遗留代码

        #endregion 保存到数据库
    }

    public void OnSessionStarted(AsyncWebSocketSession session)
    {
        try
        {
            // _sessions.TryAdd(session.SessionKey, session);
            //_sessionsCheck.TryAdd(IpAddress, new SessionsCheck() { MaxCheck = true });
            //if (!sendMessage.ClientCount.ContainsKey(IpAddress))
            //{
            //    sendMessage.ClientCount.Add(IpAddress, new CLientInfo());
            //}
            Console.WriteLine($"Webp设备远程{session}连接");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{DateTime.Now:HH:m:s}连接失败:{ex.Message}");
        }
    }

    public void OnSessionClosed(AsyncWebSocketSession session)
    {
        try
        {
            var IpAddress = session.RemoteEndPoint.Address.ToString();
            Console.WriteLine($"Webp设备远程{session.RemoteEndPoint}断开");
            _sessions.TryRemove(session.SessionKey, out AsyncWebSocketSession throwAway);
            _sessionsCheck.TryRemove(IpAddress, out SessionsCheck check);
            // sendMessage.ClientCount.Remove(IpAddress);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{DateTime.Now:HH:m:s}关闭失败:{ex.Message}");
        }
    }

    public void OnSessionTextReceived(AsyncWebSocketSession session, string text)
    {
        try
        {
            var IpAddress = session.RemoteEndPoint.Address.ToString();
            if (text.Length > 1024)
            {
                try
                {
                    var GetData = WebpImage.ToClass(Convert.FromBase64String(text));
                    if (GetData.img != null && GetData.img.Length > 1000)
                    {
                        //if (!sendMessage.ClientCount.ContainsKey(IpAddress))
                        //    sendMessage.ClientCount.Add(IpAddress, new CLientInfo());
                        //Interlocked.Increment(ref sendMessage.ClientCount[IpAddress].GetCount);
                        //Interlocked.Add(ref sendMessage.ClientCount[IpAddress].GetByte, GetData.img.Length);
                        WriteSave.Add(GetData);
                    }
                }
                catch (Exception ex)
                {
                    WriteToMessage($"{DateTime.Now:HH:m:s}接收数据失败:{ex.Message}");
                }
            }
            else
            {
                if (!_sessionsCheck.ContainsKey(IpAddress))
                    _sessionsCheck.TryAdd(IpAddress, new SessionsCheck() { MaxCheck = true });
                switch (text)
                {
                    case "true":
                        _sessionsCheck[IpAddress].MaxCheck = true;
                        break;

                    case "false":
                        _sessionsCheck[IpAddress].MaxCheck = false;
                        break;

                    case "Close":
                        //  Environment.Exit(0);
                        break;

                    default:
                        _sessionsCheck[IpAddress].IdCheckFlag = _sessionsCheck[IpAddress].IdCheck == text;
                        break;
                }
            }
            // WriteToMessage($"设置{session.RemoteEndPoint}为{_sessionsCheck[IpAddress]}");
        }
        catch (Exception ex)
        {
            WriteToMessage($"{DateTime.Now:HH:m:s}接收文本失败:{ex.Message}");
        }
    }

    public void OnSessionBinaryReceived(AsyncWebSocketSession session, byte[] data, int offset, int count)
    {
        try
        {
            var GetData = WebpImage.ToClass(data);
            if (GetData.img != null && GetData.img.Length > 1000)
            {
                var IpAddress = session.RemoteEndPoint.Address.ToString();
                //Interlocked.Increment(ref sendMessage.ClientCount[IpAddress].GetCount);
                //Interlocked.Add(ref sendMessage.ClientCount[IpAddress].GetByte, GetData.img.Length);
                WriteSave.Add(GetData);
            }
        }
        catch (Exception ex)
        {
            WriteToMessage($"{DateTime.Now:HH:m:s}接收数据失败:{ex.Message}");
        }
    }

    private int Posion = 0;
    private string Md5 = "";
    private byte[] Writing = null;
    private ArrayPool<Byte> ArrayPool = new ArrayPool<Byte>();
    private int RedownloadCount = 0;

    public Tuple<byte[], string, string> Get3(string Status)
    {
        if (string.IsNullOrEmpty(WritingSendBase))
            return null;
        string status = string.Empty;
        if (Writing != null)
            ArrayPool.Return(Writing);
        if (Status == "start")
        {
            WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | 发送{WritingSendBase} | {status}");
            Md5 = "";
            Posion = 0;
            GC.Collect();
            return new Tuple<byte[], string, string>(null, WritingSendBase, "start");
        }
        if (Status != "continue")
        {
            RedownloadCount += 1;
            WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | 重新发送{WritingSendBase} | {status} | {RedownloadCount}");

            Md5 = "";
            Posion = 0;
            GC.Collect();
        }
        using var read = File.OpenRead(@$"{ImgPath}{WritingSendBase}");
        if (read.Length - Posion > 1024 * 1024 * 10)
        {
            Writing = ArrayPool.Rent(1024 * 1024 * 10);
            read.Position = Posion;
            read.Read(Writing, 0, Writing.Length);
            Posion += 1024 * 1024 * 10;
        }
        else
        {
            RedownloadCount = 0;
            Writing = ArrayPool.Rent((int)(read.Length - Posion));
            read.Position = Posion;
            read.Read(Writing, 0, Writing.Length);
            Posion += Writing.Length;
            status = "fin";
        }
        WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {WritingSendBase} | {Posion}/{read.Length} | {Math.Round(((float)Posion / (float)read.Length) * 100, 2)} | {status}");
        read.Dispose();

        var md5 = System.Security.Cryptography.MD5.Create();
        Md5 = Encoding.UTF8.GetString(md5.ComputeHash(Writing));
        return new Tuple<byte[], string, string>(Writing, Md5, status);
    }

    public byte[] Get2(string IpAddress)
    {
        try
        {
            CLientInfo cLientInfo = null;
            if (!sendMessage.ClientCount.Exists(x => x.IP == IpAddress))
            {
                sendMessage.ClientCount.Add(new CLientInfo() { IP = IpAddress });
                Console.WriteLine($"Webp设备远程{IpAddress}连接");
            }
            return WriteSendMessage;
        }
        catch (Exception)
        {
        }
        return null;
    }

    public byte[] Get(string IpAddress)
    {
        try
        {
            GC.Collect();
            CLientInfo cLientInfo = null;
            if (!sendMessage.ClientCount.Exists(x => x.IP == IpAddress))
            {
                sendMessage.ClientCount.Add(new CLientInfo() { IP = IpAddress });
                Console.WriteLine($"Webp设备远程{IpAddress}连接");
            }

            if (WriteSend.TryTake(out WebpImage SendItem))
            {
                sendMessage.SendCount += 1;

                //var S1 = Convert.ToBase64String(SendItem.Send());
                //var S2 = Convert.ToBase64String(sendMessage.Send());

                cLientInfo = sendMessage.ClientCount.FirstOrDefault(x => x.IP == IpAddress);
                if (cLientInfo != null)
                {
                    Interlocked.Increment(ref cLientInfo.SendCount);
                    Interlocked.Add(ref cLientInfo.SendByte, SendItem.img.Length);
                }
                //WriteToMessage($"正常发送数:{sendMessage.SendCount }");
                return SendItem.Send();
            }
        }
        catch (Exception)
        {
        }
        return null;
    }

    public string Send(byte[] data, string status, string Md5, long size)
    {
        {
            try
            {
                if (status == "start")
                {
                    if (!Directory.Exists(BaseUri2)) Directory.CreateDirectory(BaseUri2);
                    if (!File.Exists($"{BaseUri2}{WritingSendBase}"))
                        File.Create($"{BaseUri2}{WritingSendBase}").Dispose();
                    else
                    {
                        do
                        {
                            File.Delete($"{BaseUri2}{WritingSendBase}");
                            Thread.Sleep(1000);
                        } while (File.Exists($"{BaseUri2}{WritingSendBase}"));
                        File.Create($"{BaseUri2}{WritingSendBase}").Dispose();
                    }
                    return "continue";
                }
                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                if (Encoding.UTF8.GetString(md5.ComputeHash(data)) == Md5)
                {
                    FileStream fileStream = new FileStream($"{BaseUri2}{WritingSendBase}", FileMode.Append, FileAccess.Write);
                    fileStream.Write(data, 0, data.Length);
                    WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {WritingSendBase} | {fileStream.Length}/{size} | {Math.Round(((float)fileStream.Length / (float)size) * 100, 2)} | {status}");
                    fileStream.Dispose();
                    if (status == "fin")
                    {
                        //if (!File.Exists($"{ImgPath}{WritingSendBase}"))
                        //    do
                        //    {
                        //        File.Delete($"{ImgPath}{WritingSendBase}");
                        //        Thread.Sleep(1000);
                        //    } while (File.Exists($"{ImgPath}{WritingSendBase}"));
                        WritingSendBase = string.Empty;
                        return "Fin";
                    }

                    return "continue";
                }
                else
                {
                    if (File.Exists($"{ImgPath}-{WritingSendBase}"))
                    {
                        do
                        {
                            File.Delete($"{ImgPath}-{WritingSendBase}");
                            Thread.Sleep(1000);
                        } while (File.Exists($"{ImgPath}-{WritingSendBase}"));
                    }
                    return "redownload";
                }
            }
            catch (Exception)
            {
                if (File.Exists($"{ImgPath}-{WritingSendBase}"))
                {
                    do
                    {
                        File.Delete($"{ImgPath}-{WritingSendBase}");
                        Thread.Sleep(1000);
                    } while (File.Exists($"{ImgPath}-{WritingSendBase}"));
                }
                return "redownload";
            }
        }
    }

    public void Set(string IpAddress, string text, byte[] data)
    {
        try
        {
            CLientInfo cLientInfo = null;
            if (!sendMessage.ClientCount.Exists(x => x.IP == IpAddress))
            {
                sendMessage.ClientCount.Add(new CLientInfo() { IP = IpAddress });
                Console.WriteLine($"Webp设备远程{IpAddress}连接");
            }
            if (data != null)
            {
                try
                {
                    var GetData = WebpImage.ToClass(data);
                    if (GetData.img != null && GetData.img.Length > 1000)
                    {
                        cLientInfo = sendMessage.ClientCount.FirstOrDefault(x => x.IP == IpAddress);
                        if (cLientInfo != null)
                        {
                            Interlocked.Increment(ref cLientInfo.GetCount);
                            Interlocked.Add(ref cLientInfo.GetByte, data.Length);
                        }

                        WriteSave.Add(GetData);
                    }
                }
                catch (Exception ex)
                {
                    WriteToMessage($"{DateTime.Now:HH:m:s}接收数据失败:{ex.Message}");
                }
            }
            else
            {
                if (!_sessionsCheck.ContainsKey(IpAddress))
                    _sessionsCheck.TryAdd(IpAddress, new SessionsCheck() { MaxCheck = true });
                switch (text)
                {
                    case "true":
                        _sessionsCheck[IpAddress].MaxCheck = true;
                        break;

                    case "false":
                        _sessionsCheck[IpAddress].MaxCheck = false;
                        break;

                    case "Close":
                        //  Environment.Exit(0);
                        break;

                    default:
                        _sessionsCheck[IpAddress].IdCheckFlag = _sessionsCheck[IpAddress].IdCheck == text;
                        break;
                }
            }
            // WriteToMessage($"设置{session.RemoteEndPoint}为{_sessionsCheck[IpAddress]}");
        }
        catch (Exception ex)
        {
            WriteToMessage($"{DateTime.Now:HH:m:s}接收文本失败:{ex.Message}");
        }
    }

    [Serializable]
    public class SendMessage
    {
        public int ReadFromSIS;
        public int ReadFromT66y;
        public int SendCount;
        public int SaveCount;
        public int PassCount;
        public string CurrectDataBase;

        //public Dictionary<string, CLientInfo> ClientCount = new Dictionary<string, CLientInfo>();
        public List<CLientInfo> ClientCount = new List<CLientInfo>();

        public Dictionary<string, long> HisOperaDataBase = new Dictionary<string, long>();
        public List<string> Message = new List<string>();
        public int WaitSendCount;
        public int WaitSaveCount;
        public string Mem;

        public byte[] Send()
        {
            Message.Clear();
            for (int i = MessageList.Count - 1; i > 0; i--)
            {
                Message.Add(MessageList.ElementAt(i));
            }
            for (int i = ClientCount.Count - 1; i > 0; i--)
            {
                //  if (!_sessions.ContainsKey(ClientCount.ElementAt(i).Key))
                //      ClientCount.Remove(ClientCount.ElementAt(i).Key);
            }

            WaitSendCount = WriteSend.Count;
            WaitSaveCount = WriteSave.Count;
            Mem = $"内存使用量:{Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB";
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter Formatter = new BinaryFormatter();
                Formatter.Serialize(stream, this);
                return stream.ToArray();
            }
        }

        public static SendMessage ToClass(byte[] data)
        {
            using var stream = new MemoryStream(data);
            IFormatter Fileformatter = new BinaryFormatter();
            Fileformatter.Binder = new UBinder();
            return Fileformatter.Deserialize(stream) as SendMessage;
        }

        public class UBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.EndsWith("SendMessage"))
                {
                    return typeof(SendMessage);
                }
                return (Assembly.GetExecutingAssembly()).GetType(typeName);
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

    internal class SessionsCheck
    {
        internal bool MaxCheck;
        internal string IdCheck;
        internal bool IdCheckFlag;
    }
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