using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace RasPiVPNChange
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var hash =
"dm1lc3M6Ly9ldzBLSUNBaWRpSTZJQ0l5SWl3TkNpQWdJbkJ6SWpvZ0l1UzZqT2VJdCtlL3UrV2ltZWU5a1doMGRIQnpPaTh2TVRnd09DNW5ZU0lzRFFvZ0lDSmhaR1FpT2lBaU1UTTVMalU1TGpFeU1pNHlOQ0lzRFFvZ0lDSndiM0owSWpvZ0lqTTBOekUxSWl3TkNpQWdJbWxrSWpvZ0lqSTRaV1ZsWXpFekxXUmlPREF0TkdOaU9TMDVaRE13TFRBMk1HWTBNalUzWkRjeU9TSXNEUW9nSUNKaGFXUWlPaUFpTUNJc0RRb2dJQ0p6WTNraU9pQWlZV1Z6TFRFeU9DMW5ZMjBpTEEwS0lDQWlibVYwSWpvZ0luZHpJaXdOQ2lBZ0luUjVjR1VpT2lBaWJtOXVaU0lzRFFvZ0lDSm9iM04wSWpvZ0lpSXNEUW9nSUNKd1lYUm9Jam9nSWk4aUxBMEtJQ0FpZEd4eklqb2dJbTV2Ym1VaUxBMEtJQ0FpYzI1cElqb2dJaUlOQ24wPQ0K";

            var Ret = Opera.ImportFromClipboardConfig(Encoding.UTF8.GetString(Convert.FromBase64String(hash)));
            var Read = JsonSerializer.Deserialize<Vmess>(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"RasPiVPNChange.config.json")).ReadToEnd());
            var options = new JsonSerializerOptions();
            options.WriteIndented = true;
            options.IgnoreNullValues = true;

            #region ss

            //var hash = "c3M6Ly9ZV1Z6TFRJMU5pMW5ZMjA2Wm1GQ1FXOUVOVFJyT0RkVlNrYzNRREUyTnk0NE9DNDJNeTR4TVRveU16YzEjJWUzJTgwJTkwJWU1JWIwJThmJWU2JWIxJTkwJWU2JTkwJWFjJWU4JWJmJTkwMDIlZTMlODAlOTElZTUlODclYmElZTclYjIlYmUlZTUlODclODYlZTglOGYlYTAlZTglOGYlOWMlZTQlYmQlOTMlZTglODIlYjIlZTclYjIlODkrJWU3JTk0JWI1JWU2JThhJWE1VEclNDBjeXE5OTkNCg==";
            //var server = ParseURL(Encoding.UTF8.GetString(Convert.FromBase64String(hash)));
            //Read.outbounds[0].tag = "proxy";
            //Read.outbounds[0].protocol = "shadowsocks";
            //Read.outbounds[0].settings.servers = null;
            //Read.outbounds[0].settings.vnext = null;
            //Read.outbounds[0].settings.servers = new List<Vmess.OutboundsItem.Settings.Servers>()
            //{
            //    new Vmess.OutboundsItem.Settings.Servers()
            //    {
            //        address = server.server,
            //        port = server.server_port,
            //        ota = false,
            //        password = server.password,
            //        level = 1,
            //        method = server.method
            //    }
            //};
            //Read.outbounds[0].streamSettings = new Vmess.OutboundsItem.StreamSettings()
            //{
            //    network = "tcp"
            //};

            #endregion ss

            File.WriteAllText("config2.json", JsonSerializer.Serialize<Vmess>(Read, options));
        }

        [Serializable]
        public class Server
        {
            public const string DefaultMethod = "chacha20-ietf-poly1305";
            public const int DefaultPort = 8388;

            #region ParseLegacyURL

            private static readonly Regex UrlFinder = new Regex(@"ss://(?<base64>[A-Za-z0-9+-/=_]+)(?:#(?<tag>\S+))?", RegexOptions.IgnoreCase);
            private static readonly Regex DetailsParser = new Regex(@"^((?<method>.+?):(?<password>.*)@(?<hostname>.+?):(?<port>\d+?))$", RegexOptions.IgnoreCase);

            #endregion ParseLegacyURL

            private const int DefaultServerTimeoutSec = 5;
            public const int MaxServerTimeoutSec = 20;

            public string server;
            public int server_port;
            public string password;
            public string method;

            // optional fields
            [DefaultValue("")]
            public string plugin;

            [DefaultValue("")]
            public string plugin_opts;

            [DefaultValue("")]
            public string plugin_args;

            [DefaultValue("")]
            public string remarks;

            [DefaultValue("")]
            public string group;

            public int timeout;

            // Set to true when imported from a legacy ss:// URL.
            public bool warnLegacyUrl;

            public override int GetHashCode()
            {
                return server.GetHashCode() ^ server_port;
            }

            public override bool Equals(object obj) => obj is Server o2 && server == o2.server && server_port == o2.server_port;

            private Dictionary<string, string> _strings = new Dictionary<string, string>();

            public override string ToString()
            {
                if (string.IsNullOrEmpty(server))
                {
                    return GetString("New server");
                }
                string GetString(string key, params object[] args)
                {
                    return string.Format(_strings.TryGetValue(key.Trim(), out var value) ? value : key, args);
                }
                string serverStr = $"{FormalHostName}:{server_port}";
                return string.IsNullOrEmpty(remarks)
                    ? serverStr
                    : $"{remarks} ({serverStr})";
            }

            public string GetURL(bool legacyUrl = false)
            {
                string tag = string.Empty;
                string url = string.Empty;

                if (legacyUrl && string.IsNullOrWhiteSpace(plugin))
                {
                    // For backwards compatiblity, if no plugin, use old url format
                    string parts = $"{method}:{password}@{server}:{server_port}";
                    string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(parts));
                    url = base64;
                }
                else
                {
                    // SIP002
                    string parts = $"{method}:{password}";
                    string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(parts));
                    string websafeBase64 = base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');

                    url = string.Format(
                        "{0}@{1}:{2}/",
                        websafeBase64,
                        FormalHostName,
                        server_port
                        );

                    if (!string.IsNullOrWhiteSpace(plugin))
                    {
                        string pluginPart = plugin;
                        if (!string.IsNullOrWhiteSpace(plugin_opts))
                        {
                            pluginPart += ";" + plugin_opts;
                        }
                        string pluginQuery = "?plugin=" + HttpUtility.UrlEncode(pluginPart, Encoding.UTF8);
                        url += pluginQuery;
                    }
                }

                if (!string.IsNullOrEmpty(remarks))
                {
                    tag = $"#{HttpUtility.UrlEncode(remarks, Encoding.UTF8)}";
                }
                return $"ss://{url}{tag}";
            }

            public string FormalHostName
            {
                get
                {
                    // CheckHostName() won't do a real DNS lookup
                    switch (Uri.CheckHostName(server))
                    {
                        case UriHostNameType.IPv6:  // Add square bracket when IPv6 (RFC3986)
                            return $"[{server}]";

                        default:    // IPv4 or domain name
                            return server;
                    }
                }
            }

            public Server()
            {
                server = "";
                server_port = DefaultPort;
                method = DefaultMethod;
                plugin = "";
                plugin_opts = "";
                plugin_args = "";
                password = "";
                remarks = "";
                timeout = DefaultServerTimeoutSec;
            }

            private static Server ParseLegacyURL(string ssURL)
            {
                var match = UrlFinder.Match(ssURL);
                if (!match.Success)
                    return null;

                Server server = new Server();
                var base64 = match.Groups["base64"].Value.TrimEnd('/');
                var tag = match.Groups["tag"].Value;
                if (!string.IsNullOrEmpty(tag))
                {
                    server.remarks = HttpUtility.UrlDecode(tag, Encoding.UTF8);
                }
                Match details = null;
                try
                {
                    details = DetailsParser.Match(Encoding.UTF8.GetString(Convert.FromBase64String(
                    base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='))));
                }
                catch (FormatException)
                {
                    return null;
                }
                if (!details.Success)
                    return null;
                server.method = details.Groups["method"].Value;
                server.password = details.Groups["password"].Value;
                server.server = details.Groups["hostname"].Value;
                server.server_port = int.Parse(details.Groups["port"].Value);
                server.warnLegacyUrl = true;
                return server;
            }

            public static Server ParseURL(string serverUrl)
            {
                string _serverUrl = serverUrl.Trim();
                if (!_serverUrl.StartsWith("ss://", StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                Server legacyServer = ParseLegacyURL(serverUrl);
                if (legacyServer != null)   //legacy
                {
                    return legacyServer;
                }
                else   //SIP002
                {
                    Uri parsedUrl;
                    try
                    {
                        parsedUrl = new Uri(serverUrl);
                    }
                    catch (UriFormatException)
                    {
                        return null;
                    }
                    Server server = new Server
                    {
                        remarks = HttpUtility.UrlDecode(parsedUrl.GetComponents(
                            UriComponents.Fragment, UriFormat.Unescaped), Encoding.UTF8),
                        server = parsedUrl.IdnHost,
                        server_port = parsedUrl.Port,
                    };

                    // parse base64 UserInfo
                    string rawUserInfo = parsedUrl.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped);
                    string base64 = rawUserInfo.Replace('-', '+').Replace('_', '/');    // Web-safe base64 to normal base64
                    string userInfo = "";
                    try
                    {
                        userInfo = Encoding.UTF8.GetString(Convert.FromBase64String(
                        base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=')));
                    }
                    catch (FormatException)
                    {
                        return null;
                    }
                    string[] userInfoParts = userInfo.Split(new char[] { ':' }, 2);
                    if (userInfoParts.Length != 2)
                    {
                        return null;
                    }
                    server.method = userInfoParts[0];
                    server.password = userInfoParts[1];

                    NameValueCollection queryParameters = HttpUtility.ParseQueryString(parsedUrl.Query);
                    string[] pluginParts = (queryParameters["plugin"] ?? "").Split(new[] { ';' }, 2);
                    if (pluginParts.Length > 0)
                    {
                        server.plugin = pluginParts[0] ?? "";
                    }

                    if (pluginParts.Length > 1)
                    {
                        server.plugin_opts = pluginParts[1] ?? "";
                    }

                    return server;
                }
            }

            public static List<Server> GetServers(string ssURL)
            {
                return ssURL
                    .Split('\r', '\n', ' ')
                    .Select(u => ParseURL(u))
                    .Where(s => s != null)
                    .ToList();
            }

            public string Identifier()
            {
                return server + ':' + server_port;
            }
        }

        public static Server ParseURL(string serverUrl)
        {
            string _serverUrl = serverUrl.Trim();
            if (!_serverUrl.StartsWith("ss://", StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            Server legacyServer = ParseLegacyURL(serverUrl);
            if (legacyServer != null)   //legacy
            {
                return legacyServer;
            }
            else   //SIP002
            {
                Uri parsedUrl;
                try
                {
                    parsedUrl = new Uri(serverUrl);
                }
                catch (UriFormatException)
                {
                    return null;
                }
                Server server = new Server
                {
                    remarks = HttpUtility.UrlDecode(parsedUrl.GetComponents(
                        UriComponents.Fragment, UriFormat.Unescaped), Encoding.UTF8),
                    server = parsedUrl.IdnHost,
                    server_port = parsedUrl.Port,
                };

                // parse base64 UserInfo
                string rawUserInfo = parsedUrl.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped);
                string base64 = rawUserInfo.Replace('-', '+').Replace('_', '/');    // Web-safe base64 to normal base64
                string userInfo = "";
                try
                {
                    userInfo = Encoding.UTF8.GetString(Convert.FromBase64String(
                    base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=')));
                }
                catch (FormatException)
                {
                    return null;
                }
                string[] userInfoParts = userInfo.Split(new char[] { ':' }, 2);
                if (userInfoParts.Length != 2)
                {
                    return null;
                }
                server.method = userInfoParts[0];
                server.password = userInfoParts[1];

                NameValueCollection queryParameters = HttpUtility.ParseQueryString(parsedUrl.Query);
                string[] pluginParts = (queryParameters["plugin"] ?? "").Split(new[] { ';' }, 2);
                if (pluginParts.Length > 0)
                {
                    server.plugin = pluginParts[0] ?? "";
                }

                if (pluginParts.Length > 1)
                {
                    server.plugin_opts = pluginParts[1] ?? "";
                }

                return server;
            }
        }

        private static Server ParseLegacyURL(string ssURL)
        {
            Regex UrlFinder = new Regex(@"ss://(?<base64>[A-Za-z0-9+-/=_]+)(?:#(?<tag>\S+))?", RegexOptions.IgnoreCase);
            Regex DetailsParser = new Regex(@"^((?<method>.+?):(?<password>.*)@(?<hostname>.+?):(?<port>\d+?))$", RegexOptions.IgnoreCase);
            var match = UrlFinder.Match(ssURL);
            if (!match.Success)
                return null;

            Server server = new Server();
            var base64 = match.Groups["base64"].Value.TrimEnd('/');
            var tag = match.Groups["tag"].Value;
            if (!string.IsNullOrEmpty(tag))
            {
                server.remarks = HttpUtility.UrlDecode(tag, Encoding.UTF8);
            }
            Match details = null;
            try
            {
                details = DetailsParser.Match(Encoding.UTF8.GetString(Convert.FromBase64String(
                base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='))));
            }
            catch (FormatException)
            {
                return null;
            }
            if (!details.Success)
                return null;
            server.method = details.Groups["method"].Value;
            server.password = details.Groups["password"].Value;
            server.server = details.Groups["hostname"].Value;
            server.server_port = int.Parse(details.Groups["port"].Value);
            server.warnLegacyUrl = true;
            return server;
        }

        internal class Vmess
        {
            public Log log { get; set; }
            public List<InboundsItem> inbounds { get; set; }
            public List<OutboundsItem> outbounds { get; set; }

            public Dns dns { get; set; }

            public Routing routing { get; set; }

            public class Log
            {
                public string access { get; set; }

                public string error { get; set; }

                public string loglevel { get; set; }
            }

            public class InboundsItem
            {
                public int port { get; set; }

                public string protocol { get; set; }

                public Settings settings { get; set; }

                public Sniffing sniffing { get; set; }

                public StreamSettings streamSettings { get; set; }

                public class StreamSettings
                {
                    public Sockopt sockopt { get; set; }

                    public class Sockopt
                    {
                        public string tproxy { get; set; }
                    }
                }

                public string tag { get; set; }

                public class Settings
                {
                    public string auth { get; set; }
                    public string network { get; set; }
                    public bool followRedirect { get; set; }
                }

                public class Sniffing
                {
                    public bool enabled { get; set; }

                    public List<string> destOverride { get; set; }
                }
            }

            public class OutboundsItem
            {
                public Mux mux { get; set; }

                public string protocol { get; set; }

                public Settings settings { get; set; }

                public StreamSettings streamSettings { get; set; }

                public string tag { get; set; }

                public class Mux
                {
                    public bool enabled { get; set; }
                    public object concurrency { get; set; }
                }

                public class Settings
                {
                    public List<VnextItem> vnext { get; set; }
                    public List<Servers> servers { get; set; }

                    public string domainStrategy { get; set; }
                    public wresponse response { get; set; }

                    public class wresponse
                    {
                        public string type { get; set; }
                    }

                    public class VnextItem
                    {
                        public string address { get; set; }

                        public int port { get; set; }

                        public List<UsersItem> users { get; set; }

                        public class UsersItem
                        {
                            public string id { get; set; }

                            public int alterId { get; set; }

                            public string security { get; set; }
                            public string email { get; set; }
                            public string encryption { get; set; }
                            public string flow { get; set; }

                            public int level { get; set; }
                        }
                    }

                    public class Servers
                    {
                        public string address { get; set; }

                        public string method { get; set; }

                        public bool ota { get; set; }

                        public string password { get; set; }

                        public int port { get; set; }

                        public int level { get; set; }
                    }
                }

                public class StreamSettings
                {
                    public string network { get; set; }

                    public string security { get; set; }

                    public WsSettings wsSettings { get; set; }

                    public class WsSettings
                    {
                        public string path { get; set; }

                        public Headers headers { get; set; }

                        public class Headers
                        {
                            public string Host { get; set; }
                        }
                    }

                    public TlsSettings tlsSettings { get; set; }

                    public class TlsSettings
                    {
                        public string serverName { get; set; }

                        public bool allowInsecure { get; set; }
                    }

                    public Sockopt sockopt { get; set; }

                    public class Sockopt
                    {
                        public int mark { get; set; }
                    }
                }
            }

            public class Dns
            {
                public List<object> servers { get; set; }
            }

            public class Routing
            {
                public string domainStrategy { get; set; }

                public List<RulesItem> rules { get; set; }

                public class RulesItem
                {
                    public string type { get; set; }

                    public List<string> inboundTag { get; set; }

                    public List<string> protocol { get; set; }
                    public List<string> ip { get; set; }
                    public List<string> domain { get; set; }
                    public object port { get; set; }

                    public string network { get; set; }

                    public string outboundTag { get; set; }
                }
            }
        }
    }

    internal class Opera
    {
        public static VmessItem ImportFromClipboardConfig(string text)
        {
            VmessItem vmessItem = new VmessItem();
            try
            {
                if (text.StartsWith("vmess://"))
                {
                    if (text.IndexOf("?") > 0)
                    {
                        vmessItem = (ResolveStdVmess(text) ?? ResolveVmess4Kitsunebi(text));
                    }
                    else
                    {
                        vmessItem.configType = 1;
                        text = text.Substring("vmess://".Length);
                        text = Base64Decode(text);
                        VmessQRCode vmessQRCode = JsonSerializer.Deserialize<VmessQRCode>(text);

                        vmessItem.network = "tcp";
                        vmessItem.headerType = "none";
                        vmessItem.configVersion = vmessQRCode.v.ExToInt();
                        vmessItem.remarks = vmessQRCode.ps.ExToString();
                        vmessItem.address = vmessQRCode.add.ExToString();
                        vmessItem.port = (vmessQRCode.port).ExToInt();
                        vmessItem.id = vmessQRCode.id.ExToString();
                        vmessItem.alterId = vmessQRCode.aid.ExToInt();
                        vmessItem.security = vmessQRCode.scy.ExToString();
                        if (!string.IsNullOrEmpty(vmessQRCode.scy))
                        {
                            vmessItem.security = vmessQRCode.scy;
                        }
                        else
                        {
                            vmessItem.security = "auto";
                        }
                        if (!string.IsNullOrEmpty(vmessQRCode.net))
                        {
                            vmessItem.network = vmessQRCode.net;
                        }
                        if (!string.IsNullOrEmpty(vmessQRCode.type))
                        {
                            vmessItem.headerType = vmessQRCode.type;
                        }
                        vmessItem.requestHost = vmessQRCode.host;
                        vmessItem.path = vmessQRCode.path;
                        vmessItem.streamSecurity = vmessQRCode.tls;
                        vmessItem.sni = vmessQRCode.sni;
                    }
                    UpgradeServerVersion(ref vmessItem);
                }
                else if (text.StartsWith("ss://"))
                {
                    vmessItem = ResolveSSLegacy(text);
                    if (vmessItem == null)
                    {
                        vmessItem = ResolveSip002(text);
                        VmessItem ResolveSip002(string result)
                        {
                            Uri uri;
                            try
                            {
                                uri = new Uri(result);
                            }
                            catch (UriFormatException)
                            {
                                return null;
                            }
                            VmessItem vmessItem = new VmessItem
                            {
                                remarks = uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
                                address = uri.IdnHost,
                                port = uri.Port
                            };
                            string text = uri.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped).Replace('-', '+').Replace('_', '/');
                            string @string;
                            try
                            {
                                @string = Encoding.UTF8.GetString(Convert.FromBase64String(text.PadRight(text.Length + (4 - text.Length % 4) % 4, '=')));
                            }
                            catch (FormatException)
                            {
                                return null;
                            }
                            string[] array = @string.Split(new char[]
                            {
                                ':'
                            }, 2);
                            if (array.Length != 2)
                            {
                                return null;
                            }
                            vmessItem.security = array[0];
                            vmessItem.id = array[1];
                            if (HttpUtility.ParseQueryString(uri.Query)["plugin"] != null)
                            {
                                return null;
                            }
                            return vmessItem;
                        }
                    }
                    if (vmessItem == null)
                    {
                        return null;
                    }
                    if (vmessItem.address.Length == 0 || vmessItem.port == 0 || vmessItem.security.Length == 0 || vmessItem.id.Length == 0)
                    {
                        return null;
                    }
                    vmessItem.configType = 3;
                }
                else if (text.StartsWith("socks://"))
                {
                    vmessItem.configType = 4;
                    text = text.Substring("socks://".Length);
                    int num = text.IndexOf("#");
                    if (num > 0)
                    {
                        try
                        {
                            vmessItem.remarks = HttpUtility.UrlDecode(text.Substring(num + 1, text.Length - num - 1));
                        }
                        catch
                        {
                        }
                        text = text.Substring(0, num);
                    }
                    if (text.IndexOf("@") <= 0)
                    {
                        text = Base64Decode(text);
                    }
                    string[] array = text.Split(new char[]
                    {
                        '@'
                    });
                    if (array.Length != 2)
                    {
                        return null;
                    }
                    string[] array2 = array[0].Split(new char[]
                    {
                        ':'
                    });
                    int num2 = array[1].LastIndexOf(":");
                    if (array2.Length != 2 || num2 < 0)
                    {
                        return null;
                    }
                    vmessItem.address = array[1].Substring(0, num2);
                    vmessItem.port = array[1].Substring(num2 + 1, array[1].Length - (num2 + 1)).ExToInt();
                    vmessItem.security = array2[0];
                    vmessItem.id = array2[1];
                }
                else if (text.StartsWith("trojan://"))
                {
                    vmessItem.configType = 6;
                    Uri uri = new Uri(text);
                    vmessItem.address = uri.IdnHost;
                    vmessItem.port = uri.Port;
                    vmessItem.id = uri.UserInfo;
                    NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(uri.Query);
                    vmessItem.sni = (nameValueCollection["sni"] ?? "");
                    string text2 = uri.Fragment.Replace("#", "");
                    if (string.IsNullOrEmpty(text2))
                    {
                        vmessItem.remarks = "NONE";
                    }
                    else
                    {
                        vmessItem.remarks = HttpUtility.UrlDecode(text2);
                    }
                }
                else if (text.StartsWith("vless://"))
                {
                    vmessItem = ResolveStdVLESS(text);
                    UpgradeServerVersion(ref vmessItem);
                }
            }
            catch
            {
                return null;
            }
            return vmessItem;
        }

        private static VmessItem ResolveStdVLESS(string result)
        {
            VmessItem vmessItem = new VmessItem
            {
                configType = 5,
                security = "none"
            };
            Uri uri = new Uri(result);
            vmessItem.address = uri.IdnHost;
            vmessItem.port = uri.Port;
            vmessItem.remarks = uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
            vmessItem.id = uri.UserInfo;
            NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(uri.Query);
            vmessItem.flow = (nameValueCollection["flow"] ?? "");
            vmessItem.security = (nameValueCollection["encryption"] ?? "none");
            vmessItem.streamSecurity = (nameValueCollection["security"] ?? "");
            vmessItem.sni = (nameValueCollection["sni"] ?? "");
            vmessItem.network = (nameValueCollection["type"] ?? "tcp");
            string network = vmessItem.network;
            if (network != null)
            {
                uint num = ComputeStringHash(network);
                uint ComputeStringHash(string s)
                {
                    uint num = 0;
                    if (s != null)
                    {
                        num = 2166136261U;
                        for (int i = 0; i < s.Length; i++)
                        {
                            num = ((uint)s[i] ^ num) * 16777619U;
                        }
                    }
                    return num;
                }
                if (num <= 1829160385U)
                {
                    if (num != 981955263U)
                    {
                        if (num != 1313315231U)
                        {
                            if (num != 1829160385U)
                            {
                                goto IL_390;
                            }
                            if (!(network == "kcp"))
                            {
                                goto IL_390;
                            }
                            vmessItem.headerType = (nameValueCollection["headerType"] ?? "none");
                            vmessItem.path = HttpUtility.UrlDecode(nameValueCollection["seed"] ?? "");
                        }
                        else
                        {
                            if (!(network == "ws"))
                            {
                                goto IL_390;
                            }
                            vmessItem.requestHost = HttpUtility.UrlDecode(nameValueCollection["host"] ?? "");
                            vmessItem.path = HttpUtility.UrlDecode(nameValueCollection["path"] ?? "/");
                        }
                    }
                    else
                    {
                        if (!(network == "quic"))
                        {
                            goto IL_390;
                        }
                        vmessItem.headerType = (nameValueCollection["headerType"] ?? "none");
                        vmessItem.requestHost = (nameValueCollection["quicSecurity"] ?? "none");
                        vmessItem.path = HttpUtility.UrlDecode(nameValueCollection["key"] ?? "");
                    }
                }
                else
                {
                    if (num <= 2666707600U)
                    {
                        if (num != 2403021823U)
                        {
                            if (num != 2666707600U)
                            {
                                goto IL_390;
                            }
                            if (!(network == "tcp"))
                            {
                                goto IL_390;
                            }
                            vmessItem.headerType = (nameValueCollection["headerType"] ?? "none");
                            vmessItem.requestHost = HttpUtility.UrlDecode(nameValueCollection["host"] ?? "");
                            return vmessItem;
                        }
                        else if (!(network == "h2"))
                        {
                            goto IL_390;
                        }
                    }
                    else if (num != 2691775653U)
                    {
                        if (num != 3378792613U)
                        {
                            goto IL_390;
                        }
                        if (!(network == "http"))
                        {
                            goto IL_390;
                        }
                    }
                    else
                    {
                        if (!(network == "grpc"))
                        {
                            goto IL_390;
                        }
                        vmessItem.path = HttpUtility.UrlDecode(nameValueCollection["serviceName"] ?? "");
                        vmessItem.headerType = HttpUtility.UrlDecode(nameValueCollection["mode"] ?? "gun");
                        return vmessItem;
                    }
                    vmessItem.network = "h2";
                    vmessItem.requestHost = HttpUtility.UrlDecode(nameValueCollection["host"] ?? "");
                    vmessItem.path = HttpUtility.UrlDecode(nameValueCollection["path"] ?? "/");
                }
                return vmessItem;
            }
        IL_390:
            return null;
        }

        private static int UpgradeServerVersion(ref VmessItem vmessItem)
        {
            try
            {
                if (vmessItem == null || vmessItem.configVersion == 2)
                {
                    return 0;
                }
                if (vmessItem.configType == 1)
                {
                    string path = "";
                    string requestHost = "";
                    string network = vmessItem.network;
                    if (network != null && !(network == "kcp"))
                    {
                        if (!(network == "ws"))
                        {
                            if (network == "h2")
                            {
                                string[] array = vmessItem.requestHost.Replace(" ", "").Split(new char[]
                                {
                                    ';'
                                });
                                if (array.Length != 0)
                                {
                                    path = array[0];
                                }
                                if (array.Length > 1)
                                {
                                    path = array[0];
                                    requestHost = array[1];
                                }
                                vmessItem.path = path;
                                vmessItem.requestHost = requestHost;
                            }
                        }
                        else
                        {
                            string[] array = vmessItem.requestHost.Replace(" ", "").Split(new char[]
                            {
                                ';'
                            });
                            if (array.Length != 0)
                            {
                                path = array[0];
                            }
                            if (array.Length > 1)
                            {
                                path = array[0];
                                requestHost = array[1];
                            }
                            vmessItem.path = path;
                            vmessItem.requestHost = requestHost;
                        }
                    }
                }
                vmessItem.configVersion = 2;
            }
            catch
            {
            }
            return 0;
        }

        private static VmessItem ResolveSSLegacy(string result)
        {
            Regex UrlFinder = new Regex("ss://(?<base64>[A-Za-z0-9+-/=_]+)(?:#(?<tag>\\S+))?", RegexOptions.IgnoreCase);
            Match match = UrlFinder.Match(result);
            if (!match.Success)
            {
                return null;
            }
            VmessItem vmessItem = new VmessItem();
            string text = match.Groups["base64"].Value.TrimEnd(new char[]
            {
                '/'
            });
            string value = match.Groups["tag"].Value;
            if (!string.IsNullOrEmpty(value))
            {
                vmessItem.remarks = HttpUtility.UrlDecode(value);
            }
            Match match2;
            try
            {
                Regex DetailsParser = new Regex("^((?<method>.+?):(?<password>.*)@(?<hostname>.+?):(?<port>\\d+?))$", RegexOptions.IgnoreCase);
                match2 = DetailsParser.Match(Encoding.UTF8.GetString(Convert.FromBase64String(text.PadRight(text.Length + (4 - text.Length % 4) % 4, '='))));
            }
            catch (FormatException)
            {
                return null;
            }
            if (!match2.Success)
            {
                return null;
            }
            vmessItem.security = match2.Groups["method"].Value;
            vmessItem.id = match2.Groups["password"].Value;
            vmessItem.address = match2.Groups["hostname"].Value;
            vmessItem.port = int.Parse(match2.Groups["port"].Value);
            return vmessItem;
        }

        private static VmessItem ResolveStdVmess(string result)
        {
            VmessItem vmessItem = new VmessItem
            {
                configType = 1,
                security = "auto"
            };
            Uri uri = new Uri(result);
            vmessItem.address = uri.IdnHost;
            vmessItem.port = uri.Port;
            vmessItem.remarks = uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
            NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(uri.Query);
            Regex StdVmessUserInfo = new Regex("^(?<network>[a-z]+)(\\+(?<streamSecurity>[a-z]+))?:(?<id>[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})-(?<alterId>[0-9]+)$");
            Match match = StdVmessUserInfo.Match(uri.UserInfo);
            if (!match.Success)
            {
                return null;
            }
            vmessItem.id = match.Groups["id"].Value;
            int alterId;
            if (!int.TryParse(match.Groups["alterId"].Value, out alterId))
            {
                return null;
            }
            vmessItem.alterId = alterId;
            if (match.Groups["streamSecurity"].Success)
            {
                vmessItem.streamSecurity = match.Groups["streamSecurity"].Value;
            }
            string text = vmessItem.streamSecurity;
            if ((text == null || !(text == "tls")) && !string.IsNullOrWhiteSpace(vmessItem.streamSecurity))
            {
                return null;
            }
            vmessItem.network = match.Groups["network"].Value;
            text = vmessItem.network;
            if (text != null)
            {
                if (!(text == "tcp"))
                {
                    if (!(text == "kcp"))
                    {
                        if (!(text == "ws"))
                        {
                            if (!(text == "http") && !(text == "h2"))
                            {
                                if (!(text == "quic"))
                                {
                                    goto IL_2CE;
                                }
                                string url = nameValueCollection["security"] ?? "none";
                                string path = nameValueCollection["key"] ?? "";
                                string headerType = nameValueCollection["type"] ?? "none";
                                vmessItem.headerType = headerType;
                                vmessItem.requestHost = HttpUtility.UrlDecode(url);
                                vmessItem.path = path;
                            }
                            else
                            {
                                vmessItem.network = "h2";
                                string path2 = nameValueCollection["path"] ?? "/";
                                string url2 = nameValueCollection["host"] ?? "";
                                vmessItem.requestHost = HttpUtility.UrlDecode(url2);
                                vmessItem.path = path2;
                            }
                        }
                        else
                        {
                            string path3 = nameValueCollection["path"] ?? "/";
                            string url3 = nameValueCollection["host"] ?? "";
                            vmessItem.requestHost = HttpUtility.UrlDecode(url3);
                            vmessItem.path = path3;
                        }
                    }
                    else
                    {
                        vmessItem.headerType = (nameValueCollection["type"] ?? "none");
                    }
                }
                else
                {
                    string headerType2 = nameValueCollection["type"] ?? "none";
                    vmessItem.headerType = headerType2;
                }
                return vmessItem;
            }
        IL_2CE:
            return null;
        }

        private static VmessItem ResolveVmess4Kitsunebi(string result)
        {
            VmessItem vmessItem = new VmessItem
            {
                configType = 1
            };
            result = result.Substring("vmess://".Length);
            int num = result.IndexOf("?");
            if (num > 0)
            {
                result = result.Substring(0, num);
            }
            result = Base64Decode(result);

            string[] array = result.Split(new char[]
            {
                '@'
            });
            if (array.Length != 2)
            {
                return null;
            }
            string[] array2 = array[0].Split(new char[]
            {
                ':'
            });
            string[] array3 = array[1].Split(new char[]
            {
                ':'
            });
            if (array2.Length != 2 || array2.Length != 2)
            {
                return null;
            }
            vmessItem.address = array3[0];
            vmessItem.port = array3[1].ExToInt();
            vmessItem.security = array2[0];
            vmessItem.id = array2[1];
            vmessItem.network = "tcp";
            vmessItem.headerType = "none";
            vmessItem.remarks = "Alien";
            vmessItem.alterId = 0;
            return vmessItem;
        }

        private static string Base64Decode(string plainText)
        {
            string result;
            try
            {
                plainText = plainText.Trim().Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "").Replace(" ", "");
                if (plainText.Length % 4 > 0)
                {
                    plainText = plainText.PadRight(plainText.Length + 4 - plainText.Length % 4, '=');
                }
                byte[] bytes = Convert.FromBase64String(plainText);
                result = Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                result = string.Empty;
            }
            return result;
        }

        public class VmessItem
        {
            // Token: 0x06000311 RID: 785 RVA: 0x00007930 File Offset: 0x00005B30
            public VmessItem()
            {
                this.configVersion = 1;
                this.address = string.Empty;
                this.port = 0;
                this.id = string.Empty;
                this.alterId = 0;
                this.security = string.Empty;
                this.network = string.Empty;
                this.remarks = string.Empty;
                this.headerType = string.Empty;
                this.requestHost = string.Empty;
                this.path = string.Empty;
                this.streamSecurity = string.Empty;
                this.allowInsecure = string.Empty;
                this.configType = 1;
                this.testResult = string.Empty;
                this.subid = string.Empty;
                this.flow = string.Empty;
            }

            public int configVersion { get; set; }
            public string address { get; set; }
            public int port { get; set; }
            public string id { get; set; }

            public int alterId { get; set; }

            public string security { get; set; }

            public string network { get; set; }

            public string remarks { get; set; }

            public string headerType { get; set; }

            public string requestHost { get; set; }

            public string path { get; set; }

            public string streamSecurity { get; set; }

            public string allowInsecure { get; set; }

            public int configType { get; set; }

            public string testResult { get; set; }

            public string subid { get; set; }

            public string flow { get; set; }

            public string sni { get; set; }
        }

        internal class VmessQRCode
        {
            // Token: 0x170000BB RID: 187
            // (get) Token: 0x060001C8 RID: 456 RVA: 0x00006AC3 File Offset: 0x00004CC3
            // (set) Token: 0x060001C9 RID: 457 RVA: 0x00006ACB File Offset: 0x00004CCB
            public string v { get; set; } = string.Empty;

            // Token: 0x170000BC RID: 188
            // (get) Token: 0x060001CA RID: 458 RVA: 0x00006AD4 File Offset: 0x00004CD4
            // (set) Token: 0x060001CB RID: 459 RVA: 0x00006ADC File Offset: 0x00004CDC
            public string ps { get; set; } = string.Empty;

            // Token: 0x170000BD RID: 189
            // (get) Token: 0x060001CC RID: 460 RVA: 0x00006AE5 File Offset: 0x00004CE5
            // (set) Token: 0x060001CD RID: 461 RVA: 0x00006AED File Offset: 0x00004CED
            public string add { get; set; } = string.Empty;

            // Token: 0x170000BE RID: 190
            // (get) Token: 0x060001CE RID: 462 RVA: 0x00006AF6 File Offset: 0x00004CF6
            // (set) Token: 0x060001CF RID: 463 RVA: 0x00006AFE File Offset: 0x00004CFE
            public string port { get; set; } = string.Empty;

            // Token: 0x170000BF RID: 191
            // (get) Token: 0x060001D0 RID: 464 RVA: 0x00006B07 File Offset: 0x00004D07
            // (set) Token: 0x060001D1 RID: 465 RVA: 0x00006B0F File Offset: 0x00004D0F
            public string id { get; set; } = string.Empty;

            // Token: 0x170000C0 RID: 192
            // (get) Token: 0x060001D2 RID: 466 RVA: 0x00006B18 File Offset: 0x00004D18
            // (set) Token: 0x060001D3 RID: 467 RVA: 0x00006B20 File Offset: 0x00004D20
            public string aid { get; set; } = string.Empty;

            // Token: 0x170000C1 RID: 193
            // (get) Token: 0x060001D4 RID: 468 RVA: 0x00006B29 File Offset: 0x00004D29
            // (set) Token: 0x060001D5 RID: 469 RVA: 0x00006B31 File Offset: 0x00004D31
            public string scy { get; set; } = string.Empty;

            // Token: 0x170000C2 RID: 194
            // (get) Token: 0x060001D6 RID: 470 RVA: 0x00006B3A File Offset: 0x00004D3A
            // (set) Token: 0x060001D7 RID: 471 RVA: 0x00006B42 File Offset: 0x00004D42
            public string net { get; set; } = string.Empty;

            // Token: 0x170000C3 RID: 195
            // (get) Token: 0x060001D8 RID: 472 RVA: 0x00006B4B File Offset: 0x00004D4B
            // (set) Token: 0x060001D9 RID: 473 RVA: 0x00006B53 File Offset: 0x00004D53
            public string type { get; set; } = string.Empty;

            // Token: 0x170000C4 RID: 196
            // (get) Token: 0x060001DA RID: 474 RVA: 0x00006B5C File Offset: 0x00004D5C
            // (set) Token: 0x060001DB RID: 475 RVA: 0x00006B64 File Offset: 0x00004D64
            public string host { get; set; } = string.Empty;

            // Token: 0x170000C5 RID: 197
            // (get) Token: 0x060001DC RID: 476 RVA: 0x00006B6D File Offset: 0x00004D6D
            // (set) Token: 0x060001DD RID: 477 RVA: 0x00006B75 File Offset: 0x00004D75
            public string path { get; set; } = string.Empty;

            // Token: 0x170000C6 RID: 198
            // (get) Token: 0x060001DE RID: 478 RVA: 0x00006B7E File Offset: 0x00004D7E
            // (set) Token: 0x060001DF RID: 479 RVA: 0x00006B86 File Offset: 0x00004D86
            public string tls { get; set; } = string.Empty;

            // Token: 0x170000C7 RID: 199
            // (get) Token: 0x060001E0 RID: 480 RVA: 0x00006B8F File Offset: 0x00004D8F
            // (set) Token: 0x060001E1 RID: 481 RVA: 0x00006B97 File Offset: 0x00004D97
            public string sni { get; set; } = string.Empty;
        }
    }

    public static class StringExtensions
    {
        // 扩展方法---计算字符串长度
        public static string ExToString(this object str)
        {
            return string.IsNullOrEmpty(str.ToString()) ? string.Empty : str.ToString();
        }

        public static int ExToInt(this object str)
        {
            return int.TryParse(str.ToString(), out int res) ? res : 0;
        }
    }
}