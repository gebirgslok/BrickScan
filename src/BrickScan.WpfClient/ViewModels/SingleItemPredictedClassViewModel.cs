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
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BrickScan.Library.Core.Dto;
using BrickScan.WpfClient.Events;
using BrickScan.WpfClient.Model;
using MahApps.Metro.Controls.Dialogs;
using PropertyChanged;
using Serilog;
using Stylet;

namespace BrickScan.WpfClient.ViewModels
{
    public sealed class SingleItemPredictedClassViewModel : PredictedClassViewModelBase, IHandle<OnImageConfirmed>
    {
        private const string BRICKLINK_PART_URL_TEMPLATE =
            "https://www.bricklink.com/v2/catalog/catalogitem.page?P={0}#T=S&C={1}";

        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly IEventAggregator _eventAggregator;
        private readonly PredictedDatasetClassDto _predictedDatasetClassDto;
        private readonly Uri[] _displayImages;

        public IUserSession UserSession { get; }

        public Uri? SelectedDisplayImage { get; set; }

        [DependsOn(nameof(SelectedDisplayImage))]
        public bool CanShowNextImage => Array.IndexOf(_displayImages, SelectedDisplayImage) < _displayImages.Length - 1;

        [DependsOn(nameof(SelectedDisplayImage))]
        public bool CanShowPreviousImage => Array.IndexOf(_displayImages, SelectedDisplayImage) > 0;

        public bool HasManyDisplayImages => _displayImages.Length > 1;

        public bool IsConfirmDatasetClassAvailable => _predictedDatasetClassDto.UnconfirmedImageId != null;

        public bool CanConfirmImageBelongsToClassAsync { get; private set; } = true;

        public string PartNo => _predictedDatasetClassDto.Items.First().Number;

        public string? AdditionalIdentifier => _predictedDatasetClassDto.Items.First().AdditionalIdentifier;

        public float Score => _predictedDatasetClassDto.Score;

        public string? BricklinkColorHtmlCode => _predictedDatasetClassDto.Items.First().Color?.BricklinkColorHtmlCode;

        public string? BricklinkColorName => _predictedDatasetClassDto.Items.First().Color?.BricklinkColorName;

        public string? ConfirmImageBelongsToClassToolTip =>
            IsConfirmDatasetClassAvailable ? 
                UserSession.IsTrusted ? 
                    Properties.Resources.ConfirmImageBelongsToClassToolTip : 
                    "Not logged on or not authorized."
                : null;

        public SingleItemPredictedClassViewModel(PredictedDatasetClassDto predictedDatasetClassDto,
            ILogger logger,
            HttpClient httpClient,
            IDialogCoordinator dialogCoordinator,
            IEventAggregator eventAggregator,
            IUserSession userSession)
        {
            _predictedDatasetClassDto = predictedDatasetClassDto;
            _logger = logger;
            _httpClient = httpClient;
            _dialogCoordinator = dialogCoordinator;
            _eventAggregator = eventAggregator;
            UserSession = userSession;

            _displayImages = _predictedDatasetClassDto
                .DisplayImageUrls?
                .Select(url => new Uri(url))
                .ToArray() ?? new Uri[0];

            SelectedDisplayImage = _displayImages.Length > 0 ? _displayImages[0] : null;
        }


        public void ShowNextImage()
        {
            var currentIndex = Array.IndexOf(_displayImages, SelectedDisplayImage);
            var nextIndex = currentIndex + 1;
            SelectedDisplayImage = _displayImages[nextIndex];
        }

        public void OpenOnBricklink()
        {
            try
            {
                var item = _predictedDatasetClassDto.Items.First();
                var partNo = item.Number;
                var colorId = item.Color!.BricklinkColorId;
                var url = string.Format(BRICKLINK_PART_URL_TEMPLATE, partNo, colorId);
                Process.Start(url);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
        }

        public void AddToInventory()
        {
            //TODO FUTURE Feature.
        }

        public async Task ConfirmImageBelongsToClassAsync()
        {
            _eventAggregator.PublishOnUIThread(new OnImageConfirmed());

            try
            {
                var controller =
                    await _dialogCoordinator.ShowProgressAsync(this,
                        Properties.Resources.Confirmation,
                        Properties.Resources.WaitingForConfirmationResponse);

                controller.SetIndeterminate();

                var imageId = _predictedDatasetClassDto.UnconfirmedImageId!.Value;
                var classId = _predictedDatasetClassDto.Id;
                var method = new HttpMethod("PATCH");

                using var request = new HttpRequestMessage(method,
                    new Uri($"dataset/images/confirm?imageId={imageId}&classId={classId}", UriKind.Relative));

                var accessToken = await UserSession.GetAccessTokenAsync();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);

                _logger.Information("Received status code = {StatusCode} for confirmation.", response.StatusCode);

                await controller.CloseAsync();

            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
        }

        public void ShowPreviousImage()
        {
            var currentIndex = Array.IndexOf(_displayImages, SelectedDisplayImage);
            var previousIndex = currentIndex - 1;
            SelectedDisplayImage = _displayImages[previousIndex];
        }

        public void Handle(OnImageConfirmed message)
        {
            CanConfirmImageBelongsToClassAsync = false;
        }
    }
}