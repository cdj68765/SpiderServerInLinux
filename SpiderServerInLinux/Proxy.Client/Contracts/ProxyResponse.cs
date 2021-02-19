﻿using System.Collections.Generic;
using System.Net;

namespace Proxy.Client.Contracts
{
    /// <summary>
    /// Proxy Response class.
    /// </summary>
    public class ProxyResponse
    {
        /// <summary>
        /// Response status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Response headers.
        /// </summary>
        public IEnumerable<ProxyHeader> Headers { get; }

        /// <summary>
        /// Response cookies.
        /// </summary>
        public IEnumerable<Cookie> Cookies { get; }

        /// <summary>
        /// Response content.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Proxy time measurements.
        /// </summary>
        public Timings Timings { get; internal set; }
        public byte[] Bin { get; }

        private ProxyResponse(HttpStatusCode statusCode, IEnumerable<ProxyHeader> headers, byte[] Byte, IEnumerable<Cookie> cookies, string content)
        {
            StatusCode = statusCode;
            Headers = headers;
            Cookies = cookies;
            Content = content;
            Bin = Byte;
        }

        /// <summary>
        /// Factory Create method that creates a proxy response object instance.
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="headers"></param>
        /// <param name="cookies"></param>
        /// <param name="content"></param>
        /// <returns>Proxy Response without Timings</returns>
        /// <remarks>Timings is not in the Create method as it is assigned at a later stage when this is invoked.</remarks>
        public static ProxyResponse Create(HttpStatusCode statusCode, IEnumerable<ProxyHeader> headers, byte[] Byte, IEnumerable<Cookie> cookies, string content)
        {
            return new ProxyResponse(statusCode, headers, Byte, cookies, content);
        }
    }
}
