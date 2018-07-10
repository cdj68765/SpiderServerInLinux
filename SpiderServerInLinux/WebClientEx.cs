using System;
using System.Net;

namespace WebClientEx
{
    public class WebClientEx : WebClient
    {
        public static CookieContainer outboundCookies;
        public static CookieCollection inboundCookies;

        private readonly int _TimeOut = 30000; //milliseconds
        private readonly string h_Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
        private readonly string h_Accept_Encoding = "gzip, deflate, br";
        private readonly string h_Accept_Language = "en-US,en;q=0.5";
        private readonly string h_Referer = string.Empty;

        private readonly string h_User_Agent =
            "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0";

        public string ErrorInfo;

        public WebResponse webResponse;

        public WebClientEx(HEADERS headers = null, int TimeOut = 30000)
        {
            _TimeOut = Math.Abs(TimeOut) < 0 ? _TimeOut : Math.Abs(TimeOut);

            if (headers != null)
            {
                h_Accept = headers.ACCEPT;
                h_Accept_Encoding = headers.ACCEPT_ENCODING;
                h_Accept_Language = headers.ACCEPT_LANGUAGE;
                h_Referer = headers.REFERER;
                h_User_Agent = headers.USER_AGENT;
            }

            Headers.Add("Accept", h_Accept);
            Headers.Add("Accept-Encoding", h_Accept_Encoding);
            Headers.Add("Accept-Language", h_Accept_Language);
            Headers.Add("User-Agent", h_User_Agent);

            if (!string.IsNullOrEmpty(h_Referer)) Headers.Add("Referer", h_Referer);

            outboundCookies = new CookieContainer();
            inboundCookies = new CookieCollection();
        }

        public CookieContainer OutboundCookies => outboundCookies;

        public CookieCollection InboundCookies => inboundCookies;

        protected override WebRequest GetWebRequest(Uri address)
        {
            var r = base.GetWebRequest(address);
            var request = r as HttpWebRequest;
            request.Timeout = _TimeOut;
            request.AllowAutoRedirect = true;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.CookieContainer = outboundCookies;
            return r;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            try
            {
                var response = base.GetWebResponse(request, result);
                webResponse = response;
                inboundCookies = (response as HttpWebResponse).Cookies ?? inboundCookies;
                return response;
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.Timeout) ErrorInfo = "Timeout";
                return null;
            }
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            try
            {
                var response = base.GetWebResponse(request);
                webResponse = response;
                inboundCookies = (response as HttpWebResponse).Cookies ?? inboundCookies;
                return response;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public class HEADERS
        {
            public string ACCEPT;
            public string ACCEPT_ENCODING;
            public string ACCEPT_LANGUAGE;
            public string REFERER;
            public string USER_AGENT;
        }
    }
}