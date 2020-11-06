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

using BrickScan.Library.Dataset.Model;

namespace BrickScan.Library.Dataset
{
    public class ConfirmUnclassififiedImageResult
    {
        public bool Success { get; }

        public DatasetImage? Image { get; }

        public string? ErrorMessage { get; }

        public int StatusCode { get; }

        private ConfirmUnclassififiedImageResult(bool success, DatasetImage? image, string? errorMessage, int statusCode)
        {
            Success = success;
            Image = image;
            ErrorMessage = errorMessage;
            StatusCode = statusCode;
        }

        internal static ConfirmUnclassififiedImageResult InvalidResourceResult(int resourceId, string resourceName, string invalidReason) =>
            new ConfirmUnclassififiedImageResult(false, null, $"Resource ({resourceName}, ID = {resourceId}) is invalid. Reason: {invalidReason}.", 400);

        internal static ConfirmUnclassififiedImageResult SuccessfulResult(DatasetImage datasetImage) =>
            new ConfirmUnclassififiedImageResult(true, datasetImage, null, 200);

        internal static ConfirmUnclassififiedImageResult ResourceNotFoundResult(int resourceId, string resourceName) 
            => new ConfirmUnclassififiedImageResult(false, null, $"Resource ({resourceName}, ID = {resourceId}) not found.", 404);
    }
}