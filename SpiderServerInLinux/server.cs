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
                        _server = new AsyncWebSocketServer(Setting._GlobalSet.ConnectPoint, ModuleCatalog);
                        _server.Listen();
                        Loger.Instance.ServerInfo("主机", $"服务器监听启动，端口{Setting._GlobalSet.ConnectPoint}");
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
                await session.SendBinaryAsync(OnlineOpera.Send(true));

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
                switch (text)
                {
                    case "Restart":
                        {
                            Loger.Instance.ServerInfo("主机", $"接收到远程{session.RemoteEndPoint}命令开始全部下载");
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

                    default:
                        break;
                }
                await Task.CompletedTask;
            }
        }

        public class DataOpera : AsyncWebSocketServerModule
        {
            private string Code = "";

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
                            DataBaseCommand.GetDataFromMiMi("GetNullStory", session, SearchText[1]);
                        }
                        break;

                    case "GetStory":
                        {
                            DataBaseCommand.GetDataFromMiMi("GetStory", session, SearchText[1]);
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