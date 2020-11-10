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

using System.Collections.Generic;
using System.IO;

namespace BrickScan.Library.Core
{
    public static class FileHelper
    {
        public static void WriteByteArrays(string file, IEnumerable<byte[]> byteArrays)
        {
            using FileStream fs = new FileStream(file, FileMode.Create);
            using BinaryWriter bw = new BinaryWriter(fs);

            foreach (var byteArray in byteArrays)
            {
                bw.Write(byteArray.Length);
                bw.Write(byteArray, 0, byteArray.Length);
            }
        }

        public static IEnumerable<byte[]> ReadByteArrays(string file)
        {
            using FileStream fs = new FileStream(file, FileMode.Open);
            using BinaryReader br = new BinaryReader(fs);

            while (br.BaseStream.Position != br.BaseStream.Length)
            {
                yield return br.ReadBytes(br.ReadInt32());
            }
        }
    }
}
