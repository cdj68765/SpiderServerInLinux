﻿using Proxy.Client.Contracts.Constants;
using Proxy.Client.Exceptions;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Proxy.Client.Utilities.Extensions
{
    /// <summary>
    /// Extension class that performs socket operations.
    /// </summary>
    internal static class SocketExtensions
    {
        private const int BufferSize = 500;
        private const int PeekBufferSize = 10;

        /// <summary>
        /// Receives the response from the destination server.
        /// </summary>
        /// <param name="socket">Underlying socket.</param>
        /// <returns>The raw response and the time to first byte</returns>
        public static (string response, float firstByteTime) ReceiveAll(this Socket socket)
        {
            var buffer = new byte[BufferSize].AsMemory();
            var placeHolder = new StringBuilder();
            var bytesRead = 0;

            var firstByteTime = TimingHelper.Measure(() =>
            {
                bytesRead = socket.Receive(buffer.Span);
            });

            if (bytesRead == 0)
                throw new ProxyException("Destination Server has no data to send.");

            var bufferString = Encoding.ASCII.GetString(buffer.Span.Slice(0, bytesRead));
            placeHolder.Append(bufferString);

            while (!bufferString.Contains(RequestConstants.CONTENT_SEPERATOR))
            {
                bytesRead = socket.Receive(buffer.Span);

                if (bytesRead == 0)
                    return (placeHolder.ToString(), firstByteTime);

                bufferString = Encoding.ASCII.GetString(buffer.Span.Slice(0, bytesRead));
                placeHolder.Append(bufferString);
            }

            var readString = placeHolder.ToString();

            if (readString.Contains(RequestConstants.CONTENT_LENGTH_HEADER)) socket.DecodeContentLength(placeHolder, buffer.Span, bufferString);
            else if (readString.Contains(RequestConstants.TRANSFER_ENCODING_CHUNKED_HEADER)) socket.DecodeChunked(placeHolder, buffer.Span, bufferString);
            else throw new ProxyException("Unknown Transfer Encoding provided by Destination Server.");

            return (placeHolder.ToString(), firstByteTime);
        }

        /// <summary>
        /// Asynchronously receives the response from the destination server.
        /// </summary>
        /// <param name="socket">Underlying socket.</param>
        /// <param name="readTimeout">Socket Read Timeout.</param>
        /// <param name="cancellationTokenSourceManager">Cancellation Token Source manager.</param>
        /// <returns>The raw response and the time to first byte</returns>
        public static async Task<(string response, float firstByteTime, byte[] Byte)> ReceiveAllAsync(this Socket socket, int readTimeout, CancellationTokenSourceManager cancellationTokenSourceManager)
        {
            var buffer = new byte[BufferSize].AsMemory();
            var placeHolder = new StringBuilder();
            var bytesRead = 0;
            var MemoryStream = new System.IO.MemoryStream();

            var firstByteTime = await TimingHelper.MeasureAsync(async () =>
            {
                bytesRead = await socket.ReceiveAsync(buffer, readTimeout, cancellationTokenSourceManager);
            });

            if (bytesRead == 0)
                throw new ProxyException("Destination Server has no data to send.");
            MemoryStream.Write(buffer.Span.Slice(0, bytesRead).ToArray(), 0, bytesRead);
            var bufferString = Encoding.ASCII.GetString(buffer.Span.Slice(0, bytesRead));
            placeHolder.Append(bufferString);

            while (!bufferString.Contains(RequestConstants.CONTENT_SEPERATOR))
            {
                bytesRead = await socket.ReceiveAsync(buffer, readTimeout, cancellationTokenSourceManager);

                if (bytesRead == 0)
                    return (placeHolder.ToString(), firstByteTime, MemoryStream.ToArray());

                bufferString = Encoding.ASCII.GetString(buffer.Span.Slice(0, bytesRead));
                MemoryStream.Write(buffer.Span.Slice(0, bytesRead).ToArray(), 0, bytesRead);

                placeHolder.Append(bufferString);
            }

            var readString = placeHolder.ToString();

            if (readString.Contains(RequestConstants.CONTENT_LENGTH_HEADER)) await socket.DecodeContentLengthAsync(placeHolder, buffer, bufferString, readTimeout, MemoryStream, cancellationTokenSourceManager);
            else if (readString.Contains(RequestConstants.TRANSFER_ENCODING_CHUNKED_HEADER)) await socket.DecodeChunkedAsync(placeHolder, buffer, bufferString, readTimeout, cancellationTokenSourceManager);
            else throw new ProxyException("Unknown Transfer Encoding provided by Destination Server.");
            var RET = MemoryStream.ToArray();
            MemoryStream.Dispose();
            var Index = IndexesOf(RET, 0, new byte[] { 0x0d, 0x0a, 0x0d, 0x0a }).ToList().First();
            return (placeHolder.ToString(), firstByteTime, RET.AsMemory().Slice((int)(Index + 4), RET.Length - (int)Index - 4).ToArray());
        }

        public static System.Collections.Generic.IEnumerable<int> IndexOf2(byte[] source, int start, byte[] pattern)
        {
            for (int i = start; i < source.Length; i++)
            {
                if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                {
                    yield return i;
                }
            }
        }

        /// <summary>
        /// Receives the response from the destination server.
        /// </summary>
        /// <param name="sslStream">Underlying SSL encrypted stream.</param>
        /// <returns>The raw response and the time to first byte</returns>
        public static (string response, float firstByteTime) ReceiveAll(this SslStream sslStream)
        {
            var buffer = new byte[BufferSize].AsMemory();
            var placeHolder = new StringBuilder();
            var bytesRead = 0;

            var firstByteTime = TimingHelper.Measure(() =>
            {
                bytesRead = sslStream.Read(buffer.Span);
            });

            if (bytesRead == 0)
                throw new ProxyException("Destination Server has no data to send.");

            var bufferString = Encoding.ASCII.GetString(buffer.Span.Slice(0, bytesRead));
            placeHolder.Append(bufferString);

            while (!bufferString.Contains(RequestConstants.CONTENT_SEPERATOR))
            {
                bytesRead = sslStream.Read(buffer.Span);

                if (bytesRead == 0)
                    return (placeHolder.ToString(), firstByteTime);

                bufferString = Encoding.ASCII.GetString(buffer.Span.Slice(0, bytesRead));
                placeHolder.Append(bufferString);
            }

            var readString = placeHolder.ToString();

            if (readString.Contains(RequestConstants.CONTENT_LENGTH_HEADER)) sslStream.DecodeContentLength(placeHolder, buffer.Span, bufferString);
            else if (readString.Contains(RequestConstants.TRANSFER_ENCODING_CHUNKED_HEADER)) sslStream.DecodeChunked(placeHolder, buffer.Span, bufferString);
            else throw new ProxyException("Unknown Transfer Encoding provided by Destination Server.");

            return (placeHolder.ToString(), firstByteTime);
        }

        /// <summary>
        /// Asynchronously receives the response from the destination server.
        /// </summary>
        /// <param name="sslStream">Underlying SSL encrypted stream.</param>
        /// <param name="readTimeout">Socket Read Timeout.</param>
        /// <param name="cancellationTokenSourceManager">Cancellation Token Source manager.</param>
        /// <returns>The raw response and the time to first byte</returns>
        public static async Task<(string response, float firstByteTime, byte[] Byte)> ReceiveAllAsync(this SslStream sslStream, int readTimeout, CancellationTokenSourceManager cancellationTokenSourceManager)
        {
            var buffer = new byte[BufferSize].AsMemory();
            var placeHolder = new StringBuilder();
            var bytesRead = 0;
            Byte[] RB;
            var MemoryStream = new System.IO.MemoryStream();
            var firstByteTime = await TimingHelper.MeasureAsync(async () =>
            {
                bytesRead = await sslStream.ReadAsync(buffer, readTimeout, cancellationTokenSourceManager);
            });

            if (bytesRead == 0)
                throw new ProxyException("Destination Server has no data to send.");

            RB = buffer.Span.Slice(0, bytesRead).ToArray();
            MemoryStream.Write(RB, 0, RB.Length);
            var bufferString = Encoding.ASCII.GetString(buffer.Span.Slice(0, bytesRead));
            placeHolder.Append(bufferString);
            while (!bufferString.Contains(RequestConstants.CONTENT_SEPERATOR))
            {
                bytesRead = await sslStream.ReadAsync(buffer, readTimeout, cancellationTokenSourceManager);

                if (bytesRead == 0)
                    return (placeHolder.ToString(), firstByteTime, MemoryStream.ToArray());
                RB = buffer.Span.Slice(0, bytesRead).ToArray();
                MemoryStream.Write(RB, 0, RB.Length);
                bufferString = Encoding.ASCII.GetString(RB);
                placeHolder.Append(bufferString);
            }

            var readString = placeHolder.ToString();
            if (readString.Contains(RequestConstants.CONTENT_LENGTH_HEADER))
                await sslStream.DecodeContentLengthAsync(placeHolder, buffer, bufferString, readTimeout, MemoryStream, cancellationTokenSourceManager);
            else if (readString.Contains(RequestConstants.TRANSFER_ENCODING_CHUNKED_HEADER))
                await sslStream.DecodeChunkedAsync(placeHolder, buffer, bufferString, readTimeout, cancellationTokenSourceManager);
            else throw new ProxyException("Unknown Transfer Encoding provided by Destination Server.");
            var RET = MemoryStream.ToArray();
            var Index = IndexesOf(MemoryStream.ToArray(), 0, new byte[] { 0x0d, 0x0a, 0x0d, 0x0a }).ToList().First();
            MemoryStream.Dispose();
            return (placeHolder.ToString(), firstByteTime, RET.AsMemory().Slice((int)(Index + 4), RET.Length - (int)Index - 4).ToArray());
        }

        public static System.Collections.Generic.IEnumerable<long> IndexesOf(byte[] source, int start, byte[] pattern)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            long valueLength = source.LongLength;
            long patternLength = pattern.LongLength;

            if ((valueLength == 0) || (patternLength == 0) || (patternLength > valueLength))
            {
                yield break;
            }

            var badCharacters = new long[256];

            for (var i = 0; i < 256; i++)
            {
                badCharacters[i] = patternLength;
            }

            var lastPatternByte = patternLength - 1;

            for (long i = 0; i < lastPatternByte; i++)
            {
                badCharacters[pattern[i]] = lastPatternByte - i;
            }

            long index = start;

            while (index <= valueLength - patternLength)
            {
                for (var i = lastPatternByte; source[index + i] == pattern[i]; i--)
                {
                    if (i == 0)
                    {
                        yield return index;
                        break;
                    }
                }

                index += badCharacters[source[index + lastPatternByte]];
            }
        }

        /// <summary>
        /// Asynchronously Connects to the Destination Server given a connect timeout.
        /// </summary>
        /// <param name="socket">Underlying socket.</param>
        /// <param name="host">Destination Host.</param>
        /// <param name="port">Destination Port.</param>
        /// <param name="connectTimeout">Socket Connect Timeout.</param>
        /// <param name="cancellationTokenSourceManager">Cancellation Token Source manager.</param>
        public static async ValueTask ConnectAsync(this Socket socket, string host, int port, int connectTimeout, CancellationTokenSourceManager cancellationTokenSourceManager)
        {
            cancellationTokenSourceManager.Start(connectTimeout);
            await socket.ConnectAsync(host, port);
            cancellationTokenSourceManager.Stop();
        }

        /// <summary>
        /// Asynchronously sends data to the Destination Server given a write timeout.
        /// </summary>
        /// <param name="socket">Underlying socket.</param>
        /// <param name="buffer">Send request byte buffer.</param>
        /// <param name="writeTimeout">Socket Write Timeout.</param>
        /// <param name="cancellationTokenSourceManager">Cancellation Token Source manager.</param>
        /// <returns>Number of sent bytes</returns>
        public static async ValueTask<int> SendAsync(this Socket socket, ReadOnlyMemory<byte> buffer, int writeTimeout, CancellationTokenSourceManager cancellationTokenSourceManager)
        {
            cancellationTokenSourceManager.Start(writeTimeout);

            var sentBytes = await socket.SendAsync(buffer, SocketFlags.None, cancellationTokenSourceManager.Token);

            cancellationTokenSourceManager.Stop();

            return sentBytes;
        }

        /// <summary>
        /// Asynchronously reads data from the Destination Server given a read timeout.
        /// </summary>
        /// <param name="socket">Underlying socket.</param>
        /// <param name="buffer">Read byte buffer.</param>
        /// <param name="readTimeout">Socket Read Timeout.</param>
        /// <param name="cancellationTokenSourceManager">Cancellation Token Source manager.</param>
        /// <returns>Number of read bytes</returns>
        public static async ValueTask<int> ReceiveAsync(this Socket socket, Memory<byte> buffer, int readTimeout, CancellationTokenSourceManager cancellationTokenSourceManager)
        {
            cancellationTokenSourceManager.Start(readTimeout);

            var readBytes = await socket.ReceiveAsync(buffer, SocketFlags.None, cancellationTokenSourceManager.Token);

            cancellationTokenSourceManager.Stop();

            return readBytes;
        }

        /// <summary>
        /// Asynchronously sends data to the Destination Server through an SSL connection given a
        /// write timeout.
        /// </summary>
        /// <param name="sslStream">Underlying SSL Stream.</param>
        /// <param name="buffer">Send request byte buffer.</param>
        /// <param name="writeTimeout">Socket Write Timeout.</param>
        /// <param name="cancellationTokenSourceManager">Cancellation Token Source manager.</param>
        public static async ValueTask WriteAsync(this SslStream sslStream, ReadOnlyMemory<byte> buffer, int writeTimeout, CancellationTokenSourceManager cancellationTokenSourceManager)
        {
            cancellationTokenSourceManager.Start(writeTimeout);

            await sslStream.WriteAsync(buffer, cancellationTokenSourceManager.Token);

            cancellationTokenSourceManager.Stop();
        }

        /// <summary>
        /// Asynchronously reads data from the Destination Server through an SSL connection given a
        /// read timeout.
        /// </summary>
        /// <param name="sslStream">Underlying SSL Stream.</param>
        /// <param name="buffer">Read byte buffer.</param>
        /// <param name="readTimeout">Socket Read Timeout.</param>
        /// <param name="cancellationTokenSourceManager">Cancellation Token Source manager.</param>
        /// <returns>Number of read bytes</returns>
        public static async ValueTask<int> ReadAsync(this SslStream sslStream, Memory<byte> buffer, int readTimeout, CancellationTokenSourceManager cancellationTokenSourceManager)
        {
            cancellationTokenSourceManager.Start(readTimeout);

            var readBytes = await sslStream.ReadAsync(buffer, cancellationTokenSourceManager.Token);

            cancellationTokenSourceManager.Stop();

            return readBytes;
        }

        #region Content Decoding Methods

        #region Content Length Methods

        private static string DecodeContentLength(this Socket socket, StringBuilder placeHolder, Span<byte> buffer, string bufferString)
        {
            var (contentLength, totalBytesRead) = ExtractContentLength(placeHolder, bufferString);

            while (totalBytesRead < contentLength)
            {
                var innerBytesRead = socket.Receive(buffer, SocketFlags.None);
                totalBytesRead += innerBytesRead;
                placeHolder.Append(Encoding.ASCII.GetString(buffer.Slice(0, innerBytesRead)));
            }

            return placeHolder.ToString();
        }

        private static async Task<string> DecodeContentLengthAsync(this Socket socket, StringBuilder placeHolder, Memory<byte> buffer, string bufferString,
            int readTimeout, System.IO.MemoryStream mem, CancellationTokenSourceManager timeoutCancellationTokenSourceWrapper)
        {
            var (contentLength, totalBytesRead) = ExtractContentLength(placeHolder, bufferString);

            while (totalBytesRead < contentLength)
            {
                var innerBytesRead = await socket.ReceiveAsync(buffer, readTimeout, timeoutCancellationTokenSourceWrapper);
                totalBytesRead += innerBytesRead;
                var Byte = buffer.Span.Slice(0, innerBytesRead).ToArray();
                placeHolder.Append(Encoding.ASCII.GetString(Byte));
                mem.Write(Byte, 0, Byte.Length);
            }

            return placeHolder.ToString();
        }

        private static void DecodeContentLength(this SslStream sslStream, StringBuilder placeHolder, Span<byte> buffer, string bufferString)
        {
            var (contentLength, totalBytesRead) = ExtractContentLength(placeHolder, bufferString);

            while (totalBytesRead < contentLength)
            {
                var innerBytesRead = sslStream.Read(buffer);
                totalBytesRead += innerBytesRead;
                placeHolder.Append(Encoding.ASCII.GetString(buffer.Slice(0, innerBytesRead)));
            }
        }

        private static async Task DecodeContentLengthAsync(this SslStream sslStream, StringBuilder placeHolder, Memory<byte> buffer, string bufferString,
            int readTimeout, System.IO.MemoryStream memory, CancellationTokenSourceManager timeoutCancellationTokenSourceWrapper)
        {
            var (contentLength, totalBytesRead) = ExtractContentLength(placeHolder, bufferString);
            while (totalBytesRead < contentLength)
            {
                var innerBytesRead = await sslStream.ReadAsync(buffer, readTimeout, timeoutCancellationTokenSourceWrapper);
                totalBytesRead += innerBytesRead;
                var TempByte = buffer.Span.Slice(0, innerBytesRead).ToArray();
                placeHolder.Append(Encoding.ASCII.GetString(TempByte));
                memory.Write(TempByte, 0, TempByte.Length);
            }
        }

        private static (int contentLength, int bytesRead) ExtractContentLength(StringBuilder placeHolder, string bufferString)
        {
            var splitBuffer = bufferString.Split(new[] { RequestConstants.CONTENT_SEPERATOR }, 2, StringSplitOptions.RemoveEmptyEntries);

            var contentLengthString = Regex.Match(placeHolder.ToString(), RequestConstants.CONTENT_LENGTH_PATTERN).Value;
            var contentLength = Convert.ToInt32(contentLengthString, CultureInfo.InvariantCulture);
            var totalBytesRead = splitBuffer.Length == 1 ? 0 : splitBuffer[1].Length;

            return (contentLength, totalBytesRead);
        }

        #endregion Content Length Methods

        #region Chunked Methods

        private static void DecodeChunked(this Socket socket, StringBuilder placeHolder, Span<byte> buffer, string bufferString)
        {
            var splitBuffer = bufferString.Split(new[] { RequestConstants.CONTENT_SEPERATOR }, 2, StringSplitOptions.RemoveEmptyEntries);

            if (splitBuffer.Length == 1)
            {
                var peekBytesRead = socket.Receive(buffer.Slice(0, PeekBufferSize), SocketFlags.None);
                bufferString = Encoding.ASCII.GetString(buffer.Slice(0, peekBytesRead));
                placeHolder.Append(bufferString);
            }
            else
            {
                bufferString = splitBuffer[1];
            }

            var (chunkSize, totalBytesRead) = ExtractChunkSize(bufferString);

            while (chunkSize != 0)
            {
                while (totalBytesRead < chunkSize)
                {
                    var remainingReadSize = chunkSize - totalBytesRead;
                    var readSize = remainingReadSize > BufferSize ? BufferSize : remainingReadSize;

                    var innerBytesRead = socket.Receive(buffer.Slice(readSize), SocketFlags.None);
                    totalBytesRead += innerBytesRead;
                    placeHolder.Append(Encoding.ASCII.GetString(buffer.Slice(0, innerBytesRead)));
                }

                var peekBytesRead = socket.Receive(buffer.Slice(0, PeekBufferSize), SocketFlags.None);
                bufferString = Encoding.ASCII.GetString(buffer.Slice(0, peekBytesRead));

                (chunkSize, totalBytesRead) = ExtractChunkSize(bufferString);
            }
        }

        private static async Task DecodeChunkedAsync(this Socket socket, StringBuilder placeHolder, Memory<byte> buffer, string bufferString,
            int readTimeout, CancellationTokenSourceManager timeoutCancellationTokenSourceWrapper)
        {
            var splitBuffer = bufferString.Split(new[] { RequestConstants.CONTENT_SEPERATOR }, 2, StringSplitOptions.RemoveEmptyEntries);

            if (splitBuffer.Length == 1)
            {
                var peekBytesRead = await socket.ReceiveAsync(buffer.Slice(0, PeekBufferSize), readTimeout, timeoutCancellationTokenSourceWrapper);
                bufferString = Encoding.ASCII.GetString(buffer.Span.Slice(0, peekBytesRead));
                placeHolder.Append(bufferString);
            }
            else
            {
                bufferString = splitBuffer[1];
            }

            var (chunkSize, totalBytesRead) = ExtractChunkSize(bufferString);

            while (chunkSize != 0)
            {
                while (totalBytesRead < chunkSize)
                {
                    var remainingReadSize = chunkSize - totalBytesRead;
                    var readSize = remainingReadSize > BufferSize ? BufferSize : remainingReadSize;

                    var innerBytesRead = await socket.ReceiveAsync(buffer.Slice(0, readSize), readTimeout, timeoutCancellationTokenSourceWrapper);
                    totalBytesRead += innerBytesRead;
                    placeHolder.Append(Encoding.ASCII.GetString(buffer.Span.Slice(0, innerBytesRead)));
                }

                var peekBytesRead = await socket.ReceiveAsync(buffer.Slice(0, PeekBufferSize), readTimeout, timeoutCancellationTokenSourceWrapper);
                bufferString = Encoding.ASCII.GetString(buffer.Span.Slice(0, peekBytesRead));

                (chunkSize, totalBytesRead) = ExtractChunkSize(bufferString);
            }
        }

        private static void DecodeChunked(this SslStream ss, StringBuilder placeHolder, Span<byte> buffer, string bufferString)
        {
            var splitBuffer = bufferString.Split(new[] { RequestConstants.CONTENT_SEPERATOR }, 2, StringSplitOptions.RemoveEmptyEntries);

            if (splitBuffer.Length == 1)
            {
                var peekBytesRead = ss.Read(buffer.Slice(0, PeekBufferSize));
                bufferString = Encoding.ASCII.GetString(buffer.Slice(0, peekBytesRead));
                placeHolder.Append(bufferString);
            }
            else
            {
                bufferString = splitBuffer[1];
            }

            var (chunkSize, totalBytesRead) = ExtractChunkSize(bufferString);

            while (chunkSize != 0)
            {
                while (totalBytesRead < chunkSize)
                {
                    var remainingReadSize = chunkSize - totalBytesRead;
                    var readSize = remainingReadSize > BufferSize ? BufferSize : remainingReadSize;

                    var innerBytesRead = ss.Read(buffer.Slice(0, readSize));
                    totalBytesRead += innerBytesRead;
                    placeHolder.Append(Encoding.ASCII.GetString(buffer.Slice(0, innerBytesRead)));
                }

                var peekBytesRead = ss.Read(buffer.Slice(0, PeekBufferSize));
                bufferString = Encoding.ASCII.GetString(buffer.Slice(0, peekBytesRead));

                (chunkSize, totalBytesRead) = ExtractChunkSize(bufferString);
            }
        }

        private static async Task DecodeChunkedAsync(this SslStream sslStream, StringBuilder placeHolder, Memory<byte> buffer, string bufferString,
            int readTimeout, CancellationTokenSourceManager timeoutCancellationTokenSourceWrapper)
        {
            var splitBuffer = bufferString.Split(new[] { RequestConstants.CONTENT_SEPERATOR }, 2, StringSplitOptions.RemoveEmptyEntries);

            if (splitBuffer.Length == 1)
            {
                var peekBytesRead = await sslStream.ReadAsync(buffer.Slice(0, PeekBufferSize), readTimeout, timeoutCancellationTokenSourceWrapper);
                bufferString = Encoding.ASCII.GetString(buffer.Span.Slice(0, peekBytesRead));
                placeHolder.Append(bufferString);
            }
            else
            {
                bufferString = splitBuffer[1];
            }

            var (chunkSize, totalBytesRead) = ExtractChunkSize(bufferString);

            while (chunkSize != 0)
            {
                while (totalBytesRead < chunkSize)
                {
                    var remainingReadSize = chunkSize - totalBytesRead;
                    var readSize = remainingReadSize > BufferSize ? BufferSize : remainingReadSize;

                    var innerBytesRead = await sslStream.ReadAsync(buffer.Slice(0, readSize), readTimeout, timeoutCancellationTokenSourceWrapper);
                    totalBytesRead += innerBytesRead;
                    placeHolder.Append(Encoding.ASCII.GetString(buffer.Span.Slice(0, innerBytesRead)));
                }

                var peekBytesRead = await sslStream.ReadAsync(buffer.Slice(0, PeekBufferSize), readTimeout, timeoutCancellationTokenSourceWrapper);
                bufferString = Encoding.ASCII.GetString(buffer.Span.Slice(0, peekBytesRead));

                (chunkSize, totalBytesRead) = ExtractChunkSize(bufferString);
            }
        }

        private static (int chunkSize, int bytesRead) ExtractChunkSize(ReadOnlySpan<char> newContentLine)
        {
            var foundNewLine = newContentLine.IndexOf(Environment.NewLine);

            if (foundNewLine == -1)
                return (0, 0);

            int endChunkStringIndex = 0;
            var chunkSizeString = newContentLine.Slice(0, foundNewLine);
            var chunkSize = int.Parse(chunkSizeString, NumberStyles.HexNumber);
            var totalBytesRead = newContentLine.Length - (chunkSizeString.Length + 4);

            while (totalBytesRead > chunkSize)
            {
                endChunkStringIndex = chunkSize + (chunkSizeString.Length + 4);
                var endChunkStringIndexSlice = newContentLine.Slice(endChunkStringIndex);
                foundNewLine = endChunkStringIndexSlice.IndexOf(Environment.NewLine);
                chunkSizeString = endChunkStringIndexSlice.Slice(0, foundNewLine - endChunkStringIndex);
                chunkSize = int.Parse(chunkSizeString, NumberStyles.HexNumber);
            }

            var totalChunkBytesRead = totalBytesRead - endChunkStringIndex;

            return (chunkSize, totalChunkBytesRead);
        }

        #endregion Chunked Methods

        #endregion Content Decoding Methods
    }
}