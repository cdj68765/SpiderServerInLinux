using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using System.Drawing;

namespace Shadowsocks.Util
{
    public struct BandwidthScaleInfo
    {
        public float value;
        public string unitName;
        public long unit;

        public BandwidthScaleInfo(float value, string unitName, long unit)
        {
            this.value = value;
            this.unitName = unitName;
            this.unit = unit;
        }
    }

    public static class Utils
    {
        private static string _tempPath = null;

        // return path to store temporary files
        public static string GetTempPath()
        {
            return _tempPath;
            if (_tempPath == null)
            {
                bool isPortableMode = Configuration.Load().portableMode;
                try
                {
                    if (isPortableMode)
                    {
                        //_tempPath = Directory.CreateDirectory("ss_win_temp").FullName;
                        // don't use "/", it will fail when we call explorer /select xxx/ss_win_temp\xxx.log
                    }
                    else
                    {
                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            }
            return _tempPath;
        }

        public enum WindowsThemeMode { Dark, Light }

        // return a full path with filename combined which pointed to the temporary directory
        public static string GetTempPath(string filename)
        {
            return Path.Combine(GetTempPath(), filename);
        }

        public static string UnGzip(byte[] buf)
        {
            byte[] buffer = new byte[1024];
            int n;
            using (MemoryStream sb = new MemoryStream())
            {
                using (GZipStream input = new GZipStream(new MemoryStream(buf),
                                                         CompressionMode.Decompress,
                                                         false))
                {
                    while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        sb.Write(buffer, 0, n);
                    }
                }
                return System.Text.Encoding.UTF8.GetString(sb.ToArray());
            }
        }

        public static string FormatBandwidth(long n)
        {
            var result = GetBandwidthScale(n);
            return $"{result.value:0.##}{result.unitName}";
        }

        public static string FormatBytes(long bytes)
        {
            const long K = 1024L;
            const long M = K * 1024L;
            const long G = M * 1024L;
            const long T = G * 1024L;
            const long P = T * 1024L;
            const long E = P * 1024L;

            if (bytes >= P * 990)
                return (bytes / (double)E).ToString("F5") + "EiB";
            if (bytes >= T * 990)
                return (bytes / (double)P).ToString("F5") + "PiB";
            if (bytes >= G * 990)
                return (bytes / (double)T).ToString("F5") + "TiB";
            if (bytes >= M * 990)
            {
                return (bytes / (double)G).ToString("F4") + "GiB";
            }
            if (bytes >= M * 100)
            {
                return (bytes / (double)M).ToString("F1") + "MiB";
            }
            if (bytes >= M * 10)
            {
                return (bytes / (double)M).ToString("F2") + "MiB";
            }
            if (bytes >= K * 990)
            {
                return (bytes / (double)M).ToString("F3") + "MiB";
            }
            if (bytes > K * 2)
            {
                return (bytes / (double)K).ToString("F1") + "KiB";
            }
            return bytes.ToString() + "B";
        }

        /// <summary>
        /// Return scaled bandwidth
        /// </summary>
        /// <param name="n">Raw bandwidth</param>
        /// <returns>The BandwidthScaleInfo struct</returns>
        public static BandwidthScaleInfo GetBandwidthScale(long n)
        {
            long scale = 1;
            float f = n;
            string unit = "B";
            if (f > 1024)
            {
                f = f / 1024;
                scale <<= 10;
                unit = "KiB";
            }
            if (f > 1024)
            {
                f = f / 1024;
                scale <<= 10;
                unit = "MiB";
            }
            if (f > 1024)
            {
                f = f / 1024;
                scale <<= 10;
                unit = "GiB";
            }
            if (f > 1024)
            {
                f = f / 1024;
                scale <<= 10;
                unit = "TiB";
            }
            return new BandwidthScaleInfo(f, unit, scale);
        }

        public static bool IsWinVistaOrHigher()
        {
            return Environment.OSVersion.Version.Major > 5;
        }

        // See: https://msdn.microsoft.com/en-us/library/hh925568(v=vs.110).aspx
    }
}