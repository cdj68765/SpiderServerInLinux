﻿using Proxy.Client.Contracts;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Proxy.Client
{
    /// <summary>
    /// Proxy Client Interface
    /// </summary>
    public interface IProxyClient : IDisposable
    {
        /// <summary>
        /// Host name or IP address of the proxy server.
        /// </summary>
        string ProxyHost { get; }

        /// <summary>
        /// Port used to connect to the proxy server.
        /// </summary>
        int ProxyPort { get; }

        /// <summary>
        /// The type of proxy.
        /// </summary>
        ProxyType ProxyType { get; }

        /// <summary>
        /// Proxy Protocol.
        /// </summary>
        ProxyScheme Scheme { get; }

        /// <summary>
        /// Host name or IP address of the destination server.
        /// </summary>
        string DestinationHost { get; }

        /// <summary>
        /// Port used to connect to the destination server.
        /// </summary>
        int DestinationPort { get; }

        /// <summary>
        /// URL Query.
        /// </summary>
        string UrlQuery { get; }

        /// <summary>
        /// Socket Connect Timeout.
        /// </summary>
        int ConnectTimeout { get; }

        /// <summary>
        /// Socket Read Timeout.
        /// </summary>
        int ReadTimeout { get; }

        /// <summary>
        /// Socket Write Timeout.
        /// </summary>
        int WriteTimeout { get; }

        /// <summary>
        /// Connects to the proxy client, sends the GET command to the destination server and returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="isKeepAlive">Indicates whether the connetion is to be disposed or kept alive.</param>
        /// <param name="headers">Headers to be sent with the GET command.</param>
        /// <param name="cookies">Cookies to be sent with the GET command.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        ProxyResponse Get(string url, bool isKeepAlive = true, IEnumerable<ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
            int readTimeout = 10000, int writeTimeout = 10000);

        /// <summary>
        /// Asynchronously connects to the proxy client, sends the GET command to the destination server and returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="isKeepAlive">Indicates whether the connetion is to be disposed or kept alive.</param>
        /// <param name="headers">Headers to be sent with the GET command.</param>
        /// <param name="cookies">Cookies to be sent with the GET command.</param>
        /// <param name="totalTimeout">Total Request Timeout in ms.</param>
        /// <param name="connectTimeout">Connect Timeout in ms.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        Task<ProxyResponse> GetAsync(string url, bool isKeepAlive = true, IEnumerable <ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
            int totalTimeout = 60000, int connectTimeout = 45000, int readTimeout = 10000, int writeTimeout = 10000);

        /// <summary>
        /// Connects to the proxy client, sends the POST command to the destination server and returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="body">Body to be sent with the POST command.</param>
        /// <param name="isKeepAlive">Indicates whether the connetion is to be disposed or kept alive.</param>
        /// <param name="headers">Headers to be sent with the POST command.</param>
        /// <param name="cookies">Cookies to be sent with the POST command.</param> 
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        ProxyResponse Post(string url, string body, bool isKeepAlive = true, IEnumerable <ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
            int readTimeout = 10000, int writeTimeout = 10000);

        /// <summary>
        /// Asynchronously connects to the proxy client, sends the POST command to the destination server and returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="body">Body to be sent with the POST request.</param>
        /// <param name="isKeepAlive">Indicates whether the connetion is to be disposed or kept alive.</param>
        /// <param name="headers">Headers to be sent with the POST command.</param>
        /// <param name="cookies">Cookies to be sent with the POST command.</param>
        /// <param name="totalTimeout">Total Request Timeout in ms.</param>
        /// <param name="connectTimeout">Connect Timeout in ms.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        Task<ProxyResponse> PostAsync(string url, string body, bool isKeepAlive = true, IEnumerable <ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
            int totalTimeout = 60000, int connectTimeout = 45000, int readTimeout = 10000, int writeTimeout = 10000);

        /// <summary>
        /// Connects to the proxy client, sends the PUT command to the destination server and returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="body">Body to be sent with the PUT command.</param>
        /// <param name="isKeepAlive">Indicates whether the connetion is to be disposed or kept alive.</param>
        /// <param name="headers">Headers to be sent with the PUT command.</param>
        /// <param name="cookies">Cookies to be sent with the PUT command.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        ProxyResponse Put(string url, string body, bool isKeepAlive = true, IEnumerable <ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
            int readTimeout = 10000, int writeTimeout = 10000);

        /// <summary>
        /// Asynchronously connects to the proxy client, sends the PUT command to the destination server and returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="body">Body to be sent with the PUT request.</param>
        /// <param name="isKeepAlive">Indicates whether the connetion is to be disposed or kept alive.</param>
        /// <param name="headers">Headers to be sent with the PUT command.</param>
        /// <param name="cookies">Cookies to be sent with the PUT command.</param>
        /// <param name="totalTimeout">Total Request Timeout in ms.</param>
        /// <param name="connectTimeout">Connect Timeout in ms.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        Task<ProxyResponse> PutAsync(string url, string body, bool isKeepAlive = true, IEnumerable <ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
            int totalTimeout = 60000, int connectTimeout = 45000, int readTimeout = 10000, int writeTimeout = 10000);

        /// <summary>
        /// Connects to the proxy client, sends the DELETE command to the destination server and returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="isKeepAlive">Indicates whether the connetion is to be disposed or kept alive.</param>
        /// <param name="headers">Headers to be sent with the DELETE command.</param>
        /// <param name="cookies">Cookies to be sent with the DELETE command.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        ProxyResponse Delete(string url, bool isKeepAlive = true, IEnumerable<ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
            int readTimeout = 10000, int writeTimeout = 10000);

        /// <summary>
        /// Asynchronously connects to the proxy client, sends the DELETE command to the destination server and returns the response.
        /// </summary>
        /// <param name="url">Destination URL.</param>
        /// <param name="isKeepAlive">Indicates whether the connetion is to be disposed or kept alive.</param>
        /// <param name="headers">Headers to be sent with the DELETE command.</param>
        /// <param name="cookies">Cookies to be sent with the DELETE command.</param>
        /// <param name="totalTimeout">Total Request Timeout in ms.</param>
        /// <param name="connectTimeout">Connect Timeout in ms.</param>
        /// <param name="readTimeout">Read Timeout in ms.</param>
        /// <param name="writeTimeout">Write Timeout in ms.</param>
        /// <returns>Proxy Response</returns>
        Task<ProxyResponse> DeleteAsync(string url, bool isKeepAlive = true, IEnumerable<ProxyHeader> headers = null, IEnumerable<Cookie> cookies = null,
            int totalTimeout = 60000, int connectTimeout = 45000, int readTimeout = 10000, int writeTimeout = 10000);
    }
}
