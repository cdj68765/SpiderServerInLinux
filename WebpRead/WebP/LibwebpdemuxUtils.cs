using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BotBiliBili.WebP
{
    internal class LibwebpdemuxUtils
    {
        //[DllImport("libwebpdemux_x86.dll", CallingConvention = CallingConvention.Cdecl)]
        //public extern static int WebPGetDemuxVersion_x86();
        //[DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        //public extern static int WebPGetDemuxVersion_x64();

        //public static int WebPGetDemuxVersion()
        //{
        //    switch (IntPtr.Size)
        //    {
        //        case 4:
        //            return WebPGetDemuxVersion_x86();
        //        case 8:
        //            return WebPGetDemuxVersion_x64();
        //        default:
        //            throw new InvalidOperationException("Invalid platform. Can not find proper function");
        //    }
        //}

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPGetDemuxVersion();

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr WebPDemuxInternal(IntPtr data, int allow_partial,
                             IntPtr state, int version);

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void WebPDemuxDelete(IntPtr dmux);

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPDemuxGetI(IntPtr dmux, WebPFormatFeature feature);

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPDemuxGetFrame(IntPtr dmux, int frame, IntPtr iter);

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPDemuxNextFrame(IntPtr iter);

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPDemuxPrevFrame(IntPtr iter);

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void WebPDemuxReleaseIterator(IntPtr iter);

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPDemuxGetChunk(IntPtr dmux,
                     [MarshalAs(UnmanagedType.LPArray, SizeConst = 4)] char[] fourcc, int chunk_num, IntPtr iter);

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPDemuxNextChunk(IntPtr iter);

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPDemuxPrevChunk(IntPtr iter);

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void WebPDemuxReleaseChunkIterator(IntPtr iter);

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPAnimDecoderOptionsInitInternal(IntPtr dec_options, int abi_version);

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr WebPAnimDecoderNewInternal(IntPtr webp_data, IntPtr dec_options, int abi_version);

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPAnimDecoderGetInfo(IntPtr dec, IntPtr info);

        [DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPAnimDecoderGetNext(IntPtr dec, ref byte[] buf_ptr, IntPtr timestamp_ptr);
    }
}