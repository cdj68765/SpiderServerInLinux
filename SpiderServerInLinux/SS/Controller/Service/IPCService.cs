using System;
using System.IO.Pipes;
using System.Net;
using System.Text;

namespace Shadowsocks.Controller
{
    internal class RequestAddUrlEventArgs : EventArgs
    {
        public readonly string Url;

        public RequestAddUrlEventArgs(string url)
        {
            this.Url = url;
        }
    }
}