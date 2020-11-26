#region License
// Copyright (c) 2020 Jens Eisenbach
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace BrickScan.WpfClient.Extensions
{
    //Code taken from https://stackoverflow.com/questions/1483892/how-to-bind-to-a-passwordbox-in-mvvm (user MoonStom) 
    [SuppressUnmanagedCodeSecurity]
    public static class SecureStringExtensions
    {
        public static string ToUnsecuredString(this SecureString? secureString)
        {
            IntPtr bstrPtr = IntPtr.Zero;
            if (secureString != null)
            {
                if (secureString.Length != 0)
                {
                    try
                    {
                        bstrPtr = Marshal.SecureStringToBSTR(secureString);
                        return Marshal.PtrToStringBSTR(bstrPtr);
                    }
                    finally
                    {
                        if (bstrPtr != IntPtr.Zero)
                            Marshal.ZeroFreeBSTR(bstrPtr);
                    }
                }
            }
            return string.Empty;
        }

        public static bool EqualsTo(this SecureString? s1, SecureString? s2)
        {
            if (s1 == null || s2 == null)
            {
                return false;
            }

            var bstr1 = IntPtr.Zero;
            var bstr2 = IntPtr.Zero;

            try
            {
                bstr1 = Marshal.SecureStringToBSTR(s1);
                bstr2 = Marshal.SecureStringToBSTR(s2);
                var length1 = Marshal.ReadInt32(bstr1, -4);
                var length2 = Marshal.ReadInt32(bstr2, -4);

                if (length1 == length2)
                {
                    for (var x = 0; x < length1; ++x)
                    {
                        var b1 = Marshal.ReadByte(bstr1, x);
                        var b2 = Marshal.ReadByte(bstr2, x);

                        if (b1 != b2)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }

                return true;
            }
            finally
            {
                if (bstr2 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(bstr2);
                }

                if (bstr1 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(bstr1);
                }
            }
        }

        //TODO do we need this?
        //public static void CopyInto(this SecureString source, SecureString destination)
        //{
        //    destination.Clear();
        //    foreach (var chr in source.ToUnsecuredString())
        //    {
        //        destination.AppendChar(chr);
        //    }
        //}

        public static SecureString ToSecuredString(this string? plainString)
        {
            if (string.IsNullOrEmpty(plainString))
            {
                return new SecureString();
            }

            SecureString secure = new SecureString();
            foreach (char c in plainString!)
            {
                secure.AppendChar(c);
            }
            return secure;
        }
    }
}