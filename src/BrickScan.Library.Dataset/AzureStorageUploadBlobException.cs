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
using System.Runtime.Serialization;

namespace BrickScan.Library.Dataset
{
    public class AzureStorageUploadBlobException : Exception
    {
        public Uri? BlobUri { get; }

        public int StatusCode { get; }

        public string? ReasonPhrase { get; }

        public override string Message =>
            $"Failed to upload blob (uri = {BlobUri}) to storage container.{Environment.NewLine}" +
            $"Received status code = {StatusCode} ({ReasonPhrase}).";

        internal AzureStorageUploadBlobException(Uri blobUri, int statusCode, string reasonPhrase)
        {
            BlobUri = blobUri;
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
        }

        private AzureStorageUploadBlobException(SerializationInfo serializationInfo, StreamingContext context) : base(serializationInfo, context)
        {
        }
    }
}