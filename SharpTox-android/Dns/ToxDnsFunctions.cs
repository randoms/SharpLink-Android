﻿using System;
using System.Text;
using System.Runtime.InteropServices;

namespace SharpToxAndroid.Dns
{
    internal static class ToxDnsFunctions
    {
        const string dll = "libtoxdns.so";

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tox_dns3_new")]
        internal static extern ToxDnsHandle New(byte[] publicKey);

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tox_dns3_kill")]
        internal static extern void Kill(IntPtr dns3Object);

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tox_generate_dns3_string")]
        internal static extern int GenerateDns3String(ToxDnsHandle dns3Object, byte[] str, ushort strMaxLength, ref uint requestId, byte[] name, byte nameLength);

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tox_decrypt_dns3_TXT")]
        internal static extern int DecryptDns3TXT(ToxDnsHandle dns3Object, byte[] toxId, byte[] idRecord, uint idRecordLenght, uint requestId);
    }
}
