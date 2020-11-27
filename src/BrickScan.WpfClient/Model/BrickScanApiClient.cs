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
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BrickScan.Library.Core.Dto;
using BrickScan.WpfClient.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace BrickScan.WpfClient.Model
{
    public sealed class BrickScanApiClient : IBrickScanApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IUserSession _userSession;
        private readonly ILogger _logger;

        public BrickScanApiClient(HttpClient httpClient, ILogger logger, IUserSession userSession)
        {
            _httpClient = httpClient;
            _logger = logger;
            _userSession = userSession;
        }

        private Uri BuildAbsoluteUri(string uriPath)
        {
            var basePath = ConfigurationManager.AppSettings["BrickScanApiBaseUrl"];
            return new Uri($"{basePath}{uriPath}", UriKind.Absolute);
        }

        public async Task<bool> SubmitClassAsync(BitmapSource? extraDisplayImage, IList<BitmapSource> trainImages,
            bool useFirstTrainImageAsDisp, IList<SubmitDatasetItemDto> items)
        {
            try
            {
                var datasetClass = new DatasetClassDto();
                var resizedTrainImages = new List<BitmapSource>();

                var maxImageSize = AppConfig.MaxNonDisplayImageWidthOrHeight;
                var maxDisplayImageSize = AppConfig.MaxDisplayImageWidthOrHeight;

                if (useFirstTrainImageAsDisp)
                {
                    resizedTrainImages.Add(trainImages.First().ClipMaxSize(maxDisplayImageSize, maxDisplayImageSize));
                    resizedTrainImages.AddRange(trainImages.Skip(1).Select(x => x.ClipMaxSize(maxImageSize, maxImageSize)));
                }
                else
                {
                    resizedTrainImages.AddRange(trainImages.Select(x => x.ClipMaxSize(maxImageSize, maxImageSize)));
                }

                var trainImagesResult = await PostImagesAsync(resizedTrainImages);

                if (!trainImagesResult.Success)
                {
                    return false;
                }

                datasetClass.TrainingImageIds.AddRange(trainImagesResult.DatasetImages.Select(img => img.Id));

                if (useFirstTrainImageAsDisp)
                {
                    datasetClass.DisplayImageIds.Add(datasetClass.TrainingImageIds.First());
                }

                if (extraDisplayImage != null)
                {
                    var resized = extraDisplayImage.ClipMaxSize(maxDisplayImageSize, maxDisplayImageSize);
                    var extraDisplayImageResult = await PostImagesAsync(new List<BitmapSource> { resized }, "extra_disp_image[{0}].png");

                    if (!extraDisplayImageResult.Success)
                    {
                        return false;
                    }

                    datasetClass.DisplayImageIds.AddRange(extraDisplayImageResult.DatasetImages.Select(img => img.Id));
                }

                for (var i = 0; i < items.Count; i++)
                {
                    var item = new DatasetItemDto
                    {
                        Number = items[i].Number ?? throw new ArgumentNullException(nameof(SubmitDatasetItemDto.Number)),
                        AdditionalIdentifier = items[i].AdditionalIdentifier,
                        DatasetColorId = items[i].DatasetColorId
                    };

                    if (items[i].Images != null)
                    {
                        var itemDisplayImageResult =
                            await PostImagesAsync(items[i].Images!.Select(x => x.ClipMaxSize(maxDisplayImageSize, maxDisplayImageSize)),
                                $"item[{i}].disp_image[{{0}}].png");

                        if (!itemDisplayImageResult.Success)
                        {
                            return false;
                        }

                        item.DisplayImageIds.AddRange(itemDisplayImageResult.DatasetImages.Select(img => img.Id));
                    }

                    datasetClass.Items.Add(item);
                }

                using var request = new HttpRequestMessage(HttpMethod.Post, BuildAbsoluteUri("classes/submit"));

                var accessToken = await _userSession.GetAccessTokenAsync(false);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var submitBody = JsonConvert.SerializeObject(datasetClass);
                request.Content = new StringContent(submitBody, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.StatusCode != HttpStatusCode.Created)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.Error("Received unexpected status code from submit dataset request. Response = {@Response}." +
                                  Environment.NewLine +
                                  "Content: {Content}", responseContent);

                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to submit dataset class.");
                return false;
            }
        }

        public async Task<PostImagesResult> PostImagesAsync(IEnumerable<BitmapSource> images, string filenameTemplate = "image[{0}].png")
        {
            var disposableContents = new List<ByteArrayContent>();

            try
            {
                var formData = new MultipartFormDataContent();
                var count = 0;

                foreach (var bitmapImage in images)
                {
                    var content = new ByteArrayContent(bitmapImage.ToByteArray());
                    content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                    disposableContents.Add(content);
                    formData.Add(content, "images", string.Format(filenameTemplate, count.ToString()));
                    count++;
                }

                using var request = new HttpRequestMessage(HttpMethod.Post, BuildAbsoluteUri("images"));

                var accessToken = await _userSession.GetAccessTokenAsync(true);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = formData;
                var response = await _httpClient.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var message = "Received JSON response (for request POST /images) did not have a success code." +
                                  Environment.NewLine +
                                  $"Expected code = 200, received = {response.StatusCode}." +
                                  Environment.NewLine +
                                  $"Body = {responseString}.";
                    throw new HttpRequestException(message);
                }

                var datasetImages = JsonConvert.DeserializeObject<List<DatasetImageDto>>(responseString);
                return new PostImagesResult(true, datasetImages);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to post images.");
                return new PostImagesResult(false, null);
            }
            finally
            {
                foreach (var content in disposableContents)
                {
                    content.Dispose();
                }
            }
        }

        public async Task<PredictionResult> PredictAsync(byte[] imageBytes)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildAbsoluteUri("prediction/predict"));

            var accessToken = await _userSession.GetAccessTokenAsync(false);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var formData = new MultipartFormDataContent();
            var content = new ByteArrayContent(imageBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            formData.Add(content, "image", "prediction-image.png");
            request.Content = formData;

            var response = await _httpClient.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var predictedClasses = JsonConvert.DeserializeObject<List<PredictedDatasetClassDto>>(responseString);
                return new PredictionResult(true, predictedDatasetClasses: predictedClasses);
            }

            var problemDetails = JObject.Parse(responseString);

            if ((int)response.StatusCode == 429)
            {
                _logger.Error("Received an 'too many requests' status code ({{StatusCode}}) " +
                              "from request URI = {RequestUri}. Problem details = {@ProblemDetails}.",
                    429, "prediction/predict", problemDetails);

                return new PredictionResult(false, errorMessage: Properties.Resources.TooManyRequestsErrorMessage);
            }

            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                _logger.Error($"Received an {nameof(HttpStatusCode.InternalServerError)} ({{StatusCode}}) " +
                              "from request URI = {RequestUri}. Problem details = {@ProblemDetails}.",
                    (int)HttpStatusCode.InternalServerError, "prediction/predict", problemDetails);

                return new PredictionResult(false, errorMessage: Properties.Resources.InternalServerErrorMessage);
            }

            _logger.Error("Received {Error} ({StatusCode}) from request URI = {RequestUri}." +
                          "Problem details = {@ProblemDetails}.",
                response.StatusCode.ToString(), (int)response.StatusCode, "prediction/predict", problemDetails);

            return new PredictionResult(false, errorMessage: Properties.Resources.RequestFailedMessage);
        }

        public async Task AssignTrainImageToClassAsync(int trainImageId, int classId)
        {
            var method = HttpMethod.Post;

            using var request = new HttpRequestMessage(method, BuildAbsoluteUri($"classes/{classId}/assign-train-image?imageId={trainImageId}"));

            var accessToken = await _userSession.GetAccessTokenAsync(true);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            _logger.Information("Received status code = {StatusCode} for confirmation.", response.StatusCode);
        }

        public async Task<IEnumerable<ColorDto>> GetColorsAsync()
        {
            var response = await _httpClient.GetAsync(BuildAbsoluteUri("colors"));
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ColorDto[]>(responseString);
        }
    }
}