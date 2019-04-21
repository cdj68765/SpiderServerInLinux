using System.IO;
using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using SpiderServerInLinux;

namespace Shadowsocks.Controller
{
    public enum ProxyMode
    {
        NoModify,
        Direct,
        Pac,
        Global,
    }

    public class ShadowsocksController
    {
        // controller: handle user actions manipulates UI interacts with low level logic

        private Listener _listener;
        private List<Listener> _port_map_listener;
        private PACServer _pacServer;
        private Configuration _config;
        public ServerTransferTotal _transfer;
        public IPRangeSet _rangeSet;
        private GFWListUpdater gfwListUpdater;
        private bool stopped = false;
        private bool firstRun = true;

        public class PathEventArgs : EventArgs
        {
            public string Path;
        }

        public event EventHandler ConfigChanged;

        public event EventHandler ToggleModeChanged;

        public event EventHandler ToggleRuleModeChanged;

        //public event EventHandler ShareOverLANStatusChanged;
        public event EventHandler ShowConfigFormEvent;

        // when user clicked Edit PAC, and PAC file has already created
        public event EventHandler<PathEventArgs> PACFileReadyToOpen;

        public event EventHandler<PathEventArgs> UserRuleFileReadyToOpen;

        public event EventHandler<GFWListUpdater.ResultEventArgs> UpdatePACFromGFWListCompleted;

        public event ErrorEventHandler UpdatePACFromGFWListError;

        public event ErrorEventHandler Errored;

        public ShadowsocksController()
        {
            bool CheckIfPortInUse(int port)
            {
                IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();
                foreach (IPEndPoint endPoint in ipEndPoints)
                {
                    if (endPoint.Port == port)
                    {
                        return true;
                    }
                }
                return false;
            }
            if (string.IsNullOrEmpty(Setting._GlobalSet.ssr_url))
            {
                Loger.Instance.ServerInfo("SSR", $"SSR地址为空");
                return;
            }
            _config = new Configuration();
            var _Random = new Random();
            IPEndPoint[] ipEndPoints = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            _config.localPort = ipEndPoints[_Random.Next(ipEndPoints.Length)].Port;
            while (true)
            {
                if (!CheckIfPortInUse(_config.localPort)) break;
                _config.localPort++;
            }
            Loger.Instance.ServerInfo("SSR", $"SSR控制器建立，内部端口{_config.localPort}");
            Setting.Socks5Point = _config.localPort;
            var Config = new Server(Setting._GlobalSet.ssr_url, "");
            if (!string.IsNullOrEmpty(Config.remarks)) Loger.Instance.ServerInfo("SSR", $"SSR服务器名称，{Config.remarks}");
            Loger.Instance.ServerInfo("SSR", $"SSR地址解析完毕，IP:{Config.server}");
            _config.configs.Add(Config);
            _transfer = ServerTransferTotal.Load();
            foreach (Server server in _config.configs)
            {
                if (_transfer.servers.ContainsKey(server.server))
                {
                    SSRSpeedInfo = (ServerTrans)_transfer.servers[server.server];
                    ServerSpeedLog log = new ServerSpeedLog(((ServerTrans)_transfer.servers[server.server]).totalUploadBytes, ((ServerTrans)_transfer.servers[server.server]).totalDownloadBytes);
                    server.SetServerSpeedLog(log);
                }
            }
            Reload();
            /* if (Setting.CheckOnline())
                 Loger.Instance.ServerInfo("SSR", $"SSR工作正常");
             else
             {
                 Loger.Instance.ServerInfo("SSR", $"SSR无法访问远程网络");
                 Stop();
                 Setting.SSR = null;
                 GC.Collect();
             }*/
        }

        public ServerTrans SSRSpeedInfo;

        public void Start()
        {
            if (!string.IsNullOrEmpty(Setting._GlobalSet.ssr_url))
            {
                Reload();
            }
        }

        protected void ReportError(Exception e)
        {
            if (Errored != null)
            {
                Errored(this, new ErrorEventArgs(e));
            }
        }

        public void ReloadIPRange()
        {
            _rangeSet = new IPRangeSet();
            _rangeSet.LoadChn();
            if (_config.proxyRuleMode == (int)ProxyRuleMode.BypassLanAndNotChina)
            {
                _rangeSet.Reverse();
            }
        }

        // always return copy
        public Configuration GetConfiguration()
        {
            return Configuration.Load();
        }

        public Configuration GetCurrentConfiguration()
        {
            return _config;
        }

        private int FindFirstMatchServer(Server server, List<Server> servers)
        {
            for (int i = 0; i < servers.Count; ++i)
            {
                if (server.isMatchServer(servers[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public void AppendConfiguration(Configuration mergeConfig, List<Server> servers)
        {
            if (servers != null)
            {
                for (int j = 0; j < servers.Count; ++j)
                {
                    if (FindFirstMatchServer(servers[j], mergeConfig.configs) == -1)
                    {
                        mergeConfig.configs.Add(servers[j]);
                    }
                }
            }
        }

        public List<Server> MergeConfiguration(Configuration mergeConfig, List<Server> servers)
        {
            List<Server> missingServers = new List<Server>();
            if (servers != null)
            {
                for (int j = 0; j < servers.Count; ++j)
                {
                    int i = FindFirstMatchServer(servers[j], mergeConfig.configs);
                    if (i != -1)
                    {
                        bool enable = servers[j].enable;
                        servers[j].CopyServer(mergeConfig.configs[i]);
                        servers[j].enable = enable;
                    }
                }
            }
            for (int i = 0; i < mergeConfig.configs.Count; ++i)
            {
                int j = FindFirstMatchServer(mergeConfig.configs[i], servers);
                if (j == -1)
                {
                    missingServers.Add(mergeConfig.configs[i]);
                }
            }
            return missingServers;
        }

        public bool SaveServersConfig(string config)
        {
            Configuration new_cfg = Configuration.Load(config);
            if (new_cfg != null)
            {
                SaveServersConfig(new_cfg);
                return true;
            }
            return false;
        }

        public void SaveServersConfig(Configuration config)
        {
            List<Server> missingServers = MergeConfiguration(_config, config.configs);
            _config.CopyFrom(config);
            foreach (Server s in missingServers)
            {
                s.GetConnections().CloseAll();
            }
            SelectServerIndex(_config.index);
        }

        public void SaveServersPortMap(Configuration config)
        {
            _config.portMap = config.portMap;
            SelectServerIndex(_config.index);
            _config.FlushPortMapCache();
        }

        public bool AddServerBySSURL(string ssURL, string force_group = null, bool toLast = false)
        {
            if (ssURL.StartsWith("ss://", StringComparison.OrdinalIgnoreCase) || ssURL.StartsWith("ssr://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var server = new Server(ssURL, force_group);
                    if (toLast)
                    {
                        _config.configs.Add(server);
                    }
                    else
                    {
                        int index = _config.index + 1;
                        if (index < 0 || index > _config.configs.Count)
                            index = _config.configs.Count;
                        _config.configs.Insert(index, server);
                    }
                    return true;
                }
                catch (Exception e)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void SelectServerIndex(int index)
        {
            _config.index = index;
        }

        public void Stop()
        {
            if (stopped)
            {
                return;
            }
            stopped = true;

            if (_port_map_listener != null)
            {
                foreach (Listener l in _port_map_listener)
                {
                    l.Stop();
                }
                _port_map_listener = null;
            }
            if (_listener != null)
            {
                _listener.Stop();
            }
        }

        public void ClearTransferTotal(string server_addr)
        {
            _transfer.Clear(server_addr);
            foreach (Server server in _config.configs)
            {
                if (server.server == server_addr)
                {
                    if (_transfer.servers.ContainsKey(server.server))
                    {
                        server.ServerSpeedLog().ClearTrans();
                    }
                }
            }
        }

        public void UpdatePACFromGFWList()
        {
            if (gfwListUpdater != null)
            {
                gfwListUpdater.UpdatePACFromGFWList(_config);
            }
        }

        public void UpdatePACFromOnlinePac(string url)
        {
            if (gfwListUpdater != null)
            {
                gfwListUpdater.UpdatePACFromGFWList(_config, url);
            }
        }

        protected void Reload()
        {
            if (_port_map_listener != null)
            {
                foreach (Listener l in _port_map_listener)
                {
                    l.Stop();
                }
                _port_map_listener = null;
            }
            // some logic in configuration updated the config when saving, we need to read it again
            //_config = MergeGetConfiguration(_config);
            _config.FlushPortMapCache();
            ReloadIPRange();

            HostMap hostMap = new HostMap();
            //hostMap.LoadHostFile();
            HostMap.Instance().Clear(hostMap);
            if (_pacServer == null)
            {
                _pacServer = new PACServer();
            }
            _pacServer.UpdateConfiguration(_config);
            if (gfwListUpdater == null)
            {
                gfwListUpdater = new GFWListUpdater();
                gfwListUpdater.UpdateCompleted += pacServer_PACUpdateCompleted;
                gfwListUpdater.Error += pacServer_PACUpdateError;
            }

            // don't put polipoRunner.Start() before pacServer.Stop() or bind will fail when
            // switching bind address from 0.0.0.0 to 127.0.0.1 though UseShellExecute is set to true
            // now http://stackoverflow.com/questions/10235093/socket-doesnt-close-after-application-exits-if-a-launched-process-is-open
            bool _firstRun = firstRun;
            for (int i = 1; i <= 5; ++i)
            {
                _firstRun = false;
                try
                {
                    if (_listener != null && !_listener.isConfigChange(_config))
                    {
                        Local local = new Local(_config, _transfer, _rangeSet);
                        _listener.GetServices()[0] = local;
                    }
                    else
                    {
                        if (_listener != null)
                        {
                            _listener.Stop();
                            _listener = null;
                        }
                        Local local = new Local(_config, _transfer, _rangeSet);
                        List<Listener.Service> services = new List<Listener.Service>();
                        services.Add(local);
                        services.Add(_pacServer);
                        services.Add(new APIServer(this, _config));
                        _listener = new Listener(services);
                        _listener.Start(_config, 0);
                    }
                    break;
                }
                catch (Exception e)
                {
                    // translate Microsoft language into human language i.e. An attempt was made to
                    // access a socket in a way forbidden by its access permissions => Port already
                    // in use
                    if (e is SocketException)
                    {
                        SocketException se = (SocketException)e;
                        if (se.SocketErrorCode == SocketError.AccessDenied)
                        {
                            e = new Exception("Port already in use" + string.Format(" {0}", _config.localPort), e);
                        }
                    }
                    if (!_firstRun)
                    {
                        ReportError(e);
                        break;
                    }
                    else
                    {
                        Thread.Sleep(1000 * i * i);
                    }
                    if (_listener != null)
                    {
                        _listener.Stop();
                        _listener = null;
                    }
                }
            }

            _port_map_listener = new List<Listener>();
            foreach (KeyValuePair<int, PortMapConfigCache> pair in _config.GetPortMapCache())
            {
                try
                {
                    Local local = new Local(_config, _transfer, _rangeSet);
                    List<Listener.Service> services = new List<Listener.Service>();
                    services.Add(local);
                    Listener listener = new Listener(services);
                    listener.Start(_config, pair.Key);
                    _port_map_listener.Add(listener);
                }
                catch (Exception e)
                {
                    // translate Microsoft language into human language i.e. An attempt was made to
                    // access a socket in a way forbidden by its access permissions => Port already
                    // in use
                    if (e is SocketException)
                    {
                        SocketException se = (SocketException)e;
                        if (se.SocketErrorCode == SocketError.AccessDenied)
                        {
                            e = new Exception("Port already in use" + string.Format(" {0}", pair.Key), e);
                        }
                    }
                    ReportError(e);
                }
            }

            ConfigChanged?.Invoke(this, new EventArgs());
        }

        private void pacServer_PACUpdateCompleted(object sender, GFWListUpdater.ResultEventArgs e)
        {
            if (UpdatePACFromGFWListCompleted != null)
                UpdatePACFromGFWListCompleted(sender, e);
        }

        private void pacServer_PACUpdateError(object sender, ErrorEventArgs e)
        {
            if (UpdatePACFromGFWListError != null)
                UpdatePACFromGFWListError(sender, e);
        }
    }
}