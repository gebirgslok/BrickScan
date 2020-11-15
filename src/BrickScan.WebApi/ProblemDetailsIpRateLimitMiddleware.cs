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

using System.Text.Json;
using System.Threading.Tasks;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BrickScan.WebApi
{
    public class ProblemDetailsIpRateLimitMiddleware : IpRateLimitMiddleware
    {
        private readonly IpRateLimitOptions _options;

        public ProblemDetailsIpRateLimitMiddleware(RequestDelegate next, 
            IOptions<IpRateLimitOptions> options, 
            IRateLimitCounterStore counterStore, 
            IIpPolicyStore policyStore, 
            IRateLimitConfiguration config, 
            ILogger<IpRateLimitMiddleware> logger) : base(next, options, counterStore, policyStore, config, logger)
        {
            _options = options.Value;
        }

        public override Task ReturnQuotaExceededResponse(HttpContext httpContext, RateLimitRule rule, string retryAfter)
        {
            var message = $"API calls quota exceeded for route = {rule.Endpoint}! " +
                          $"Maximum admitted {rule.Limit} per {rule.Period}. " +
                          $"Retry after {retryAfter}.";

            if (!_options.DisableRateLimitHeaders)
            {
                httpContext.Response.Headers["Retry-After"] = retryAfter;
            }
            
            var statusCode = _options.QuotaExceededResponse?.StatusCode ?? _options.HttpStatusCode;
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = _options.QuotaExceededResponse?.ContentType ?? "application/json";

            var details = new ProblemDetails
            {
                Detail = message,
                Instance = httpContext.Request.Path,
                Status = statusCode,
                Title = ReasonPhrases.GetReasonPhrase(statusCode),
                Type = $"https://httpstatuses.com/{statusCode}",
                Extensions = { { "traceId", httpContext.TraceIdentifier } }
            };
            
            return httpContext.Response.WriteAsync(JsonSerializer.Serialize(details));
        }
    }
}