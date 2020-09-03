using System;
using HtmlAgilityPack;
using Cowboy;
using System.Text;
using System.Threading.Tasks;
using SpiderServerInLinux;
using Cowboy.WebSockets;

namespace Server
{
    internal class Program
    {
        private static readonly TaskCompletionSource<byte> ShutdownResetEvent = new TaskCompletionSource<byte>();

        private static async Task<int> Main(string[] args)
        {
            Loger.Instance.ServerInfo("主机", $"服务器监听启动，端口{1200}");
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Loger.Instance.LocalInfo($"服务器启动");
            Setting.server = new server();

            return await ShutdownResetEvent.Task.ConfigureAwait(false);
        }
    }
}