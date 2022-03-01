using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebpInter;
using LiteDB;
using System.Runtime;

namespace SpiderServerInLinux
{
    internal class Image2Webp : IImage2Webp
    {
        private static byte[] WriteSendMessage = null;
        private static ConcurrentDictionary<string, SessionsCheck> _sessionsCheck = new ConcurrentDictionary<string, SessionsCheck>();
        private static SendMessage sendMessage = new SendMessage();
        private static ConcurrentQueue<string> MessageList = new ConcurrentQueue<string>();
        private static BlockingCollection<WebpImage> WriteSend = new BlockingCollection<WebpImage>(20);
        private static BlockingCollection<WebpImage> WriteSave = new BlockingCollection<WebpImage>();
        private string BaseUri = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64" ? @"Z:\publish\" : @$"{DataBaseCommand.ImageUri}";
        private string BaseUri2 = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64" ? @"Z:\publish\NewImage\" : @$"{DataBaseCommand.ImageUri}/NewImage/";
        private string SendMessageByte = $"{new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName}{Path.DirectorySeparatorChar}SendMessageByte.dat";
        private bool Writeing = false;
        private string WritingSendBase = string.Empty;
        private string ImgPath = $"{(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64" ? @"C:\Users\cdj68\Desktop\" : @$"{DataBaseCommand.ImageUri}")}Image{Path.DirectorySeparatorChar}";

        private void WriteToMessage(string s)
        {
            if (!MessageList.Contains(s))
            {
                //Console.WriteLine(s);
                Loger.Instance.ServerInfo("Webp", $"{s}");

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

            {
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
                //    await Task.Delay(1000);
                //    Console.WriteLine("Wait");
                //}
                if (sendMessage.HisOperaDataBase == null)
                    sendMessage.HisOperaDataBase = new Dictionary<string, long>();
                do
                {
                    try
                    {
                        ConcurrentDictionary<string, int> ReTry = new ConcurrentDictionary<string, int>();
                        var DirInfo = new DirectoryInfo(ImgPath).GetFiles().Select(x => new Tuple<string>(x.Name)).ToArray();
                        var ReadCount = 0;
                        foreach (var Dir in DirInfo)
                        {
                            if (Setting.SISDownloadIng || Setting.T66yDownloadIng)
                                WriteToMessage($" Download Waiting");
                            while (Setting.SISDownloadIng || Setting.T66yDownloadIng)
                            {
                                await Task.Delay(1000);
                            }
                            ReadCount += 1;
                            try
                            {
                                if (Path.GetFileNameWithoutExtension(Dir.Item1) == DateTime.Now.ToString("yyyy-MM-dd")) continue;
                                if (Path.GetFileNameWithoutExtension(Dir.Item1) == DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd")) continue;
                                if (Path.GetFileNameWithoutExtension(Dir.Item1) == DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd")) continue;
                                if (Dir.Item1.ToLower().Contains("log")) continue;
                                if (sendMessage.HisOperaDataBase.ContainsKey(Dir.Item1) && new FileInfo($"{ImgPath}{Dir.Item1}").Length < 1024 * 1024 * 700)
                                {
                                    if (sendMessage.HisOperaDataBase[Dir.Item1] == new FileInfo($"{ImgPath}{Dir.Item1}").Length)
                                        continue;
                                    else if (sendMessage.HisOperaDataBase[Dir.Item1] > 0 && sendMessage.HisOperaDataBase[Dir.Item1] < 2)
                                    {
                                        WriteToMessage($"{Dir.Item1}重试{sendMessage.HisOperaDataBase[Dir.Item1]}次，跳过");
                                        sendMessage.HisOperaDataBase[Dir.Item1] = new FileInfo($"{ImgPath}{Dir.Item1}").Length;
                                        await File.WriteAllBytesAsync(SendMessageByte, sendMessage.Send());
                                        continue;
                                    }
                                }
                                sendMessage.CurrectDataBase = $"{Dir.Item1} | {ReadCount}/{DirInfo.Length}";
                                try
                                {
                                    //WriteToMessage($"{sendMessage.CurrectDataBase} Start");
                                    //WritingSendBase = Dir.Item1;
                                    //while (WritingSendBase == Dir.Item1)
                                    //{
                                    //    WriteToMessage($"{Dir.Item1} |  WaitSendDataBase");
                                    //    await Task.Delay(1000);
                                    //}
                                    //if (!sendMessage.HisOperaDataBase.ContainsKey(Dir.Item1))
                                    //    sendMessage.HisOperaDataBase.Add(Dir.Item1, new FileInfo($"{ImgPath}{Dir.Item1}").Length);
                                    //else
                                    //    sendMessage.HisOperaDataBase[Dir.Item1] = new FileInfo($"{ImgPath}{Dir.Item1}").Length;
                                    //File.WriteAllBytes(SendMessageByte, sendMessage.Send());
                                    if (new FileInfo($"{ImgPath}{Dir.Item1}").Length > 1024 * 1024 * 200)
                                    {
                                        WriteToMessage($" {sendMessage.CurrectDataBase} Start");
                                        WritingSendBase = Dir.Item1;
                                        while (WritingSendBase == Dir.Item1)
                                        {
                                            WriteToMessage($" {Dir.Item1} |  WaitSendDataBase");
                                            await Task.Delay(1000);
                                        }
                                    }
                                    else
                                    {
                                        using var Tempdb2 = new LiteDatabase(@$"Filename={ImgPath}{Dir.Item1};");
                                        var Count = Tempdb2.GetCollection<WebpImage>("WebpData").Count(x => !x.Status);
                                        Tempdb2.Dispose();
                                        if (Count != 0)
                                        {
                                            WriteToMessage($" {sendMessage.CurrectDataBase} Start");
                                            WritingSendBase = Dir.Item1;
                                            Tempdb2.Dispose();
                                            while (WritingSendBase == Dir.Item1)
                                            {
                                                WriteToMessage($" {Dir.Item1} |  WaitSendDataBase");
                                                await Task.Delay(1000);
                                            }
                                        }
                                        else
                                        {
                                            if (!sendMessage.HisOperaDataBase.ContainsKey(Dir.Item1))
                                                sendMessage.HisOperaDataBase.Add(Dir.Item1, new FileInfo($"{ImgPath}{Dir.Item1}").Length);
                                            else
                                                sendMessage.HisOperaDataBase[Dir.Item1] = new FileInfo($"{ImgPath}{Dir.Item1}").Length;
                                            File.WriteAllBytes(SendMessageByte, sendMessage.Send());
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                }
                                GC.Collect();
                            }
                            catch (Exception ex)
                            {
                                WriteToMessage(ex.Message);
                                if (!sendMessage.HisOperaDataBase.ContainsKey(Dir.Item1))
                                    sendMessage.HisOperaDataBase.Add(Dir.Item1, 0);
                                else
                                    sendMessage.HisOperaDataBase[Dir.Item1] += 1;
                            }
                        }
                        WriteToMessage("无可处理数据，等待24小时");
                        await Task.Delay(1000 * 60 * 60 * 24);
                    }
                    catch (Exception ex)
                    {
                        WriteToMessage(ex.Message);
                    }
                } while (true);
            });

            #endregion 数据库读取
        }

        private int Posion = 0;
        private string Md5 = "";
        private byte[] Writing = null;
        private LiteDB.ArrayPool<Byte> ArrayPool = new LiteDB.ArrayPool<Byte>();
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
                WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | 发送{sendMessage.CurrectDataBase} | {status}");
                Md5 = "";
                Posion = 0;
                GC.Collect();
                return new Tuple<byte[], string, string>(null, WritingSendBase, "start");
            }
            if (Status != "continue")
            {
                RedownloadCount += 1;
                if (RedownloadCount != 1)
                {
                    WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | 重新发送{sendMessage.CurrectDataBase} | {status} | {RedownloadCount}");
                }

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
            WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {sendMessage.CurrectDataBase} | {HumanReadableFilesize(Posion)}/{ HumanReadableFilesize(read.Length)} | {Math.Round(((float)Posion / (float)read.Length) * 100, 2)} | {status}");
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
                    WriteToMessage($"Webp设备远程{IpAddress}连接");
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
                    WriteToMessage($"Webp设备远程{IpAddress}连接");
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

        private string HumanReadableFilesize(double size)
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
                        WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {sendMessage.CurrectDataBase} | {HumanReadableFilesize(fileStream.Length)}/{HumanReadableFilesize(size)} | {Math.Round(((float)fileStream.Length / (float)size) * 100, 2)} | {status}");
                        fileStream.Dispose();
                        if (status == "fin")
                        {
                            if (File.Exists($"{ImgPath}{WritingSendBase}"))
                            {
                                do
                                {
                                    File.Delete($"{ImgPath}{WritingSendBase}");
                                    Thread.Sleep(1000);
                                } while (File.Exists($"{ImgPath}{WritingSendBase}"));
                            }
                            File.Move($"{BaseUri2}{WritingSendBase}", $"{ImgPath}{WritingSendBase}");
                            WriteToMessage($" {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB | {sendMessage.CurrectDataBase}完成");
                            if (!sendMessage.HisOperaDataBase.ContainsKey(WritingSendBase))
                                sendMessage.HisOperaDataBase.Add(WritingSendBase, new FileInfo($"{ImgPath}{WritingSendBase}").Length);
                            else
                                sendMessage.HisOperaDataBase[WritingSendBase] = new FileInfo($"{ImgPath}{WritingSendBase}").Length;
                            File.WriteAllBytes(SendMessageByte, sendMessage.Send());
                            GC.Collect();
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
                    WriteToMessage($"Webp设备远程{IpAddress}连接");
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
}