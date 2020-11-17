﻿/*
Copyright © 2012-2015 Ruslan Khuduev <x-rus@list.ru>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocksSharp.Extensions;
using SocksSharp.Helpers;

namespace SocksSharp.Proxy.Response
{
    internal class ResponseBuilder : IResponseBuilder
    {
        private static readonly byte[] openHtmlSignature = Encoding.ASCII.GetBytes("<html");
        private static readonly byte[] closeHtmlSignature = Encoding.ASCII.GetBytes("</html>");
        private readonly int bufferSize;

        private readonly string newLine = "\r\n";

        private CancellationToken cancellationToken;
        private Stream commonStream;
        private Dictionary<string, List<string>> contentHeaders;

        private int contentLength;

        private readonly CookieContainer cookies;

        private NetworkStream networkStream;

        private readonly ReceiveHelper receiveHelper;

        private HttpResponseMessage response;
        private readonly Uri uri;

        public ResponseBuilder(int bufferSize, CookieContainer cookies = null, Uri uri = null)
        {
            this.bufferSize = bufferSize;

            this.cookies = cookies;
            this.uri = uri;

            contentLength = -1;
            receiveHelper = new ReceiveHelper(bufferSize);
        }

        public int ReceiveTimeout { get; set; }

        public Task<HttpResponseMessage> GetResponseAsync(HttpRequestMessage request, Stream stream)
        {
            return GetResponseAsync(request, stream, CancellationToken.None);
        }

        public Task<HttpResponseMessage> GetResponseAsync(HttpRequestMessage request, Stream stream,
            CancellationToken cancellationToken)
        {
            networkStream = stream as NetworkStream;
            commonStream = stream;

            this.cancellationToken = cancellationToken;

            receiveHelper.Init(stream);

            response = new HttpResponseMessage();
            contentHeaders = new Dictionary<string, List<string>>();

            response.RequestMessage = request;

            var task = Task.Run(() =>
            {
                ReceiveStartingLine();
                ReceiveHeaders();
                ReceiveContent();

                return response;
            });

            return task;
        }

        private void ReceiveStartingLine()
        {
            string startingLine;
            while (true)
            {
                startingLine = receiveHelper.ReadLine();
                if (startingLine.Length == 0)
                {
                    //throw exception;
                }
                else if (startingLine == newLine)
                {
                    continue;
                }
                else
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            var version = startingLine.Substring("HTTP/", " ");
            var statusCode = startingLine.Substring(" ", " ");
            if (statusCode.Length == 0)
                statusCode = startingLine.Substring(" ", newLine);
            if (version.Length == 0 || statusCode.Length == 0)
                throw new ProxyException("Received empty response");

            response.Version = Version.Parse(version);
            response.StatusCode = (HttpStatusCode) Enum.Parse(typeof(HttpStatusCode), statusCode);
        }

        private void ReceiveHeaders()
        {
            while (true)
            {
                var header = receiveHelper.ReadLine();

                if (header == newLine)
                    return;

                var separatorPos = header.IndexOf(':');
                if (separatorPos == -1)
                {
                    //string message = string.Format(
                    //Resources.HttpException_WrongHeader, header, Address.Host);
                    //throw NewHttpException(message);
                }
                var headerName = header.Substring(0, separatorPos);
                var headerValue = header.Substring(separatorPos + 1).Trim(' ', '\t', '\r', '\n');

                if (cookies != null && headerName.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                {
                    SetCookie(headerValue);
                }
                else if (ContentHelper.IsContentHeader(headerName))
                {
                    List<string> values;
                    if (contentHeaders.TryGetValue(headerName, out values))
                    {
                        values.Add(headerValue);
                    }
                    else
                    {
                        values = new List<string>();
                        values.Add(headerValue);

                        contentHeaders.Add(headerName, values);
                    }
                }
                else
                {
                    response.Headers.Add(headerName, headerValue);
                }


                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private void ReceiveContent()
        {
            if (contentHeaders.Count != 0)
            {
                contentLength = GetContentLength();


                var memoryStream = new MemoryStream(
                    contentLength == -1 ? 0 : contentLength);

                try
                {
                    var source = GetMessageBodySource();
                    foreach (var bytes in source)
                    {
                        memoryStream.Write(bytes.Value, 0, bytes.Length);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is InvalidOperationException)
                    {
                        //throw NewHttpException(Resources.HttpException_FailedReceiveMessageBody, ex);
                    }
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                response.Content = new StreamContent(memoryStream);
                foreach (var pair in contentHeaders)
                    response.Content.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
            }
        }

        private void SetCookie(string value)
        {
            if (value.Length == 0)
                return;

            // Ищем позицию, где заканчивается куки и начинается описание его параметров.
            var endCookiePos = value.IndexOf(';');

            // Ищем позицию между именем и значением куки.
            var separatorPos = value.IndexOf('=');

            if (separatorPos == -1)
                throw new Exception("Invalid cookie");

            string cookieValue;
            var cookieName = value.Substring(0, separatorPos);

            if (endCookiePos == -1)
            {
                cookieValue = value.Substring(separatorPos + 1);
            }
            else
            {
                cookieValue = value.Substring(separatorPos + 1,
                    endCookiePos - separatorPos - 1);

                #region Get cookie expires time

                var expiresPos = value.IndexOf("expires=");

                if (expiresPos != -1)
                {
                    string expiresStr;
                    var endExpiresPos = value.IndexOf(';', expiresPos);

                    expiresPos += 8;

                    if (endExpiresPos == -1)
                        expiresStr = value.Substring(expiresPos);
                    else
                        expiresStr = value.Substring(expiresPos, endExpiresPos - expiresPos);

                    DateTime expires;

                    // Если время куки вышло, то удаляем её.
                    if (DateTime.TryParse(expiresStr, out expires) &&
                        expires < DateTime.Now)
                    {
                        var collection = cookies.GetCookies(uri);
                        collection[cookieName].Expired = true;
                    }
                }

                #endregion
            }

            // Если куки нужно удалить.
            if (cookieValue.Length == 0 ||
                cookieValue.Equals("deleted", StringComparison.OrdinalIgnoreCase))
            {
                var collection = cookies.GetCookies(uri);
                collection[cookieName].Expired = true;
            }
            else
            {
                cookies.Add(new Cookie(cookieName, cookieValue, "/", uri.Host));
            }
        }

        private IEnumerable<BytesWraper> GetMessageBodySource()
        {
            if (contentHeaders.ContainsKey("Content-Encoding"))
                return GetMessageBodySourceZip();

            return GetMessageBodySourceStd();
        }

        // Загрузка сжатых данных.
        private IEnumerable<BytesWraper> GetMessageBodySourceZip()
        {
            if (contentHeaders.ContainsKey("Transfer-Encoding"))
                return ReceiveMessageBodyChunkedZip();

            if (contentLength != -1)
                return ReceiveMessageBodyZip(contentLength);

            var streamWrapper = new ZipWraperStream(commonStream, receiveHelper);

            return ReceiveMessageBody(GetZipStream(streamWrapper));
        }

        // Загрузка обычных данных.
        private IEnumerable<BytesWraper> GetMessageBodySourceStd()
        {
            if (contentHeaders.ContainsKey("Transfer-Encoding"))
                return ReceiveMessageBodyChunked();
            if (contentLength != -1)
                return ReceiveMessageBody(contentLength);

            return ReceiveMessageBody(commonStream);
        }

        private int GetContentLength()
        {
            List<string> values;
            int length;

            if (contentHeaders.TryGetValue("Content-Length", out values))
                if (int.TryParse(values[0], out length))
                    return length;

            return -1;
        }

        private string GetContentEncoding()
        {
            List<string> values;
            var encoding = "";

            if (contentHeaders.TryGetValue("Content-Encoding", out values))
                encoding = values[0];

            return encoding;
        }

        private void WaitData()
        {
            var sleepTime = 0;
            var delay = ReceiveTimeout < 10 ? 10 : ReceiveTimeout;

            var dataAvailable = networkStream?.DataAvailable;
            while (dataAvailable.HasValue && !dataAvailable.Value)
            {
                if (sleepTime >= delay)
                    throw new ProxyException("Wait data timeout");

                sleepTime += 10;
                Thread.Sleep(10);
            }
        }

        private sealed class BytesWraper
        {
            public int Length { get; set; }

            public byte[] Value { get; set; }
        }

        private sealed class ZipWraperStream : Stream
        {
            public ZipWraperStream(Stream baseStream, ReceiveHelper receiverHelper)
            {
                _baseStream = baseStream;
                _receiverHelper = receiverHelper;
            }

            #region Поля (закрытые)

            private readonly Stream _baseStream;
            private readonly ReceiveHelper _receiverHelper;

            #endregion


            #region Свойства (открытые)

            public int BytesRead { get; private set; }

            public int TotalBytesRead { get; set; }

            public int LimitBytesRead { get; set; }

            #region Переопределённые

            public override bool CanRead => _baseStream.CanRead;

            public override bool CanSeek => _baseStream.CanSeek;

            public override bool CanTimeout => _baseStream.CanTimeout;

            public override bool CanWrite => _baseStream.CanWrite;

            public override long Length => _baseStream.Length;

            public override long Position
            {
                get => _baseStream.Position;
                set => _baseStream.Position = value;
            }

            #endregion

            #endregion


            #region Методы (открытые)

            public override void Flush()
            {
                _baseStream.Flush();
            }

            public override void SetLength(long value)
            {
                _baseStream.SetLength(value);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _baseStream.Seek(offset, origin);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                // Если установлен лимит на количество считанных байт.
                if (LimitBytesRead != 0)
                {
                    var length = LimitBytesRead - TotalBytesRead;

                    // Если лимит достигнут.
                    if (length == 0)
                        return 0;

                    if (length > buffer.Length)
                        length = buffer.Length;

                    if (_receiverHelper.HasData)
                        BytesRead = _receiverHelper.Read(buffer, offset, length);
                    else
                        BytesRead = _baseStream.Read(buffer, offset, length);
                }
                else
                {
                    if (_receiverHelper.HasData)
                        BytesRead = _receiverHelper.Read(buffer, offset, count);
                    else
                        BytesRead = _baseStream.Read(buffer, offset, count);
                }

                TotalBytesRead += BytesRead;

                return BytesRead;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _baseStream.Write(buffer, offset, count);
            }

            #endregion
        }

        #region Receive Content (F*cking trash, but works (not sure (really)))

        // Загрузка тела сообщения неизвестной длины.
        private IEnumerable<BytesWraper> ReceiveMessageBody(Stream stream)
        {
            var bytesWraper = new BytesWraper();
            var buffer = new byte[bufferSize];
            bytesWraper.Value = buffer;
            var begBytesRead = 0;

            // Считываем начальные данные из тела сообщения.
            if (stream is GZipStream || stream is DeflateStream)
            {
                begBytesRead = stream.Read(buffer, 0, bufferSize);
            }
            else
            {
                if (receiveHelper.HasData)
                    begBytesRead = receiveHelper.Read(buffer, 0, bufferSize);
                if (begBytesRead < bufferSize)
                    begBytesRead += stream.Read(buffer, begBytesRead, bufferSize - begBytesRead);
            }
            // Возвращаем начальные данные.
            bytesWraper.Length = begBytesRead;
            yield return bytesWraper;
            // Проверяем, есть ли открывающий тег '<html'.
            // Если есть, то считываем данные то тех пор, пока не встретим закрывающий тек '</html>'.
            var isHtml = FindSignature(buffer, begBytesRead, openHtmlSignature);
            if (isHtml)
            {
                var found = FindSignature(buffer, begBytesRead, closeHtmlSignature);
                // Проверяем, есть ли в начальных данных закрывающий тег.
                if (found)
                    yield break;
            }
            while (true)
            {
                var bytesRead = stream.Read(buffer, 0, bufferSize);
                // Если тело сообщения представляет HTML.
                if (isHtml)
                {
                    if (bytesRead == 0)
                    {
                        WaitData();
                        continue;
                    }
                    var found = FindSignature(buffer, bytesRead, closeHtmlSignature);
                    if (found)
                    {
                        bytesWraper.Length = bytesRead;
                        yield return bytesWraper;
                        yield break;
                    }
                }
                else if (bytesRead == 0)
                {
                    yield break;
                }
                bytesWraper.Length = bytesRead;
                yield return bytesWraper;
            }
        }

        // Загрузка тела сообщения известной длины.
        private IEnumerable<BytesWraper> ReceiveMessageBody(int contentLength)
        {
            //Stream stream = _request.ClientStream;
            var bytesWraper = new BytesWraper();
            var buffer = new byte[bufferSize];
            bytesWraper.Value = buffer;

            var totalBytesRead = 0;
            while (totalBytesRead != contentLength)
            {
                int bytesRead;
                if (receiveHelper.HasData)
                    bytesRead = receiveHelper.Read(buffer, 0, bufferSize);
                else
                    bytesRead = commonStream.Read(buffer, 0, bufferSize);
                if (bytesRead == 0)
                {
                    WaitData();
                }
                else
                {
                    totalBytesRead += bytesRead;
                    bytesWraper.Length = bytesRead;
                    yield return bytesWraper;
                }
            }
        }

        // Загрузка тела сообщения частями.
        private IEnumerable<BytesWraper> ReceiveMessageBodyChunked()
        {
            //Stream stream = _request.ClientStream;
            var bytesWraper = new BytesWraper();
            var buffer = new byte[bufferSize];
            bytesWraper.Value = buffer;
            while (true)
            {
                var line = receiveHelper.ReadLine();
                // Если достигнут конец блока.
                if (line == newLine)
                    continue;
                line = line.Trim(' ', '\r', '\n');
                // Если достигнут конец тела сообщения.
                if (line == string.Empty)
                    yield break;
                int blockLength;
                var totalBytesRead = 0;

                #region Задаём длину блока

                try
                {
                    blockLength = Convert.ToInt32(line, 16);
                }
                catch (Exception ex)
                {
                    if (ex is FormatException || ex is OverflowException)
                    {
                        //throw NewHttpException(string.Format(
                        //Resources.HttpException_WrongChunkedBlockLength, line), ex);
                    }
                    throw;
                }

                #endregion

                // Если достигнут конец тела сообщения.
                if (blockLength == 0)
                    yield break;
                while (totalBytesRead != blockLength)
                {
                    var length = blockLength - totalBytesRead;
                    if (length > bufferSize)
                        length = bufferSize;
                    int bytesRead;
                    if (receiveHelper.HasData)
                        bytesRead = receiveHelper.Read(buffer, 0, length);
                    else
                        bytesRead = commonStream.Read(buffer, 0, length);
                    if (bytesRead == 0)
                    {
                        WaitData();
                    }
                    else
                    {
                        totalBytesRead += bytesRead;
                        bytesWraper.Length = bytesRead;
                        yield return bytesWraper;
                    }
                }
            }
        }

        private IEnumerable<BytesWraper> ReceiveMessageBodyZip(int contentLength)
        {
            var bytesWraper = new BytesWraper();
            var streamWrapper = new ZipWraperStream(commonStream, receiveHelper);
            using (var stream = GetZipStream(streamWrapper))
            {
                var buffer = new byte[bufferSize];
                bytesWraper.Value = buffer;

                while (true)
                {
                    var bytesRead = stream.Read(buffer, 0, bufferSize);
                    if (bytesRead == 0)
                        if (streamWrapper.TotalBytesRead == contentLength)
                        {
                            yield break;
                        }
                        else
                        {
                            WaitData();
                            continue;
                        }
                    bytesWraper.Length = bytesRead;
                    yield return bytesWraper;
                }
            }
        }

        private IEnumerable<BytesWraper> ReceiveMessageBodyChunkedZip()
        {
            var bytesWraper = new BytesWraper();
            var streamWrapper = new ZipWraperStream(commonStream, receiveHelper);

            using (var stream = GetZipStream(streamWrapper))
            {
                var buffer = new byte[bufferSize];
                bytesWraper.Value = buffer;
                while (true)
                {
                    var line = receiveHelper.ReadLine();
                    // Если достигнут конец блока.
                    if (line == newLine)
                        continue;
                    line = line.Trim(' ', '\r', '\n');
                    // Если достигнут конец тела сообщения.
                    if (line == string.Empty)
                        yield break;
                    int blockLength;

                    #region Задаём длину блока

                    try
                    {
                        blockLength = Convert.ToInt32(line, 16);
                    }
                    catch (Exception ex)
                    {
                        if (ex is FormatException || ex is OverflowException)
                        {
                            //throw NewHttpException(string.Format(
                            //Resources.HttpException_WrongChunkedBlockLength, line), ex);
                        }
                        throw;
                    }

                    #endregion

                    // Если достигнут конец тела сообщения.
                    if (blockLength == 0)
                        yield break;
                    streamWrapper.TotalBytesRead = 0;
                    streamWrapper.LimitBytesRead = blockLength;
                    while (true)
                    {
                        var bytesRead = stream.Read(buffer, 0, bufferSize);
                        if (bytesRead == 0)
                            if (streamWrapper.TotalBytesRead == blockLength)
                            {
                                break;
                            }
                            else
                            {
                                WaitData();
                                continue;
                            }
                        bytesWraper.Length = bytesRead;
                        yield return bytesWraper;
                    }
                }
            }
        }

        private Stream GetZipStream(Stream stream)
        {
            var contentEncoding = GetContentEncoding().ToLower();

            switch (contentEncoding)
            {
                case "gzip":
                    return new GZipStream(stream, CompressionMode.Decompress, true);
                case "deflate":
                    return new DeflateStream(stream, CompressionMode.Decompress, true);
                default:
                    throw new InvalidOperationException($"'{contentEncoding}' not supported encoding format");
            }
        }

        private bool FindSignature(byte[] source, int sourceLength, byte[] signature)
        {
            var length = sourceLength - signature.Length + 1;
            for (var sourceIndex = 0; sourceIndex < length; ++sourceIndex)
            for (var signatureIndex = 0; signatureIndex < signature.Length; ++signatureIndex)
            {
                var sourceByte = source[signatureIndex + sourceIndex];
                var sourceChar = (char) sourceByte;
                if (char.IsLetter(sourceChar))
                    sourceChar = char.ToLower(sourceChar);
                sourceByte = (byte) sourceChar;
                if (sourceByte != signature[signatureIndex])
                    break;
                if (signatureIndex == signature.Length - 1)
                    return true;
            }
            return false;
        }

        #endregion
    }
}