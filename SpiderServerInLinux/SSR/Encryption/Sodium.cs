using ShadowsocksR.Controller;
using SpiderServerInLinux;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ShadowsocksR.Encryption
{
    public class Sodium
    {
        public static void crypto_stream_salsa20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic, byte[] k)
        {
            if (Setting.Platform)
            {
                LibApi.crypto_stream_salsa20_xor_ic(c, m, mlen, n, ic, k);
            }
            else
            {
                LibApiArm.crypto_stream_salsa20_xor_ic(c, m, mlen, n, ic, k);
            }
        }

        public static void crypto_stream_chacha20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic, byte[] k)
        {
            if (Setting.Platform)
            {
                LibApi.crypto_stream_chacha20_xor_ic(c, m, mlen, n, ic, k);
            }
            else
            {
                LibApiArm.crypto_stream_chacha20_xor_ic(c, m, mlen, n, ic, k);
            }
        }

        public static int crypto_stream_chacha20_ietf_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, uint ic, byte[] k)
        {
            try
            {
                if (Setting.Platform)
                {
                    return LibApi.crypto_stream_chacha20_ietf_xor_ic(c, m, mlen, n, ic, k);
                }
                else
                {
                    return LibApiArm.crypto_stream_chacha20_ietf_xor_ic(c, m, mlen, n, ic, k);
                }
            }
            catch (Exception e)
            {
                Loger.Instance.LocalInfo(e.Message);
            }
            return 0;
        }

        public class LibApi
        {
            [DllImport("libsscrypto64.dll", CallingConvention = CallingConvention.Cdecl)]
            public extern static void crypto_stream_salsa20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic, byte[] k);

            [DllImport("libsscrypto64.dll", CallingConvention = CallingConvention.Cdecl)]
            public extern static void crypto_stream_chacha20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic, byte[] k);

            [DllImport("libsscrypto64.dll", CallingConvention = CallingConvention.Cdecl)]
            public extern static int crypto_stream_chacha20_ietf_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, uint ic, byte[] k);
        }

        public class LibApiArm
        {
            private const string DLLNAME = "./libsodium.so";

            [DllImport(DLLNAME)]
            public extern static void crypto_stream_salsa20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic, byte[] k);

            [DllImport(DLLNAME)]
            public extern static void crypto_stream_chacha20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic, byte[] k);

            [DllImport(DLLNAME)]
            public extern static int crypto_stream_chacha20_ietf_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, uint ic, byte[] k);
        }
    }
}