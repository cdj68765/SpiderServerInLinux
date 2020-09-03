using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cowboy.Buffer;

namespace Cowboy.WebSockets
{
    public sealed class AsyncWebSocketServer
    {
        #region Fields

        private TcpListener _listener;
        public readonly ConcurrentDictionary<string, AsyncWebSocketSession> _sessions = new ConcurrentDictionary<string, AsyncWebSocketSession>();
        private readonly AsyncWebSocketServerModuleCatalog _catalog;
        private readonly AsyncWebSocketServerConfiguration _configuration;
        private AsyncWebSocketRouteResolver _routeResolver;

        private int _state;
        private const int _none = 0;
        private const int _listening = 1;
        private const int _disposed = 5;

        #endregion Fields

        #region Constructors

        public AsyncWebSocketServer(int listenedPort, AsyncWebSocketServerModuleCatalog catalog, AsyncWebSocketServerConfiguration configuration = null)
            : this(IPAddress.Any, listenedPort, catalog, configuration)
        {
        }

        public AsyncWebSocketServer(IPAddress listenedAddress, int listenedPort, AsyncWebSocketServerModuleCatalog catalog, AsyncWebSocketServerConfiguration configuration = null)
            : this(new IPEndPoint(listenedAddress, listenedPort), catalog, configuration)
        {
        }

        public AsyncWebSocketServer(IPEndPoint listenedEndPoint, AsyncWebSocketServerModuleCatalog catalog, AsyncWebSocketServerConfiguration configuration = null)
        {
            if (listenedEndPoint == null)
                throw new ArgumentNullException("listenedEndPoint");
            if (catalog == null)
                throw new ArgumentNullException("catalog");

            this.ListenedEndPoint = listenedEndPoint;
            _catalog = catalog;
            _configuration = configuration ?? new AsyncWebSocketServerConfiguration();

            if (_configuration.BufferManager == null)
                throw new InvalidProgramException("The buffer manager in configuration cannot be null.");

            Initialize();
        }

        private void Initialize()
        {
            _routeResolver = new AsyncWebSocketRouteResolver(_catalog);
        }

        #endregion Constructors

        #region Properties

        public IPEndPoint ListenedEndPoint { get; private set; }
        public bool IsListening { get { return _state == _listening; } }
        public int SessionCount { get { return _sessions.Count; } }

        public IEnumerable<string> EnabledExtensions
        {
            get { return _configuration.EnabledExtensions != null ? _configuration.EnabledExtensions.Keys : null; }
        }

        public IEnumerable<string> EnabledSubProtocols
        {
            get { return _configuration.EnabledSubProtocols != null ? _configuration.EnabledSubProtocols.Keys : null; }
        }

        #endregion Properties

        #region Server

        public void Listen()
        {
            int origin = Interlocked.CompareExchange(ref _state, _listening, _none);
            if (origin == _disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            else if (origin != _none)
            {
                throw new InvalidOperationException("This websocket server has already started.");
            }

            try
            {
                _listener = new TcpListener(this.ListenedEndPoint);
                _listener.Start(_configuration.PendingConnectionBacklog);

                Task.Factory.StartNew(async () =>
                {
                    await Accept();
                },
                TaskCreationOptions.LongRunning)
                .Forget();
            }
            catch (Exception ex) when (!ShouldThrow(ex)) { }
        }

        public void Shutdown()
        {
            if (Interlocked.Exchange(ref _state, _disposed) == _disposed)
            {
                return;
            }

            try
            {
                _listener.Stop();
                _listener = null;

                Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        foreach (var session in _sessions.Values)
                        {
                            await session.Close(WebSocketCloseCode.NormalClosure);
                        }
                    }
                    catch (Exception ex) when (!ShouldThrow(ex)) { }
                },
                TaskCreationOptions.PreferFairness)
                .Wait();
            }
            catch (Exception ex) when (!ShouldThrow(ex)) { }
        }

        public bool Pending()
        {
            if (!IsListening)
                throw new InvalidOperationException("The websocket server is not active.");

            // determine if there are pending connection requests.
            return _listener.Pending();
        }

        private async Task Accept()
        {
            try
            {
                while (IsListening)
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync();
                    Task.Factory.StartNew(async () =>
                    {
                        await Process(tcpClient);
                    },
                    TaskCreationOptions.PreferFairness)
                    .Forget();
                }
            }
            catch (Exception ex) when (!ShouldThrow(ex)) { }
            catch (Exception ex)
            {
                InfoEvent.CowbotEvent.Message = string.Format(ex.Message, ex);
            }
        }

        private async Task Process(TcpClient acceptedTcpClient)
        {
            var session = new AsyncWebSocketSession(acceptedTcpClient, _configuration, _configuration.BufferManager, _routeResolver, this);

            if (_sessions.TryAdd(session.SessionKey, session))
            {
                InfoEvent.CowbotEvent.Message = string.Format("New session [{0}].", session);
                try
                {
                    await session.Start();
                }
                catch (Exception ex)
                when (ex is TimeoutException || ex is WebSocketException)
                {
                    InfoEvent.CowbotEvent.Message = string.Format(ex.Message, ex);
                }
                finally
                {
                    AsyncWebSocketSession throwAway;
                    if (_sessions.TryRemove(session.SessionKey, out throwAway))
                    {
                        InfoEvent.CowbotEvent.Message = string.Format("Close session [{0}].", throwAway);
                    }
                }
            }
        }

        private bool ShouldThrow(Exception ex)
        {
            if (ex is ObjectDisposedException
                || ex is InvalidOperationException
                || ex is SocketException
                || ex is IOException)
            {
                return false;
            }
            return true;
        }

        #endregion Server

        #region Send

        public async Task SendTextToAsync(string sessionKey, string text)
        {
            AsyncWebSocketSession sessionFound;
            if (_sessions.TryGetValue(sessionKey, out sessionFound))
            {
                await sessionFound.SendTextAsync(text);
            }
            else
            {
                InfoEvent.CowbotEvent.Message = string.Format("Cannot find session [{0}].", sessionKey);
            }
        }

        public async Task SendTextToAsync(AsyncWebSocketSession session, string text)
        {
            AsyncWebSocketSession sessionFound;
            if (_sessions.TryGetValue(session.SessionKey, out sessionFound))
            {
                await sessionFound.SendTextAsync(text);
            }
            else
            {
                InfoEvent.CowbotEvent.Message = string.Format("Send text data but cannot find session [{0}].", session);
            }
        }

        public async Task SendBinaryToAsync(string sessionKey, byte[] data)
        {
            await SendBinaryToAsync(sessionKey, data, 0, data.Length);
        }

        public async Task SendBinaryToAsync(string sessionKey, byte[] data, int offset, int count)
        {
            AsyncWebSocketSession sessionFound;
            if (_sessions.TryGetValue(sessionKey, out sessionFound))
            {
                await sessionFound.SendBinaryAsync(data, offset, count);
            }
            else
            {
                InfoEvent.CowbotEvent.Message = string.Format("Cannot find session [{0}].", sessionKey);
            }
        }

        public async Task SendBinaryToAsync(AsyncWebSocketSession session, byte[] data)
        {
            await SendBinaryToAsync(session, data, 0, data.Length);
        }

        public async Task SendBinaryToAsync(AsyncWebSocketSession session, byte[] data, int offset, int count)
        {
            AsyncWebSocketSession sessionFound;
            if (_sessions.TryGetValue(session.SessionKey, out sessionFound))
            {
                await sessionFound.SendBinaryAsync(data, offset, count);
            }
            else
            {
                InfoEvent.CowbotEvent.Message = string.Format("Send binary data but cannot find session [{0}].", session);
            }
        }

        public async Task BroadcastTextAsync(string text)
        {
            foreach (var session in _sessions.Values)
            {
                await session.SendTextAsync(text);
            }
        }

        public async Task BroadcastBinaryAsync(byte[] data)
        {
            await BroadcastBinaryAsync(data, 0, data.Length);
        }

        public async Task BroadcastBinaryAsync(byte[] data, int offset, int count)
        {
            foreach (var session in _sessions.Values)
            {
                await session.SendBinaryAsync(data, offset, count);
            }
        }

        #endregion Send

        #region Session

        public bool HasSession(string sessionKey)
        {
            return _sessions.ContainsKey(sessionKey);
        }

        public AsyncWebSocketSession GetSession(string sessionKey)
        {
            AsyncWebSocketSession session = null;
            _sessions.TryGetValue(sessionKey, out session);
            return session;
        }

        public async Task CloseSession(string sessionKey)
        {
            AsyncWebSocketSession session = null;
            if (_sessions.TryGetValue(sessionKey, out session))
            {
                await session.Close(WebSocketCloseCode.NormalClosure);
            }
        }

        #endregion Session
    }
}