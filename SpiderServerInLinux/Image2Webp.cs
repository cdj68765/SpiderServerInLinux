using Cowboy.WebSockets;
using LiteDB;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace SpiderServerInLinux
{
    internal class Image2Webp : AsyncWebSocketServerModule
    {
        private static BlockingCollection<ImgData> WriteSend = new BlockingCollection<ImgData>();
        private static BlockingCollection<ImgData> WriteSave = new BlockingCollection<ImgData>();

        public ConcurrentDictionary<string, List<ImgData>> _sessionsMission = new ConcurrentDictionary<string, List<ImgData>>();

        public ConcurrentDictionary<string, bool> _sessionsCheck = new ConcurrentDictionary<string, bool>();
        private SendMessage sendMessage = new SendMessage();
        private static ConcurrentQueue<string> MessageList = new ConcurrentQueue<string>();

        [Serializable]
        public class SendMessage
        {
            public int ReadFromSIS;
            public int ReadFromT66y;
            public int SendCount;
            public int SaveCount;
            public int PassCount;
            public Dictionary<string, CLientInfo> ClientCount = new Dictionary<string, CLientInfo>();

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
        }

        [Serializable]
        public class OriImgData : SISImgData
        {
            public new bool Status;
        }

        [Serializable]
        internal class ImgData2 : T66yImgData
        {
            public string Type { get; set; }

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

        private void WriteToMessage(string s)
        {
            if (!MessageList.Contains(s))
                MessageList.Enqueue(s);
            if (MessageList.Count > 50)
                MessageList.TryDequeue(out string ss);
        }

        public Image2Webp() : base(@"/Webp")
        {
            bool SISWebdbCheck = false;
            bool T66yWebdbCheck = false;
            while (!Debugger.IsAttached)
            {
                Loger.Instance.LocalInfo("wait");
                Thread.Sleep(1000);
            }
            //Debugger.Break();
            {
                //using var db = new LiteDatabase($@"Filename={DataBaseCommand.BaseUri}{DataBase};Connection=Direct;");
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
                if (File.Exists($"{Path.DirectorySeparatorChar}root{Path.DirectorySeparatorChar}SendMessageByte.dat"))
                {
                    sendMessage = SendMessage.ToClass(File.ReadAllBytes($"{Path.DirectorySeparatorChar}root{Path.DirectorySeparatorChar}SendMessageByte.dat"));
                }
            }

            #region 数据库读取

            _ = Task.Factory.StartNew(async () =>
         {
             do
             {
                 int SecondCount = 0;
                 while (Setting.SISDownloadIng)
                 {
                     await Task.Delay(1000);
                     SecondCount += 1;
                     WriteToMessage($"SIS发送等待:{SecondCount}秒");
                 }
                 if (!Setting.SISDownloadIng && _sessionsCheck.Count != 0 && WriteSend.Count < 5 && WriteSave.Count < 20)
                 {
                     while (SISWebdbCheck)
                     {
                         await Task.Delay(100);
                     }
                     SISWebdbCheck = true;
                     using var SISWebdb = new LiteDatabase($@"Filename={DataBaseCommand.BaseUri}SISWeb.db;Connection=Shared;");
                     using var db = new LiteDatabase($@"Filename={DataBaseCommand.BaseUri}SIS.db;ReadOnly=True;Connection=Shared;");
                     try
                     {
                         var Img_Data = SISWebdb.GetCollection("ImgData");
                         var SISDB = db.GetCollection<SISImgData>("ImgData");
                         foreach (var item in Img_Data.Find(Query.EQ("Status", "false")))
                         {
                             sendMessage.ReadFromSIS += 1;
                             var SisData = SISDB.FindById(item["Uri"]);
                             if (SisData.img != null && SisData.img.Length > 1024)
                             {
                                 var WriteSendDate = new ImgData()
                                 {
                                     Date = SisData.Date,
                                     FromList = SisData.FromList,
                                     Hash = SisData.Hash,
                                     id = SisData.id,
                                     img = SisData.img,
                                     Status = true,
                                     Type = ""
                                 };
                                 WriteSend.Add(WriteSendDate);
                                 if (WriteSend.Count > 25) break;
                             }
                             else
                             {
                                 item["Status"] = "True";
                                 Img_Data.Update(item);
                             }
                         }
                     }
                     catch (Exception ex)
                     {
                         var Message = $"SIS读取失败:{DateTime.Now:HH:m:s}-{ sendMessage.ReadFromSIS }-{ex}";
                         File.AppendAllLines("Error.txt", new string[] { Message });
                         WriteToMessage($"{Message}");
                     }
                     finally
                     {
                         GC.Collect();
                         SISWebdbCheck = false;
                     }
                 }
                 else
                 {
                     await Task.Delay(1000);
                 }
             } while (true);
         });
            _ = Task.Factory.StartNew(async () =>
            {
                do
                {
                    int SecondCount = 0;
                    while (Setting.T66yDownloadIng)
                    {
                        await Task.Delay(1000);
                        SecondCount += 1;
                        WriteToMessage($"T66y发送等待:{SecondCount}秒");
                    }
                    if (!Setting.T66yDownloadIng && _sessionsCheck.Count != 0 && WriteSend.Count < 5 && WriteSave.Count < 20)
                    {
                        while (T66yWebdbCheck)
                        {
                            await Task.Delay(100);
                        }
                        T66yWebdbCheck = true;
                        using var db = new LiteDatabase($@"Filename={DataBaseCommand.BaseUri}T66y.db;ReadOnly=True;Connection=Shared;");
                        using var T66yWebdb = new LiteDatabase($@"Filename={DataBaseCommand.BaseUri}T66yWeb.db;Connection=Shared;");

                        try
                        {
                            var Img_Data = T66yWebdb.GetCollection("ImgData");
                            var T66yDB = db.GetCollection<T66yImgData>("ImgData");
                            foreach (var item in Img_Data.Find(Query.EQ("Status", "false")))
                            {
                                sendMessage.ReadFromT66y += 1;
                                var T66yData = T66yDB.FindById(item["Uri"]);
                                if (T66yData.img != null && T66yData.img.Length > 1024)
                                {
                                    var WriteSendDate = new ImgData()
                                    {
                                        Date = T66yData.Date,
                                        FromList = T66yData.FromList,
                                        Hash = T66yData.Hash,
                                        id = T66yData.id,
                                        img = T66yData.img,
                                        Status = false,
                                        Type = ""
                                    };
                                    WriteSend.Add(WriteSendDate);
                                    if (WriteSend.Count > 25) break;
                                }
                                else
                                {
                                    item["Status"] = "True";
                                    Img_Data.Update(item);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var Message = $"T66y读取失败:{DateTime.Now:HH:m:s}-{ sendMessage.ReadFromT66y }-{ex}";
                            File.AppendAllLines("Error.txt", new string[] { Message });
                            WriteToMessage($"{Message}");
                        }
                        finally
                        {
                            GC.Collect();
                            T66yWebdbCheck = false;
                        }
                    }
                    else
                    {
                        await Task.Delay(1000);
                    }
                } while (true);
            });

            #endregion 数据库读取

            #region 发送数据

            _ = Task.Factory.StartNew(async () =>
            {
                do
                {
                    try
                    {
                        if (_sessions.Count != 0)
                        {
                            foreach (var session in _sessions)
                            {
                                var IpAddress = session.Value.RemoteEndPoint.Address.ToString();
                                if (_sessionsCheck[IpAddress])
                                {
                                    if (WriteSend.TryTake(out ImgData SendItem))
                                    {
                                        sendMessage.SendCount += 1;
                                        await session.Value.SendBinaryAsync(SendItem.Send());
                                        Interlocked.Increment(ref sendMessage.ClientCount[IpAddress].SendCount);
                                        Interlocked.Add(ref sendMessage.ClientCount[IpAddress].SendByte, SendItem.img.Length);
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

            _ = Task.Factory.StartNew(async () =>
            {
                do
                {
                    try
                    {
                        await Task.Delay(1000);

                        if (_sessions.Count != 0)
                        {
                            var SendData = sendMessage.Send();
                            var SendBase = Convert.ToBase64String(SendData);
                            await File.WriteAllBytesAsync($"SendMessageByte.dat", SendData);
                            SendData = null;
                            foreach (var session in _sessions)
                            {
                                await session.Value.SendTextAsync(SendBase);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                } while (true);
            });

            #endregion 发送服务器信息

            #region 保存到数据库

            _ = Task.Factory.StartNew(async () =>
            {
                var DataBase = "Image.db";
                do
                {
                    try
                    {
                        if (!WriteSave.TryTake(out ImgData item)) continue;
                        using var db = new LiteDatabase($@"Filename={DataBaseCommand.BaseUri}{DataBase};Connection=Shared;");
                        var SISDB = db.GetCollection<ImgData>("ImgData");
                        try
                        {
                            if (item.img != null)
                            {
                                if (!SISDB.Exists(x => x.id == item.id))
                                {
                                    SISDB.Upsert(item);
                                    sendMessage.SaveCount += 1;
                                }
                                else
                                {
                                    sendMessage.PassCount += 1;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            File.WriteAllLines("Error.txt", new string[] { $"保存失败{ex}|{item.id}" });
                            WriteToMessage($"{DateTime.Now:HH:m:s}保存失败{ex}");
                            File.AppendAllLines($"{DataBaseCommand.BaseUri}TempImage.txt", new string[] { Convert.ToBase64String(item.Send()) });
                        }
                        finally
                        {
                            GC.Collect();
                        }

                        if (item.Status)
                        {
                            while (SISWebdbCheck)
                            {
                                await Task.Delay(100);
                            }
                            SISWebdbCheck = true;
                            using var SISWebdb = new LiteDatabase($@"Filename={DataBaseCommand.BaseUri}SISWeb.db;Connection=Shared;");
                            try
                            {
                                var Img_Data = SISWebdb.GetCollection("ImgData");
                                var FindOne = Img_Data.FindOne(x => x["Uri"] == item.id);
                                if (FindOne != null)
                                {
                                    FindOne["Status"] = "True";
                                    Img_Data.Update(FindOne);
                                }
                            }
                            catch (Exception ex)
                            {
                                File.WriteAllLines("Error.txt", new string[] { $"修改SISWebd信息失败{ex}|{item.id}" });
                                WriteToMessage($"{DateTime.Now:HH:m:s}保存失败{ex}");
                            }
                            SISWebdb.Dispose();
                            SISWebdbCheck = false;
                        }
                        else
                        {
                            while (T66yWebdbCheck)
                            {
                                await Task.Delay(100);
                            }
                            T66yWebdbCheck = true;
                            using var T66yWebdb = new LiteDatabase($@"Filename={DataBaseCommand.BaseUri}T66yWeb.db;Connection=Shared;");
                            try
                            {
                                var Img_Data = T66yWebdb.GetCollection("ImgData");
                                var FindOne = Img_Data.FindOne(x => x["Uri"] == item.id);
                                if (FindOne != null)
                                {
                                    FindOne["Status"] = "True";
                                    Img_Data.Update(FindOne);
                                }
                            }
                            catch (Exception ex)
                            {
                                File.WriteAllLines("Error.txt", new string[] { $"修改T66yWebd信息失败{ex}|{item.id}" });
                                WriteToMessage($"{DateTime.Now:HH:m:s}保存失败{ex}");
                            }
                            T66yWebdb.Dispose();
                            T66yWebdbCheck = false;
                        }
                    }
                    catch (Exception)
                    {
                    }
                } while (true);
            });

            #endregion 保存到数据库
        }

        public override async Task OnSessionStarted(AsyncWebSocketSession session)
        {
            try
            {
                var IpAddress = session.RemoteEndPoint.Address.ToString();
                _sessions.TryAdd(session.SessionKey, session);
                _sessionsCheck.TryAdd(IpAddress, true);
                _sessionsMission.TryAdd(IpAddress, new List<ImgData>());
                if (!sendMessage.ClientCount.ContainsKey(IpAddress))
                {
                    sendMessage.ClientCount.Add(IpAddress, new CLientInfo());
                }
                Loger.Instance.ServerInfo("主机", $"Webp设备远程{session.RemoteEndPoint}连接");
            }
            catch (Exception ex)
            {
                WriteToMessage($"{DateTime.Now:HH:m:s}连接失败:{ex.Message}");
            }

            await Task.CompletedTask;
        }

        public override async Task OnSessionClosed(AsyncWebSocketSession session)
        {
            try
            {
                var IpAddress = session.RemoteEndPoint.Address.ToString();
                Loger.Instance.ServerInfo("主机", $"Webp设备远程{session.RemoteEndPoint}断开");
                _sessions.TryRemove(session.SessionKey, out AsyncWebSocketSession throwAway);
                _sessionsCheck.TryRemove(IpAddress, out bool check);
                _sessionsMission.TryRemove(IpAddress, out List<ImgData> value);
                sendMessage.ClientCount.Remove(IpAddress);
            }
            catch (Exception ex)
            {
                WriteToMessage($"{DateTime.Now:HH:m:s}关闭失败:{ex.Message}");
            }

            await Task.CompletedTask;
        }

        public override async Task OnSessionTextReceived(AsyncWebSocketSession session, string text)
        {
            try
            {
                var IpAddress = session.RemoteEndPoint.Address.ToString();
                _sessionsCheck[IpAddress] = text == "true";
                // WriteToMessage($"设置{session.RemoteEndPoint}为{_sessionsCheck[IpAddress]}");
            }
            catch (Exception ex)
            {
                WriteToMessage($"{DateTime.Now:HH:m:s}接收文本失败:{ex.Message}");
            }

            await Task.CompletedTask;
        }

        public override async Task OnSessionBinaryReceived(AsyncWebSocketSession session, byte[] data, int offset, int count)
        {
            try
            {
                var GetData = ImgData.ToClass(data);
                if (GetData.img != null && GetData.img.Length > 1000)
                {
                    var IpAddress = session.RemoteEndPoint.Address.ToString();
                    Interlocked.Increment(ref sendMessage.ClientCount[IpAddress].GetCount);
                    Interlocked.Add(ref sendMessage.ClientCount[IpAddress].GetByte, GetData.img.Length);
                    WriteSave.Add(GetData);
                    if (_sessionsMission.ContainsKey(IpAddress))
                    {
                        var Find = _sessionsMission[IpAddress].FindIndex(x => x.id == GetData.id);
                        if (Find != -1)
                        {
                            _sessionsMission[IpAddress].RemoveAt(Find);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToMessage($"{DateTime.Now:HH:m:s}接收数据失败:{ex.Message}");
            }

            await Task.CompletedTask;
        }
    }
}