﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cowboy.Buffer;
using Cowboy.WebSockets.Extensions;
using Cowboy.WebSockets.SubProtocols;
using Starksoft.Aspen.Proxy;

namespace Cowboy.WebSockets
{
    public sealed class AsyncWebSocketClient : IDisposable
    {
        #region Fields

        private TcpClient _tcpClient;
        private readonly IAsyncWebSocketClientMessageDispatcher _dispatcher;
        private readonly AsyncWebSocketClientConfiguration _configuration;
        private readonly IFrameBuilder _frameBuilder = new WebSocketFrameBuilder();
        private IPEndPoint _remoteEndPoint;
        private Stream _stream;
        private ArraySegment<byte> _receiveBuffer = default(ArraySegment<byte>);
        private int _receiveBufferOffset = 0;

        private readonly Uri _uri;
        private bool _sslEnabled = false;
        private string _secWebSocketKey;

        private int _state;
        private const int _none = 0;
        private const int _connecting = 1;
        private const int _connected = 2;
        private const int _closing = 3;
        private const int _closed = 5;

        private readonly SemaphoreSlim _keepAliveLocker = new SemaphoreSlim(1, 1);
        private KeepAliveTracker _keepAliveTracker;
        private Timer _keepAliveTimeoutTimer;
        private Timer _closingTimeoutTimer;

        #endregion Fields

        #region Constructors

        public AsyncWebSocketClient(Uri uri, IAsyncWebSocketClientMessageDispatcher dispatcher, AsyncWebSocketClientConfiguration configuration = null)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");
            if (dispatcher == null)
                throw new ArgumentNullException("dispatcher");

            if (!Consts.WebSocketSchemes.Contains(uri.Scheme.ToLowerInvariant()))
                throw new NotSupportedException(
                    string.Format("Not support the specified scheme [{0}].", uri.Scheme));

            _uri = uri;
            _remoteEndPoint = ResolveRemoteEndPoint(_uri);
            _dispatcher = dispatcher;
            _configuration = configuration ?? new AsyncWebSocketClientConfiguration();
            _sslEnabled = uri.Scheme.ToLowerInvariant() == "wss";

            if (_configuration.BufferManager == null)
                throw new InvalidProgramException("The buffer manager in configuration cannot be null.");
        }

        public AsyncWebSocketClient(Uri uri,
            Func<AsyncWebSocketClient, string, Task> onServerTextReceived = null,
            Func<AsyncWebSocketClient, byte[], int, int, Task> onServerBinaryReceived = null,
            Func<AsyncWebSocketClient, Task> onServerConnected = null,
            Func<AsyncWebSocketClient, Task> onServerDisconnected = null,
            AsyncWebSocketClientConfiguration configuration = null)
            : this(uri,
                 new InternalAsyncWebSocketClientMessageDispatcherImplementation(
                     onServerTextReceived, onServerBinaryReceived, onServerConnected, onServerDisconnected),
                 configuration)
        {
        }

        public AsyncWebSocketClient(Uri uri,
            Func<AsyncWebSocketClient, string, Task> onServerTextReceived = null,
            Func<AsyncWebSocketClient, byte[], int, int, Task> onServerBinaryReceived = null,
            Func<AsyncWebSocketClient, Task> onServerConnected = null,
            Func<AsyncWebSocketClient, Task> onServerDisconnected = null,
            Func<AsyncWebSocketClient, byte[], int, int, Task> onServerFragmentationStreamOpened = null,
            Func<AsyncWebSocketClient, byte[], int, int, Task> onServerFragmentationStreamContinued = null,
            Func<AsyncWebSocketClient, byte[], int, int, Task> onServerFragmentationStreamClosed = null,
            AsyncWebSocketClientConfiguration configuration = null)
            : this(uri,
                 new InternalAsyncWebSocketClientMessageDispatcherImplementation(
                     onServerTextReceived, onServerBinaryReceived, onServerConnected, onServerDisconnected,
                     onServerFragmentationStreamOpened, onServerFragmentationStreamContinued, onServerFragmentationStreamClosed),
                 configuration)
        {
        }

        private IPEndPoint ResolveRemoteEndPoint(Uri uri)
        {
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : uri.Scheme.ToLowerInvariant() == "wss" ? 443 : 80;

            IPAddress ipAddress;
            if (IPAddress.TryParse(host, out ipAddress))
            {
                return new IPEndPoint(ipAddress, port);
            }
            else
            {
                if (host.ToLowerInvariant() == "localhost")
                {
                    return new IPEndPoint(IPAddress.Parse(@"127.0.0.1"), port);
                }
                else
                {
                    IPAddress[] addresses = Dns.GetHostAddresses(host);
                    if (addresses.Length > 0)
                    {
                        return new IPEndPoint(addresses[0], port);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            string.Format("Cannot resolve host [{0}] by DNS.", host));
                    }
                }
            }
        }

        #endregion Constructors

        #region Properties

        private bool Connected { get { return _tcpClient != null && _tcpClient.Client.Connected; } }
        public IPEndPoint RemoteEndPoint { get { return Connected ? (IPEndPoint)_tcpClient.Client.RemoteEndPoint : _remoteEndPoint; } }
        public IPEndPoint LocalEndPoint { get { return Connected ? (IPEndPoint)_tcpClient.Client.LocalEndPoint : null; } }

        public Uri Uri { get { return _uri; } }

        public TimeSpan ConnectTimeout { get { return _configuration.ConnectTimeout; } }
        public TimeSpan CloseTimeout { get { return _configuration.CloseTimeout; } }
        public TimeSpan KeepAliveInterval { get { return _configuration.KeepAliveInterval; } }
        public TimeSpan KeepAliveTimeout { get { return _configuration.KeepAliveTimeout; } }

        public IDictionary<string, IWebSocketExtensionNegotiator> EnabledExtensions { get { return _configuration.EnabledExtensions; } }
        public IDictionary<string, IWebSocketSubProtocolNegotiator> EnabledSubProtocols { get { return _configuration.EnabledSubProtocols; } }
        public IEnumerable<WebSocketExtensionOfferDescription> OfferedExtensions { get { return _configuration.OfferedExtensions; } }
        public IEnumerable<WebSocketSubProtocolRequestDescription> RequestedSubProtocols { get { return _configuration.RequestedSubProtocols; } }

        public WebSocketState State
        {
            get
            {
                switch (_state)
                {
                    case _none:
                        return WebSocketState.None;

                    case _connecting:
                        return WebSocketState.Connecting;

                    case _connected:
                        return WebSocketState.Open;

                    case _closing:
                        return WebSocketState.Closing;

                    case _closed:
                        return WebSocketState.Closed;

                    default:
                        return WebSocketState.Closed;
                }
            }
        }

        public override string ToString()
        {
            return string.Format("RemoteEndPoint[{0}], LocalEndPoint[{1}]",
                this.RemoteEndPoint, this.LocalEndPoint);
        }

        #endregion Properties

        #region Connect

        public async Task Connect()
        {
            int origin = Interlocked.Exchange(ref _state, _connecting);
            if (!(origin == _none || origin == _closed))
            {
                await InternalClose(false);
                throw new InvalidOperationException("This websocket client is in invalid state when connecting.");
            }

            try
            {
                Clean(); // forcefully clean all things
                ResetKeepAlive();

                _tcpClient = new TcpClient(_remoteEndPoint.Address.AddressFamily);

                var awaiter = _tcpClient.ConnectAsync(_remoteEndPoint.Address, _remoteEndPoint.Port);
                if (!awaiter.Wait(ConnectTimeout))
                {
                    await InternalClose(false);
                    throw new TimeoutException(string.Format(
                        "Connect to [{0}] timeout [{1}].", _remoteEndPoint, ConnectTimeout));
                }

                ConfigureClient();
                var negotiator = NegotiateStream(_tcpClient.GetStream());
                if (!negotiator.Wait(ConnectTimeout))
                {
                    await InternalClose(false);
                    throw new TimeoutException(string.Format(
                        "Negotiate SSL/TSL with remote [{0}] timeout [{1}].", RemoteEndPoint, ConnectTimeout));
                }
                _stream = negotiator.Result;

                _receiveBuffer = _configuration.BufferManager.BorrowBuffer();
                _receiveBufferOffset = 0;

                var handshaker = OpenHandshake();
                if (!handshaker.Wait(ConnectTimeout))
                {
                    await Close(WebSocketCloseCode.ProtocolError, "Opening handshake timeout.");
                    throw new TimeoutException(string.Format(
                        "Handshake with remote [{0}] timeout [{1}].", RemoteEndPoint, ConnectTimeout));
                }
                if (!handshaker.Result)
                {
                    await Close(WebSocketCloseCode.ProtocolError, "Opening handshake failed.");
                    throw new WebSocketException(string.Format(
                        "Handshake with remote [{0}] failed.", RemoteEndPoint));
                }

                if (Interlocked.CompareExchange(ref _state, _connected, _connecting) != _connecting)
                {
                    await InternalClose(false);
                    throw new InvalidOperationException("This websocket client is in invalid state when connected.");
                }

                bool isErrorOccurredInUserSide = false;
                try
                {
                    await _dispatcher.OnServerConnected(this);
                }
                catch (Exception ex)
                {
                    isErrorOccurredInUserSide = true;
                    await HandleUserSideError(ex);
                }

                if (!isErrorOccurredInUserSide)
                {
                    Task.Factory.StartNew(async () =>
                    {
                        _keepAliveTracker.StartTimer();
                        await Process();
                    },
                    TaskCreationOptions.LongRunning)
                    .Forget();
                }
                else
                {
                    await InternalClose(true); // user side handle tcp connection error occurred
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task Connect(string Host, int Port)
        {
            int origin = Interlocked.Exchange(ref _state, _connecting);
            if (!(origin == _none || origin == _closed))
            {
                await InternalClose(false);
                throw new InvalidOperationException("This websocket client is in invalid state when connecting.");
            }

            try
            {
                Clean(); // forcefully clean all things
                ResetKeepAlive();
                Socks5ProxyClient proxyClient = new Socks5ProxyClient(Host, Port);
                _tcpClient = proxyClient.CreateConnection(_remoteEndPoint.Address.ToString(), _remoteEndPoint.Port);
                //_tcpClient = new TcpClient(_remoteEndPoint.Address.AddressFamily);
                //var awaiter = _tcpClient.ConnectAsync(_remoteEndPoint.Address, _remoteEndPoint.Port);
                //_tcpClient.Connect(Host, Port);
                ConfigureClient();
                var negotiator = NegotiateStream(_tcpClient.GetStream());
                if (!negotiator.Wait(ConnectTimeout))
                {
                    await InternalClose(false);
                    throw new TimeoutException(string.Format(
                        "Negotiate SSL/TSL with remote [{0}] timeout [{1}].", RemoteEndPoint, ConnectTimeout));
                }
                _stream = negotiator.Result;

                _receiveBuffer = _configuration.BufferManager.BorrowBuffer();
                _receiveBufferOffset = 0;

                var handshaker = OpenHandshake();
                if (!handshaker.Wait(ConnectTimeout))
                {
                    await Close(WebSocketCloseCode.ProtocolError, "Opening handshake timeout.");
                    throw new TimeoutException(string.Format(
                        "Handshake with remote [{0}] timeout [{1}].", RemoteEndPoint, ConnectTimeout));
                }
                if (!handshaker.Result)
                {
                    await Close(WebSocketCloseCode.ProtocolError, "Opening handshake failed.");
                    throw new WebSocketException(string.Format(
                        "Handshake with remote [{0}] failed.", RemoteEndPoint));
                }

                if (Interlocked.CompareExchange(ref _state, _connected, _connecting) != _connecting)
                {
                    await InternalClose(false);
                    throw new InvalidOperationException("This websocket client is in invalid state when connected.");
                }

                bool isErrorOccurredInUserSide = false;
                try
                {
                    await _dispatcher.OnServerConnected(this);
                }
                catch (Exception ex)
                {
                    isErrorOccurredInUserSide = true;
                    await HandleUserSideError(ex);
                }

                if (!isErrorOccurredInUserSide)
                {
                    Task.Factory.StartNew(async () =>
                    {
                        _keepAliveTracker.StartTimer();
                        await Process();
                    },
                    TaskCreationOptions.LongRunning)
                    .Forget();
                }
                else
                {
                    await InternalClose(true); // user side handle tcp connection error occurred
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void ConfigureClient()
        {
            _tcpClient.ReceiveBufferSize = _configuration.ReceiveBufferSize;
            _tcpClient.SendBufferSize = _configuration.SendBufferSize;
            _tcpClient.ReceiveTimeout = (int)_configuration.ReceiveTimeout.TotalMilliseconds;
            _tcpClient.SendTimeout = (int)_configuration.SendTimeout.TotalMilliseconds;
            _tcpClient.NoDelay = _configuration.NoDelay;
            _tcpClient.LingerState = _configuration.LingerState;
        }

        private async Task<Stream> NegotiateStream(Stream stream)
        {
            if (!_sslEnabled)
                return stream;

            var validateRemoteCertificate = new RemoteCertificateValidationCallback(
                (object sender,
                X509Certificate certificate,
                X509Chain chain,
                SslPolicyErrors sslPolicyErrors)
                =>
                {
                    if (sslPolicyErrors == SslPolicyErrors.None)
                        return true;

                    if (_configuration.SslPolicyErrorsBypassed)
                        return true;
                    else
                        InfoEvent.CowbotEvent.Message = string.Format("Error occurred when validating remote certificate: [{0}], [{1}].",
        this.RemoteEndPoint, sslPolicyErrors);
                    return false;
                });

            var sslStream = new SslStream(
                stream,
                false,
                validateRemoteCertificate,
                null,
                _configuration.SslEncryptionPolicy);

            if (_configuration.SslClientCertificates == null || _configuration.SslClientCertificates.Count == 0)
            {
                await sslStream.AuthenticateAsClientAsync( // No client certificates are used in the authentication. The certificate revocation list is not checked during authentication.
                    _configuration.SslTargetHost); // The name of the server that will share this SslStream. The value specified for targetHost must match the name on the server's certificate.
            }
            else
            {
                await sslStream.AuthenticateAsClientAsync(
                    _configuration.SslTargetHost, // The name of the server that will share this SslStream. The value specified for targetHost must match the name on the server's certificate.
                    _configuration.SslClientCertificates, // The X509CertificateCollection that contains client certificates.
                    _configuration.SslEnabledProtocols, // The SslProtocols value that represents the protocol used for authentication.
                    _configuration.SslCheckCertificateRevocation); // A Boolean value that specifies whether the certificate revocation list is checked during authentication.
            }

            // When authentication succeeds, you must check the IsEncrypted and IsSigned properties
            // to determine what security services are used by the SslStream. Check the
            // IsMutuallyAuthenticated property to determine whether mutual authentication occurred.
            InfoEvent.CowbotEvent.Message = string.Format(
                "Ssl Stream: SslProtocol[{0}], IsServer[{1}], IsAuthenticated[{2}], IsEncrypted[{3}], IsSigned[{4}], IsMutuallyAuthenticated[{5}], "
                + "HashAlgorithm[{6}], HashStrength[{7}], KeyExchangeAlgorithm[{8}], KeyExchangeStrength[{9}], CipherAlgorithm[{10}], CipherStrength[{11}].",
                sslStream.SslProtocol,
                sslStream.IsServer,
                sslStream.IsAuthenticated,
                sslStream.IsEncrypted,
                sslStream.IsSigned,
                sslStream.IsMutuallyAuthenticated,
                sslStream.HashAlgorithm,
                sslStream.HashStrength,
                sslStream.KeyExchangeAlgorithm,
                sslStream.KeyExchangeStrength,
                sslStream.CipherAlgorithm,
                sslStream.CipherStrength);

            return sslStream;
        }

        private async Task<bool> OpenHandshake()
        {
            bool handshakeResult = false;

            try
            {
                var requestBuffer = WebSocketClientHandshaker.CreateOpenningHandshakeRequest(this, out _secWebSocketKey);
                await _stream.WriteAsync(requestBuffer, 0, requestBuffer.Length);

                int terminatorIndex = -1;
                while (!WebSocketHelpers.FindHttpMessageTerminator(_receiveBuffer.Array, _receiveBuffer.Offset, _receiveBufferOffset, out terminatorIndex))
                {
                    int receiveCount = await _stream.ReadAsync(
                        _receiveBuffer.Array,
                        _receiveBuffer.Offset + _receiveBufferOffset,
                        _receiveBuffer.Count - _receiveBufferOffset);
                    if (receiveCount == 0)
                    {
                        throw new WebSocketHandshakeException(string.Format(
                            "Handshake with remote [{0}] failed due to receive zero bytes.", RemoteEndPoint));
                    }

                    SegmentBufferDeflector.ReplaceBuffer(_configuration.BufferManager, ref _receiveBuffer, ref _receiveBufferOffset, receiveCount);

                    if (_receiveBufferOffset > 2048)
                    {
                        throw new WebSocketHandshakeException(string.Format(
                            "Handshake with remote [{0}] failed due to receive weird stream.", RemoteEndPoint));
                    }
                }

                handshakeResult = WebSocketClientHandshaker.VerifyOpenningHandshakeResponse(
                    this,
                    _receiveBuffer.Array,
                    _receiveBuffer.Offset,
                    terminatorIndex + Consts.HttpMessageTerminator.Length,
                    _secWebSocketKey);

                SegmentBufferDeflector.ShiftBuffer(
                    _configuration.BufferManager,
                    terminatorIndex + Consts.HttpMessageTerminator.Length,
                    ref _receiveBuffer,
                    ref _receiveBufferOffset);
            }
            catch (WebSocketHandshakeException ex)
            {
                InfoEvent.CowbotEvent.Message = string.Format(ex.Message, ex);
                handshakeResult = false;
            }
            catch (Exception)
            {
                handshakeResult = false;
                throw;
            }

            return handshakeResult;
        }

        private void ResetKeepAlive()
        {
            _keepAliveTracker = KeepAliveTracker.Create(KeepAliveInterval, new TimerCallback((s) => OnKeepAlive()));
            _keepAliveTimeoutTimer = new Timer(new TimerCallback((s) => OnKeepAliveTimeout()), null, Timeout.Infinite, Timeout.Infinite);
            _closingTimeoutTimer = new Timer(new TimerCallback((s) => OnCloseTimeout()), null, Timeout.Infinite, Timeout.Infinite);
        }

        #endregion Connect

        #region Process

        private async Task Process()
        {
            try
            {
                Header frameHeader;
                byte[] payload;
                int payloadOffset;
                int payloadCount;
                int consumedLength = 0;

                while (State == WebSocketState.Open || State == WebSocketState.Closing)
                {
                    int receiveCount = await _stream.ReadAsync(
                        _receiveBuffer.Array,
                        _receiveBuffer.Offset + _receiveBufferOffset,
                        _receiveBuffer.Count - _receiveBufferOffset);
                    if (receiveCount == 0)
                        break;

                    _keepAliveTracker.OnDataReceived();
                    SegmentBufferDeflector.ReplaceBuffer(_configuration.BufferManager, ref _receiveBuffer, ref _receiveBufferOffset, receiveCount);
                    consumedLength = 0;

                    while (true)
                    {
                        frameHeader = null;
                        payload = null;
                        payloadOffset = 0;
                        payloadCount = 0;

                        if (_frameBuilder.TryDecodeFrameHeader(
                            _receiveBuffer.Array,
                            _receiveBuffer.Offset + consumedLength,
                            _receiveBufferOffset - consumedLength,
                            out frameHeader)
                            && frameHeader.Length + frameHeader.PayloadLength <= _receiveBufferOffset - consumedLength)
                        {
                            try
                            {
                                if (frameHeader.IsMasked)
                                {
                                    await Close(WebSocketCloseCode.ProtocolError, "A client MUST close a connection if it detects a masked frame.");
                                    throw new WebSocketException(string.Format(
                                        "Client received masked frame [{0}] from remote [{1}].", frameHeader.OpCode, RemoteEndPoint));
                                }

                                _frameBuilder.DecodePayload(
                                    _receiveBuffer.Array,
                                    _receiveBuffer.Offset + consumedLength,
                                    frameHeader,
                                    out payload, out payloadOffset, out payloadCount);

                                switch (frameHeader.OpCode)
                                {
                                    case OpCode.Continuation:
                                        await HandleContinuationFrame(frameHeader, payload, payloadOffset, payloadCount);
                                        break;

                                    case OpCode.Text:
                                        await HandleTextFrame(frameHeader, payload, payloadOffset, payloadCount);
                                        break;

                                    case OpCode.Binary:
                                        await HandleBinaryFrame(frameHeader, payload, payloadOffset, payloadCount);
                                        break;

                                    case OpCode.Close:
                                        await HandleCloseFrame(frameHeader, payload, payloadOffset, payloadCount);
                                        break;

                                    case OpCode.Ping:
                                        await HandlePingFrame(frameHeader, payload, payloadOffset, payloadCount);
                                        break;

                                    case OpCode.Pong:
                                        await HandlePongFrame(frameHeader, payload, payloadOffset, payloadCount);
                                        break;

                                    default:
                                        {
                                            // Incoming data MUST always be validated by both clients
                                            // and servers. If, at any time, an endpoint is faced
                                            // with data that it does not understand or that violates
                                            // some criteria by which the endpoint determines safety
                                            // of input, or when the endpoint sees an opening
                                            // handshake that does not correspond to the values it is
                                            // expecting (e.g., incorrect path or origin in the
                                            // client request), the endpoint MAY drop the TCP
                                            // connection. If the invalid data was received after a
                                            // successful WebSocket handshake, the endpoint SHOULD
                                            // send a Close frame with an appropriate status code
                                            // (Section 7.4) before proceeding to _Close the
                                            // WebSocket Connection_. Use of a Close frame with an
                                            // appropriate status code can help in diagnosing the
                                            // problem. If the invalid data is sent during the
                                            // WebSocket handshake, the server SHOULD return an
                                            // appropriate HTTP [RFC2616] status code.
                                            await Close(WebSocketCloseCode.InvalidMessageType);
                                            throw new NotSupportedException(
                                                string.Format("Not support received opcode [{0}].", (byte)frameHeader.OpCode));
                                        }
                                }
                            }
                            catch (Exception ex)
                            {
                                InfoEvent.CowbotEvent.Message = string.Format(ex.Message, ex);
                                throw;
                            }
                            finally
                            {
                                consumedLength += frameHeader.Length + frameHeader.PayloadLength;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (_receiveBuffer != null && _receiveBuffer.Array != null)
                    {
                        SegmentBufferDeflector.ShiftBuffer(_configuration.BufferManager, consumedLength, ref _receiveBuffer, ref _receiveBufferOffset);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // looking forward to a graceful quit from the ReadAsync but the inside EndRead will
                // raise the ObjectDisposedException, so a gracefully close for the socket should be
                // a Shutdown, but we cannot avoid the Close triggers this happen.
            }
            catch (Exception ex)
            {
                await HandleReceiveOperationException(ex);
            }
            finally
            {
                await InternalClose(true); // read async buffer returned, remote notifies closed
            }
        }

        private async Task HandleContinuationFrame(Header frameHeader, byte[] payload, int payloadOffset, int payloadCount)
        {
            if (!frameHeader.IsFIN)
            {
                try
                {
                    await _dispatcher.OnServerFragmentationStreamContinued(this, payload, payloadOffset, payloadCount);
                }
                catch (Exception ex)
                {
                    await HandleUserSideError(ex);
                }
            }
            else
            {
                try
                {
                    await _dispatcher.OnServerFragmentationStreamClosed(this, payload, payloadOffset, payloadCount);
                }
                catch (Exception ex)
                {
                    await HandleUserSideError(ex);
                }
            }
        }

        private async Task HandleTextFrame(Header frameHeader, byte[] payload, int payloadOffset, int payloadCount)
        {
            if (frameHeader.IsFIN)
            {
                try
                {
                    var text = Encoding.UTF8.GetString(payload, payloadOffset, payloadCount);
                    await _dispatcher.OnServerTextReceived(this, text);
                }
                catch (Exception ex)
                {
                    await HandleUserSideError(ex);
                }
            }
            else
            {
                try
                {
                    await _dispatcher.OnServerFragmentationStreamOpened(this, payload, payloadOffset, payloadCount);
                }
                catch (Exception ex)
                {
                    await HandleUserSideError(ex);
                }
            }
        }

        private async Task HandleBinaryFrame(Header frameHeader, byte[] payload, int payloadOffset, int payloadCount)
        {
            if (frameHeader.IsFIN)
            {
                try
                {
                    await _dispatcher.OnServerBinaryReceived(this, payload, payloadOffset, payloadCount);
                }
                catch (Exception ex)
                {
                    await HandleUserSideError(ex);
                }
            }
            else
            {
                try
                {
                    await _dispatcher.OnServerFragmentationStreamOpened(this, payload, payloadOffset, payloadCount);
                }
                catch (Exception ex)
                {
                    await HandleUserSideError(ex);
                }
            }
        }

        private async Task HandleCloseFrame(Header frameHeader, byte[] payload, int payloadOffset, int payloadCount)
        {
            if (!frameHeader.IsFIN)
            {
                throw new WebSocketException(string.Format(
                    "Client received unfinished frame [{0}] from remote [{1}].", frameHeader.OpCode, RemoteEndPoint));
            }

            if (payloadCount > 1)
            {
                var statusCode = payload[payloadOffset + 0] * 256 + payload[payloadOffset + 1];
                var closeCode = (WebSocketCloseCode)statusCode;
                var closeReason = string.Empty;

                if (payloadCount > 2)
                {
                    closeReason = Encoding.UTF8.GetString(payload, payloadOffset + 2, payloadCount - 2);
                }
#if DEBUG
                InfoEvent.CowbotEvent.Message = string.Format("Receive server side close frame [{0}] [{1}].", closeCode, closeReason);
#endif
                // If an endpoint receives a Close frame and did not previously send a Close frame,
                // the endpoint MUST send a Close frame in response. (When sending a Close frame in
                // response, the endpoint typically echos the status code it received.) It SHOULD do
                // so as soon as practical.
                await Close(closeCode, closeReason);
            }
            else
            {
#if DEBUG
                InfoEvent.CowbotEvent.Message = string.Format("Receive server side close frame but no status code.");
#endif
                await Close(WebSocketCloseCode.InvalidPayloadData);
            }
        }

        private async Task HandlePingFrame(Header frameHeader, byte[] payload, int payloadOffset, int payloadCount)
        {
            if (!frameHeader.IsFIN)
            {
                throw new WebSocketException(string.Format(
                    "Client received unfinished frame [{0}] from remote [{1}].", frameHeader.OpCode, RemoteEndPoint));
            }

            // Upon receipt of a Ping frame, an endpoint MUST send a Pong frame in response, unless
            // it already received a Close frame. It SHOULD respond with Pong frame as soon as is
            // practical. Pong frames are discussed in Section 5.5.3.
            //
            // An endpoint MAY send a Ping frame any time after the connection is established and
            // before the connection is closed.
            //
            // A Ping frame may serve either as a keep-alive or as a means to verify that the remote
            // endpoint is still responsive.
            var ping = Encoding.UTF8.GetString(payload, payloadOffset, payloadCount);
#if DEBUG
            InfoEvent.CowbotEvent.Message = string.Format("Receive server side ping frame [{0}].", ping);
#endif
            if (State == WebSocketState.Open)
            {
                // A Pong frame sent in response to a Ping frame must have identical "Application
                // data" as found in the message body of the Ping frame being replied to.
                var pong = new PongFrame(ping).ToArray(_frameBuilder);
                await SendFrame(pong);
#if DEBUG
                InfoEvent.CowbotEvent.Message = string.Format("Send client side pong frame [{0}].", ping);
#endif
            }
        }

        private async Task HandlePongFrame(Header frameHeader, byte[] payload, int payloadOffset, int payloadCount)
        {
            if (!frameHeader.IsFIN)
            {
                throw new WebSocketException(string.Format(
                    "Client received unfinished frame [{0}] from remote [{1}].", frameHeader.OpCode, RemoteEndPoint));
            }

            // If an endpoint receives a Ping frame and has not yet sent Pong frame(s) in response to
            // previous Ping frame(s), the endpoint MAY elect to send a Pong frame for only the most
            // recently processed Ping frame.
            //
            // A Pong frame MAY be sent unsolicited. This serves as a unidirectional heartbeat. A
            // response to an unsolicited Pong frame is not expected.
            var pong = Encoding.UTF8.GetString(payload, payloadOffset, payloadCount);
            StopKeepAliveTimeoutTimer();
#if DEBUG
            InfoEvent.CowbotEvent.Message = string.Format("Receive server side pong frame [{0}].", pong);
#endif
            await Task.CompletedTask;
        }

        #endregion Process

        #region Close

        public async Task Close(WebSocketCloseCode closeCode)
        {
            await Close(closeCode, null);
        }

        public async Task Close(WebSocketCloseCode closeCode, string closeReason)
        {
            if (State == WebSocketState.Closed || State == WebSocketState.None)
                return;

            var priorState = Interlocked.Exchange(ref _state, _closing);
            switch (priorState)
            {
                case _connected:
                    {
                        var closingHandshake = new CloseFrame(closeCode, closeReason).ToArray(_frameBuilder);
                        try
                        {
                            StartClosingTimer();
#if DEBUG
                            InfoEvent.CowbotEvent.Message = string.Format("Send client side close frame [{0}] [{1}].", closeCode, closeReason);
#endif
                            var awaiter = _stream.WriteAsync(closingHandshake, 0, closingHandshake.Length);
                            if (!awaiter.Wait(ConnectTimeout))
                            {
                                await InternalClose(true);
                                throw new TimeoutException(string.Format(
                                    "Closing handshake with [{0}] timeout [{1}].", _remoteEndPoint, ConnectTimeout));
                            }
                        }
                        catch (Exception ex)
                        {
                            await HandleSendOperationException(ex);
                        }
                        return;
                    }
                case _connecting:
                case _closing:
                    {
                        await InternalClose(true);
                        return;
                    }
                case _closed:
                case _none:
                default:
                    return;
            }
        }

        private async Task InternalClose(bool shallNotifyUserSide)
        {
            if (Interlocked.Exchange(ref _state, _closed) == _closed)
            {
                return;
            }

            Shutdown();

            if (shallNotifyUserSide)
            {
                InfoEvent.CowbotEvent.Message = string.Format("Disconnected from server [{0}] with dispatcher [{1}] on [{2}].",
                    this.RemoteEndPoint,
                    _dispatcher.GetType().Name,
                    DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm:ss.fffffff"));
                try
                {
                    await _dispatcher.OnServerDisconnected(this);
                }
                catch (Exception ex)
                {
                    await HandleUserSideError(ex);
                }
            }

            Clean();
        }

        public void Shutdown()
        {
            // The correct way to shut down the connection (especially if you are in a full-duplex
            // conversation) is to call socket.Shutdown(SocketShutdown.Send) and give the remote
            // party some time to close their send channel. This ensures that you receive any pending
            // data instead of slamming the connection shut. ObjectDisposedException should never be
            // part of the normal application flow.
            if (_tcpClient != null && _tcpClient.Connected)
            {
                _tcpClient.Client.Shutdown(SocketShutdown.Send);
            }
        }

        private void Clean()
        {
            try
            {
                try
                {
                    if (_keepAliveTracker != null)
                    {
                        _keepAliveTracker.StopTimer();
                        _keepAliveTracker.Dispose();
                    }
                }
                catch { }
                try
                {
                    if (_keepAliveTimeoutTimer != null)
                    {
                        _keepAliveTimeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        _keepAliveTimeoutTimer.Dispose();
                    }
                }
                catch { }
                try
                {
                    if (_closingTimeoutTimer != null)
                    {
                        _closingTimeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        _closingTimeoutTimer.Dispose();
                    }
                }
                catch { }
                try
                {
                    if (_stream != null)
                    {
                        _stream.Dispose();
                    }
                }
                catch { }
                try
                {
                    if (_tcpClient != null)
                    {
                        _tcpClient.Dispose();
                    }
                }
                catch { }
            }
            catch { }
            finally
            {
                _keepAliveTracker = null;
                _keepAliveTimeoutTimer = null;
                _closingTimeoutTimer = null;
                _stream = null;
                _tcpClient = null;
            }

            if (_receiveBuffer != default(ArraySegment<byte>))
                _configuration.BufferManager.ReturnBuffer(_receiveBuffer);
            _receiveBuffer = default(ArraySegment<byte>);
            _receiveBufferOffset = 0;
        }

        public async Task Abort()
        {
            await InternalClose(true);
        }

        private void StartClosingTimer()
        {
            // In abnormal cases (such as not having received a TCP Close from the server after a
            // reasonable amount of time) a client MAY initiate the TCP Close.
            _closingTimeoutTimer.Change((int)CloseTimeout.TotalMilliseconds, Timeout.Infinite);
        }

        private async void OnCloseTimeout()
        {
            // After both sending and receiving a Close message, an endpoint considers the WebSocket
            // connection closed and MUST close the underlying TCP connection. The server MUST close
            // the underlying TCP connection immediately; the client SHOULD wait for the server to
            // close the connection but MAY close the connection at any time after sending and
            // receiving a Close message, e.g., if it has not received a TCP Close from the server in
            // a reasonable time period.
            InfoEvent.CowbotEvent.Message = string.Format("Closing timer timeout [{0}] then close automatically.", CloseTimeout);
            await InternalClose(true);
        }

        #endregion Close

        #region Exception Handler

        private async Task HandleSendOperationException(Exception ex)
        {
            if (IsSocketTimeOut(ex))
            {
                await CloseIfShould(ex);
                throw new WebSocketException(ex.Message, new TimeoutException(ex.Message, ex));
            }

            await CloseIfShould(ex);
            throw new WebSocketException(ex.Message, ex);
        }

        private async Task HandleReceiveOperationException(Exception ex)
        {
            if (IsSocketTimeOut(ex))
            {
                await CloseIfShould(ex);
                throw new WebSocketException(ex.Message, new TimeoutException(ex.Message, ex));
            }

            await CloseIfShould(ex);
            throw new WebSocketException(ex.Message, ex);
        }

        private bool IsSocketTimeOut(Exception ex)
        {
            return ex is IOException
                && ex.InnerException != null
                && ex.InnerException is SocketException
                && (ex.InnerException as SocketException).SocketErrorCode == SocketError.TimedOut;
        }

        private async Task<bool> CloseIfShould(Exception ex)
        {
            if (ex is ObjectDisposedException
                || ex is InvalidOperationException
                || ex is SocketException
                || ex is IOException
                || ex is NullReferenceException // buffer array operation
                || ex is ArgumentException      // buffer array operation
                )
            {
                InfoEvent.CowbotEvent.Message = string.Format(ex.Message, ex);

                await InternalClose(false); // intend to close the session

                return true;
            }

            return false;
        }

        private async Task HandleUserSideError(Exception ex)
        {
            InfoEvent.CowbotEvent.Message = string.Format(string.Format("Client [{0}] error occurred in user side [{1}].", this, ex.Message), ex);
            await Task.CompletedTask;
        }

        #endregion Exception Handler

        #region Send

        public async Task SendTextAsync(string text)
        {
            await SendFrame(new TextFrame(text).ToArray(_frameBuilder));
        }

        public async Task SendBinaryAsync(byte[] data)
        {
            await SendBinaryAsync(data, 0, data.Length);
        }

        public async Task SendBinaryAsync(byte[] data, int offset, int count)
        {
            await SendFrame(new BinaryFrame(data, offset, count).ToArray(_frameBuilder));
        }

        public async Task SendBinaryAsync(ArraySegment<byte> segment)
        {
            await SendFrame(new BinaryFrame(segment).ToArray(_frameBuilder));
        }

        public async Task SendStreamAsync(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            int fragmentLength = _configuration.ReasonableFragmentSize;
            var buffer = new byte[fragmentLength];
            int readCount = 0;

            readCount = await stream.ReadAsync(buffer, 0, fragmentLength);
            if (readCount == 0)
                return;
            await SendFrame(new BinaryFragmentationFrame(OpCode.Binary, buffer, 0, readCount, isFin: false).ToArray(_frameBuilder));

            while (true)
            {
                readCount = await stream.ReadAsync(buffer, 0, fragmentLength);
                if (readCount != 0)
                {
                    await SendFrame(new BinaryFragmentationFrame(OpCode.Continuation, buffer, 0, readCount, isFin: false).ToArray(_frameBuilder));
                }
                else
                {
                    await SendFrame(new BinaryFragmentationFrame(OpCode.Continuation, buffer, 0, 0, isFin: true).ToArray(_frameBuilder));
                    break;
                }
            }
        }

        private async Task SendFrame(byte[] frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException("frame");
            }
            if (State != WebSocketState.Open)
            {
                throw new InvalidOperationException("This websocket client has not connected to server.");
            }

            try
            {
                await _stream.WriteAsync(frame, 0, frame.Length);
                _keepAliveTracker.OnDataSent();
            }
            catch (Exception ex)
            {
                await HandleSendOperationException(ex);
            }
        }

        #endregion Send

        #region Keep Alive

        private void StartKeepAliveTimeoutTimer()
        {
            _keepAliveTimeoutTimer.Change((int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);
        }

        private void StopKeepAliveTimeoutTimer()
        {
            _keepAliveTimeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private async void OnKeepAliveTimeout()
        {
            InfoEvent.CowbotEvent.Message = string.Format("Keep-alive timer timeout [{0}].", KeepAliveTimeout);
            await Close(WebSocketCloseCode.AbnormalClosure, "Keep-Alive Timeout");
        }

        private async void OnKeepAlive()
        {
            if (await _keepAliveLocker.WaitAsync(0))
            {
                try
                {
                    if (State != WebSocketState.Open)
                        return;

                    if (_keepAliveTracker.ShouldSendKeepAlive())
                    {
                        var keepAliveFrame = new PingFrame().ToArray(_frameBuilder);
                        await SendFrame(keepAliveFrame);
                        StartKeepAliveTimeoutTimer();
#if DEBUG
                        InfoEvent.CowbotEvent.Message = string.Format("Send client side ping frame [{0}].", string.Empty);
#endif
                        _keepAliveTracker.ResetTimer();
                    }
                }
                catch (Exception ex)
                {
                    InfoEvent.CowbotEvent.Message = string.Format(ex.Message, ex);
                    await Close(WebSocketCloseCode.EndpointUnavailable);
                }
                finally
                {
                    _keepAliveLocker.Release();
                }
            }
        }

        #endregion Keep Alive

        #region Extensions

        internal void AgreeExtensions(IEnumerable<string> extensions)
        {
            if (extensions == null)
                throw new ArgumentNullException("extensions");

            // If a server gives an invalid response, such as accepting a PMCE that the client did
            // not offer, the client MUST _Fail the WebSocket Connection_.
            if (this.OfferedExtensions == null
                || !this.OfferedExtensions.Any()
                || this.EnabledExtensions == null
                || !this.EnabledExtensions.Any())
                throw new WebSocketHandshakeException(string.Format(
                    "Negotiate extension with remote [{0}] failed due to no extension enabled.", this.RemoteEndPoint));

            // Note that the order of extensions is significant. Any interactions between multiple
            // extensions MAY be defined in the documents defining the extensions. In the absence of
            // such definitions, the interpretation is that the header fields listed by the client in
            // its request represent a preference of the header fields it wishes to use, with the
            // first options listed being most preferable. The extensions listed by the server in
            // response represent the extensions actually in use for the connection. Should the
            // extensions modify the data and/or framing, the order of operations on the data should
            // be assumed to be the same as the order in which the extensions are listed in the
            // server's response in the opening handshake. For example, if there are two extensions
            // "foo" and "bar" and if the header field |Sec-WebSocket-Extensions| sent by the server
            // has the value "foo, bar", then operations on the data will be made as bar(foo(data)),
            // be those changes to the data itself (such as
            // compression) or changes to the framing that may "stack".
            var agreedExtensions = new SortedList<int, IWebSocketExtension>();
            var suggestedExtensions = string.Join(",", extensions).Split(',')
                .Select(p => p.TrimStart().TrimEnd()).Where(p => !string.IsNullOrWhiteSpace(p));

            int order = 0;
            foreach (var extension in suggestedExtensions)
            {
                order++;

                var offeredExtensionName = extension.Split(';').First();

                // Extensions not listed by the client MUST NOT be listed.
                if (!this.EnabledExtensions.ContainsKey(offeredExtensionName))
                    throw new WebSocketHandshakeException(string.Format(
                        "Negotiate extension with remote [{0}] failed due to un-enabled extensions [{1}].",
                        this.RemoteEndPoint, offeredExtensionName));

                var extensionNegotiator = this.EnabledExtensions[offeredExtensionName];

                string invalidParameter;
                IWebSocketExtension negotiatedExtension;
                if (!extensionNegotiator.NegotiateAsClient(extension, out invalidParameter, out negotiatedExtension)
                    || !string.IsNullOrEmpty(invalidParameter)
                    || negotiatedExtension == null)
                {
                    throw new WebSocketHandshakeException(string.Format(
                        "Negotiate extension with remote [{0}] failed due to extension [{1}] has invalid parameter [{2}].",
                        this.RemoteEndPoint, extension, invalidParameter));
                }

                agreedExtensions.Add(order, negotiatedExtension);
            }

            // If a server gives an invalid response, such as accepting a PMCE that the client did
            // not offer, the client MUST _Fail the WebSocket Connection_.
            foreach (var extension in agreedExtensions.Values)
            {
                if (!this.OfferedExtensions.Any(x => x.ExtensionNegotiationOffer.StartsWith(extension.Name)))
                    throw new WebSocketHandshakeException(string.Format(
                        "Negotiate extension with remote [{0}] failed due to extension [{1}] not be offered.",
                        this.RemoteEndPoint, extension.Name));
            }

            // A server MUST NOT accept a PMCE extension negotiation offer together with another
            // extension if the PMCE will conflict with the extension on their use of the RSV1 bit. A
            // client that received a response accepting a PMCE extension negotiation offer together
            // with such an extension MUST _Fail the WebSocket Connection_.
            bool isRsv1BitOccupied = false;
            bool isRsv2BitOccupied = false;
            bool isRsv3BitOccupied = false;
            foreach (var extension in agreedExtensions.Values)
            {
                if ((isRsv1BitOccupied && extension.Rsv1BitOccupied)
                    || (isRsv2BitOccupied && extension.Rsv2BitOccupied)
                    || (isRsv3BitOccupied && extension.Rsv3BitOccupied))
                    throw new WebSocketHandshakeException(string.Format(
                        "Negotiate extension with remote [{0}] failed due to conflict bit occupied.", this.RemoteEndPoint));

                isRsv1BitOccupied = isRsv1BitOccupied | extension.Rsv1BitOccupied;
                isRsv2BitOccupied = isRsv2BitOccupied | extension.Rsv2BitOccupied;
                isRsv3BitOccupied = isRsv3BitOccupied | extension.Rsv3BitOccupied;
            }

            _frameBuilder.NegotiatedExtensions = agreedExtensions;
        }

        #endregion Extensions

        #region Sub-Protocols

        internal void UseSubProtocol(string protocol)
        {
            if (string.IsNullOrWhiteSpace(protocol))
                throw new ArgumentNullException("protocol");

            if (this.RequestedSubProtocols == null
                || !this.RequestedSubProtocols.Any()
                || this.EnabledSubProtocols == null
                || !this.EnabledSubProtocols.Any())
                throw new WebSocketHandshakeException(string.Format(
                    "Negotiate sub-protocol with remote [{0}] failed due to sub-protocol [{1}] is not enabled.",
                    this.RemoteEndPoint, protocol));

            var requestedSubProtocols = string.Join(",", this.RequestedSubProtocols.Select(s => s.RequestedSubProtocol))
                .Split(',').Select(p => p.TrimStart().TrimEnd()).Where(p => !string.IsNullOrWhiteSpace(p));

            if (!requestedSubProtocols.Contains(protocol))
                throw new WebSocketHandshakeException(string.Format(
                    "Negotiate sub-protocol with remote [{0}] failed due to sub-protocol [{1}] has not been requested.",
                    this.RemoteEndPoint, protocol));

            // format : name.version.parameter
            var segements = protocol.Split('.')
                .Select(p => p.TrimStart().TrimEnd()).Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
            string protocolName = segements[0];
            string protocolVersion = segements.Length > 1 ? segements[1] : null;
            string protocolParameter = segements.Length > 2 ? segements[2] : null;

            if (!this.EnabledSubProtocols.ContainsKey(protocolName))
                throw new WebSocketHandshakeException(string.Format(
                    "Negotiate sub-protocol with remote [{0}] failed due to sub-protocol [{1}] is not enabled.",
                    this.RemoteEndPoint, protocolName));

            var subProtocolNegotiator = this.EnabledSubProtocols[protocolName];

            string invalidParameter;
            IWebSocketSubProtocol negotiatedSubProtocol;
            if (!subProtocolNegotiator.NegotiateAsClient(protocolName, protocolVersion, protocolParameter, out invalidParameter, out negotiatedSubProtocol)
                || !string.IsNullOrEmpty(invalidParameter)
                || negotiatedSubProtocol == null)
            {
                throw new WebSocketHandshakeException(string.Format(
                    "Negotiate sub-protocol with remote [{0}] failed due to sub-protocol [{1}] has invalid parameter [{2}].",
                    this.RemoteEndPoint, protocol, invalidParameter));
            }
        }

        #endregion Sub-Protocols

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_keepAliveTimeoutTimer")]
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_keepAliveLocker")]
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_closingTimeoutTimer")]
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    InternalClose(false).Wait(); // disposing
                }
                catch (Exception ex)
                {
                    InfoEvent.CowbotEvent.Message = string.Format(ex.Message, ex);
                }
            }
        }

        #endregion IDisposable Members
    }
}