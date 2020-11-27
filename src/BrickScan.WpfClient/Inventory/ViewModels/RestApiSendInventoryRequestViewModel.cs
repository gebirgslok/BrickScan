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
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using BrickScan.WpfClient.Events;
using BrickScan.WpfClient.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Stylet;

namespace BrickScan.WpfClient.Inventory.ViewModels
{
    internal class RestApiDefaults
    {
        public const string PART_NO = "$(PartNo)";
        public const string COLOR_ID = "$(ColorId)";
        public const string CONDITION = "$(Condition)";
        public const string UNIT_PRICE = "$(UnitPrice)";
        public const string STORAGE = "$(Storage)";
        public const string QUANTITY = "$(Quantity)";
    }

    public class RestApiSendInventoryRequestViewModel : PropertyChangedBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IUserConfiguration _userConfiguration;
        private readonly OnInventoryServiceRequested _request;

        public InventoryParameterViewModel InventoryParameterViewModel { get; }

        public bool IsProcessing { get; private set; }

        public bool WasSubmissionSuccessful { get; private set; }

        public bool IsSubmitted { get; private set; }

        public string? Response { get; private set; }

        public RestApiSendInventoryRequestViewModel(OnInventoryServiceRequested request, 
            IUserConfiguration userConfiguration, 
            HttpClient httpClient, 
            ILogger logger)
        {
            _request = request;
            _userConfiguration = userConfiguration;
            _httpClient = httpClient;
            _logger = logger;
            InventoryParameterViewModel = new InventoryParameterViewModel
            {
                Condition = userConfiguration.SelectedInventoryCondition
            };
        }

        private string ParseUrlTemplate(string urlTemplate)
        {
            var builder = new StringBuilder(urlTemplate);
            builder.Replace(RestApiDefaults.PART_NO, _request.Item.Number);
            builder.Replace(RestApiDefaults.COLOR_ID, _request.Item.BricklinkColor.ToString());
            builder.Replace(RestApiDefaults.CONDITION,
                InventoryParameterViewModel.Condition.ToString().ToLowerInvariant());
            builder.Replace(RestApiDefaults.QUANTITY, InventoryParameterViewModel.Quantity.ToString());
            builder.Replace(RestApiDefaults.STORAGE, InventoryParameterViewModel.StorageOrBin ?? "not_available");
            builder.Replace(RestApiDefaults.UNIT_PRICE,
                InventoryParameterViewModel.PricePerPart.ToString(new CultureInfo("en-US")));

            return builder.ToString();
        }

        public async Task SendRequestAsync()
        {
            IsProcessing = true;

            string? urlTemplate = null;
            string? url = null;
            try
            {
                urlTemplate = _userConfiguration.RestApiUrl;

                if (string.IsNullOrEmpty(urlTemplate))
                {
                    throw new InvalidOperationException($"{nameof(_userConfiguration.RestApiUrl)} cannot be Null or empty.");
                }

                url = ParseUrlTemplate(urlTemplate!);

                using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url));

                if (_userConfiguration.RestApiAuthScheme != null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue(_userConfiguration.RestApiAuthScheme,
                        _userConfiguration.RestApiAuthParameter.ToUnsecuredString());
                }

                var response = await _httpClient.SendAsync(request);

                var stringResponse = await response.Content.ReadAsStringAsync();

                try
                {
                    var token = JObject.Parse(stringResponse);
                    Response = token.ToString(Formatting.Indented);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to parse response to JSON. Using {RawResponse} instead.", stringResponse);
                    Response = stringResponse;
                }

                WasSubmissionSuccessful = true;
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to send HTTP request for: " + 
                                         Environment.NewLine + 
                                         "URL: {Url}," +
                                         "URL template: {UrlTemplate}", url ?? "Null", urlTemplate ?? "Null");
                WasSubmissionSuccessful = false;
            }

            IsProcessing = false;
            IsSubmitted = true;
        }
    }
}