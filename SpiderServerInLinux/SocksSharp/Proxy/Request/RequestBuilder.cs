using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using SocksSharp.Extensions;

namespace SocksSharp.Proxy.Request
{
    internal class RequestBuilder
    {
        private readonly string newLine = "\r\n";
        private readonly CookieContainer cookies;

        private readonly HttpRequestMessage request;

        public RequestBuilder(HttpRequestMessage request, CookieContainer cookies = null)
        {
            this.request = request;
            this.cookies = cookies;
        }

        public byte[] BuildStartingLine()
        {
            var uri = request.RequestUri;

            var startingLine
                = $"{request.Method.Method} {uri.PathAndQuery} HTTP/{request.Version}" + newLine;

            startingLine += "Host: " + uri.Host + newLine;

            return ToByteArray(startingLine);
        }

        public byte[] BuildHeaders(bool hasContent)
        {
            var headers = GetHeaders(request.Headers);
            if (hasContent)
            {
                var contentHeaders = GetHeaders(request.Content.Headers);
                headers = string.Join(newLine, headers, contentHeaders);
            }

            return ToByteArray(headers + newLine + newLine);
        }

        private string GetHeaders(HttpHeaders headers)
        {
            var headersList = new List<string>();

            foreach (var header in headers)
            {
                var headerKeyAndValue = string.Empty;
                var values = header.Value as string[];

                if (values != null && values.Length < 2)
                {
                    if (values.Length > 0 && !string.IsNullOrEmpty(values[0]))
                        headerKeyAndValue = header.Key + ": " + values[0];
                }
                else
                {
                    var headerValue = headers.GetHeaderString(header.Key);
                    if (!string.IsNullOrEmpty(headerValue))
                        headerKeyAndValue = header.Key + ": " + headerValue;
                }

                if (!string.IsNullOrEmpty(headerKeyAndValue))
                    headersList.Add(headerKeyAndValue);
            }

            if (headers is HttpContentHeaders && !headersList.Contains("Content-Length"))
            {
                var content = headers as HttpContentHeaders;
                if (content.ContentLength.HasValue && content.ContentLength.Value > 0)
                    headersList.Add($"Content-Length: {content.ContentLength}");
            }

            if (cookies != null)
            {
                var cookiesCollection = cookies.GetCookies(request.RequestUri);
                var rawCookies = "Cookie: ";

                foreach (var cookie in cookiesCollection)
                    rawCookies += cookie + "; ";

                if (cookiesCollection.Count > 0)
                    headersList.Add(rawCookies);
            }

            return string.Join("\r\n", headersList.ToArray());
        }

        private byte[] ToByteArray(string data)
        {
            return Encoding.ASCII.GetBytes(data);
        }
    }
}