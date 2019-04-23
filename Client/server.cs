using Client.Properties;
using Cowboy.WebSockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    [Serializable]
    internal class GlobalSet
    {
        private string _NyaaAddress;
        private string _JavAddress;
        private int _NyaaLastPageIndex;
        private int _JavLastPageIndex;
        private int _ConnectPoint;
        private int _Socks5Point;
        private bool _SocksCheck = false;
        internal string NyaaAddress { get { return _NyaaAddress; } set { _NyaaAddress = value; } }
        internal string JavAddress { get { return _JavAddress; } set { _JavAddress = value; } }
        internal int NyaaLastPageIndex { get { return _NyaaLastPageIndex; } }
        internal int JavLastPageIndex { get { return _JavLastPageIndex; } }
        internal int ConnectPoint { get { return _ConnectPoint; } set { _ConnectPoint = value; } }
        internal int Socks5Point { get { return _Socks5Point; } set { _Socks5Point = value; } }
        internal bool SocksCheck { get { return _SocksCheck; } set { _SocksCheck = value; } }

        internal GlobalSet()
        {
        }

        internal GlobalSet Open(byte[] Data)
        {
            using (Stream stream = new MemoryStream(Data))
            {
                IFormatter Formatter = new BinaryFormatter();
                Formatter.Binder = new UBinder();
                return (GlobalSet)Formatter.Deserialize(stream);
            }
        }

        internal void Save(server _server)
        {
            if (_server?._client.State == WebSocketState.Open)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    IFormatter Fileformatter = new BinaryFormatter();
                    Fileformatter.Serialize(stream, this);
                    _server._client.SendBinaryAsync(stream.ToArray());
                    Class1.MainForm.ShowStatus("配置发送完毕");
                }
            }
            else
            {
                Class1.MainForm.ShowStatus("服务器连接错误，发送失败");
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

    internal class server : IDisposable
    {
        internal AsyncWebSocketClient _client;

        internal server(string IP, string Point)
        {
            Task.Factory.StartNew(async () =>
            {
                var uri = new Uri($"ws://{IP}:{Point}/");
                _client = new AsyncWebSocketClient(uri, new ServerDateOperation());
                await _client.Connect();
            });

            /* Task.Factory.StartNew(async () =>
             {
                 try
                 {
                     var uri = new Uri($"ws://{IP}:{Point}/");
                     _client = new AsyncWebSocketClient(uri, new ServerDateOperation());
                     await _client.Connect();
                     if (_client.State == WebSocketState.Open)
                     {
                         Class1.MainForm.Connecting(true);
                         return;
                     }
                 }
                 catch (Exception ex)
                 {
                     Class1.MainForm.ShowStatus(ex.ToString());
                     Class1.MainForm.Connecting(false);
                 }
             });*/
        }

        internal void Connect3Set()
        {
            Task.Factory.StartNew(async () =>
            {
                var uri = new Uri($"ws://{Settings.Default.ip}:{Settings.Default.point}/Data");
                var client = new AsyncWebSocketClient(uri, new ServerDataBaseOperation());
                await client.Connect();
                await client.SendTextAsync("Get");
            });
        }

        internal void Connect2Set()
        {
            Task.Factory.StartNew(async () =>
            {
                var uri = new Uri($"ws://{Settings.Default.ip}:{Settings.Default.point}/set");
                var client = new AsyncWebSocketClient(uri, (c, s) =>
                {
                    Console.WriteLine();
                    return Task.CompletedTask;
                }, (c, a, b, d) =>
                {
                    Console.WriteLine();
                    return Task.CompletedTask;
                }, null, null, null);
                await client.Connect();
            });
        }

        public void Dispose()
        {
            _client.Close(WebSocketCloseCode.NormalClosure);
            _client.Dispose();
        }
    }

    internal class ServerDataBaseOperation : IAsyncWebSocketClientMessageDispatcher
    {
        public Task OnServerBinaryReceived(AsyncWebSocketClient client, byte[] data, int offset, int count)
        {
            File.WriteAllBytes("jav.db", data);
            return Task.CompletedTask;
        }

        public Task OnServerConnected(AsyncWebSocketClient client)
        {
            return Task.CompletedTask;
        }

        public Task OnServerDisconnected(AsyncWebSocketClient client)
        {
            return Task.CompletedTask;
        }

        public Task OnServerFragmentationStreamClosed(AsyncWebSocketClient client, byte[] data, int offset, int count)
        {
            return Task.CompletedTask;
        }

        public Task OnServerFragmentationStreamContinued(AsyncWebSocketClient client, byte[] data, int offset, int count)
        {
            return Task.CompletedTask;
        }

        public Task OnServerFragmentationStreamOpened(AsyncWebSocketClient client, byte[] data, int offset, int count)
        {
            return Task.CompletedTask;
        }

        public Task OnServerTextReceived(AsyncWebSocketClient client, string text)
        {
            return Task.CompletedTask;
        }
    }

    public class ServerDateOperation : IAsyncWebSocketClientMessageDispatcher
    {
        public Task OnServerBinaryReceived(AsyncWebSocketClient client, byte[] data, int offset, int count)
        {
            //Class1.MainForm.Init(new GlobalSet().Open(data));
            return Task.CompletedTask;
        }

        public Task OnServerConnected(AsyncWebSocketClient client)
        {
            return Task.CompletedTask;
        }

        public Task OnServerDisconnected(AsyncWebSocketClient client)
        {
            Class1.MainForm.Connecting(false);
            return Task.CompletedTask;
        }

        public Task OnServerFragmentationStreamClosed(AsyncWebSocketClient client, byte[] data, int offset, int count)
        {
            return Task.CompletedTask;
        }

        public Task OnServerFragmentationStreamContinued(AsyncWebSocketClient client, byte[] data, int offset, int count)
        {
            return Task.CompletedTask;
        }

        public Task OnServerFragmentationStreamOpened(AsyncWebSocketClient client, byte[] data, int offset, int count)
        {
            return Task.CompletedTask;
        }

        public Task OnServerTextReceived(AsyncWebSocketClient client, string text)
        {
            return Task.CompletedTask;
        }
    }
}