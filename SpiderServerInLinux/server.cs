using Cowboy.WebSockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace SpiderServerInLinux
{
    internal class server
    {
        private OnlineCheck _OnlineCheck;
        private SetOpera _SetOpera;

        internal server()
        {
            Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var ModuleCatalog = new AsyncWebSocketServerModuleCatalog();

                        ModuleCatalog.RegisterModule(new OnlineCheck());
                        ModuleCatalog.RegisterModule(new SetOpera());
                        ModuleCatalog.RegisterModule(new DataOpera());
                        var _server = new AsyncWebSocketServer(Setting._GlobalSet.ConnectPoint, ModuleCatalog);
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
            public OnlineCheck() : base(@"")
            {
            }

            public override async Task OnSessionStarted(AsyncWebSocketSession session)
            {
                Loger.Instance.ServerInfo("主机", $"远程{session.RemoteEndPoint}连接");
                await Task.CompletedTask;
            }

            public override async Task OnSessionClosed(AsyncWebSocketSession session)
            {
                Loger.Instance.ServerInfo("主机", $"远程{session.RemoteEndPoint}断开");
                await Task.CompletedTask;
            }

            public override Task OnSessionBinaryReceived(AsyncWebSocketSession session, byte[] data, int offset, int count)
            {
                Loger.Instance.ServerInfo($"{session.RemoteEndPoint}", $"接收到远程配置，正在更改本地设置");
                Setting._GlobalSet.Open(data);
                return Task.CompletedTask;
            }
        }

        private static List<AsyncWebSocketSession> SetSession = new List<AsyncWebSocketSession>();

        public class SetOpera : AsyncWebSocketServerModule
        {
            public SetOpera() : base(@"/set")
            {
            }

            public override async Task OnSessionStarted(AsyncWebSocketSession session)
            {
                Loger.Instance.ServerInfo("主机", $"远程{session.RemoteEndPoint}连接到设置");
                SetSession.Add(session);
                await Task.CompletedTask;
            }

            public override async Task OnSessionClosed(AsyncWebSocketSession session)
            {
                Loger.Instance.ServerInfo("主机", $"远程{session.RemoteEndPoint}断开设置");
                SetSession.Remove(session);
                await Task.CompletedTask;
            }
        }

        public class DataOpera : AsyncWebSocketServerModule
        {
            public DataOpera() : base(@"/Data")
            {
            }

            public override async Task OnSessionTextReceived(AsyncWebSocketSession session, string text)
            {
                Loger.Instance.ServerInfo("主机", $"远程{session.RemoteEndPoint}命令{text}");
                if (text == "Get")
                {
                    await session.SendBinaryAsync(File.ReadAllBytes("Jav.db"));
                }
                await Task.CompletedTask;
            }
        }
    }
}