﻿using Proxy.Client.Contracts;
using Proxy.Client.Contracts.Constants;
using Proxy.Client.Exceptions;
using Proxy.Client.Utilities;
using Proxy.Client.Utilities.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Proxy.Client
{
    /// <summary>
    /// Base proxy client class containing the shared logic to be implemented in all derived proxy clients.
    /// </summary>
    public abstract class BaseProxyClient : IProxyClient
    {
        /// <summary>
        /// Host name or IP address of the proxy server.
        /// </summary>
        public string ProxyHost { get; protected set; }

        /// <summary>
        /// Port used to connect to the proxy server.
        /// </summary>
        public int ProxyPort { get; protected set; }

        /// <summary>
        /// The type of proxy.
        /// </summary>
        public ProxyType ProxyType { get; protected set; }

        /// <summary>
        /// The type of proxy.
        /// </summary>
        public ProxyScheme Scheme { get; private set; }

        /// <summary>
        /// Host name or IP address of the destination server.
        /// </summary>
        public string DestinationHost { get; private set; }

        /// <summary>
        /// Port used to connect to the destination server.
        /// </summary>
        public int DestinationPort { get; private set; }

        /// <summary>
        /// URL Query.
        /// </summary>
        public string UrlQuery { get; private set; }

        /// <summary>
        /// Socket Connect Timeout.
        /// </summary>
        public int ConnectTimeout { get; private set; }

        /// <summary>
        /// Socket Read Timeout.
        /// </summary>
        public int ReadTimeout { get; private set; }

        /// <summary>
        /// Socket Write Timeout.
        /// </summary>
        public int WriteTimeout { get; private set; }

        /// <summary>
        /// Underlying socket used to send and receive requests.
        /// </summary>
        protected internal Socket Socket { get; private set; }

        /// <summary>
        /// Stream for SSL reads and writes on the underlying socket.
        /// </summary>
        protected internal SslStream SslStream { get; private set; }

        /// <summary>
        /// Destination URI.
        /// </summary>
        protected internal Uri DestinationUri { get; private set; }

        /// <summary>
        /// Cancellation Token Source Manager.
        /// </summary>
        protected internal CancellationTokenSourceManager CancellationTokenSourceManager { get; private set; }

        /// <summary>
        /// Indicates whether the destination server explicitly closes the underlying connection.
        /// </summary>
        protected internal bool IsConnectionClosed { get; private set; }

        /// <summary>
        /// Indicates whether the request has thrown an exception or not.
        /// </summary>
        protected internal bool IsFaulted { get; internal set; }

        /// <summary>
        /// Indicates whether the underlying Socket and dependencies are disposed.
        /// </summary>
        protected internal bool IsDisposed { get; private set; }

        /// <summary>
        /// Connects to the proxy client, sends the GET command to the destination server and
        /// returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the GET command.</param>
        /// <param name="cookies">Cookies to be sent with the GET command.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        public abstract ProxyResponse Get(string url, bool isKeepAlive = true, IEnumerable<ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
            int readTimeout = 10000, int writeTimeout = 10000);

        /// <summary>
        /// Asynchronously connects to the proxy client, sends the GET command to the destination
        /// server and returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the GET command.</param>
        /// <param name="cookies">Cookies to be sent with the GET command.</param>
        /// <param name="totalTimeout">Total Request Timeout in ms.</param>
        /// <param name="connectTimeout">Connect Timeout in ms.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        public abstract Task<ProxyResponse> GetAsync(string url, bool isKeepAlive = true, IEnumerable<ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
            int totalTimeout = 60000, int connectTimeout = 45000, int readTimeout = 10000, int writeTimeout = 10000);

        /// <summary>
        /// Connects to the proxy client, sends the POST command to the destination server and
        /// returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="body">Body to be sent with the POST command.</param>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the POST command.</param>
        /// <param name="cookies">Cookies to be sent with the POST command.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        public abstract ProxyResponse Post(string url, string body, bool isKeepAlive = true, IEnumerable<ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
            int readTimeout = 10000, int writeTimeout = 10000);

        /// <summary>
        /// Asynchronously connects to the proxy client, sends the POST command to the destination
        /// server and returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="body">Body to be sent with the POST request.</param>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the POST command.</param>
        /// <param name="cookies">Cookies to be sent with the POST command.</param>
        /// <param name="totalTimeout">Total Request Timeout in ms.</param>
        /// <param name="connectTimeout">Connect Timeout in ms.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        public abstract Task<ProxyResponse> PostAsync(string url, string body, bool isKeepAlive = true, IEnumerable<ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
            int totalTimeout = 60000, int connectTimeout = 45000, int readTimeout = 10000, int writeTimeout = 10000);

        /// <summary>
        /// Connects to the proxy client, sends the PUT command to the destination server and
        /// returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="body">Body to be sent with the PUT command.</param>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the PUT command.</param>
        /// <param name="cookies">Cookies to be sent with the PUT command.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        public abstract ProxyResponse Put(string url, string body, bool isKeepAlive = true, IEnumerable<ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
            int readTimeout = 10000, int writeTimeout = 10000);

        /// <summary>
        /// Asynchronously connects to the proxy client, sends the PUT command to the destination
        /// server and returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="body">Body to be sent with the PUT request.</param>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the PUT command.</param>
        /// <param name="cookies">Cookies to be sent with the PUT command.</param>
        /// <param name="totalTimeout">Total Request Timeout in ms.</param>
        /// <param name="connectTimeout">Connect Timeout in ms.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        public abstract Task<ProxyResponse> PutAsync(string url, string body, bool isKeepAlive = true, IEnumerable<ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
             int totalTimeout = 60000, int connectTimeout = 45000, int readTimeout = 10000, int writeTimeout = 10000);

        /// <summary>
        /// Connects to the proxy client, sends the DELETE command to the destination server and
        /// returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the DELETE command.</param>
        /// <param name="cookies">Cookies to be sent with the DELETE command.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        public abstract ProxyResponse Delete(string url, bool isKeepAlive = true, IEnumerable<ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
            int readTimeout = 10000, int writeTimeout = 10000);

        /// <summary>
        /// Asynchronously connects to the proxy client, sends the DELETE command to the destination
        /// server and returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the DELETE command.</param>
        /// <param name="cookies">Cookies to be sent with the DELETE command.</param>
        /// <param name="totalTimeout">Total Request Timeout in ms.</param>
        /// <param name="connectTimeout">Connect Timeout in ms.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        public abstract Task<ProxyResponse> DeleteAsync(string url, bool isKeepAlive = true, IEnumerable<ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
            int totalTimeout = 60000, int connectTimeout = 45000, int readTimeout = 10000, int writeTimeout = 10000);

        /// <summary>
        /// Disposes the socket dependencies.
        /// </summary>
        public virtual void Dispose()
        {
            Socket?.Close();
            SslStream?.Close();
            CancellationTokenSourceManager?.Dispose();
            IsDisposed = true;
        }

        /// <summary>
        /// Connects to the Destination Server.
        /// </summary>
        protected internal abstract void SendConnectCommand();

        /// <summary>
        /// Asynchronously connects to the Destination Server.
        /// </summary>
        protected internal abstract Task SendConnectCommandAsync();

        /// <summary>
        /// Handles the given request based on if the proxy client is connected or not.
        /// </summary>
        /// <param name="connectNegotiationFn">
        /// Performs connection negotations with the destination server.
        /// </param>
        /// <param name="requestFn">Sends the request on the underlying socket.</param>
        /// <param name="url">Destination Url.</param>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        protected internal ProxyResponse HandleRequest(Action connectNegotiationFn,
            Func<(ProxyResponse response, float firstByteTime)> requestFn, string url, bool isKeepAlive,
            int readTimeout, int writeTimeout)
        {
            try
            {
                var (cachedDestinationHost, cachedScheme) = ParseAndReturnCachedItems(url);
                float connectTime = 0;

                ReadTimeout = readTimeout > 0 ? readTimeout : 1;
                WriteTimeout = writeTimeout > 0 ? writeTimeout : 1;

                if (IsCreateSocket())
                {
                    connectTime = CreateSocket();
                }
                else if (IsDisposeAndCreateSocket(cachedDestinationHost, cachedScheme, isKeepAlive))
                {
                    Dispose();
                    connectTime = CreateSocket();
                }

                var (requestTime, innerResult) = TimingHelper.Measure(() =>
                {
                    return requestFn();
                });

                innerResult.response.Timings = Timings.Create(connectTime, connectTime + requestTime, connectTime + innerResult.firstByteTime);

                CheckConnectionClosed(innerResult.response.Headers);

                return innerResult.response;
            }
            catch (SocketException socketEx)
            {
                IsFaulted = true;
                //Console.WriteLine($"{socketEx.StackTrace}");

                if (socketEx.SocketErrorCode == SocketError.TimedOut)
                    throw new TimeoutException($"Proxy host {ProxyHost} on port {ProxyPort} timed out.", socketEx);

                throw new ProxyException(String.Format(CultureInfo.InvariantCulture,
                   $"Connection to proxy host {ProxyHost} on port {ProxyPort} failed."), socketEx);
            }
            catch (Exception genericEx)
            {
                // Console.WriteLine($"{genericEx.StackTrace}");

                IsFaulted = true;
                throw new ProxyException(String.Format(CultureInfo.InvariantCulture,
                    $"Connection to proxy host {ProxyHost} on port {ProxyPort} failed."), genericEx);
            }

            float CreateSocket()
            {
                return TimingHelper.Measure(() =>
                {
                    IsDisposed = false;
                    Socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
                    {
                        ReceiveTimeout = ReadTimeout,
                        SendTimeout = WriteTimeout
                    };

                    Socket.Connect(ProxyHost, ProxyPort);
                    connectNegotiationFn();
                });
            }
        }

        /// <summary>
        /// Asynchronously handles the given request based on if the proxy client is connected or not.
        /// </summary>
        /// <param name="connectNegotiationFn">
        /// Performs connection negotations with the destination server.
        /// </param>
        /// <param name="requestedFn">Sends the request on the underlying socket.</param>
        /// <param name="url">Destination Url.</param>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="connectTimeout">Connect Timeout in ms.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        protected internal async Task<ProxyResponse> HandleRequestAsync(Func<Task> connectNegotiationFn,
            Func<Task<(ProxyResponse response, float firstByteTime)>> requestedFn, string url, bool isKeepAlive,
            int connectTimeout, int readTimeout, int writeTimeout)
        {
            try
            {
                var (cachedDestinationHost, cachedScheme) = ParseAndReturnCachedItems(url);

                ConnectTimeout = connectTimeout;
                ReadTimeout = readTimeout;
                WriteTimeout = writeTimeout;

                float connectTime = 0;

                if (IsCreateSocket())
                {
                    connectTime = await CreateSocketAsync();
                }
                else if (IsDisposeAndCreateSocket(cachedDestinationHost, cachedScheme, isKeepAlive))
                {
                    Dispose();
                    connectTime = await CreateSocketAsync();
                }

                var (requestTime, innerResult) = await TimingHelper.MeasureAsync(() =>
                {
                    return requestedFn();
                });

                innerResult.response.Timings = Timings.Create(connectTime, connectTime + requestTime, connectTime + innerResult.firstByteTime);

                CheckConnectionClosed(innerResult.response.Headers);

                return innerResult.response;
            }
            catch (OperationCanceledException cancelledEx)
            {
                IsFaulted = true;
                throw new TimeoutException($"Proxy host {ProxyHost} on port {ProxyPort} timed out.", cancelledEx);
            }
            catch (Exception genericEx)
            {
                //Console.WriteLine($"{genericEx.StackTrace}");

                IsFaulted = true;
                throw new ProxyException(String.Format(CultureInfo.InvariantCulture,
                    $"Connection to proxy host {ProxyHost} on port {ProxyPort} failed."), genericEx);
            }

            Task<float> CreateSocketAsync()
            {
                return TimingHelper.MeasureAsync(async () =>
                {
                    IsDisposed = false;
                    IsFaulted = false;

                    Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    CancellationTokenSourceManager = new CancellationTokenSourceManager();

                    await Socket.ConnectAsync(ProxyHost, ProxyPort, ConnectTimeout, CancellationTokenSourceManager);
                    await connectNegotiationFn();
                });
            }
        }

        /// <summary>
        /// Sends the GET command to the destination server, and creates the proxy response.
        /// </summary>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the GET command.</param>
        /// <param name="cookies">Cookies to be sent with the GET command.</param>
        /// <returns>Proxy Response with the time to first byte</returns>
        protected internal (ProxyResponse response, float firstByteTime) SendGetCommand(bool isKeepAlive, IEnumerable<ProxyHeader> headers, IEnumerable<Cookie> cookies)
        {
            var writeBuffer = CommandHelper.GetCommand(DestinationUri.AbsoluteUri, DestinationUri.Authority, isKeepAlive, headers, cookies);
            return HandleRequestCommand(writeBuffer);
        }

        /// <summary>
        /// Asynchronously sends the GET command to the destination server, and creates the proxy response.
        /// </summary>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the GET command.</param>
        /// <param name="cookies">Cookies to be sent with the GET command.</param>
        /// <returns>Proxy Response with the time to first byte</returns>
        protected internal Task<(ProxyResponse response, float firstByteTime)> SendGetCommandAsync(bool isKeepAlive, IEnumerable<ProxyHeader> headers, IEnumerable<Cookie> cookies)
        {
            var writeBuffer = CommandHelper.GetCommand(DestinationUri.AbsoluteUri, DestinationUri.Authority, isKeepAlive, headers, cookies);
            return HandleRequestCommandAsync(writeBuffer);
        }

        /// <summary>
        /// Sends the POST command to the destination server, and creates the proxy response.
        /// </summary>
        /// <param name="body">Body to be sent with the POST command.</param>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the POST command.</param>
        /// <param name="cookies">Cookies to be sent with the POST command.</param>
        /// <returns>Proxy Response with the time to first byte</returns>
        protected internal (ProxyResponse response, float firstByteTime) SendPostCommand(string body, bool isKeepAlive, IEnumerable<ProxyHeader> headers, IEnumerable<Cookie> cookies)
        {
            var writeBuffer = CommandHelper.PostCommand(DestinationUri.AbsoluteUri, DestinationUri.Authority, body, isKeepAlive, headers, cookies);
            return HandleRequestCommand(writeBuffer);
        }

        /// <summary>
        /// Asynchronously sends the POST command to the destination server, and creates the proxy response.
        /// </summary>
        /// <param name="body">Body to be sent with the POST command.</param>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the POST command.</param>
        /// <param name="cookies">Cookies to be sent with the POST command.</param>
        /// <returns>Proxy Response with the time to first byte</returns>
        protected internal Task<(ProxyResponse response, float firstByteTime)> SendPostCommandAsync(string body, bool isKeepAlive, IEnumerable<ProxyHeader> headers, IEnumerable<Cookie> cookies)
        {
            var writeBuffer = CommandHelper.PostCommand(DestinationUri.AbsoluteUri, DestinationUri.Authority, body, isKeepAlive, headers, cookies);
            return HandleRequestCommandAsync(writeBuffer);
        }

        /// <summary>
        /// Sends the PUT command to the destination server, and creates the proxy response.
        /// </summary>
        /// <param name="body">Body to be sent with the PUT command.</param>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the PUT command.</param>
        /// <param name="cookies">Cookies to be sent with the PUT command.</param>
        /// <returns>Proxy Response with the time to first byte</returns>
        protected internal (ProxyResponse response, float firstByteTime) SendPutCommand(string body, bool isKeepAlive, IEnumerable<ProxyHeader> headers, IEnumerable<Cookie> cookies)
        {
            var writeBuffer = CommandHelper.PutCommand(DestinationUri.AbsoluteUri, DestinationUri.Authority, body, isKeepAlive, headers, cookies);
            return HandleRequestCommand(writeBuffer);
        }

        /// <summary>
        /// Asynchronously sends the PUT command to the destination server, and creates the proxy response.
        /// </summary>
        /// <param name="body">Body to be sent with the PUT command.</param>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the PUT command.</param>
        /// <param name="cookies">Cookies to be sent with the PUT command.</param>
        /// <returns>Proxy Response with the time to first byte</returns>
        protected internal Task<(ProxyResponse response, float firstByteTime)> SendPutCommandAsync(string body, bool isKeepAlive, IEnumerable<ProxyHeader> headers, IEnumerable<Cookie> cookies)
        {
            var writeBuffer = CommandHelper.PutCommand(DestinationUri.AbsoluteUri, DestinationUri.Authority, body, isKeepAlive, headers, cookies);
            return HandleRequestCommandAsync(writeBuffer);
        }

        /// <summary>
        /// Sends the DELETE command to the destination server, and creates the proxy response.
        /// </summary>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the DELETE command.</param>
        /// <param name="cookies">Cookies to be sent with the DELETE command.</param>
        /// <returns>Proxy Response with the time to first byte</returns>
        protected internal (ProxyResponse response, float firstByteTime) SendDeleteCommand(bool isKeepAlive, IEnumerable<ProxyHeader> headers, IEnumerable<Cookie> cookies)
        {
            var writeBuffer = CommandHelper.DeleteCommand(DestinationUri.AbsoluteUri, DestinationUri.Authority, isKeepAlive, headers, cookies);
            return HandleRequestCommand(writeBuffer);
        }

        /// <summary>
        /// Asynchronously sends the DELETE command to the destination server, and creates the proxy response.
        /// </summary>
        /// <param name="isKeepAlive">
        /// Indicates whether the connetion is to be disposed or kept alive.
        /// </param>
        /// <param name="headers">Headers to be sent with the DELETE command.</param>
        /// <param name="cookies">Cookies to be sent with the DELETE command.</param>
        /// <returns>Proxy Response with the time to first byte</returns>
        protected internal Task<(ProxyResponse response, float firstByteTime)> SendDeleteCommandAsync(bool isKeepAlive, IEnumerable<ProxyHeader> headers, IEnumerable<Cookie> cookies)
        {
            var writeBuffer = CommandHelper.DeleteCommand(DestinationUri.AbsoluteUri, DestinationUri.Authority, isKeepAlive, headers, cookies);
            return HandleRequestCommandAsync(writeBuffer);
        }

        /// <summary>
        /// Performs the SSL Handshake with the Destination Server.
        /// </summary>
        protected internal void HandleSslHandshake()
        {
            var networkStream = new NetworkStream(Socket);
            SslStream = new SslStream(networkStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);

            SslStream.AuthenticateAsClient(DestinationHost);
        }

        /// <summary>
        /// Asynchronously performs the SSL Handshake with the Destination Server.
        /// </summary>
        protected internal async Task HandleSslHandshakeAsync()
        {
            var networkStream = new NetworkStream(Socket);
            SslStream = new SslStream(networkStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);

            await SslStream.AuthenticateAsClientAsync(DestinationHost);
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => sslPolicyErrors == SslPolicyErrors.None;

        private (ProxyResponse response, float firstByteTime) HandleRequestCommand(ReadOnlySpan<byte> writeBuffer)
        {
            string response;
            float firstByteTime;

            if (Scheme == ProxyScheme.HTTPS)
            {
                SslStream.Write(writeBuffer);
                (response, firstByteTime) = SslStream.ReceiveAll();
            }
            else
            {
                Socket.Send(writeBuffer);
                (response, firstByteTime) = Socket.ReceiveAll();
            }

            return (ResponseBuilderHelper.BuildProxyResponse(response, null, DestinationUri), firstByteTime);
        }

        private async Task<(ProxyResponse response, float firstByteTime)> HandleRequestCommandAsync(ReadOnlyMemory<byte> writeBuffer)
        {
            string response;
            float firstByteTime;
            byte[] Byte = null;
            if (Scheme == ProxyScheme.HTTPS)
            {
                await SslStream.WriteAsync(writeBuffer, WriteTimeout, CancellationTokenSourceManager);
                (response, firstByteTime, Byte) = await SslStream.ReceiveAllAsync(ReadTimeout, CancellationTokenSourceManager);
            }
            else
            {
                await Socket.SendAsync(writeBuffer, WriteTimeout, CancellationTokenSourceManager);
                (response, firstByteTime, Byte) = await Socket.ReceiveAllAsync(ReadTimeout, CancellationTokenSourceManager);
            }
            return (ResponseBuilderHelper.BuildProxyResponse(response, Byte, DestinationUri), firstByteTime);
        }

        private (string cachedDestHost, string cachedScheme) ParseAndReturnCachedItems(string url)
        {
            if (String.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));

            bool result = Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (!result)
                throw new ProxyException($"Invalid URL provided: {url}.");

            var cachedDestinationHost = DestinationHost;
            var cachedScheme = DestinationUri?.Scheme;

            DestinationUri = uriResult;

            Scheme = (ProxyScheme)Enum.Parse(typeof(ProxyScheme), uriResult.Scheme, true);
            UrlQuery = uriResult.PathAndQuery;
            DestinationHost = uriResult.Host;
            DestinationPort = uriResult.Port;

            return (cachedDestinationHost, cachedScheme);
        }

        private bool IsCreateSocket() => Socket == null || IsDisposed;

        private bool IsDisposeAndCreateSocket(string cachedDestHost, string cachedScheme, bool isKeepAlive)
        {
            return IsConnectionClosed || IsFaulted || !isKeepAlive || (ProxyType == ProxyType.HTTP && Scheme == ProxyScheme.HTTP
                ? !cachedScheme.Equals(DestinationUri.Scheme)
                : !cachedDestHost.Equals(DestinationHost) || !cachedScheme.Equals(DestinationUri.Scheme));
        }

        private void CheckConnectionClosed(IEnumerable<ProxyHeader> headers)
        {
            if (!headers.Any())
            {
                IsConnectionClosed = true;
                return;
            }

            var connectionHeader = headers.Where(x => x.Name.Equals(RequestConstants.CONNECTION_HEADER) || x.Name.Equals(RequestConstants.PROXY_CONNECTION_HEADER)).SingleOrDefault();

            if (connectionHeader != null)
            {
                IsConnectionClosed = !connectionHeader.Value.ToLower().Equals("keep-alive");
            }
        }
    }
}