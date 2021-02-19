using Cowboy.WebSockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using static SpiderServerInLinux.OnlineOpera;
using xNet;
using System.Net.Http;
using SocksSharp;
using SocksSharp.Proxy;
using System.Drawing;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using System.Net;
using System.Collections.Concurrent;

namespace SpiderServerInLinux
{
    internal class server
    {
        internal AsyncWebSocketServerModuleCatalog ModuleCatalog;
        private OnlineCheck _OnlineCheck;
        private SetOpera _SetOpera;
        internal AsyncWebSocketServer _server;

        internal server()
        {
            Task.Factory.StartNew(() =>
                {
                    try
                    {
                        ModuleCatalog = new AsyncWebSocketServerModuleCatalog();
                        ModuleCatalog.RegisterModule(new OnlineCheck());
                        ModuleCatalog.RegisterModule(new SetOpera());
                        ModuleCatalog.RegisterModule(new DataOpera());
                        // _server = new
                        // AsyncWebSocketServer(CheckIfPortInUse(Setting._GlobalSet.ConnectPoint), ModuleCatalog);
                        _server = new AsyncWebSocketServer(Setting._GlobalSet.ConnectPoint, ModuleCatalog);
                        int CheckIfPortInUse(int port)
                        {
                            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                            foreach (IPEndPoint endPoint in ipProperties.GetActiveTcpListeners())
                            {
                                if (endPoint.Port == port)
                                {
                                    return port;
                                }
                            }
                            return CheckIfPortInUse(port += 1);
                        }
                        _server.Listen();
                        Loger.Instance.ServerInfo("主机", $"服务器监听启动，端口{_server.ListenedEndPoint.Port}");
                    }
                    catch (Exception e)
                    {
                        Loger.Instance.ServerInfo("主机", e);
                    }
                });
        }

        public class OnlineCheck : AsyncWebSocketServerModule
        {
            public OnlineCheck() : base(@"/Online")
            {
            }

            public override async Task OnSessionStarted(AsyncWebSocketSession session)
            {
                _sessions.TryAdd(session.SessionKey, session);
                Loger.Instance.ServerInfo("主机", $"远程{session.RemoteEndPoint}连接");
                var SendData = OnlineOpera.Send(true);
                await session.SendBinaryAsync(SendData);
                SendData = null;

                //await session.SendBinaryAsync(Setting._GlobalSet.Send());
                await Task.CompletedTask;
            }

            public override async Task OnSessionClosed(AsyncWebSocketSession session)
            {
                AsyncWebSocketSession throwAway; _sessions.TryRemove(session.SessionKey, out throwAway);
                Loger.Instance.ServerInfo("主机", $"远程{session.RemoteEndPoint}断开");
                await Task.CompletedTask;
            }

            public override async Task OnSessionBinaryReceived(AsyncWebSocketSession session, byte[] data, int offset, int count)
            {
                Loger.Instance.ServerInfo($"主机", $"接收到远程{session.RemoteEndPoint}发送的配置，正在更改本地设置");
                using (Stream stream = new MemoryStream(data))
                {
                    IFormatter Formatter = new BinaryFormatter();
                    Formatter.Binder = new UBinder();
                    var OnlineOpera = Formatter.Deserialize(stream) as OnlineOpera;
                    await ChangeSet(OnlineOpera);
                    Loger.Instance.ServerInfo($"主机", $"配置更改完毕");

                    if (OnlineOpera.ConnectPoint != Setting._GlobalSet.ConnectPoint)
                    {
                        Setting._GlobalSet.ConnectPoint = OnlineOpera.ConnectPoint;
                        await this.Broadcast(OnlineOpera.Send(true));
                        Loger.Instance.ServerInfo("主机", "检测到端口改变，正在更改主机连接");
                        Setting.server._server.Shutdown();
                        Setting.server = null;
                        GC.Collect();
                        Setting.server = new server();
                        Loger.Instance.ServerInfo("主机", "端口改变完毕");
                    }
                    await this.Broadcast(Send(true));
                }
                await Task.CompletedTask;
            }

            public override async Task OnSessionTextReceived(AsyncWebSocketSession session, string text)
            {
                if (text == "GetStatus")
                {
                    await session.SendBinaryAsync(OnlineOpera.Send());
                }
                await Task.CompletedTask;
            }
        }

        public class SetOpera : AsyncWebSocketServerModule
        {
            public SetOpera() : base(@"/set")
            {
            }

            /* public override async Task OnSessionStarted(AsyncWebSocketSession session)
             {
                 Loger.Instance.ServerInfo("主机", $"远程{session.RemoteEndPoint}连接到设置");
                 await Task.CompletedTask;
             }

             public override async Task OnSessionClosed(AsyncWebSocketSession session)
             {
                 Loger.Instance.ServerInfo("主机", $"远程{session.RemoteEndPoint}断开设置");
                 await Task.CompletedTask;
             }*/

            public override async Task OnSessionTextReceived(AsyncWebSocketSession session, string text)
            {
                Loger.Instance.ServerInfo("主机", $"接收到远程{text}命令");

                switch (text)
                {
                    case "Restart":
                        {
                            if (Setting.DownloadManage == null)
                            {
                                Setting.DownloadManage = new DownloadManage();
                            }
                            else if (Setting.DownloadManage.GetJavNewDataTimer == null)
                            {
                                Setting.DownloadManage.Load();
                            }
                            else
                            {
                                Setting.DownloadManage.GetJavNewDataTimer.Interval = 1000;
                                Setting.DownloadManage.GetMiMiNewDataTimer.Interval = 1000;
                                Setting.DownloadManage.GetNyaaNewDataTimer.Interval = 1000;
                                Setting.DownloadManage.GetMiMiAiStoryDataTimer.Interval = 1000;
                                Setting.DownloadManage.GetT66yDataTimer.Interval = 10000;
                            }
                        }
                        break;

                    case "CloseMiMiStory":
                        {
                            if (Setting.DownloadManage != null)
                            {
                                Setting.DownloadManage.MiMiAiStoryDownloadCancel.Cancel();
                            }
                        }
                        break;

                    case "StartT66y":
                        {
                            try
                            {
                                if (Setting.DownloadManage == null)
                                {
                                    Setting.DownloadManage = new DownloadManage(false);
                                }
                                if (Setting.DownloadManage.GetT66yDataTimer == null)
                                    Setting.DownloadManage.GetT66yData();
                            }
                            catch (Exception)
                            {
                            }

                            try
                            {
                                if (Setting.DownloadManage.GetT66yDataTimer != null)
                                    Setting.DownloadManage.GetT66yDataTimer.Interval = 10000;
                            }
                            catch (Exception)
                            {
                            }
                        }
                        break;

                    case "StartSIS":
                        {
                            try
                            {
                                if (Setting.DownloadManage == null)
                                {
                                    Setting.DownloadManage = new DownloadManage(false);
                                }
                                if (Setting.DownloadManage.GetSISDataTimer == null)
                                    Setting.DownloadManage.GetSis001Data();
                            }
                            catch (Exception)
                            {
                            }

                            try
                            {
                                if (Setting.DownloadManage.GetSISDataTimer != null)
                                    Setting.DownloadManage.GetSISDataTimer.Interval = 10000;
                            }
                            catch (Exception)
                            {
                            }
                        }
                        break;

                    case "StartJav":
                        {
                            try
                            {
                                if (Setting.DownloadManage == null)
                                {
                                    Setting.DownloadManage = new DownloadManage(false);
                                }
                                if (Setting.DownloadManage.GetJavNewDataTimer == null)
                                    Setting.DownloadManage.GetJavNewData();
                            }
                            catch (Exception)
                            {
                            }

                            try
                            {
                                Setting.DownloadManage.GetJavNewDataTimer.Interval = 10000;
                            }
                            catch (Exception)
                            {
                            }
                        }
                        break;

                    case "CloseT66y":
                        {
                            if (Setting.DownloadManage != null)
                            {
                                Setting.DownloadManage.T66yDownloadCancel.Cancel();
                            }
                        }
                        break;

                    case "CloseDownload":
                        {
                            if (Setting.DownloadManage != null)
                            {
                                Setting.DownloadManage.Dispose();
                                Setting.DownloadManage = null;
                                GC.Collect();
                            }
                        }
                        break;

                    case "CloseNyaa":
                        {
                            if (Setting.DownloadManage != null)
                            {
                                Setting.DownloadManage.NyaaDownloadCancel.Cancel();
                                GC.Collect();
                            }
                        }
                        break;

                    case "Close":
                        {
                            Setting.DownloadManage.Dispose();
                            Setting.DownloadManage = null;
                            GC.Collect();
                            Setting.ShutdownResetEvent.SetResult(0);
                            Environment.Exit(0);
                        }
                        break;

                    case "ReLoad":
                        {
                            Loger.Instance.ServerInfo("主机", $"正在关闭下载");
                            Setting.DownloadManage.Dispose();
                            Setting.DownloadManage = null;
                            GC.Collect();
                            Loger.Instance.ServerInfo("主机", $"下载关闭完成，正在准备重启");
                            Setting.DownloadManage = new DownloadManage(false);
                            Loger.Instance.ServerInfo("主机", $"下载进程重启完毕");
                        }
                        break;

                    default:
                        break;
                }
                await Task.CompletedTask;
            }
        }

        public class DataOpera : AsyncWebSocketServerModule
        {
            private string Code = "";
            private BlockingCollection<string> DownList = null;

            public DataOpera() : base(@"/Data")
            {
            }

            public override Task OnSessionBinaryReceived(AsyncWebSocketSession session, byte[] data, int offset, int count)
            {
                switch (Code)
                {
                    case "SetMiMiAiStory":
                        {
                            DataBaseCommand.SaveToMiMiStoryDataUnit(UnitData: MiMiAiStory.ToClass(data));
                        }
                        break;

                    default:
                        break;
                }
                return Task.CompletedTask;
            }

            private CancellationTokenSource Storycancel = new CancellationTokenSource();

            public override async Task OnSessionTextReceived(AsyncWebSocketSession session, string text)
            {
                Loger.Instance.ServerInfo("主机", $"远程{session.RemoteEndPoint}命令{text}");
                var SearchText = text.Split('|');
                switch (SearchText[0])
                {
                    case "Get":
                        await session.SendBinaryAsync(File.ReadAllBytes("Jav.db"));
                        break;

                    case "GetNullStory":
                        {
                            CancellationTokenSource Storycancel = new CancellationTokenSource();
                            DataBaseCommand.GetDataFromMiMi("GetNullStory", session, Cancel: Storycancel, SearchText[1]);
                        }
                        break;

                    case "GetStory":
                        {
                            CancellationTokenSource Storycancel = new CancellationTokenSource();
                            DataBaseCommand.GetDataFromMiMi("GetStory", session, Cancel: Storycancel, SearchText[1]);
                        }
                        break;

                    case "Stop":
                        {
                            Storycancel.Cancel();
                        }
                        break;

                    case "GetT66y":
                        {
                            CancellationTokenSource Storycancel = new CancellationTokenSource();
                            DataBaseCommand.GetDataFromT66y(SearchText[1], SearchText[2], session, Cancel: Storycancel);
                        }
                        break;

                    case "GetSIS":
                        {
                            CancellationTokenSource Storycancel = new CancellationTokenSource();
                            DataBaseCommand.GetDataFromT66y(SearchText[1], SearchText[2], session, Cancel: Storycancel);
                        }
                        break;

                    case "ReDownloadT66y":
                        {
                            CancellationTokenSource Storycancel = new CancellationTokenSource();
                            if (DownList == null)
                            {
                                if (Setting.DownloadManage == null) Setting.DownloadManage = new DownloadManage(false);
                                DownList = new BlockingCollection<string>();
                                DownList.Add(SearchText[1]);
                                ThreadPool.QueueUserWorkItem(async obj =>
                                {
                                    foreach (var item in DownList.GetConsumingEnumerable())
                                    {
                                        T66yImgData SearchImg = DataBaseCommand.GetDataFromT66y("img", item);
                                        if (SearchImg != null)
                                        {
                                            Loger.Instance.ServerInfo("主机", $"下载图片{SearchImg.id}中");
                                            var RET = Setting.DownloadManage.DownloadImgAsync(SearchImg.id).Result;
                                            if (RET != null)
                                            {
                                                SearchImg.img = RET;
                                                SearchImg.Hash = Convert.ToBase64String(new MD5CryptoServiceProvider().ComputeHash(SearchImg.img));
                                                DataBaseCommand.SaveToT66yDataUnit(UnitData: SearchImg, Update: true);
                                                Loger.Instance.ServerInfo("主机", $"下载图片{SearchImg.id}成功，发送新图片");
                                                await session.SendBinaryAsync(SearchImg.ToByte());
                                            }
                                            else
                                            {
                                                Loger.Instance.ServerInfo("主机", $"下载图片{SearchImg.id}失败，返回空");
                                            }
                                        }
                                    }
                                });
                            }
                            else
                            {
                                DownList.Add(SearchText[1]);
                            }
                        }
                        break;

                    default:
                        Code = text;
                        break;
                }
                if (text == "Get")
                {
                }
                await Task.CompletedTask;
            }
        }
    }
}