using Client.Properties;
using Cowboy.WebSockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    internal class server : IDisposable
    {
        internal AsyncWebSocketClient _client;

        //internal AsyncWebSocketClient _setclient;

        internal server(string IP, string Point)
        {
            Task.Factory.StartNew(async () =>
            {
                var Online = new Uri($"ws://{IP}:{Point}/Online");
                _client = new AsyncWebSocketClient(Online, new ServerDateOperation());
                await _client.Connect();
            });
        }

        internal Task Connect3Set()
        {
            Task.Factory.StartNew(async () =>
            {
                var uri = new Uri($"ws://{Settings.Default.ip}:{Settings.Default.point}/Data");
                var client = new AsyncWebSocketClient(uri, new ServerDataBaseOperation());
                await client.Connect();
                await client.SendTextAsync("GetNullStory");
            });
            return Task.CompletedTask;
        }

        internal Task Connect2SetAsync(string v)
        {
            ThreadPool.QueueUserWorkItem(async (object state) =>
            {
                var seturi = new Uri($"ws://{Settings.Default.ip}:{Settings.Default.point}/set");
                using var _setclient = new AsyncWebSocketClient(seturi, null, null, null, null, null);
                //_setclient = new AsyncWebSocketClient(seturi, new ServerDataBaseOperation());
                await _setclient.Connect();
                await _setclient.SendTextAsync(v);
            });
            return Task.CompletedTask;
        }

        public async void Dispose()
        {
            await _client.Close(WebSocketCloseCode.NormalClosure);
            _client.Dispose();
        }
    }

    [Serializable]
    internal class MiMiAiStory
    {
        public int id { get; set; }
        public string Uri { get; set; }
        public string Title { get; set; }
        public string Story { get; set; }
        public byte[] Data { get; set; }

        public byte[] ToByte()
        {
            using var stream = new MemoryStream();
            IFormatter Fileformatter = new BinaryFormatter();
            Fileformatter.Serialize(stream, this);
            return stream.ToArray();
        }

        public static MiMiAiStory ToClass(byte[] data)
        {
            using var stream = new MemoryStream(data);
            IFormatter Fileformatter = new BinaryFormatter();
            Fileformatter.Binder = new UBinder();
            return Fileformatter.Deserialize(stream) as MiMiAiStory;
        }

        public class UBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.EndsWith("MiMiAiStory"))
                {
                    return typeof(MiMiAiStory);
                }
                return (Assembly.GetExecutingAssembly()).GetType(typeName);
            }
        }
    }

    internal class ServerDataBaseOperation : IAsyncWebSocketClientMessageDispatcher
    {
        public async Task OnServerBinaryReceived(AsyncWebSocketClient client, byte[] data, int offset, int count)
        {
            //File.WriteAllBytes("jav.db", data);
            var New = MiMiAiStory.ToClass(data);
            await client.SendTextAsync("SetMiMiAiStory");
            var _HtmlDoc = new HtmlAgilityPack.HtmlDocument();
            var Text = Encoding.UTF8.GetString(New.Data);
            _HtmlDoc.LoadHtml(Text);
            Text = Text.Replace("<br>", "");
            Text = Text.Replace("&nbsp;", "");
            Text = Text.Replace("&quot;", "\"");
            Text = Text.Replace("quot;", "\"");

            Text = Text.Replace("\r\n\r\n", "\r\n");
            Text = Text.Replace("\r\n", Environment.NewLine);
            File.WriteAllText(@"C:\Users\cdj68\Desktop\无标题2.txt", Text);
            //await client.SendBinaryAsync(New.ToByte());
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

    [Serializable]
    public class OnlineOpera
    {
        internal string NyaaAddress;
        internal string JavAddress;
        internal string MiMiAiAddress;
        internal string ssr_url;
        internal string ssr4Nyaa;
        internal string MiMiInterval;
        internal string JavInterval;
        internal string NyaaInterval;
        internal string MiMiStoryInterval;
        internal string Memory;
        internal TimeSpan MiMiSpan;
        internal TimeSpan JavSpan;
        internal TimeSpan NyaaSpan;
        internal TimeSpan MiMiStorySpan;

        internal int ConnectPoint;
        internal int SSRPoint;
        internal int SocksPoint;
        internal int NyaaSSRPoint;
        internal int NyaaSocksPoint;
        internal long TotalUploadBytes;
        internal long TotalDownloadBytes;
        internal List<string> LocalInfo;
        internal List<string> RemoteInfo;
        internal bool SocksCheck;
        internal bool NyaaSocksCheck;

        internal bool OnlyList = true;
        internal bool AutoRun;

        public static void Open(byte[] data)
        {
            var stream = new MemoryStream(data);
            IFormatter Formatter = new BinaryFormatter();
            Formatter.Binder = new UBinder();
            Class1.OnlineOpera = Formatter.Deserialize(stream) as OnlineOpera;
        }

        public class UBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.EndsWith("OnlineOpera"))
                {
                    return typeof(OnlineOpera);
                }
                return (Assembly.GetExecutingAssembly()).GetType(typeName);
            }
        }
    }

    public class ServerDateOperation : IAsyncWebSocketClientMessageDispatcher
    {
        public Task OnServerBinaryReceived(AsyncWebSocketClient client, byte[] data, int offset, int count)
        {
            //Class1.MainForm.Init(new GlobalSet().Open(data));
            OnlineOpera.Open(data);
            Class1.MainForm.Invoke(new MethodInvoker(() =>
            {
                Class1.MainForm.UpdateUI();
            }));
            return Task.CompletedTask;
        }

        public Task OnServerConnected(AsyncWebSocketClient client)
        {
            ThreadPool.QueueUserWorkItem(async (object state) =>
            {
                while (client.State != WebSocketState.Closed)
                {
                    Thread.Sleep(1000);
                    if (client.State == WebSocketState.Open)
                        await client.SendTextAsync("GetStatus");
                }
            });
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
}