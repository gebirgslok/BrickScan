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

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace BrickScan.WebApi
{
    public class ApiResponse
    {
        public int StatusCode { get; }

        public IEnumerable<string> Errors { get; }

        public string Message { get; }

        public object? Data { get; }

        internal ApiResponse(int statusCode, string? message = null, object? data = null, IEnumerable<string>? errors = null)
        {
            StatusCode = statusCode;
            Message = message ?? GetDefaultMessageForStatusCode(StatusCode);
            Data = data;
            Errors = errors ?? new List<string>();
        }

        private static string GetDefaultMessageForStatusCode(int statusCode)
        {
            return statusCode switch
            {
                200 => "Request successful.",
                201 => "Resource(s) created.",
                400 => "Bad request.",
                401 => "Resource not found.",
                415 => "Unsupported media type.",
                500 => "An unhandled error occurred.",
                _ => "An unknown error occurred."
            };
        }
    }
}
