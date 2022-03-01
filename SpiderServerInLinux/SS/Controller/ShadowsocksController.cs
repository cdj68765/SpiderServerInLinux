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
          
       
            if (File.Exists(@"SSHis.txt"))
                SSUri = new HashSet<string>(File.ReadAllLines(@"SSHis.txt"));
            else
                SSUri = new HashSet<string>();
             var Hash = "dm1lc3M6Ly9leUoySWpvZ0lqSWlMQ0FpY0hNaU9pQWlYSFUyTm1ZMFhIVTJOV0l3WEhVMFpUaGxPakEwTFRFeElERTJPakF3SUMwZ1lua2dRblZNYVc1ckxuaDVlaUlzSUNKaFpHUWlPaUFpWEhVMFpqZG1YSFUzTlRJNFhIVTFNalJrWEhVNFltSXdYSFUxWmprM1hIVTJObVkwWEhVMk5XSXdYSFU0WW1FeVhIVTVOakExSWl3Z0luQnZjblFpT2lBaU1DSXNJQ0pwWkNJNklDSTJZVE5pWTJNd09DMDVZemMzTFRSak1ESXRPRFEwWWkwMFlUWTVOR00wWmpKbVpXRWlMQ0FpWVdsa0lqb2dJakFpTENBaWJtVjBJam9nSW5SamNDSXNJQ0owZVhCbElqb2dJbTV2Ym1VaUxDQWlhRzl6ZENJNklDSWlMQ0FpY0dGMGFDSTZJQ0lpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaU1qTXVNakkxTGpFMk5TNHlORElpTENBaWRpSTZJQ0l5SWl3Z0luQnpJam9nSW1kcGRHaDFZaTVqYjIwdlpuSmxaV1p4SUMwZ1hIVTNaamhsWEhVMU5tWmtYSFUxTW1Fd1hIVTFNakk1WEhVM09UaG1YSFUxWXpOalhIVTBaVGxoWEhVMVpHUmxYSFUyWkRGaVhIVTJOelE1WEhVM04yWTJYSFUxWlRBeVEyOXdaWEpoZEdsdmJpQkRiMnh2WTNScGIyNWNkVFkxTnpCY2RUWXpObVZjZFRSbE1tUmNkVFZtWXpNZ01TSXNJQ0p3YjNKMElqb2dORFF6TENBaWFXUWlPaUFpTXpSbU9HUmtNbVV0TlRWbU1DMDBaRGsxTFdKbVpHRXROekUwWmpVd1lqWmpNR1V4SWl3Z0ltRnBaQ0k2SUNJMk5DSXNJQ0p1WlhRaU9pQWlkM01pTENBaWRIbHdaU0k2SUNJaUxDQWlhRzl6ZENJNklDSWlMQ0FpY0dGMGFDSTZJQ0l2Y0dGMGFDOHhOakF6TVRjek5ESTFNRFlpTENBaWRHeHpJam9nSW5Sc2N5SjkKdm1lc3M6Ly9leUpoWkdRaU9pQWlNak11TWpJMExqTXhMakl3TWlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVGRtT0dWY2RUVTJabVJjZFRVeVlUQmNkVFV5TWpsY2RUYzVPR1pjZFRWak0yTmNkVFJsT1dGY2RUVmtaR1ZjZFRaa01XSmNkVFkzTkRsY2RUYzNaalpjZFRWbE1ESkRiM0JsY21GMGFXOXVJRU52Ykc5amRHbHZibHgxTmpVM01GeDFOak0yWlZ4MU5HVXlaRngxTldaak15QXlJaXdnSW5CdmNuUWlPaUEwTkRNc0lDSnBaQ0k2SUNJeU9HRTROR0kyT1MxbE1URTRMVFEzWVRBdE9UZzJPUzFtTXprM1ltTmpPV1ppWW1RaUxDQWlZV2xrSWpvZ0lqWTBJaXdnSW01bGRDSTZJQ0ozY3lJc0lDSjBlWEJsSWpvZ0lpSXNJQ0pvYjNOMElqb2dJaUlzSUNKd1lYUm9Jam9nSWk5d1lYUm9MekUyTURNeE56TTBNalV3TmlJc0lDSjBiSE1pT2lBaWRHeHpJbjA9CnZtZXNzOi8vZXlKaFpHUWlPaUFpTWpNdU1qSTFMakl4TXk0eU5ESWlMQ0FpZGlJNklDSXlJaXdnSW5Ceklqb2dJbWRwZEdoMVlpNWpiMjB2Wm5KbFpXWnhJQzBnWEhVM1pqaGxYSFUxTm1aa1hIVTFNbUV3WEhVMU1qSTVYSFUzT1RobVhIVTFZek5qWEhVMFpUbGhYSFUxWkdSbFhIVTJaREZpWEhVMk56UTVYSFUzTjJZMlhIVTFaVEF5UTJWeVlVNWxkSGR2Y210elhIVTJOVGN3WEhVMk16WmxYSFUwWlRKa1hIVTFabU16SURNaUxDQWljRzl5ZENJNklEUTBNeXdnSW1sa0lqb2dJakV4WXpjd00yRTRMV1l6WldJdE5HSXpZUzFpWXpsaUxUSTFNemxqTm1Gak5qYzVOaUlzSUNKaGFXUWlPaUFpTmpRaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5SNWNHVWlPaUFpSWl3Z0ltaHZjM1FpT2lBaUlpd2dJbkJoZEdnaU9pQWlMM0JoZEdndk1UWXdNekUzTXpReU5UQTJJaXdnSW5Sc2N5STZJQ0owYkhNaWZRPT0Kdm1lc3M6Ly9leUpoWkdRaU9pQWlNVEk0TGpFMExqRTFNeTQwTWlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVGRtT0dWY2RUVTJabVJjZFRVeVlUQmNkVFV5TWpsY2RUYzVPR1pjZFRWak0yTmNkVFJsT1dGY2RUVmtaR1ZjZFRaa01XSmNkVFkzTkRsY2RUYzNaalphWlc1c1lYbGxjbHgxTmpVM01GeDFOak0yWlZ4MU5HVXlaRngxTldaak15QTBJaXdnSW5CdmNuUWlPaUF4TXpjeU15d2dJbWxrSWpvZ0ltRTVNRFU1TjJNeExXSmhZak10TkRJeE55MWhaRFptTFRBNE16ZzJOelZqT0RZek15SXNJQ0poYVdRaU9pQWlNU0lzSUNKdVpYUWlPaUFpZDNNaUxDQWlkSGx3WlNJNklDSWlMQ0FpYUc5emRDSTZJQ0oxYzJFeUxXNXZaR1V1TXpNMk5uUmxjM1F1WTI5dElpd2dJbkJoZEdnaU9pQWlMM0poZVNJc0lDSjBiSE1pT2lBaWRHeHpJbjA9CnZtZXNzOi8vZXlKaFpHUWlPaUFpTWpNdU1qSTBMakUyTkM0eE1EQWlMQ0FpZGlJNklDSXlJaXdnSW5Ceklqb2dJbWRwZEdoMVlpNWpiMjB2Wm5KbFpXWnhJQzBnWEhVM1pqaGxYSFUxTm1aa1hIVTFNbUV3WEhVMU1qSTVYSFUzT1RobVhIVTFZek5qWEhVMFpUbGhYSFUxWkdSbFhIVTJaREZpWEhVMk56UTVYSFUzTjJZMlhIVTFaVEF5UTI5d1pYSmhkR2x2YmlCRGIyeHZZM1JwYjI1Y2RUWTFOekJjZFRZek5tVmNkVFJsTW1SY2RUVm1Zek1nTlNJc0lDSndiM0owSWpvZ05EUXpMQ0FpYVdRaU9pQWlaREkxWVRZMU9ETXROVEpqWVMwME9UWm1MVGczWVdVdFpqSXlOelZpTTJJd1pHUmtJaXdnSW1GcFpDSTZJQ0kyTkNJc0lDSnVaWFFpT2lBaWQzTWlMQ0FpZEhsd1pTSTZJQ0lpTENBaWFHOXpkQ0k2SUNJaUxDQWljR0YwYUNJNklDSXZjR0YwYUM4eE5qQXpNVGN6TkRJMU1EWWlMQ0FpZEd4eklqb2dJblJzY3lKOQp2bWVzczovL2V5SjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFpqWkRWY2RUVTJabVJQVmtnZ1UwRlRJRFlpTENBaVlXUmtJam9nSWpreExqRXpOQzR5TXpndU1UZzJJaXdnSW5CdmNuUWlPaUFpTkRReklpd2dJbWxrSWpvZ0ltRTVPREl4TmpCbExUQTJaR1F0TkRKallTMDVOakl4TFdJM01USTNaRE0zTmpaak5TSXNJQ0poYVdRaU9pQWlOalFpTENBaWJtVjBJam9nSW5keklpd2dJblI1Y0dVaU9pQWlibTl1WlNJc0lDSm9iM04wSWpvZ0ltRndjSE11YVhScExtZHZkaTVsWnlJc0lDSndZWFJvSWpvZ0lpOXpjMmhyYVhRaUxDQWlkR3h6SWpvZ0luUnNjeUlzSUNKemJta2lPaUFpSW4wPQp2bWVzczovL2V5SmhaR1FpT2lBaWFuQXdNaTVwYm1ScGFHOXRaUzUwYXlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFkxWlRWY2RUWTNNbU5jZFRVNU1qZGNkVGsyTW1GY2RUVmxPV05jZFRVNU1qZGNkVGsyTW1GTmFXTnliM052Wm5SY2RUWTFOekJjZFRZek5tVmNkVFJsTW1SY2RUVm1Zek1nTnlJc0lDSndiM0owSWpvZ01qQXdNakFzSUNKcFpDSTZJQ0kwWVRsalpEQmhOaTB4WmpZeUxUUTVOV010WWpZMVppMDBNV1V3TkRZNU9HSmtaV01pTENBaVlXbGtJam9nSWpFaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5SNWNHVWlPaUFpSWl3Z0ltaHZjM1FpT2lBaUlpd2dJbkJoZEdnaU9pQWlMeUlzSUNKMGJITWlPaUFpSW4wPQp2bWVzczovL2V5SmhaR1FpT2lBaWFHRnJkWEpsYVRFdWRITjFkSE4xTG5SdmNDSXNJQ0oySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRZMVpUVmNkVFkzTW1OY2RUUmxNV05jZFRSbFlXTkVhV2RwZEdGc0lGWk5JRGdpTENBaWNHOXlkQ0k2SURRME15d2dJbWxrSWpvZ0lqVmxNemcxWW1VNExXRTBOVEV0TkdReU1TMWhNbVZtTFdZeU5EQXhNR1ZqWlRZek55SXNJQ0poYVdRaU9pQWlNQ0lzSUNKdVpYUWlPaUFpZDNNaUxDQWlkSGx3WlNJNklDSWlMQ0FpYUc5emRDSTZJQ0pvWVd0MWNtVnBNUzUwYzNWMGMzVXVkRzl3SWl3Z0luQmhkR2dpT2lBaUwzUnpkWFJ6ZFNJc0lDSjBiSE1pT2lBaWRHeHpJbjA9CnZtZXNzOi8vZXlKMklqb2dJaklpTENBaWNITWlPaUFpWjJsMGFIVmlMbU52YlM5bWNtVmxabkVnTFNCY2RUZG1PR1ZjZFRVMlptUmNkVFV4T0RWY2RUVXpOR1ZjZFRobVltVmNkVFZrWkdWY2RUWXlZemxjZFRZMVlXWmNkVGRsWmpSY2RUVXlZVEJjZFRZMVlXWkNkWGxXVFNBNUlpd2dJbUZrWkNJNklDSjJNaTB3T0M1emMzSnpkV0l1YjI1bElpd2dJbkJ2Y25RaU9pQWlNVFV6SWl3Z0ltbGtJam9nSWpaaVlqazVNREV5TFRVeE5qa3ROREExWWkwNU5EUXpMVEExWkRnM056Tm1ZekV5TUNJc0lDSmhhV1FpT2lBaU5qUWlMQ0FpYm1WMElqb2dJbmR6SWl3Z0luUjVjR1VpT2lBaWJtOXVaU0lzSUNKb2IzTjBJam9nSW00ME5taHROVEkzTnpNdWJHRnZkMkZ1ZUdsaGJtY3VZMjl0SWl3Z0luQmhkR2dpT2lBaUlpd2dJblJzY3lJNklDSWlMQ0FpYzI1cElqb2dJaUo5CnZtZXNzOi8vZXlKaFpHUWlPaUFpU2xBd01TNHhMbFpPTkVVeExrNVBSRVV1TWpNemVYVnVMbWx1SWl3Z0ltRnBaQ0k2SURJc0lDSm9iM04wSWpvZ0lrcFFNREV1TVM1V1RqUkZNUzVPVDBSRkxqSXpNM2wxYmk1cGJpSXNJQ0pwWkNJNklDSXdNMkpqT0RaaFpDMDNPR1kyTFRNeU16Y3RZbUZsT0MxbE9HRmtNRE5rTkRJd056RWlMQ0FpYm1WMElqb2dJbmR6SWl3Z0luQmhkR2dpT2lBaUx6SXpNM2wxYmk5Mk1uSmhlU0lzSUNKd2IzSjBJam9nTWpNekxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFkxWlRWY2RUWTNNbU5jZFRSbE1XTmNkVFJsWVdOQmJXRjZiMjVjZFRZMU56QmNkVFl6Tm1WY2RUUmxNbVJjZFRWbVl6TWdNVEFpTENBaWRHeHpJam9nSW01dmJtVWlMQ0FpZEhsd1pTSTZJQ0p1YjI1bElpd2dJbllpT2lBeWZRPT0Kdm1lc3M6Ly9leUoySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRkbU9HVmNkVFUyWm1SRGJHOTFaRVpzWVhKbFhIVTFNVFpqWEhVMU0yWTRRMFJPWEhVNE1qZ3lYSFUzTUdJNUlERXhJaXdnSW1Ga1pDSTZJQ0ptY21WbExtMXBiR0ZuY205d1pYUnpMblJsWTJnaUxDQWljRzl5ZENJNklDSTRNQ0lzSUNKcFpDSTZJQ0ptWlRSaU1EbGtOaTA0WVdZMkxUTXpZVEl0T0RNME1DMWtNelV6T1dZNFl6aGhOR01pTENBaVlXbGtJam9nSWpBaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5SNWNHVWlPaUFpYm05dVpTSXNJQ0pvYjNOMElqb2dJaUlzSUNKd1lYUm9Jam9nSWk5eVlYa2lMQ0FpZEd4eklqb2dJaUlzSUNKemJta2lPaUFpSW4wPQp2bWVzczovL2V5SmhaR1FpT2lBaVNFc3dNUzR4TGxCTU9GVkZMazVQUkVVdU1qTXplWFZ1TG1sdUlpd2dJbUZwWkNJNklESXNJQ0pvYjNOMElqb2dJa2hMTURFdU1TNVFURGhWUlM1T1QwUkZMakl6TTNsMWJpNXBiaUlzSUNKcFpDSTZJQ0l3TTJKak9EWmhaQzAzT0dZMkxUTXlNemN0WW1GbE9DMWxPR0ZrTUROa05ESXdOekVpTENBaWJtVjBJam9nSW5keklpd2dJbkJoZEdnaU9pQWlMekl6TTNsMWJpOTJNbkpoZVNJc0lDSndiM0owSWpvZ01qTXpMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRrNU9UbGNkVFpsTW1aQmJXRjZiMjVjZFRZMU56QmNkVFl6Tm1WY2RUUmxNbVJjZFRWbVl6TWdNVElpTENBaWRHeHpJam9nSW01dmJtVWlMQ0FpZEhsd1pTSTZJQ0p1YjI1bElpd2dJbllpT2lBeWZRPT0Kdm1lc3M6Ly9leUoySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRVelpqQmNkVFpsTjJWY2RUYzNNREZjZFRZMVlqQmNkVFV6TVRkY2RUVmxNREpjZFRSbE1tUmNkVFV6TkdWY2RUYzFNelZjZFRSbVpURWdNVE1pTENBaVlXUmtJam9nSW5SM0xtWXdNUzV3WVc5d1lXOWpiRzkxWkM1amVXOTFJaXdnSW5CdmNuUWlPaUFpTXpNd05pSXNJQ0pwWkNJNklDSXhOalk1TUdGa015MWlNak5qTFROa05HUXRZalJoTUMwek56a3daVFU0WmpsaVpURWlMQ0FpWVdsa0lqb2dJaklpTENBaWJtVjBJam9nSW5SamNDSXNJQ0owZVhCbElqb2dJbTV2Ym1VaUxDQWlhRzl6ZENJNklDSWlMQ0FpY0dGMGFDSTZJQ0l2SWl3Z0luUnNjeUk2SUNJaWZRPT0Kc3M6Ly9ZMmhoWTJoaE1qQXRhV1YwWmkxd2IyeDVNVE13TlRwemVVTnBTbXd6Ym1JNFQwUUBzcy51cy5zc2htYXgubmV0OjU3NDc4I2dpdGh1Yi5jb20vZnJlZWZxJTIwLSUyMCVFNyVCRSU4RSVFNSU5QiVCRCVFNSVCQyU5NyVFNSU5MCU4OSVFNSVCMCVCQyVFNCVCQSU5QSVFNSVCNyU5RSVFNiU5NiU4NyVFNyU4OSVCOSVFNSVCMSVCMSVFNSU4NiU5QyVFNSU5QyVCQU9WSCVFNiU5NSVCMCVFNiU4RCVBRSVFNCVCOCVBRCVFNSVCRiU4MyUyMDE0CnNzOi8vWVdWekxUSTFOaTFuWTIwNlIzUXlPVlJTVDJReGJYQllAYmdwNC5nZW1nZW1zLm5ldDoxNzE5NCNnaXRodWIuY29tL2ZyZWVmcSUyMC0lMjAlRTUlQjklQkYlRTQlQjglOUMlRTclOUMlODElRTUlQjklQkYlRTUlQjclOUUlRTUlQjglODIlRTclQTclQkIlRTUlOEElQTglMjAxNQp0cm9qYW46Ly9zc3JzdWJAdDA4LnNzcnN1Yi5vbmU6NDQzI2dpdGh1Yi5jb20vZnJlZWZxJTIwLSUyMCVFNyVCRSU4RSVFNSU5QiVCRCVFNSU4NiU4NSVFNSU4RCU4RSVFOCVCRSVCRSVFNSVCNyU5RSVFNiU4QiU4OSVFNiU5NiVBRiVFNyVCQiVCNCVFNSU4QSVBMCVFNiU5NiVBRkJ1eVZNJTIwMTYKdm1lc3M6Ly9leUpoWkdRaU9pQWlkakl0TURrdWMzTnljM1ZpTG05dVpTSXNJQ0oySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRkbU9HVmNkVFUyWm1SY2RUZGxZbVJjZFRkbFlUWmNkVFZrWkdWY2RUZGxZbVJjZFRkbFlUWkNkWGxXVFNBeE55SXNJQ0p3YjNKMElqb2dNVFV6TENBaWFXUWlPaUFpTm1KaU9Ua3dNVEl0TlRFMk9TMDBNRFZpTFRrME5ETXRNRFZrT0RjM00yWmpNVEl3SWl3Z0ltRnBaQ0k2SUNJMk5DSXNJQ0p1WlhRaU9pQWlkM01pTENBaWRIbHdaU0k2SUNJaUxDQWlhRzl6ZENJNklDSnVORFpvYlRVeU56Y3pMbXhoYjNkaGJuaHBZVzVuTG1OdmJTSXNJQ0p3WVhSb0lqb2dJaThpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaVlXZ3RZM1V3TVM1b1lXOTVaUzVqWmlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFZpT0RsY2RUVm1ZbVJjZFRjM01ERmNkVGd3TlRSY2RUa3dNV0VnTVRnaUxDQWljRzl5ZENJNklEVXdNRE15TENBaWFXUWlPaUFpTkdFNVkyUXdZVFl0TVdZMk1pMDBPVFZqTFdJMk5XWXROREZsTURRMk9UaGlaR1ZqSWl3Z0ltRnBaQ0k2SUNJeUlpd2dJbTVsZENJNklDSjNjeUlzSUNKMGVYQmxJam9nSWlJc0lDSm9iM04wSWpvZ0lpSXNJQ0p3WVhSb0lqb2dJaThpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaVlYVjBieTVtY21WbGRqSXVkRzl3SWl3Z0ltWnBiR1VpT2lBaUlpd2dJbWxrSWpvZ0lqSTROamt5TTJNeUxUTXlZelF0TkRJNE5pMDVOR1JtTFdNeFlqWmxaR1EzTVRZNFpDSXNJQ0p1WlhRaU9pQWlkR053SWl3Z0ltaHZjM1FpT2lBaUlpd2dJbkJ2Y25RaU9pQWlNVFk0TXpjaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFkxWlRWY2RUWTNNbU5jZFRSbE1XTmNkVFJsWVdOTWFXNXZaR1ZjZFRZMU56QmNkVFl6Tm1WY2RUUmxNbVJjZFRWbVl6TWdNVGtpTENBaWRHeHpJam9nSWlJc0lDSjJJam9nTWl3Z0ltRnBaQ0k2SURFc0lDSjBlWEJsSWpvZ0ltNXZibVVpZlE9PQp2bWVzczovL2V5SmhaR1FpT2lBaVkzTXRZM1V3TVM1b1lXOTVaUzVqWmlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFpsTlRaY2RUVXpOVGRjZFRjM01ERmNkVGd3TlRSY2RUa3dNV0VnTWpBaUxDQWljRzl5ZENJNklESXdNREEzTENBaWFXUWlPaUFpTkdFNVkyUXdZVFl0TVdZMk1pMDBPVFZqTFdJMk5XWXROREZsTURRMk9UaGlaR1ZqSWl3Z0ltRnBaQ0k2SUNJeUlpd2dJbTVsZENJNklDSjNjeUlzSUNKMGVYQmxJam9nSWlJc0lDSm9iM04wSWpvZ0lpSXNJQ0p3WVhSb0lqb2dJaThpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaWFHNHRZMjB3TVM1b1lXOTVaUzVqWmlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFpqWWpOY2RUVXpOVGRjZFRjM01ERmNkVFprTVdKY2RUazJNek5jZFRWbE1ESmNkVGM1Wm1KY2RUVXlZVGdnTWpFaUxDQWljRzl5ZENJNklERXhPREEzTENBaWFXUWlPaUFpTkdFNVkyUXdZVFl0TVdZMk1pMDBPVFZqTFdJMk5XWXROREZsTURRMk9UaGlaR1ZqSWl3Z0ltRnBaQ0k2SUNJeUlpd2dJbTVsZENJNklDSjNjeUlzSUNKMGVYQmxJam9nSWlJc0lDSm9iM04wSWpvZ0lpSXNJQ0p3WVhSb0lqb2dJaThpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaWFuQXdNUzVwYm1ScGFHOXRaUzUwYXlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVGRtT0dWY2RUVTJabVJjZFRVeVlUQmNkVFV5TWpsY2RUYzVPR1pjZFRWak0yTmNkVFJsT1dGY2RUVmtaR1ZjZFRVM01qTmNkVFV4TkdKY2RUWXlZemxjZFRZeVl6bE5hV055YjNOdlpuUmNkVFV4Tm1OY2RUVXpaamdnTWpJaUxDQWljRzl5ZENJNklESXdNREl3TENBaWFXUWlPaUFpTkdFNVkyUXdZVFl0TVdZMk1pMDBPVFZqTFdJMk5XWXROREZsTURRMk9UaGlaR1ZqSWl3Z0ltRnBaQ0k2SUNJeElpd2dJbTVsZENJNklDSjNjeUlzSUNKMGVYQmxJam9nSWlJc0lDSm9iM04wSWpvZ0lpSXNJQ0p3WVhSb0lqb2dJaThpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaVVsVXdNeTR4TGpjd00wZzVMazVQUkVVdU1qTXplWFZ1TG1sdUlpd2dJbUZwWkNJNklESXNJQ0pvYjNOMElqb2dJbEpWTURNdU1TNDNNRE5JT1M1T1QwUkZMakl6TTNsMWJpNXBiaUlzSUNKcFpDSTZJQ0l3TTJKak9EWmhaQzAzT0dZMkxUTXlNemN0WW1GbE9DMWxPR0ZrTUROa05ESXdOekVpTENBaWJtVjBJam9nSW5keklpd2dJbkJoZEdnaU9pQWlMekl6TTNsMWJpOTJNbkpoZVNJc0lDSndiM0owSWpvZ01qTXpMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRSbVl6UmNkVGRtTlRkY2RUWTFZV1pjZFRVMU9EQmNkVFZqTnpGS2RYTjBTRzl6ZENBeU15SXNJQ0owYkhNaU9pQWlibTl1WlNJc0lDSjBlWEJsSWpvZ0ltNXZibVVpTENBaWRpSTZJREo5CnZtZXNzOi8vZXlKaFpHUWlPaUFpTVRnMUxqSTBNeTQxTnk0eE16SWlMQ0FpZGlJNklDSXlJaXdnSW5Ceklqb2dJbWRwZEdoMVlpNWpiMjB2Wm5KbFpXWnhJQzBnWEhVM1pqaGxYSFUxTm1aa1hIVTFNbUV3WEhVMU1qSTVYSFUzT1RobVhIVTFZek5qWEhVMFpUbGhYSFUxWkdSbFhIVTJaREZpWEhVMk56UTVYSFUzTjJZMlVITjVZMmg2WEhVMk5UY3dYSFUyTXpabFhIVTBaVEprWEhVMVptTXpJREkwSWl3Z0luQnZjblFpT2lBME5ETXNJQ0pwWkNJNklDSm1aR0k0Tm1OaVl5MWpOVE0zTFRSaE9USXRPVGN4Tnkwd1pEUmlZVEkyWVdJNVpXRWlMQ0FpWVdsa0lqb2dJaklpTENBaWJtVjBJam9nSW5keklpd2dJblI1Y0dVaU9pQWlJaXdnSW1odmMzUWlPaUFpWkdRdU1UazVNekF4TG5oNWVpSXNJQ0p3WVhSb0lqb2dJaTlrWkdVM1ptRTBMeUlzSUNKMGJITWlPaUFpZEd4ekluMD0Kdm1lc3M6Ly9leUpoWkdRaU9pQWlkakl0TURjdWMzTnljM1ZpTG05dVpTSXNJQ0oySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRSbVl6UmNkVGRtTlRkY2RUWTFZV1pjZFRZMVlqQmNkVGc1TjJaY2RUUm1NbVpjZFRVeU1qbGNkVFJsT1dGS2RYTjBTRzl6ZENBeU5TSXNJQ0p3YjNKMElqb2dNVFV6TENBaWFXUWlPaUFpTm1KaU9Ua3dNVEl0TlRFMk9TMDBNRFZpTFRrME5ETXRNRFZrT0RjM00yWmpNVEl3SWl3Z0ltRnBaQ0k2SUNJMk5DSXNJQ0p1WlhRaU9pQWlkM01pTENBaWRIbHdaU0k2SUNJaUxDQWlhRzl6ZENJNklDSnVORFpvYlRVeU56Y3pMbXhoYjNkaGJuaHBZVzVuTG1OdmJTSXNJQ0p3WVhSb0lqb2dJaThpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaVl5MTFjek11YjI5NFl5NWpZeUlzSUNKMklqb2dJaklpTENBaWNITWlPaUFpWjJsMGFIVmlMbU52YlM5bWNtVmxabkVnTFNCY2RUZG1PR1ZjZFRVMlptUkRiRzkxWkVac1lYSmxYSFU0TWpneVhIVTNNR0k1SURJMklpd2dJbkJ2Y25RaU9pQTBORE1zSUNKcFpDSTZJQ0prWWpWa01XRmhNeTA1TURoaUxUUTBaREV0WW1Vd1lTMDBaVFpoT0dRMFpUUmpaR0VpTENBaVlXbGtJam9nSWpZMElpd2dJbTVsZENJNklDSjNjeUlzSUNKMGVYQmxJam9nSWlJc0lDSm9iM04wSWpvZ0ltTXRkWE16TG05dmVHTXVZMk1pTENBaWNHRjBhQ0k2SUNJdmFtb2lMQ0FpZEd4eklqb2dJblJzY3lKOQp2bWVzczovL2V5SmhaR1FpT2lBaWFHc3dNUzVwYm1ScGFHOXRaUzUwYXlJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVGs1T1RsY2RUWmxNbVpOYVdOeWIzTnZablJjZFRZMU56QmNkVFl6Tm1WY2RUUmxNbVJjZFRWbVl6TWdNamNpTENBaWNHOXlkQ0k2SURJd01ERXpMQ0FpYVdRaU9pQWlOR0U1WTJRd1lUWXRNV1kyTWkwME9UVmpMV0kyTldZdE5ERmxNRFEyT1RoaVpHVmpJaXdnSW1GcFpDSTZJQ0l4SWl3Z0ltNWxkQ0k2SUNKM2N5SXNJQ0owZVhCbElqb2dJaUlzSUNKb2IzTjBJam9nSWlJc0lDSndZWFJvSWpvZ0lpOGlMQ0FpZEd4eklqb2dJaUo5CnZtZXNzOi8vZXlKaFpHUWlPaUFpWm5WcmVXOHVjM05tWmpZMk5pNTNiM0pyWlhKekxtUmxkaUlzSUNKMklqb2dJaklpTENBaWNITWlPaUFpWjJsMGFIVmlMbU52YlM5bWNtVmxabkVnTFNCY2RUZG1PR1ZjZFRVMlptUkRiRzkxWkVac1lYSmxYSFUxTVRaalhIVTFNMlk0UTBST1hIVTRNamd5WEhVM01HSTVJREk0SWl3Z0luQnZjblFpT2lBME5ETXNJQ0pwWkNJNklDSmhaRGd3TmpRNE55MHlaREkyTFRRMk16WXRPVGhpTmkxaFlqZzFZMk00TlRJeFpqY2lMQ0FpWVdsa0lqb2dJalkwSWl3Z0ltNWxkQ0k2SUNKM2N5SXNJQ0owZVhCbElqb2dJaUlzSUNKb2IzTjBJam9nSWlJc0lDSndZWFJvSWpvZ0lpOGlMQ0FpZEd4eklqb2dJblJzY3lKOQp2bWVzczovL2V5SmhaR1FpT2lBaVVsVXdNUzR4TGpSVlJEWXdMazVQUkVVdU1qTXplWFZ1TG1sdUlpd2dJbllpT2lBaU1pSXNJQ0p3Y3lJNklDSm5hWFJvZFdJdVkyOXRMMlp5WldWbWNTQXRJRngxTkdaak5GeDFOMlkxTjF4MU5qVmhabHgxTmpWaU1GeDFPRGszWmx4MU5HWXlabHgxTlRJeU9WeDFOR1U1WVdwMWMzUm9iM04wSURJNUlpd2dJbkJ2Y25RaU9pQXlNek1zSUNKcFpDSTZJQ0l3TTJKak9EWmhaQzAzT0dZMkxUTXlNemN0WW1GbE9DMWxPR0ZrTUROa05ESXdOekVpTENBaVlXbGtJam9nSWpJaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5SNWNHVWlPaUFpSWl3Z0ltaHZjM1FpT2lBaVVsVXdNUzR4TGpSVlJEWXdMazVQUkVVdU1qTXplWFZ1TG1sdUlpd2dJbkJoZEdnaU9pQWlMekl6TTNsMWJpOTJNbkpoZVNJc0lDSjBiSE1pT2lBaUluMD0Kdm1lc3M6Ly9leUpoWkdRaU9pQWlVMGN3TVM0eExrdEZWMWhETGs1UFJFVXVNak16ZVhWdUxtbHVJaXdnSW1GcFpDSTZJRElzSUNKb2IzTjBJam9nSWxOSE1ERXVNUzVMUlZkWVF5NU9UMFJGTGpJek0zbDFiaTVwYmlJc0lDSnBaQ0k2SUNJd00ySmpPRFpoWkMwM09HWTJMVE15TXpjdFltRmxPQzFsT0dGa01ETmtOREl3TnpFaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5CaGRHZ2lPaUFpTHpJek0zbDFiaTkyTW5KaGVTSXNJQ0p3YjNKMElqb2dNak16TENBaWNITWlPaUFpWjJsMGFIVmlMbU52YlM5bWNtVmxabkVnTFNCY2RUWTFZakJjZFRVeVlUQmNkVFUzTmpGQmJXRjZiMjVjZFRZMU56QmNkVFl6Tm1WY2RUUmxNbVJjZFRWbVl6TWdNekFpTENBaWRHeHpJam9nSW01dmJtVWlMQ0FpZEhsd1pTSTZJQ0p1YjI1bElpd2dJbllpT2lBeWZRPT0Kdm1lc3M6Ly9leUpoWkdRaU9pQWlWVk13TVM0eExqVXdNMVJHTGs1UFJFVXVNak16ZVhWdUxtbHVJaXdnSW1GcFpDSTZJRElzSUNKb2IzTjBJam9nSWxWVE1ERXVNUzQxTUROVVJpNU9UMFJGTGpJek0zbDFiaTVwYmlJc0lDSnBaQ0k2SUNJd00ySmpPRFpoWkMwM09HWTJMVE15TXpjdFltRmxPQzFsT0dGa01ETmtOREl3TnpFaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5CaGRHZ2lPaUFpTHpJek0zbDFiaTkyTW5KaGVTSXNJQ0p3YjNKMElqb2dNak16TENBaWNITWlPaUFpWjJsMGFIVmlMbU52YlM5bWNtVmxabkVnTFNCY2RUZG1PR1ZjZFRVMlptUmNkVFV5WVRCY2RUVXlNamxjZFRjNU9HWmNkVFZqTTJOY2RUUmxPV0ZjZFRWa1pHVmNkVFprTVdKY2RUWTNORGxjZFRjM1pqWkJiV0Y2YjI1Y2RUWTFOekJjZFRZek5tVmNkVFJsTW1SY2RUVm1Zek1nTXpFaUxDQWlkR3h6SWpvZ0ltNXZibVVpTENBaWRIbHdaU0k2SUNKdWIyNWxJaXdnSW5ZaU9pQXlmUT09CnZtZXNzOi8vZXlKaFpHUWlPaUFpUzFJd01TNHhMazlKTUVsWUxrNVBSRVV1TWpNemVYVnVMbWx1SWl3Z0ltRnBaQ0k2SURJc0lDSm9iM04wSWpvZ0lrdFNNREV1TVM1UFNUQkpXQzVPVDBSRkxqSXpNM2wxYmk1cGJpSXNJQ0pwWkNJNklDSXdNMkpqT0RaaFpDMDNPR1kyTFRNeU16Y3RZbUZsT0MxbE9HRmtNRE5rTkRJd056RWlMQ0FpYm1WMElqb2dJbmR6SWl3Z0luQmhkR2dpT2lBaUx6SXpNM2wxYmk5Mk1uSmhlU0lzSUNKd2IzSjBJam9nTWpNekxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVGszWlRsY2RUVTJabVJjZFRrNU9UWmNkVFZqTVRSQmJXRjZiMjVjZFRZMU56QmNkVFl6Tm1WY2RUUmxNbVJjZFRWbVl6TWdNeklpTENBaWRHeHpJam9nSW01dmJtVWlMQ0FpZEhsd1pTSTZJQ0p1YjI1bElpd2dJbllpT2lBeWZRPT0Kdm1lc3M6Ly9leUpoWkdRaU9pQWlZV2d0WTNVd01TNW9ZVzk1WlM1alppSXNJQ0oySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRWaU9EbGNkVFZtWW1SY2RUYzNNREZjZFRnd05UUmNkVGt3TVdFZ016TWlMQ0FpY0c5eWRDSTZJRFV3TURRd0xDQWlhV1FpT2lBaU5HRTVZMlF3WVRZdE1XWTJNaTAwT1RWakxXSTJOV1l0TkRGbE1EUTJPVGhpWkdWaklpd2dJbUZwWkNJNklDSXlJaXdnSW01bGRDSTZJQ0ozY3lJc0lDSjBlWEJsSWpvZ0lpSXNJQ0pvYjNOMElqb2dJaUlzSUNKd1lYUm9Jam9nSWk4aUxDQWlkR3h6SWpvZ0lpSjkKdm1lc3M6Ly9leUpoWkdRaU9pQWlZMnd1Ykc5dlozTnZiUzU0ZVhvaUxDQWlkaUk2SUNJeUlpd2dJbkJ6SWpvZ0ltZHBkR2gxWWk1amIyMHZabkpsWldaeElDMGdYSFUzWmpobFhIVTFObVprWEhVMU1tRXdYSFUxTWpJNVhIVTNPVGhtWEhVMVl6TmpYSFUwWlRsaFhIVTFaR1JsWEhVMlpERmlYSFUyTnpRNVhIVTNOMlkyVFZWTVZFRkRUMDFjZFRZMU56QmNkVFl6Tm1WY2RUUmxNbVJjZFRWbVl6TWdNelFpTENBaWNHOXlkQ0k2SURRME15d2dJbWxrSWpvZ0ltRXhPRFkzTW1NMUxUQTFOVEl0TkdObVlTMDRNek5oTFRSaFl6RmhOak01T1daaFlpSXNJQ0poYVdRaU9pQWlOQ0lzSUNKdVpYUWlPaUFpZDNNaUxDQWlkSGx3WlNJNklDSWlMQ0FpYUc5emRDSTZJQ0lpTENBaWNHRjBhQ0k2SUNJdmRpSXNJQ0owYkhNaU9pQWlkR3h6SW4wPQp0cm9qYW46Ly9mYXN0c3NoLmNvbUBzZzEudmxlc3MuY286NDQzI2dpdGh1Yi5jb20vZnJlZWZxJTIwLSUyMCVFNiU5NiVCMCVFNSU4QSVBMCVFNSU5RCVBMU5ld01lZGlhJUU2JTk1JUIwJUU2JThEJUFFJUU0JUI4JUFEJUU1JUJGJTgzJTIwMzUKdm1lc3M6Ly9leUpoWkdRaU9pQWlVbFV3TWk0eExrSlZSRTVCTGs1UFJFVXVNak16ZVhWdUxtbHVJaXdnSW1GcFpDSTZJRElzSUNKb2IzTjBJam9nSWxKVk1ESXVNUzVDVlVST1FTNU9UMFJGTGpJek0zbDFiaTVwYmlJc0lDSnBaQ0k2SUNJd00ySmpPRFpoWkMwM09HWTJMVE15TXpjdFltRmxPQzFsT0dGa01ETmtOREl3TnpFaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5CaGRHZ2lPaUFpTHpJek0zbDFiaTkyTW5KaGVTSXNJQ0p3YjNKMElqb2dNak16TENBaWNITWlPaUFpWjJsMGFIVmlMbU52YlM5bWNtVmxabkVnTFNCY2RUUm1ZelJjZFRkbU5UZGNkVFkxWVdaY2RUZ3pZV0pjZFRZMVlXWmNkVGM1WkRGS2RYTjBTRzl6ZENBek5pSXNJQ0owYkhNaU9pQWlibTl1WlNJc0lDSjBlWEJsSWpvZ0ltNXZibVVpTENBaWRpSTZJREo5CnZtZXNzOi8vZXlKaFpHUWlPaUFpZEhkd2NtODJNREkxTG1GNmVtbGpieTV6Y0dGalpTSXNJQ0oySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRWbE4yWmNkVFJsTVdOY2RUYzNNREZjZFRWbE4yWmNkVFZrWkdWY2RUVmxNREpjZFRjNVptSmNkVFV5WVRnZ016Y2lMQ0FpY0c5eWRDSTZJREV4TlRVMExDQWlhV1FpT2lBaU9HSmtaREk1TWpVdE56SXhPQzB6TVRSaUxUazROMkV0WkdOaVlqUTBaREE1T0RVeUlpd2dJbUZwWkNJNklDSXlJaXdnSW01bGRDSTZJQ0ozY3lJc0lDSjBlWEJsSWpvZ0lpSXNJQ0pvYjNOMElqb2dJblIzY0hKdk5qQXlOQzVoZW5wcFkyOHVjSGNpTENBaWNHRjBhQ0k2SUNJdmRtbGtaVzhpTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaWRqSXRNRFl1YzNOeWMzVmlMbTl1WlNJc0lDSjJJam9nSWpJaUxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFJtWXpSY2RUZG1OVGRjZFRZMVlXWmNkVFkxWWpCY2RUZzVOMlpjZFRSbU1tWmNkVFV5TWpsY2RUUmxPV0ZxZFhOMGFHOXpkQ0F6T0NJc0lDSndiM0owSWpvZ01UVXpMQ0FpYVdRaU9pQWlObUppT1Rrd01USXROVEUyT1MwME1EVmlMVGswTkRNdE1EVmtPRGMzTTJaak1USXdJaXdnSW1GcFpDSTZJQ0kyTkNJc0lDSnVaWFFpT2lBaWQzTWlMQ0FpZEhsd1pTSTZJQ0lpTENBaWFHOXpkQ0k2SUNKdU5EWm9iVFV5TnpjekxteGhiM2RoYm5ocFlXNW5MbU52YlNJc0lDSndZWFJvSWpvZ0lpOGlMQ0FpZEd4eklqb2dJaUo5CnZtZXNzOi8vZXlKaFpHUWlPaUFpZW1odmJtZDZhSFZoYm1kaGVtaHJMbmhwWVc5aVlXbDVkVzR1YldVaUxDQWlkaUk2SUNJeUlpd2dJbkJ6SWpvZ0ltZHBkR2gxWWk1amIyMHZabkpsWldaeElDMGdYSFUzWmpobFhIVTFObVprWEhVMU1tRXdYSFUxTWpJNVhIVTNPVGhtWEhVMVl6TmpYSFUwWlRsaFhIVTFaR1JsWEhVMU56SXpYSFUxTVRSaVhIVTJNbU01WEhVMk1tTTVUV2xqY205emIyWjBYSFUxTVRaalhIVTFNMlk0SURNNUlpd2dJbkJ2Y25RaU9pQTRNalFzSUNKcFpDSTZJQ0l4TkRJMVpEbGlaUzFoWVRrM0xUTmlOek10WVRaak1TMWhNR05qWldVMFkyVmhNak1pTENBaVlXbGtJam9nSWpJaUxDQWlibVYwSWpvZ0luZHpJaXdnSW5SNWNHVWlPaUFpSWl3Z0ltaHZjM1FpT2lBaVpXeHplSGhpYkhrdWVHbGhiMkpoYVhsMWJpNXRaU0lzSUNKd1lYUm9Jam9nSWk5b2JITWlMQ0FpZEd4eklqb2dJaUo5CnZtZXNzOi8vZXlKaFpHUWlPaUFpUTBFd01TNHhMbEk1UVU5TExrNVBSRVV1TWpNemVYVnVMbWx1SWl3Z0ltRnBaQ0k2SURJc0lDSm9iM04wSWpvZ0lrTkJNREV1TVM1U09VRlBTeTVPVDBSRkxqSXpNM2wxYmk1cGJpSXNJQ0pwWkNJNklDSXdNMkpqT0RaaFpDMDNPR1kyTFRNeU16Y3RZbUZsT0MxbE9HRmtNRE5rTkRJd056RWlMQ0FpYm1WMElqb2dJbmR6SWl3Z0luQmhkR2dpT2lBaUx6SXpNM2wxYmk5Mk1uSmhlU0lzSUNKd2IzSjBJam9nTWpNekxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVFV5WVRCY2RUWXlabVpjZFRVNU1qZGNkVGxpTkRGY2RUVXpNVGRjZFRVeE5HSmNkVGMzTURGY2RUZzBPVGxjZFRjeU56bGNkVFV5TWpsY2RUVmpNVFJCYldGNmIyNWNkVFkxTnpCY2RUWXpObVZjZFRSbE1tUmNkVFZtWXpNZ05EQWlMQ0FpZEd4eklqb2dJbTV2Ym1VaUxDQWlkSGx3WlNJNklDSnViMjVsSWl3Z0luWWlPaUF5ZlE9PQp2bWVzczovL2V5SmhaR1FpT2lBaVpHUXVNVGs1TXpBeExuaDVlaUlzSUNKMklqb2dJaklpTENBaWNITWlPaUFpWjJsMGFIVmlMbU52YlM5bWNtVmxabkVnTFNCY2RUZG1PR1ZjZFRVMlptUmNkVFV5WVRCY2RUVXlNamxjZFRjNU9HWmNkVFZqTTJOY2RUUmxPV0ZjZFRWa1pHVmNkVFprTVdKY2RUWTNORGxjZFRjM1pqWlFjM2xqYUhwY2RUWTFOekJjZFRZek5tVmNkVFJsTW1SY2RUVm1Zek1nTkRFaUxDQWljRzl5ZENJNklEUTBNeXdnSW1sa0lqb2dJbVprWWpnMlkySmpMV00xTXpjdE5HRTVNaTA1TnpFM0xUQmtOR0poTWpaaFlqbGxZU0lzSUNKaGFXUWlPaUFpTWlJc0lDSnVaWFFpT2lBaWQzTWlMQ0FpZEhsd1pTSTZJQ0lpTENBaWFHOXpkQ0k2SUNKa1pDNHhPVGt6TURFdWVIbDZJaXdnSW5CaGRHZ2lPaUFpTDJSa1pUZG1ZVFF2SWl3Z0luUnNjeUk2SUNKMGJITWlmUT09CnZtZXNzOi8vZXlKaFpHUWlPaUFpTVRBMExqRTJMakU0TWk0eE5TSXNJQ0oySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRkbU9HVmNkVFUyWm1SRGJHOTFaRVpzWVhKbFhIVTFNVFpqWEhVMU0yWTRRMFJPWEhVNE1qZ3lYSFUzTUdJNUlEUXlJaXdnSW5CdmNuUWlPaUEwTkRNc0lDSnBaQ0k2SUNKa1lqVmtNV0ZoTXkwNU1EaGlMVFEwWkRFdFltVXdZUzAwWlRaaE9HUTBaVFJqWkdFaUxDQWlZV2xrSWpvZ0lqWTBJaXdnSW01bGRDSTZJQ0ozY3lJc0lDSjBlWEJsSWpvZ0lpSXNJQ0pvYjNOMElqb2dJbU10Y25VekxtOXZlR011WTJNaUxDQWljR0YwYUNJNklDSXZhbW9pTENBaWRHeHpJam9nSW5Sc2N5SjkKdHJvamFuOi8vc3Nyc3ViQHQxMS5zc3JzdWIub25lOjQ0MyNnaXRodWIuY29tL2ZyZWVmcSUyMC0lMjAlRTQlQkYlODQlRTclQkQlOTclRTYlOTYlQUYlMjAlMjA0Mwp0cm9qYW46Ly9zc3JzdWJAdDA5LnNzcnN1Yi5vbmU6NDQzI2dpdGh1Yi5jb20vZnJlZWZxJTIwLSUyMCVFNyVCRSU4RSVFNSU5QiVCRCVFNyVCQSVCRCVFNyVCQSVBNiVFNSVCNyU5RSVFNyVCQSVCRCVFNyVCQSVBNkJ1eVZNJTIwNDQKdm1lc3M6Ly9leUpoWkdRaU9pQWlkM2QzTG1ScFoybDBZV3h2WTJWaGJpNWpiMjBpTENBaWRpSTZJQ0l5SWl3Z0luQnpJam9nSW1kcGRHaDFZaTVqYjIwdlpuSmxaV1p4SUMwZ1hIVTNaamhsWEhVMU5tWmtRMnh2ZFdSR2JHRnlaVngxTlRFMlkxeDFOVE5tT0VORVRseDFPREk0TWx4MU56QmlPU0EwTlNJc0lDSndiM0owSWpvZ05EUXpMQ0FpYVdRaU9pQWlaR0kxWkRGaFlUTXRPVEE0WWkwME5HUXhMV0psTUdFdE5HVTJZVGhrTkdVMFkyUmhJaXdnSW1GcFpDSTZJQ0kyTkNJc0lDSnVaWFFpT2lBaWQzTWlMQ0FpZEhsd1pTSTZJQ0lpTENBaWFHOXpkQ0k2SUNKakxYSjFNeTV2YjNoakxtTmpJaXdnSW5CaGRHZ2lPaUFpTDJwcUlpd2dJblJzY3lJNklDSjBiSE1pZlE9PQp2bWVzczovL2V5SmhaR1FpT2lBaWVtaHZibWQ2YUhWaGJtZGhlbWhyTG5ocFlXOWlZV2w1ZFc0dWJXVWlMQ0FpZGlJNklDSXlJaXdnSW5Ceklqb2dJbWRwZEdoMVlpNWpiMjB2Wm5KbFpXWnhJQzBnWEhVM1pqaGxYSFUxTm1aa1hIVTFNbUV3WEhVMU1qSTVYSFUzT1RobVhIVTFZek5qWEhVMFpUbGhYSFUxWkdSbFhIVTFOekl6WEhVMU1UUmlYSFUyTW1NNVhIVTJNbU01VFdsamNtOXpiMlowWEhVMU1UWmpYSFUxTTJZNElEUTJJaXdnSW5CdmNuUWlPaUE0TWpRc0lDSnBaQ0k2SUNJelpqVTNaalU0TVMwNE1XRXpMVE0wWlRBdFlqZ3hZaTFsTkRZd01qTXdOall5WVRZaUxDQWlZV2xrSWpvZ0lqSWlMQ0FpYm1WMElqb2dJbmR6SWl3Z0luUjVjR1VpT2lBaUlpd2dJbWh2YzNRaU9pQWlaV3h6ZUhoaWJIa3VlR2xoYjJKaGFYbDFiaTV0WlNJc0lDSndZWFJvSWpvZ0lpOW9iSE1pTENBaWRHeHpJam9nSWlKOQp2bWVzczovL2V5SmhaR1FpT2lBaVkyUnVaR1V1YVhKMFpYbDZMblJ2WkdGNUlpd2dJbllpT2lBaU1pSXNJQ0p3Y3lJNklDSm5hWFJvZFdJdVkyOXRMMlp5WldWbWNTQXRJRngxTjJZNFpWeDFOVFptWkVOc2IzVmtSbXhoY21WY2RUVXhObU5jZFRVelpqaERSRTVjZFRneU9ESmNkVGN3WWprZ05EY2lMQ0FpY0c5eWRDSTZJRFEwTXl3Z0ltbGtJam9nSWpOaU5XVXlOVGhsTFRoak5XVXRORFZrTXkxaU4yUXlMVEF5WXpobU5XWmpNR0ppTWlJc0lDSmhhV1FpT2lBaU5qUWlMQ0FpYm1WMElqb2dJbmR6SWl3Z0luUjVjR1VpT2lBaUlpd2dJbWh2YzNRaU9pQWlZMlJ1WkdVdWFYSjBaWGw2TG5SdlpHRjVJaXdnSW5CaGRHZ2lPaUFpTHlJc0lDSjBiSE1pT2lBaWRHeHpJbjA9CnZtZXNzOi8vZXlKaFpHUWlPaUFpUjBJd01TNHhMbGxZVUVWYUxrNVBSRVV1TWpNemVYVnVMbWx1SWl3Z0ltRnBaQ0k2SURJc0lDSm9iM04wSWpvZ0lrZENNREV1TVM1WldGQkZXaTVPVDBSRkxqSXpNM2wxYmk1cGJpSXNJQ0pwWkNJNklDSXdNMkpqT0RaaFpDMDNPR1kyTFRNeU16Y3RZbUZsT0MxbE9HRmtNRE5rTkRJd056RWlMQ0FpYm1WMElqb2dJbmR6SWl3Z0luQmhkR2dpT2lBaUx6SXpNM2wxYmk5Mk1uSmhlU0lzSUNKd2IzSjBJam9nTWpNekxDQWljSE1pT2lBaVoybDBhSFZpTG1OdmJTOW1jbVZsWm5FZ0xTQmNkVGRtT0dWY2RUVTJabVJjZFRsbFltSmNkVGMzTURGY2RUYzBNRFpjZFRWa1pUVmNkVFZpTmpaY2RUazJOaklnTkRnaUxDQWlkR3h6SWpvZ0ltNXZibVVpTENBaWRIbHdaU0k2SUNKdWIyNWxJaXdnSW5ZaU9pQXlmUT09CnZtZXNzOi8vZXlKaFpHUWlPaUFpWXkxeWRUTXViMjk0WXk1all5SXNJQ0oySWpvZ0lqSWlMQ0FpY0hNaU9pQWlaMmwwYUhWaUxtTnZiUzltY21WbFpuRWdMU0JjZFRkbU9HVmNkVFUyWm1SRGJHOTFaRVpzWVhKbFhIVTFNVFpqWEhVMU0yWTRRMFJPWEhVNE1qZ3lYSFUzTUdJNUlEUTVJaXdnSW5CdmNuUWlPaUEwTkRNc0lDSnBaQ0k2SUNKa1lqVmtNV0ZoTXkwNU1EaGlMVFEwWkRFdFltVXdZUzAwWlRaaE9HUTBaVFJqWkdFaUxDQWlZV2xrSWpvZ0lqWTBJaXdnSW01bGRDSTZJQ0ozY3lJc0lDSjBlWEJsSWpvZ0lpSXNJQ0pvYjNOMElqb2dJbU10Y25VekxtOXZlR011WTJNaUxDQWljR0YwYUNJNklDSXZhbW9pTENBaWRHeHpJam9nSW5Sc2N5SjkKCnZtZXNzOi8vZXlKd2IzSjBJam9nTUN3Z0ltRnBaQ0k2SURBc0lDSnBaQ0k2SUNJMllUTmlZMk13T0MwNVl6YzNMVFJqTURJdE9EUTBZaTAwWVRZNU5HTTBaakptWldFaUxDQWljSE1pT2lBaUxTQmNkVFJsWlRWY2RUUmxNR0pjZFRneU9ESmNkVGN3WWpsY2RUUmxNMkZDZFV4cGJtdGNkVGd4WldGY2RUVmxabUVnWEhVNU5qVXdYSFUyTnpBNFhIVTJaRFF4WEhVNU1XTm1OVWNnWEhVMU1UUmtYSFU0WkRNNVhIVTRaRFEwWEhVMlpUa3dYSFU0WW1ZM1hIVTFOREE0WEhVM05EQTJYSFUwWmpkbVhIVTNOVEk0SUMwaUxDQWlZV1JrSWpvZ0lseDFOR1ZsTlZ4MU5HVXdZbHgxTlRFMFpGeDFPR1F6T1Z4MU9ESTRNbHgxTnpCaU9WeDFPR0poTVZ4MU5tUTBNVngxT1RGalppSXNJQ0owYkhNaU9pQWlibTl1WlNJc0lDSjJJam9nSWpJaUxDQWlibVYwSWpvZ0luUmpjQ0lzSUNKb2IzTjBJam9nSWlJc0lDSndZWFJvSWpvZ0lpSXNJQ0owZVhCbElqb2dJbTV2Ym1VaWZRPT0Kdm1lc3M6Ly9leUp3YjNKMElqb2dORFF6TENBaVlXbGtJam9nTUN3Z0ltbGtJam9nSW1KbE1qUmxPR1EyTFdGbE1tUXRORGMzT1MwNVlUa3pMVEl5T0RjMFpXVm1ZalE0TnlJc0lDSndjeUk2SUNKaWRXeHBibXNnWEhVM1pqVXhYSFUxTTJOaVhIVTFNakEyWEhVMFpXRmlYSFUzWldKbVhIVTRaR1ZtSURFd0lGeDFOemxtWWx4MU5USmhPQ0lzSUNKaFpHUWlPaUFpYm1veUxtSjFiR2x1YXk1NGVYb3VabTlpZW5NdVkyOXRJaXdnSW5Sc2N5STZJQ0p1YjI1bElpd2dJbllpT2lBaU1pSXNJQ0p1WlhRaU9pQWlkR053SWl3Z0ltaHZjM1FpT2lBaUlpd2dJbkJoZEdnaU9pQWlJaXdnSW5SNWNHVWlPaUFpYm05dVpTSjkKdm1lc3M6Ly9leUp3YjNKMElqb2dORFF6TENBaVlXbGtJam9nTUN3Z0ltbGtJam9nSW1KbE1qUmxPR1EyTFdGbE1tUXRORGMzT1MwNVlUa3pMVEl5T0RjMFpXVm1ZalE0TnlJc0lDSndjeUk2SUNKaWRXeHBibXNnWEhVM1pqVXhYSFUxTTJOaVhIVTFNakEyWEhVMFpXRmlYSFUzWldKbVhIVTRaR1ZtSURFeElGeDFOR1V3T1Z4MU4yWTFNU0lzSUNKaFpHUWlPaUFpYkdFeU1TNWlkV3hwYm1zdWVIbDZMbVp2WW5wekxtTnZiU0lzSUNKMGJITWlPaUFpYm05dVpTSXNJQ0oySWpvZ0lqSWlMQ0FpYm1WMElqb2dJblJqY0NJc0lDSm9iM04wSWpvZ0lpSXNJQ0p3WVhSb0lqb2dJaUlzSUNKMGVYQmxJam9nSW01dmJtVWlmUT09CnZtZXNzOi8vZXlKd2IzSjBJam9nTkRRekxDQWlZV2xrSWpvZ01Dd2dJbWxrSWpvZ0ltSmxNalJsT0dRMkxXRmxNbVF0TkRjM09TMDVZVGt6TFRJeU9EYzBaV1ZtWWpRNE55SXNJQ0p3Y3lJNklDSmlkV3hwYm1zZ1hIVTNaalV4WEhVMU0yTmlYSFUxTWpBMlhIVTBaV0ZpWEhVM1pXSm1YSFU0WkdWbUlERXlJRngxTkdVd09WeDFOMlkxTVNBd0xqa2lMQ0FpWVdSa0lqb2dJbXhoTWprdVluVnNhVzVyTG5oNWVpNW1iMko2Y3k1amIyMGlMQ0FpZEd4eklqb2dJbTV2Ym1VaUxDQWlkaUk2SUNJeUlpd2dJbTVsZENJNklDSjBZM0FpTENBaWFHOXpkQ0k2SUNJaUxDQWljR0YwYUNJNklDSWlMQ0FpZEhsd1pTSTZJQ0p1YjI1bEluMD0Kdm1lc3M6Ly9leUp3YjNKMElqb2dORFF6TENBaVlXbGtJam9nTUN3Z0ltbGtJam9nSW1KbE1qUmxPR1EyTFdGbE1tUXRORGMzT1MwNVlUa3pMVEl5T0RjMFpXVm1ZalE0TnlJc0lDSndjeUk2SUNKaWRXeHBibXNnWEhVM1pqVXhYSFUxTTJOaVhIVTFNakEyWEhVMFpXRmlYSFUzWldKbVhIVTRaR1ZtSURJZ1hIVTBaVEE1WEhVM1pqVXhJREF1TlZ4MU5UQXdaQ0lzSUNKaFpHUWlPaUFpYkdFeUxtSjFiR2x1YXk1NGVYb3VabTlpZW5NdVkyOXRJaXdnSW5Sc2N5STZJQ0p1YjI1bElpd2dJbllpT2lBaU1pSXNJQ0p1WlhRaU9pQWlkR053SWl3Z0ltaHZjM1FpT2lBaUlpd2dJbkJoZEdnaU9pQWlJaXdnSW5SNWNHVWlPaUFpYm05dVpTSjkKdm1lc3M6Ly9leUp3YjNKMElqb2dORFF6TENBaVlXbGtJam9nTUN3Z0ltbGtJam9nSW1KbE1qUmxPR1EyTFdGbE1tUXRORGMzT1MwNVlUa3pMVEl5T0RjMFpXVm1ZalE0TnlJc0lDSndjeUk2SUNKaWRXeHBibXNnWEhVM1pqVXhYSFUxTTJOaVhIVTFNakEyWEhVMFpXRmlYSFUzWldKbVhIVTRaR1ZtSURVZ1hIVTBaVEE1WEhVM1pqVXhJaXdnSW1Ga1pDSTZJQ0pzWVRRdVluVnNhVzVyTG5oNWVpNW1iMko2Y3k1amIyMGlMQ0FpZEd4eklqb2dJbTV2Ym1VaUxDQWlkaUk2SUNJeUlpd2dJbTVsZENJNklDSjBZM0FpTENBaWFHOXpkQ0k2SUNJaUxDQWljR0YwYUNJNklDSWlMQ0FpZEhsd1pTSTZJQ0p1YjI1bEluMD0Kdm1lc3M6Ly9leUp3YjNKMElqb2dORFF6TENBaVlXbGtJam9nTUN3Z0ltbGtJam9nSW1KbE1qUmxPR1EyTFdGbE1tUXRORGMzT1MwNVlUa3pMVEl5T0RjMFpXVm1ZalE0TnlJc0lDSndjeUk2SUNKaWRXeHBibXNnWEhVM1pqVXhYSFUxTTJOaVhIVTFNakEyWEhVMFpXRmlYSFUzWldKbVhIVTRaR1ZtSURZZ1hIVTNPV1ppWEhVMU1tRTRJaXdnSW1Ga1pDSTZJQ0p1YWpFdVluVnNhVzVyTG5oNWVpNW1iMko2Y3k1amIyMGlMQ0FpZEd4eklqb2dJbTV2Ym1VaUxDQWlkaUk2SUNJeUlpd2dJbTVsZENJNklDSjBZM0FpTENBaWFHOXpkQ0k2SUNJaUxDQWljR0YwYUNJNklDSWlMQ0FpZEhsd1pTSTZJQ0p1YjI1bEluMD0Kdm1lc3M6Ly9leUp3YjNKMElqb2dORFF6TENBaVlXbGtJam9nTUN3Z0ltbGtJam9nSW1KbE1qUmxPR1EyTFdGbE1tUXRORGMzT1MwNVlUa3pMVEl5T0RjMFpXVm1ZalE0TnlJc0lDSndjeUk2SUNKaWRXeHBibXNnWEhVM1pqVXhYSFUxTTJOaVhIVTFNakEyWEhVMFpXRmlYSFUzWldKbVhIVTRaR1ZtSURjZ1hIVTBaVEE1WEhVM1pqVXhJaXdnSW1Ga1pDSTZJQ0pzWVRFd0xtSjFiR2x1YXk1NGVYb3VabTlpZW5NdVkyOXRJaXdnSW5Sc2N5STZJQ0p1YjI1bElpd2dJbllpT2lBaU1pSXNJQ0p1WlhRaU9pQWlkR053SWl3Z0ltaHZjM1FpT2lBaUlpd2dJbkJoZEdnaU9pQWlJaXdnSW5SNWNHVWlPaUFpYm05dVpTSjkKdm1lc3M6Ly9leUp3YjNKMElqb2dORFF6TENBaVlXbGtJam9nTUN3Z0ltbGtJam9nSW1KbE1qUmxPR1EyTFdGbE1tUXRORGMzT1MwNVlUa3pMVEl5T0RjMFpXVm1ZalE0TnlJc0lDSndjeUk2SUNKaWRXeHBibXNnWEhVM1pqVXhYSFUxTTJOaVhIVTFNakEyWEhVMFpXRmlYSFUzWldKbVhIVTRaR1ZtSURnZ1hIVTBaVEE1WEhVM1pqVXhJaXdnSW1Ga1pDSTZJQ0pzWVRFeExtSjFiR2x1YXk1NGVYb3VabTlpZW5NdVkyOXRJaXdnSW5Sc2N5STZJQ0p1YjI1bElpd2dJbllpT2lBaU1pSXNJQ0p1WlhRaU9pQWlkR053SWl3Z0ltaHZjM1FpT2lBaUlpd2dJbkJoZEdnaU9pQWlJaXdnSW5SNWNHVWlPaUFpYm05dVpTSjkK";

            foreach (var item in Encoding.UTF8.GetString(Convert.FromBase64String(Hash)).Split(Environment.NewLine.ToCharArray()))
            {
                if (!item.StartsWith("ss")) continue;
                var Config = Server.ParseURL(item);
                var Intp = Encryption.OpenSSL.GetCipherInfo(Config.method);
                if (Intp != IntPtr.Zero)
                {
                    _config.configs.Add(Config);
                    SSUri.Add(item);
                }
            }
/* foreach (var item in SSUri)
{
    File.WriteAllLines(@"SSHis.txt", new string[] { item });
}*/
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
            using var Download = new BetterHttpClient.HttpClient(new BetterHttpClient.Proxy("127.0.0.1", 1089));

            Download.Proxy.ProxyType = BetterHttpClient.ProxyTypeEnum.Socks;
             Hash = Download.DownloadString(ssr_url);
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