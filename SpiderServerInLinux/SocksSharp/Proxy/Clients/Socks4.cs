﻿/*
Copyright © 2012-2015 Ruslan Khuduev <x-rus@list.ru>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
 */

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SocksSharp.Helpers;

namespace SocksSharp.Proxy
{
    public class Socks4 : IProxy
    {
        public IProxySettings Settings { get; set; }

        /// <summary>
        ///     Create connection to destination host via proxy server.
        /// </summary>
        /// <param name="destinationHost">Host</param>
        /// <param name="destinationPort">Port</param>
        /// <param name="tcpClient">Connection with proxy server.</param>
        /// <returns>Connection to destination host</returns>
        /// <exception cref="System.ArgumentException">
        ///     Value of <paramref name="destinationHost" /> is <see langword="null" /> or
        ///     empty.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     Value of <paramref name="destinationPort" /> less than 1 or
        ///     greater than 65535.
        /// </exception>
        /// <exception cref="ProxyException">Error while working with proxy.</exception>
        public TcpClient CreateConnection(string destinationHost, int destinationPort, TcpClient client)
        {
            if (string.IsNullOrEmpty(destinationHost))
                throw new ArgumentException(nameof(destinationHost));

            if (!ExceptionHelper.ValidateTcpPort(destinationPort))
                throw new ArgumentOutOfRangeException(nameof(destinationPort));

            if (client == null && !client.Connected)
                throw new SocketException();

            try
            {
                SendCommand(client.GetStream(), CommandConnect, destinationHost, destinationPort);
            }
            catch (Exception ex)
            {
                client.Close();

                if (ex is IOException || ex is SocketException)
                    throw new ProxyException("Error while working with proxy", ex);

                throw;
            }

            return client;
        }

        #region Constants (protected)

        protected internal const int DefaultPort = 1080;

        protected internal const byte VersionNumber = 4;
        protected internal const byte CommandConnect = 0x01;
        protected internal const byte CommandBind = 0x02;
        protected internal const byte CommandReplyRequestGranted = 0x5a;
        protected internal const byte CommandReplyRequestRejectedOrFailed = 0x5b;
        protected internal const byte CommandReplyRequestRejectedCannotConnectToIdentd = 0x5c;
        protected internal const byte CommandReplyRequestRejectedDifferentIdentd = 0x5d;

        #endregion

        #region Methods (protected)

        protected internal virtual void SendCommand(NetworkStream nStream, byte command, string destinationHost,
            int destinationPort)
        {
            var dstPort = GetIPAddressBytes(destinationHost);
            var dstIp = GetPortBytes(destinationPort);

            var userId = new byte[0];
            if (Settings.Credentials != null)
                if (!string.IsNullOrEmpty(Settings.Credentials.UserName))
                    userId = Encoding.ASCII.GetBytes(Settings.Credentials.UserName);

            // +----+----+----+----+----+----+----+----+----+----+....+----+
            // | VN | CD | DSTPORT |      DSTIP        | USERID       |NULL|
            // +----+----+----+----+----+----+----+----+----+----+....+----+
            //    1    1      2              4           variable       1
            var request = new byte[9 + userId.Length];

            request[0] = VersionNumber;
            request[1] = command;
            dstIp.CopyTo(request, 2);
            dstPort.CopyTo(request, 4);
            userId.CopyTo(request, 8);
            request[8 + userId.Length] = 0x00;

            nStream.Write(request, 0, request.Length);

            // +----+----+----+----+----+----+----+----+
            // | VN | CD | DSTPORT |      DSTIP        |
            // +----+----+----+----+----+----+----+----+
            //   1    1       2              4
            var response = new byte[8];

            nStream.Read(response, 0, response.Length);

            var reply = response[1];

            if (reply != CommandReplyRequestGranted)
                HandleCommandError(reply);
        }

        protected internal byte[] GetIPAddressBytes(string destinationHost)
        {
            IPAddress ipAddr = null;

            if (!IPAddress.TryParse(destinationHost, out ipAddr))
                try
                {
                    var ips = Dns.GetHostAddresses(destinationHost);

                    if (ips.Length > 0)
                        ipAddr = ips[0];
                }
                catch (Exception ex)
                {
                    if (ex is SocketException || ex is ArgumentException)
                        throw new ProxyException("Failed to get host address", ex);

                    throw;
                }

            return ipAddr.GetAddressBytes();
        }

        protected internal byte[] GetPortBytes(int port)
        {
            var array = new byte[2];

            array[0] = (byte) (port / 256);
            array[1] = (byte) (port % 256);

            return array;
        }

        protected internal void HandleCommandError(byte command)
        {
            string errorMessage;

            switch (command)
            {
                case CommandReplyRequestRejectedOrFailed:
                    errorMessage = "Request rejected or failed";
                    break;

                case CommandReplyRequestRejectedCannotConnectToIdentd:
                    errorMessage = "Request rejected: cannot connect to identd";
                    break;

                case CommandReplyRequestRejectedDifferentIdentd:
                    errorMessage = "Request rejected: different identd";
                    break;

                default:
                    errorMessage = "Unknown socks error";
                    break;
            }

            throw new ProxyException(errorMessage);
        }

        #endregion
    }
}