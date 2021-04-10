using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Shadowsocks.Controller.Service;
using Shadowsocks.Controller.Strategy;
using Shadowsocks.Model;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    public class ShadowsocksController
    {
        private readonly HttpClient httpClient;

        // controller: handle user actions manipulates UI interacts with low level logic

        #region Members definition

        private Thread _trafficThread;

        private Listener _listener;
        private PACServer _pacServer;
        private Configuration _config;
        private StrategyManager _strategyManager;
        private PrivoxyRunner privoxyRunner;
        private readonly ConcurrentDictionary<Server, Sip003Plugin> _pluginsByServer;

        private long _inboundCounter = 0;
        private long _outboundCounter = 0;
        public long InboundCounter => Interlocked.Read(ref _inboundCounter);
        public long OutboundCounter => Interlocked.Read(ref _outboundCounter);
        public Queue<TrafficPerSecond> trafficPerSecondQueue;

        private bool stopped = false;

        public class PathEventArgs : EventArgs
        {
            public string Path;
        }

        public class UpdatedEventArgs : EventArgs
        {
            public string OldVersion;
            public string NewVersion;
        }

        public class TrafficPerSecond
        {
            public long inboundCounter;
            public long outboundCounter;
            public long inboundIncreasement;
            public long outboundIncreasement;
        }

        public event EventHandler ConfigChanged;

        public event EventHandler EnableStatusChanged;

        public event EventHandler EnableGlobalChanged;

        public event EventHandler ShareOverLANStatusChanged;

        public event EventHandler VerboseLoggingStatusChanged;

        public event EventHandler ShowPluginOutputChanged;

        public event EventHandler TrafficChanged;

        // when user clicked Edit PAC, and PAC file has already created
        public event EventHandler<PathEventArgs> PACFileReadyToOpen;

        public event EventHandler<PathEventArgs> UserRuleFileReadyToOpen;

        public event ErrorEventHandler UpdatePACFromGeositeError;

        public event ErrorEventHandler Errored;

        // Invoked when controller.Start();
        public event EventHandler<UpdatedEventArgs> ProgramUpdated;

        #endregion Members definition

        public ShadowsocksController()
        {
            httpClient = new HttpClient();
            _config = Configuration.Load();
            Configuration.Process(ref _config);
            _strategyManager = new StrategyManager(this);
            _pluginsByServer = new ConcurrentDictionary<Server, Sip003Plugin>();
            StartTrafficStatistics(61);

            ProgramUpdated += (o, e) =>
            {
                // version update precedures
                if (e.OldVersion == "4.3.0.0" || e.OldVersion == "4.3.1.0")
                    _config.geositeDirectGroups.Add("private");
            };
        }

        private HashSet<string> SSUri = new HashSet<string>();

        public ShadowsocksController(string ssr_url)
        {
            //Load His
            _config = Configuration.Load();
            _config.localPort = 1089;
            httpClient = new HttpClient();
            var Hash = httpClient.GetStringAsync(ssr_url).Result;
            if (File.Exists(@"SSHis.txt"))
                SSUri = new HashSet<string>(File.ReadAllLines(@"SSHis.txt"));
            else
                SSUri = new HashSet<string>();

            foreach (var item in Encoding.UTF8.GetString(Convert.FromBase64String(Hash)).Split(Environment.NewLine.ToCharArray()))
            {
                if (!item.StartsWith("ss")) continue;
                //var Config = Server.ParseURL(item);
                //var Intp = Encryption.OpenSSL.GetCipherInfo(Config.method);
                //if (Intp != IntPtr.Zero)
                {
                    //_config.configs.Add(Config);
                    SSUri.Add(item);
                }
            }
            foreach (var item in SSUri.ToArray())
            {
                var Config = Server.ParseURL(item);
                var Intp = Encryption.OpenSSL.GetCipherInfo(Config.method);
                if (Intp != IntPtr.Zero)
                    _config.configs.Add(Config);
                else
                    SSUri.Remove(item);
            }
            //var RET = Encryption.OpenSSL.GetCipherInfo(Config.method);
            //Configuration.Process(ref _config);
            //_strategyManager = new StrategyManager(this);
            Reload();
        }

        #region Basic

        public void Start(bool systemWakeUp = false)
        {
            if (_config.firstRunOnNewVersion && !systemWakeUp)
            {
                ProgramUpdated.Invoke(this, new UpdatedEventArgs()
                {
                    OldVersion = _config.version,
                });
                // delete pac.txt when regeneratePacOnUpdate is true
                if (_config.regeneratePacOnUpdate)
                    try
                    {
                    }
                    catch (Exception e)
                    {
                    }
                // finish up first run of new version
                _config.firstRunOnNewVersion = false;
                Configuration.Save(_config);
            }
            Reload();
        }

        public void Stop()
        {
            if (stopped)
            {
                return;
            }
            stopped = true;
            if (_listener != null)
            {
                _listener.Stop();
            }
            StopPlugins();
            if (privoxyRunner != null)
            {
                privoxyRunner.Stop();
            }
            if (_config.enabled)
            {
            }
            Encryption.RNG.Close();
        }

        protected void Reload()
        {
            Encryption.RNG.Reload();
            // some logic in configuration updated the config when saving, we need to read it again
            //_config = Configuration.Load();
            Configuration.Process(ref _config);

            // set User-Agent for httpClient
            /*  try
              {
                  if (!string.IsNullOrWhiteSpace(_config.userAgentString))
                      httpClient.DefaultRequestHeaders.Add("User-Agent", _config.userAgentString);
              }
              catch
              {
                  // reset userAgent to default and reapply
                  Configuration.ResetUserAgent(_config);
                  httpClient.DefaultRequestHeaders.Add("User-Agent", _config.userAgentString);
              }*/

            //privoxyRunner = privoxyRunner ?? new PrivoxyRunner();

            _listener?.Stop();
            //StopPlugins();

            // don't put PrivoxyRunner.Start() before pacServer.Stop() or bind will fail when
            // switching bind address from 0.0.0.0 to 127.0.0.1 though UseShellExecute is set to
            // true now http://stackoverflow.com/questions/10235093/socket-doesnt-close-after-application-exits-if-a-launched-process-is-open
            //privoxyRunner.Stop();
            try
            {
                var strategy = GetCurrentStrategy();
                strategy?.ReloadServers();

                //StartPlugin();
                //privoxyRunner.Start(_config);

                TCPRelay tcpRelay = new TCPRelay(this, _config);
                tcpRelay.OnInbound += UpdateInboundCounter;
                tcpRelay.OnOutbound += UpdateOutboundCounter;
                tcpRelay.OnFailed += (o, e) => GetCurrentStrategy()?.SetFailure(e.server);

                UDPRelay udpRelay = new UDPRelay(this);
                List<Listener.IService> services = new List<Listener.IService>
                {
                    tcpRelay,
                    udpRelay,
                    _pacServer,
                };
                _listener = new Listener(services);
                _listener.Start(_config);
            }
            catch (Exception e)
            {
                // translate Microsoft language into human language i.e. An attempt was made to
                // access a socket in a way forbidden by its access permissions => Port already in use
                if (e is SocketException se)
                {
                    if (se.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    {
                        e = new Exception(I18N.GetString("Port {0} already in use", _config.localPort), e);
                    }
                    else if (se.SocketErrorCode == SocketError.AccessDenied)
                    {
                        e = new Exception(I18N.GetString("Port {0} is reserved by system", _config.localPort), e);
                    }
                }
                ReportError(e);
            }

            ConfigChanged?.Invoke(this, new EventArgs());
        }

        protected void SaveConfig(Configuration newConfig)
        {
            //Configuration.Save(newConfig);
            Reload();
        }

        protected void ReportError(Exception e)
        {
            Errored?.Invoke(this, new ErrorEventArgs(e));
        }

        public HttpClient GetHttpClient() => httpClient;

        public Server GetCurrentServer() => _config.GetCurrentServer();

        public Configuration GetCurrentConfiguration() => _config;

        public Server GetAServer(IStrategyCallerType type, IPEndPoint localIPEndPoint, EndPoint destEndPoint)
        {
            IStrategy strategy = GetCurrentStrategy();
            if (strategy != null)
            {
                return strategy.GetAServer(type, localIPEndPoint, destEndPoint);
            }
            if (_config.index < 0)
            {
                _config.index = 0;
            }
            return GetCurrentServer();
        }

        public void SaveServers(List<Server> servers, int localPort, bool portableMode)
        {
            _config.configs = servers;
            _config.localPort = localPort;
            _config.portableMode = portableMode;
            Configuration.Save(_config);
        }

        public void SelectServerIndex(int index)
        {
            _config.index = index;
            _config.strategy = null;
            SaveConfig(_config);
        }

        public void ToggleShareOverLAN(bool enabled)
        {
            _config.shareOverLan = enabled;
            SaveConfig(_config);

            ShareOverLANStatusChanged?.Invoke(this, new EventArgs());
        }

        #endregion Basic

        #region OS Proxy

        public void ToggleEnable(bool enabled)
        {
            _config.enabled = enabled;
            SaveConfig(_config);

            EnableStatusChanged?.Invoke(this, new EventArgs());
        }

        public void ToggleGlobal(bool global)
        {
            _config.global = global;
            SaveConfig(_config);

            EnableGlobalChanged?.Invoke(this, new EventArgs());
        }

        public void SaveProxy(ForwardProxyConfig proxyConfig)
        {
            _config.proxy = proxyConfig;
            SaveConfig(_config);
        }

        #endregion OS Proxy

        #region PAC

        private void PacServer_PACUpdateError(object sender, ErrorEventArgs e)
        {
            UpdatePACFromGeositeError?.Invoke(this, e);
        }

        private static readonly IEnumerable<char> IgnoredLineBegins = new[] { '!', '[' };

        public void SavePACUrl(string pacUrl)
        {
            _config.pacUrl = pacUrl;
            SaveConfig(_config);

            ConfigChanged?.Invoke(this, new EventArgs());
        }

        public void UseOnlinePAC(bool useOnlinePac)
        {
            _config.useOnlinePac = useOnlinePac;
            SaveConfig(_config);

            ConfigChanged?.Invoke(this, new EventArgs());
        }

        public void ToggleSecureLocalPac(bool enabled)
        {
            _config.secureLocalPac = enabled;
            SaveConfig(_config);

            ConfigChanged?.Invoke(this, new EventArgs());
        }

        public void ToggleRegeneratePacOnUpdate(bool enabled)
        {
            _config.regeneratePacOnUpdate = enabled;
            SaveConfig(_config);
            ConfigChanged?.Invoke(this, new EventArgs());
        }

        #endregion PAC

        #region SIP002

        public bool AddServerBySSURL(string ssURL)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ssURL))
                    return false;

                var servers = Server.GetServers(ssURL);
                if (servers == null || servers.Count == 0)
                    return false;

                foreach (var server in servers)
                {
                    _config.configs.Add(server);
                    if (server.warnLegacyUrl) ;
                }
                _config.index = _config.configs.Count - 1;
                SaveConfig(_config);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public string GetServerURLForCurrentServer()
        {
            return GetCurrentServer().GetURL(_config.generateLegacyUrl);
        }

        #endregion SIP002

        #region Misc

        public void ToggleVerboseLogging(bool enabled)
        {
            _config.isVerboseLogging = enabled;
            SaveConfig(_config);

            VerboseLoggingStatusChanged?.Invoke(this, new EventArgs());
        }

        public void ToggleCheckingUpdate(bool enabled)
        {
            _config.autoCheckUpdate = enabled;
            Configuration.Save(_config);

            ConfigChanged?.Invoke(this, new EventArgs());
        }

        public void ToggleCheckingPreRelease(bool enabled)
        {
            _config.checkPreRelease = enabled;
            Configuration.Save(_config);
            ConfigChanged?.Invoke(this, new EventArgs());
        }

        public void SaveSkippedUpdateVerion(string version)
        {
            _config.skippedUpdateVersion = version;
            Configuration.Save(_config);
        }

        #endregion Misc

        #region Strategy

        public void SelectStrategy(string strategyID)
        {
            _config.index = -1;
            _config.strategy = strategyID;
            SaveConfig(_config);
        }

        public IList<IStrategy> GetStrategies()
        {
            return _strategyManager.GetStrategies();
        }

        public IStrategy GetCurrentStrategy()
        {
            foreach (var strategy in _strategyManager.GetStrategies())
            {
                if (strategy.ID == _config.strategy)
                {
                    return strategy;
                }
            }
            return null;
        }

        public void UpdateInboundCounter(object sender, SSTransmitEventArgs args)
        {
            GetCurrentStrategy()?.UpdateLastRead(args.server);
            Interlocked.Add(ref _inboundCounter, args.length);
        }

        public void UpdateOutboundCounter(object sender, SSTransmitEventArgs args)
        {
            GetCurrentStrategy()?.UpdateLastWrite(args.server);
            Interlocked.Add(ref _outboundCounter, args.length);
        }

        #endregion Strategy

        #region SIP003

        private void StartPlugin()
        {
            var server = _config.GetCurrentServer();
            GetPluginLocalEndPointIfConfigured(server);
        }

        private void StopPlugins()
        {
            foreach (var serverAndPlugin in _pluginsByServer)
            {
                serverAndPlugin.Value?.Dispose();
            }
            _pluginsByServer.Clear();
        }

        public EndPoint GetPluginLocalEndPointIfConfigured(Server server)
        {
            return null;
        }

        public void ToggleShowPluginOutput(bool enabled)
        {
            _config.showPluginOutput = enabled;
            SaveConfig(_config);

            ShowPluginOutputChanged?.Invoke(this, new EventArgs());
        }

        #endregion SIP003

        #region Traffic Statistics

        private void StartTrafficStatistics(int queueMaxSize)
        {
            trafficPerSecondQueue = new Queue<TrafficPerSecond>();
            for (int i = 0; i < queueMaxSize; i++)
            {
                trafficPerSecondQueue.Enqueue(new TrafficPerSecond());
            }
            _trafficThread = new Thread(new ThreadStart(() => TrafficStatistics(queueMaxSize)))
            {
                IsBackground = true
            };
            _trafficThread.Start();
        }

        private void TrafficStatistics(int queueMaxSize)
        {
            TrafficPerSecond previous, current;
            while (true)
            {
                previous = trafficPerSecondQueue.Last();
                current = new TrafficPerSecond
                {
                    inboundCounter = InboundCounter,
                    outboundCounter = OutboundCounter
                };
                current.inboundIncreasement = current.inboundCounter - previous.inboundCounter;
                current.outboundIncreasement = current.outboundCounter - previous.outboundCounter;

                trafficPerSecondQueue.Enqueue(current);
                if (trafficPerSecondQueue.Count > queueMaxSize)
                    trafficPerSecondQueue.Dequeue();

                TrafficChanged?.Invoke(this, new EventArgs());

                Thread.Sleep(1000);
            }
        }

        #endregion Traffic Statistics

        #region SIP008

        public async Task<bool> UpdateOnlineConfig(string url)
        {
            var selected = GetCurrentServer();
            try
            {
            }
            catch (Exception e)
            {
                return false;
            }
            _config.index = _config.configs.IndexOf(selected);
            SaveConfig(_config);
            return true;
        }

        public void SaveOnlineConfigSource(List<string> sources)
        {
            _config.onlineConfigSource = sources;
            SaveConfig(_config);
        }

        public void RemoveOnlineConfig(string url)
        {
            _config.onlineConfigSource.RemoveAll(v => v == url);
            _config.configs = Configuration.SortByOnlineConfig(
                _config.configs.Where(c => c.group != url)
                );
            SaveConfig(_config);
        }

        #endregion SIP008
    }
}