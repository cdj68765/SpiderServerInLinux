using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Shadowsocks.Controller;
using Shadowsocks.Util;
using SpiderServerInLinux;

namespace Shadowsocks.Encryption
{
    public class MbedTLS
    {
        public const int MBEDTLS_ENCRYPT = 1;
        public const int MBEDTLS_DECRYPT = 0;
        public const int MD5_CTX_SIZE = 88;

        public const int MBEDTLS_MD_MD5 = 3;
        public const int MBEDTLS_MD_SHA1 = 4;
        public const int MBEDTLS_MD_SHA224 = 5;
        public const int MBEDTLS_MD_SHA256 = 6;
        public const int MBEDTLS_MD_SHA384 = 7;
        public const int MBEDTLS_MD_SHA512 = 8;
        public const int MBEDTLS_MD_RIPEMD160 = 9;

        public interface HMAC
        {
            byte[] ComputeHash(byte[] buffer, int offset, int count);
        }

        public class HMAC_MD5 : HMAC
        {
            private byte[] key;

            public HMAC_MD5(byte[] key)
            {
                this.key = key;
            }

            public byte[] ComputeHash(byte[] buffer, int offset, int count)
            {
                byte[] output = new byte[64];
                if (Setting.Platform)
                {
                    MD5API.ss_hmac_ex(MBEDTLS_MD_MD5, key, key.Length, buffer, offset, count, output);
                }
                else
                {
                    try
                    {
                        if (offset == 0)
                        {
                            MD5API.mbedtls_md_hmac(MD5API.mbedtls_md_info_from_type(MBEDTLS_MD_MD5), key, key.Length, buffer, count, output);
                        }
                        else
                        {
                            byte[] TempByte = new byte[buffer.Length - offset];
                            Array.ConstrainedCopy(buffer, offset, TempByte, 0, TempByte.Length);
                            MD5API.mbedtls_md_hmac(MD5API.mbedtls_md_info_from_type(MBEDTLS_MD_MD5), key, key.Length, TempByte, count, output);
                        }
                    }
                    catch (Exception ex)
                    {
                        Loger.Instance.ServerInfo("SSR", ex);
                    }
                }
                return output;
            }
        }

        public class HMAC_SHA1 : HMAC
        {
            private byte[] key;

            public HMAC_SHA1(byte[] key)
            {
                this.key = key;
            }

            public byte[] ComputeHash(byte[] buffer, int offset, int count)
            {
                byte[] output = new byte[64];
                if (Setting.Platform)
                {
                    MD5API.ss_hmac_ex(MBEDTLS_MD_SHA1, key, key.Length, buffer, offset, count, output);
                }
                else
                {
                    try
                    {
                        if (offset == 0)
                        {
                            MD5API.mbedtls_md_hmac(MD5API.mbedtls_md_info_from_type(MBEDTLS_MD_SHA1), key, key.Length, buffer, count, output);
                        }
                        else
                        {
                            byte[] TempByte = new byte[buffer.Length - offset];
                            Array.ConstrainedCopy(buffer, offset, TempByte, 0, TempByte.Length);
                            MD5API.mbedtls_md_hmac(MD5API.mbedtls_md_info_from_type(MBEDTLS_MD_SHA1), key, key.Length, TempByte, count, output);
                        }
                    }
                    catch (Exception ex)
                    {
                        Loger.Instance.ServerInfo("SSR", ex);
                    }
                }
                return output;
            }
        }

        public static byte[] MD5(byte[] input)
        {
            byte[] output = new byte[16];
            MD5CryptoServiceProvider _md5 = new MD5CryptoServiceProvider();
            var r = _md5.ComputeHash(input);
            return _md5.ComputeHash(input); ;
            /*   byte[] output = new byte[16];
               md5(input, input.Length, output);
               return output;*/
        }

        public static byte[] SHA1(byte[] input)
        {
            byte[] output = new byte[20];
            if (Setting.Platform)
            {
                MD5API.ss_md(MBEDTLS_MD_SHA1, input, 0, input.Length, output);
            }
            else
            {
                MD5API.mbedtls_md(MD5API.mbedtls_md_info_from_type(MBEDTLS_MD_SHA1), input, input.Length, output);
            }
            return output;
        }

        public static byte[] SHA512(byte[] input)
        {
            byte[] output = new byte[64];

            if (Setting.Platform)
            {
                MD5API.ss_md(MBEDTLS_MD_SHA512, input, 0, input.Length, output);
            }
            else
            {
                MD5API.mbedtls_md(MD5API.mbedtls_md_info_from_type(MBEDTLS_MD_SHA512), input, input.Length, output);
            }
            return output;
        }

        public static IntPtr cipher_info_from_string(string cipher_name)
        {
            if (Setting.Platform)
            {
                return MD5API.cipher_info_from_string(cipher_name);
            }
            else
            {
                return MD5API.mbedtls_cipher_info_from_string(cipher_name);
            }
        }

        public static void cipher_init(IntPtr ctx)
        {
            if (Setting.Platform)
            {
                MD5API.cipher_init(ctx);
            }
            else
            {
                MD5API.mbedtls_cipher_init(ctx);
            }
        }

        public static int cipher_setup(IntPtr ctx, IntPtr cipher_info)
        {
            if (Setting.Platform)
            {
                return MD5API.cipher_setup(ctx, cipher_info);
            }
            else
            {
                return MD5API.mbedtls_cipher_setup(ctx, cipher_info);
            }
        }

        public static int cipher_setkey(IntPtr ctx, byte[] key, int key_bitlen, int operation)
        {
            if (Setting.Platform)
            {
                return MD5API.cipher_setkey(ctx, key, key_bitlen, operation);
            }
            else
            {
                return MD5API.mbedtls_cipher_setkey(ctx, key, key_bitlen, operation);
            }
        }

        public static int cipher_set_iv(IntPtr ctx, byte[] iv, int iv_len)
        {
            if (Setting.Platform)
            {
                return MD5API.cipher_set_iv(ctx, iv, iv_len);
            }
            else
            {
                return MD5API.mbedtls_cipher_set_iv(ctx, iv, iv_len);
            }
        }

        public static int cipher_reset(IntPtr ctx)
        {
            if (Setting.Platform)
            {
                return MD5API.cipher_reset(ctx);
            }
            else
            {
                return MD5API.mbedtls_cipher_reset(ctx);
            }
        }

        public static int cipher_update(IntPtr ctx, byte[] input, int ilen, byte[] output, ref int olen)
        {
            if (Setting.Platform)
            {
                return MD5API.cipher_update(ctx, input, ilen, output, ref olen);
            }
            else
            {
                return MD5API.mbedtls_cipher_update(ctx, input, ilen, output, ref olen);
            }
        }

        public static void cipher_free(IntPtr ctx)
        {
            if (Setting.Platform)
            {
                MD5API.cipher_free(ctx);
            }
            else
            {
                MD5API.mbedtls_cipher_free(ctx);
            }
        }

        public static int cipher_get_size_ex()
        {
            if (Setting.Platform)
            {
                return MD5API.cipher_get_size_ex();
            }
            else
            {
                return 100;
            }
        }

        public class MD5API
        {
            private const string DLLNAME = "./libmbedcrypto.so";

            [DllImport(DLLNAME)]
            public static extern IntPtr mbedtls_cipher_info_from_string(string cipher_name);

            [DllImport(DLLNAME)]
            public static extern void mbedtls_cipher_init(IntPtr ctx);

            [DllImport(DLLNAME)]
            public static extern int mbedtls_cipher_setup(IntPtr ctx, IntPtr cipher_info);

            // XXX: Check operation before using it
            [DllImport(DLLNAME)]
            public static extern int mbedtls_cipher_setkey(IntPtr ctx, byte[] key, int key_bitlen, int operation);

            [DllImport(DLLNAME)]
            public static extern int mbedtls_cipher_set_iv(IntPtr ctx, byte[] iv, int iv_len);

            [DllImport(DLLNAME)]
            public static extern int mbedtls_cipher_reset(IntPtr ctx);

            [DllImport(DLLNAME)]
            public static extern int mbedtls_cipher_update(IntPtr ctx, byte[] input, int ilen, byte[] output, ref int olen);

            [DllImport(DLLNAME)]
            public static extern void mbedtls_cipher_free(IntPtr ctx);

            [DllImport(DLLNAME)]
            public static extern void mbedtls_md5(byte[] input, int ilen, byte[] output);

            [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern void mbedtls_md(IntPtr md_type, byte[] input, int ilen, byte[] output);

            [DllImport(DLLNAME)]
            public static extern IntPtr mbedtls_md_info_from_type(int type);

            [DllImport(DLLNAME)]
            public static extern void mbedtls_md_hmac(IntPtr md_type, byte[] key, int keylen, byte[] input, int ilen, byte[] output);

            public const string DLLNAME2 = "libsscrypto64";

            [DllImport(DLLNAME2, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr cipher_info_from_string(string cipher_name);

            [DllImport(DLLNAME2, CallingConvention = CallingConvention.Cdecl)]
            public static extern void cipher_init(IntPtr ctx);

            [DllImport(DLLNAME2, CallingConvention = CallingConvention.Cdecl)]
            public static extern int cipher_setup(IntPtr ctx, IntPtr cipher_info);

            [DllImport(DLLNAME2, CallingConvention = CallingConvention.Cdecl)]
            public static extern int cipher_setkey(IntPtr ctx, byte[] key, int key_bitlen, int operation);

            [DllImport(DLLNAME2, CallingConvention = CallingConvention.Cdecl)]
            public static extern int cipher_set_iv(IntPtr ctx, byte[] iv, int iv_len);

            [DllImport(DLLNAME2, CallingConvention = CallingConvention.Cdecl)]
            public static extern int cipher_reset(IntPtr ctx);

            [DllImport(DLLNAME2, CallingConvention = CallingConvention.Cdecl)]
            public static extern int cipher_update(IntPtr ctx, byte[] input, int ilen, byte[] output, ref int olen);

            [DllImport(DLLNAME2, CallingConvention = CallingConvention.Cdecl)]
            public static extern void cipher_free(IntPtr ctx);

            [DllImport(DLLNAME2, CallingConvention = CallingConvention.Cdecl)]
            public static extern void md5(byte[] input, int ilen, byte[] output);

            [DllImport(DLLNAME2, CallingConvention = CallingConvention.Cdecl)]
            public static extern void ss_md(int md_type, byte[] input, int offset, int ilen, byte[] output);

            [DllImport(DLLNAME2, CallingConvention = CallingConvention.Cdecl)]
            public static extern void ss_hmac_ex(int md_type, byte[] key, int keylen, byte[] input, int offset, int ilen, byte[] output);

            [DllImport(DLLNAME2, CallingConvention = CallingConvention.Cdecl)]
            public static extern int cipher_get_size_ex();
        }
    }
}