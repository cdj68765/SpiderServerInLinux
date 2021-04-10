using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ShadowsocksR.Controller
{
    public class FileManager
    {
        public static byte[] DeflateCompress(byte[] content, int index, int count, out int size)
        {
            size = 0;
            try
            {
                MemoryStream memStream = new MemoryStream();
                using (DeflateStream ds = new DeflateStream(memStream, CompressionMode.Compress))
                {
                    ds.Write(content, index, count);
                }
                byte[] buffer = memStream.ToArray();
                size = buffer.Length;
                return buffer;
            }
            catch (Exception _Exception)
            {
            }
            return null;
        }

        public static byte[] DeflateDecompress(byte[] content, int index, int count, out int size)
        {
            size = 0;
            try
            {
                byte[] buffer = new byte[16384];
                DeflateStream ds = new DeflateStream(new MemoryStream(content, index, count), CompressionMode.Decompress);
                int readsize;
                while (true)
                {
                    readsize = ds.Read(buffer, size, buffer.Length - size);
                    if (readsize == 0)
                    {
                        break;
                    }
                    size += readsize;
                    byte[] newbuffer = new byte[buffer.Length * 2];
                    buffer.CopyTo(newbuffer, 0);
                    buffer = newbuffer;
                }
                return buffer;
            }
            catch (Exception _Exception)
            {
            }
            return null;
        }
    }
}