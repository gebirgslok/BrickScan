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

using BrickScan.Library.Dataset;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace BrickScan.WebApi.Classes
{
    internal static class ConfirmUnclassifiedImageResultExtensions
    {
        public static IActionResult GetActionResult(this ConfirmUnclassifiedImageResult r, string instance, string trace)
        {
            if (r.Success)
            {
                return new OkResult();
            }

            var problemDetails = new ProblemDetails
            {
                Detail = r.ErrorMessage,
                Instance = instance,
                Status = r.StatusCode,
                Title = ReasonPhrases.GetReasonPhrase(r.StatusCode),
                Type = $"https://httpstatuses.com/{r.StatusCode}"
            };

            problemDetails.Extensions.Add("traceId", trace);
            

            if (r.StatusCode == 404)
            {
                return new NotFoundObjectResult(problemDetails);
            }

            return new BadRequestObjectResult(problemDetails);
        }
    }
}
