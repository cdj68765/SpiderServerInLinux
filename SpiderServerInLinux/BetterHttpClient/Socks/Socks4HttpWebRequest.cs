using BetterHttpClient.Socks.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BetterHttpClient.Socks
{
    internal class Socks4HttpWebRequest: WebRequest
    {
        private static readonly StringCollection validHttpVerbs = new StringCollection { "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "OPTIONS" };
        private WebHeaderCollection _HttpRequestHeaders;
        private string _method;
        private byte[] _requestContentBuffer;
        private NeverEndingStream _requestContentStream;
        private SocksHttpWebResponse _response;

        private Socks4HttpWebRequest(Uri requestUri)
        {
            RequestUri = requestUri;
        }

        public override int Timeout { get; set; }

        public string UserAgent
        {
            get { return _HttpRequestHeaders["User-Agent"]; }
            set { SetSpecialHeaders("User-Agent", value ?? string.Empty); }
        }

        public string Referer
        {
            get { return _HttpRequestHeaders["Referer"]; }
            set { SetSpecialHeaders("Referer", value ?? string.Empty); }
        }

        public string Accept
        {
            get { return _HttpRequestHeaders["Accept"]; }
            set { SetSpecialHeaders("Accept", value ?? string.Empty); }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get
            {
                var result = DecompressionMethods.None;
                string encoding = _HttpRequestHeaders["Accept-Encoding"] ?? string.Empty;
                foreach (string value in encoding.Split(','))
                {
                    switch (value.Trim())
                    {
                        case "gzip":
                            result |= DecompressionMethods.GZip;
                            break;

                        case "deflate":
                            result |= DecompressionMethods.Deflate;
                            break;
                    }
                }

                return result;
            }
            set
            {
                string result = string.Empty;
                if ((value & DecompressionMethods.GZip) != 0)
                    result = "gzip";
                if ((value & DecompressionMethods.Deflate) != 0)
                {
                    if (!string.IsNullOrEmpty(result))
                        result += ", ";
                    result += "deflate";
                }

                SetSpecialHeaders("Accept-Encoding", result);
            }
        }

        public override Uri RequestUri { get; }

        public override IWebProxy Proxy { get; set; }

        public override WebHeaderCollection Headers
        {
            get { return _HttpRequestHeaders ?? (_HttpRequestHeaders = new WebHeaderCollection()); }
            set
            {
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException("This operation cannot be performed after the request has been submitted.");
                }
                _HttpRequestHeaders = value;
            }
        }

        public bool RequestSubmitted { get; private set; }

        public override string Method
        {
            get { return _method ?? "GET"; }
            set
            {
                if (validHttpVerbs.Contains(value))
                {
                    _method = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"'{value}' is not a known HTTP verb.");
                }
            }
        }

        public override long ContentLength { get; set; }

        public override string ContentType { get; set; }
        public bool AllowAutoRedirect { get; set; } = true;

        public override WebResponse GetResponse()
        {
            if (Proxy == null)
            {
                throw new InvalidOperationException("Proxy property cannot be null.");
            }
            if (string.IsNullOrEmpty(Method))
            {
                throw new InvalidOperationException("Method has not been set.");
            }

            if (RequestSubmitted)
            {
                return _response;
            }
            _response = InternalGetResponse();
            RequestSubmitted = true;
            return _response;
        }

        public override Stream GetRequestStream()
        {
            if (RequestSubmitted)
            {
                throw new InvalidOperationException("This operation cannot be performed after the request has been submitted.");
            }

            if (_requestContentBuffer == null)
            {
                if (ContentLength == 0)
                {
                    _requestContentStream = new NeverEndingStream();
                    return _requestContentStream;
                }

                _requestContentBuffer = new byte[ContentLength];
            }
            else if (ContentLength == default(long))
            {
                _requestContentBuffer = new byte[int.MaxValue];
            }
            else if (_requestContentBuffer.Length != ContentLength)
            {
                Array.Resize(ref _requestContentBuffer, (int)ContentLength);
            }
            return new MemoryStream(_requestContentBuffer);
        }

        public override IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        {
            if (Proxy == null)
            {
                throw new InvalidOperationException("Proxy property cannot be null.");
            }
            if (string.IsNullOrEmpty(Method))
            {
                throw new InvalidOperationException("Method has not been set.");
            }
           
            var task = Task.Factory.StartNew(() => {
                if (RequestSubmitted)
                {
                    return _response;
                }
                _response = InternalGetResponse();
                RequestSubmitted = true;
                return _response;
            });

            //var task = Task.Run<WebResponse>(() =>
            //{
            //    if (RequestSubmitted)
            //    {
            //        return _response;
            //    }
            //    _response = InternalGetResponse();
            //    RequestSubmitted = true;
            //    return _response;
            //});

            return task.AsApm(callback, state);
        }

        public override WebResponse EndGetResponse(IAsyncResult asyncResult)
        {
            var task = asyncResult as Task<WebResponse>;

            try
            {
                return task.Result;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is IOException || ex.InnerException is System.Net.Sockets.SocketException)
                    throw new WebException("Proxy error " + ex.InnerException.Message, ex.InnerException, WebExceptionStatus.ConnectFailure,
                        SocksHttpWebResponse.CreateErrorResponse(HttpStatusCode.GatewayTimeout));
                throw ex.InnerException;
            }
        }

        public new static WebRequest Create(string requestUri)
        {
            return new Socks4HttpWebRequest(new Uri(requestUri));
        }

        public new static WebRequest Create(Uri requestUri)
        {
            return new Socks4HttpWebRequest(requestUri);
        }

        private void SetSpecialHeaders(string headerName, string value)
        {
            _HttpRequestHeaders.Remove(headerName);
            if (value.Length != 0)
            {
                _HttpRequestHeaders.Add(headerName, value);
            }
        }

        private string BuildHttpRequestMessage(Uri requestUri)
        {
            if (RequestSubmitted)
            {
                throw new InvalidOperationException("This operation cannot be performed after the request has been submitted.");
            }

            // See if we have a stream instead of byte array
            if (_requestContentBuffer == null && _requestContentStream != null)
            {
                _requestContentBuffer = _requestContentStream.ToArray();
                _requestContentStream.ForceClose();
                _requestContentStream.Dispose();
                _requestContentStream = null;
                ContentLength = _requestContentBuffer.Length;
            }

            var message = new StringBuilder();
            message.AppendFormat("{0} {1} HTTP/1.1\r\nHost: {2}\r\n", Method, requestUri, requestUri.Host);

            Headers.Set(HttpRequestHeader.Connection, "close");

            // add the headers
            foreach (var key in Headers.Keys)
            {
                string value = Headers[key.ToString()];
                if (!string.IsNullOrEmpty(value))
                    message.AppendFormat("{0}: {1}\r\n", key, value);
            }

            if (!string.IsNullOrEmpty(ContentType))
            {
                message.AppendFormat("Content-Type: {0}\r\n", ContentType);
            }
            if (ContentLength > 0)
            {
                message.AppendFormat("Content-Length: {0}\r\n", ContentLength);
            }

            // add a blank line to indicate the end of the headers
            message.Append("\r\n");

            // add content
            if (_requestContentBuffer != null && _requestContentBuffer.Length > 0)
            {
                using (var stream = new MemoryStream(_requestContentBuffer, false))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        message.Append(reader.ReadToEnd());
                    }
                }
            }
            else if (_requestContentStream != null)
            {
                _requestContentStream.Position = 0;

                using (var reader = new StreamReader(_requestContentStream))
                {
                    message.Append(reader.ReadToEnd());
                }


                _requestContentStream.ForceClose();
                _requestContentStream.Dispose();
            }

            return message.ToString();
        }

        private SocksHttpWebResponse InternalGetResponse()
        {
            Uri requestUri = RequestUri;

            int redirects = 0;
            const int maxAutoredirectCount = 10;
            while (redirects++ < maxAutoredirectCount)
            {
                // Loop while redirecting

                var proxyUri = Proxy.GetProxy(requestUri);
                var ipAddress = GetProxyIpAddress(proxyUri);
                var response2 = new List<byte>();

                using (var client = new TcpClient(ipAddress.ToString(), proxyUri.Port))
                {
                    int timeout = Timeout;
                    if (timeout == 0)
                        timeout = 30 * 1000;
                    client.ReceiveTimeout = timeout;
                    client.SendTimeout = timeout;
                    var networkStream = client.GetStream();
                    // auth
                    // +----+----+----+----+----+----+----+----+----+----+....+----+
                    // | VN | CD | DSTPORT |      DSTIP        | USERID       |NULL|
                    // +----+----+----+----+----+----+----+----+----+----+....+----+
                    //    1    1      2              4           variable       1
                    var request = new byte[9];
                    byte[] userId = new byte[0];

                    request[0] = 0x04; // Version
                    request[1] = 0x01; // NMETHODS
                    var destIP = Dns.GetHostEntry(requestUri.DnsSafeHost).AddressList[0];
                    var ipBytes = destIP.GetAddressBytes();
                    var port = Uri.UriSchemeHttps == requestUri.Scheme ? 443 : 80;
                    request[2] = (byte)(port / 256);
                    request[3] = (byte)(port % 256);
                    ipBytes.CopyTo(request, 4);
                    request[8 + userId.Length] = 0x00;

                    //index += (ushort)ipBytes.Length;
                    networkStream.Write(request, 0, request.Length);

                    // response
                    // +----+----+----+----+----+----+----+----+
                    // | VN | CD | DSTPORT |      DSTIP        |
                    // +----+----+----+----+----+----+----+----+
                    //   1    1       2          

                    byte[] response = new byte[8];

                    networkStream.Read(response, 0, response.Length);
                    if (response[0] != 0)
                    {
                        throw new IOException("Invalid Socks Version");
                    }
                    //if (response[1] == 91|| response[1] == 92|| response[1] == 93)
                    //{
                    //    throw new IOException("Socks Server does not support no-auth");
                    //}
                    if (response[1] != 0x5a)
                    {
                        throw new Exception("Socks Server did choose bogus auth");
                    }
                    //networkStream.Read(request, 0, 2);
                    //var rport = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(request, 0));

                    //var rdest = string.Empty;
                    //networkStream.Read(request, 0, 4);
                    //var v4 = BitConverter.ToUInt32(request, 0);
                    //rdest = new IPAddress(v4).ToString();
                    

                    Stream readStream = null;
                    if (Uri.UriSchemeHttps == requestUri.Scheme)
                    {
                        var ssl = new SslStream(networkStream);
                        ssl.AuthenticateAsClient(requestUri.DnsSafeHost);
                        readStream = ssl;
                    }
                    else
                    {
                        readStream = networkStream;
                    }

                    string requestString = BuildHttpRequestMessage(requestUri);

                    var request1 = Encoding.ASCII.GetBytes(requestString);
                    readStream.Write(request1, 0, request1.Length);
                    readStream.Flush();

                    var buffer = new byte[client.ReceiveBufferSize];

                    var readlen = 0;
                    do
                    {
                        readlen = readStream.Read(buffer, 0, buffer.Length);
                        response2.AddRange(buffer.Take(readlen));
                    } while (readlen != 0);

                    readStream.Close();
                }

                var webResponse = new SocksHttpWebResponse(requestUri, response2.ToArray());

                if (webResponse.StatusCode == HttpStatusCode.Moved || webResponse.StatusCode == HttpStatusCode.MovedPermanently)
                {
                    string redirectUrl = webResponse.Headers["Location"];
                    if (string.IsNullOrEmpty(redirectUrl))
                        throw new WebException("Missing location for redirect");

                    requestUri = new Uri(requestUri, redirectUrl);
                    if (AllowAutoRedirect)
                    {
                        continue;
                    }
                    return webResponse;
                }

                if ((int)webResponse.StatusCode < 200 || (int)webResponse.StatusCode > 299)
                    throw new WebException(webResponse.StatusDescription, null, WebExceptionStatus.UnknownError, webResponse);

                return webResponse;
            }

            throw new WebException("Too many redirects", null, WebExceptionStatus.ProtocolError, SocksHttpWebResponse.CreateErrorResponse(HttpStatusCode.BadRequest));
        }

        private static IPAddress GetProxyIpAddress(Uri proxyUri)
        {
            IPAddress ipAddress;
            if (!IPAddress.TryParse(proxyUri.Host, out ipAddress))
            {
                try
                {
                    return Dns.GetHostEntry(proxyUri.Host).AddressList[0];
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Unable to resolve proxy hostname '{proxyUri.Host}' to a valid IP address.", e);
                }
            }
            return ipAddress;
        }
    
    }
}
