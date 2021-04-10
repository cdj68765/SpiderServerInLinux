using System;
using System.IO;
using System.Runtime.InteropServices;
using Shadowsocks.Controller;
using Shadowsocks.Util;

namespace Shadowsocks.Encryption
{
    public static class Sodium
    {
        private const string DLLNAME = "libsodium";

        private static bool _initialized = false;
        private static readonly object _initLock = new object();

        public static bool AES256GCMAvailable { get; private set; } = false;

        static Sodium()
        {
        }

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sodium_init();

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int crypto_aead_aes256gcm_is_available();

        #region AEAD

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sodium_increment(byte[] n, int nlen);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int crypto_aead_chacha20poly1305_ietf_encrypt(byte[] c, ref ulong clen_p, byte[] m,
            ulong mlen, byte[] ad, ulong adlen, byte[] nsec, byte[] npub, byte[] k);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int crypto_aead_chacha20poly1305_ietf_decrypt(byte[] m, ref ulong mlen_p,
            byte[] nsec, byte[] c, ulong clen, byte[] ad, ulong adlen, byte[] npub, byte[] k);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int crypto_aead_xchacha20poly1305_ietf_encrypt(byte[] c, ref ulong clen_p, byte[] m, ulong mlen,
            byte[] ad, ulong adlen, byte[] nsec, byte[] npub, byte[] k);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int crypto_aead_xchacha20poly1305_ietf_decrypt(byte[] m, ref ulong mlen_p, byte[] nsec, byte[] c,
            ulong clen, byte[] ad, ulong adlen, byte[] npub, byte[] k);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int crypto_aead_aes256gcm_encrypt(byte[] c, ref ulong clen_p, byte[] m, ulong mlen,
            byte[] ad, ulong adlen, byte[] nsec, byte[] npub, byte[] k);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int crypto_aead_aes256gcm_decrypt(byte[] m, ref ulong mlen_p, byte[] nsec, byte[] c,
            ulong clen, byte[] ad, ulong adlen, byte[] npub, byte[] k);

        #endregion AEAD

        #region Stream

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int crypto_stream_salsa20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic,
            byte[] k);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int crypto_stream_chacha20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic,
            byte[] k);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int crypto_stream_chacha20_ietf_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, uint ic,
            byte[] k);

        #endregion Stream
    }
}