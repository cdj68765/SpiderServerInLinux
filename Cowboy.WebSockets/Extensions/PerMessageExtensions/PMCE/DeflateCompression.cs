﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using Cowboy.Buffer;

namespace Cowboy.WebSockets.Extensions
{
    public class DeflateCompression
    {
        private readonly ISegmentBufferManager _bufferAllocator;

        public DeflateCompression(ISegmentBufferManager bufferAllocator)
        {
            if (bufferAllocator == null)
                throw new ArgumentNullException("bufferAllocator");
            _bufferAllocator = bufferAllocator;
        }

        public byte[] Compress(byte[] raw)
        {
            return Compress(raw, 0, raw.Length);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public byte[] Compress(byte[] raw, int offset, int count)
        {
            using (var memory = new MemoryStream())
            {
                using (var deflate = new DeflateStream(memory, CompressionMode.Compress, leaveOpen: true))
                {
                    deflate.Write(raw, offset, count);
                }

                return memory.ToArray();
            }
        }

        public byte[] Decompress(byte[] raw)
        {
            return Decompress(raw, 0, raw.Length);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public byte[] Decompress(byte[] raw, int offset, int count)
        {
            var buffer = _bufferAllocator.BorrowBuffer();

            try
            {
                using (var input = new MemoryStream(raw, offset, count))
                using (var deflate = new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true))
                using (var memory = new MemoryStream())
                {
                    int readCount = 0;
                    do
                    {
                        readCount = deflate.Read(buffer.Array, buffer.Offset, buffer.Count);
                        if (readCount > 0)
                        {
                            memory.Write(buffer.Array, buffer.Offset, readCount);
                        }
                    }
                    while (readCount > 0);

                    return memory.ToArray();
                }
            }
            finally
            {
                _bufferAllocator.ReturnBuffer(buffer);
            }
        }
    }
}
