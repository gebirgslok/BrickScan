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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace BrickScan.WpfClient.Model
{
    //Code taken from https://github.com/secile/UsbCamera/blob/master/UsbCamera.cs.

    public class UsbCamera
    {
        public int Index { get; }

        public string DisplayName { get; }

        private UsbCamera(int index, string displayName)
        {
            Index = index;
            DisplayName = displayName;
        }

        public override string ToString()
        {
            return $"{Index} - {DisplayName}";
        }

        internal static UsbCamera[] FindDevices()
        {
            return DirectShow.GetDevices(DirectShow.DsGuid.ClsidVideoInputDeviceCategory)
                .Select((n, i) => new UsbCamera(i, n))
                .ToArray();
        }
    }

    static class DirectShow
    {
        internal static class DsGuid
        {
            public static readonly Guid ClsidVideoInputDeviceCategory = new Guid("{860BB310-5D01-11d0-BD3B-00A0C911CE86}");
            public static readonly Guid ClsidSystemDeviceEnum = new Guid("{62BE5D10-60EB-11d0-BD3B-00A0C911CE86}");
            public static readonly Guid IidIPropertyBag = new Guid("{55272A00-42CB-11CE-8135-00AA004BB851}");
        }

        public static List<string> GetDevices(Guid category)
        {
            var result = new List<string>();

            EnumMonikers(category, (moniker, prop) =>
            {
                object? value = null;
                prop.Read("FriendlyName", ref value, 0);
                var name = (string)value!;
                result.Add(name);
                return false;
            });

            return result;
        }

        private static void EnumMonikers(Guid category, Func<IMoniker, IPropertyBag, bool> func)
        {
            IEnumMoniker? enumerator = null;
            ICreateDevEnum? device = null;

            try
            {
                device = (ICreateDevEnum)Activator.CreateInstance(Type.GetTypeFromCLSID(DsGuid.ClsidSystemDeviceEnum));
                device.CreateClassEnumerator(ref category, ref enumerator, 0);

                if (enumerator == null)
                {
                    return;
                }

                var monikers = new IMoniker[1];
                var fetched = IntPtr.Zero;

                while (enumerator.Next(monikers.Length, monikers, fetched) == 0)
                {
                    var moniker = monikers[0];

                    var guid = DsGuid.IidIPropertyBag;
                    moniker.BindToStorage(null, null, ref guid, out object value);
                    var prop = (IPropertyBag)value;

                    try
                    {
                        var rc = func(moniker, prop);

                        if (rc)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(prop);
                        Marshal.ReleaseComObject(moniker);
                    }
                }
            }
            finally
            {
                if (enumerator != null)
                {
                    Marshal.ReleaseComObject(enumerator);
                }

                if (device != null)
                {
                    Marshal.ReleaseComObject(device);
                }
            }
        }


        [ComVisible(true), ComImport, Guid("29840822-5B84-11D0-BD3B-00A0C911CE86"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ICreateDevEnum
        {
            int CreateClassEnumerator([In] ref Guid pType, [In, Out] ref IEnumMoniker? ppEnumMoniker, [In] int dwFlags);
        }

        [ComVisible(true), ComImport, Guid("55272A00-42CB-11CE-8135-00AA004BB851"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPropertyBag
        {
            int Read([MarshalAs(UnmanagedType.LPWStr)] string propName, ref object? var, int errorLog);

            int Write(string propName, ref object? var);
        }
    }
}