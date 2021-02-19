﻿namespace Proxy.Client.Contracts.Constants
{
    /// <summary>
    /// Request Constants class.
    /// </summary>
    internal class RequestConstants
    {
        internal const string CONTENT_SEPERATOR = "\r\n\r\n";
        internal const string CONTENT_LENGTH_HEADER = "Content-Length";
        internal const string CONNECTION_HEADER = "Connection";
        internal const string PROXY_CONNECTION_HEADER = "Proxy-Connection";
        internal const string TRANSFER_ENCODING_CHUNKED_HEADER = "chunked";
        internal const string CONTENT_LENGTH_PATTERN = "(?<=Content-Length: )[0-9]*";
        internal const string SET_COOKIE_HEADER = "set-cookie";
        internal const string STATUS_CODE_PATTERN = "\\d\\d\\d";
    }
}
