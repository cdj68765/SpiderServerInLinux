﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Shadowsocks.Model
{
    public class IPRangeSet
    {
        private const string APNIC_FILENAME = "delegated-apnic-latest";
        private const string APNIC_EXT_FILENAME = "delegated-apnic-extended-latest";
        private const string CHN_FILENAME = "chn_ip.txt";
        private uint[] _set;

        public IPRangeSet()
        {
            _set = new uint[256 * 256 * 8];
        }

        public void InsertRange(uint begin, uint end)
        {
            begin /= 256;
            end /= 256;
            for (uint i = begin; i <= end; ++i)
            {
                uint pos = i / 32;
                int mv = (int)(i & 31);
                _set[pos] |= (1u << mv);
            }
        }

        public void Insert(uint begin, uint size)
        {
            InsertRange(begin, begin + size - 1);
        }

        public void Insert(IPAddress addr, uint size)
        {
            byte[] bytes_addr = addr.GetAddressBytes();
            Array.Reverse(bytes_addr);
            Insert(BitConverter.ToUInt32(bytes_addr, 0), size);
        }

        public void Insert(IPAddress addr_beg, IPAddress addr_end)
        {
            byte[] bytes_addr_beg = addr_beg.GetAddressBytes();
            Array.Reverse(bytes_addr_beg);
            byte[] bytes_addr_end = addr_end.GetAddressBytes();
            Array.Reverse(bytes_addr_end);
            InsertRange(BitConverter.ToUInt32(bytes_addr_beg, 0), BitConverter.ToUInt32(bytes_addr_end, 0));
        }

        public bool isIn(uint ip)
        {
            ip /= 256;
            uint pos = ip / 32;
            int mv = (int)(ip & 31);
            return (_set[pos] & (1u << mv)) != 0;
        }

        public bool IsInIPRange(IPAddress addr)
        {
            byte[] bytes_addr = addr.GetAddressBytes();
            Array.Reverse(bytes_addr);
            return isIn(BitConverter.ToUInt32(bytes_addr, 0));
        }

        public bool LoadChn()
        {
            return false;
        }

        public void Reverse()
        {
            IPAddress addr_beg, addr_end;
            IPAddress.TryParse("240.0.0.0", out addr_beg);
            IPAddress.TryParse("255.255.255.255", out addr_end);
            Insert(addr_beg, addr_end);
            for (uint i = 0; i < _set.Length; ++i)
            {
                _set[i] = ~_set[i];
            }
        }
    }
}