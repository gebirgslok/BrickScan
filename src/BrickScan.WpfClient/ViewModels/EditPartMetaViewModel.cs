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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using BrickScan.Library.Core.Dto;
using BrickScan.WpfClient.Events;
using BrickScan.WpfClient.Extensions;
using BrickScan.WpfClient.Model;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Identity.Client;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PropertyChanged;
using Stylet;

namespace BrickScan.WpfClient.ViewModels
{
    public sealed class EditPartMetaViewModel : PropertyChangedBase, IDisposable
    {
        //TODO: External class
        class PostImagesResult
        {
            public List<DatasetImageDto> DatasetImages { get; }

            //TODO: eval
            public bool Success { get; }

            public PostImagesResult(bool success, List<DatasetImageDto>? datasetImages)
            {
                Success = success;
                DatasetImages = datasetImages ?? new List<DatasetImageDto>();
            }
        }

        private readonly Dictionary<PartConfigViewModel, IEventBinding> _bindings = new Dictionary<PartConfigViewModel, IEventBinding>();
        private readonly Func<PartConfigViewModel> _partConfigViewModelFactory;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly HttpClient _httpClient;
        private readonly IUserSession _userSession;
        private bool _disposed;

        public bool UseFirstTrainImageAsDisplayImage { get; set; } = true;

        public BindableCollection<BitmapSource> TrainImages { get; }

        [DependsOn(nameof(UseFirstTrainImageAsDisplayImage), nameof(ExtraDisplayImage))]
        public IEnumerable<BitmapSource> DisplayImages => GetDisplayImages();

        public BitmapSource? ExtraDisplayImage { get; private set; }

        [DependsOn(nameof(ExtraDisplayImage), nameof(UseFirstTrainImageAsDisplayImage))]
        public bool CanRemoveExtraDisplayImage => ExtraDisplayImage != null && UseFirstTrainImageAsDisplayImage;

        [DependsOn(nameof(UseFirstTrainImageAsDisplayImage), nameof(ExtraDisplayImage))]
        public bool CanToggleUseFirstTrainImageAsDisplayImage =>
            !UseFirstTrainImageAsDisplayImage || ExtraDisplayImage != null;

        public BindableCollection<PartConfigViewModel> PartConfigViewModels { get; }

        public bool CanRemoveComplementaryPart => PartConfigViewModels.Count > 1;

        [DependsOn(nameof(IsSubmitted))]
        public bool CanSubmitAsync => !IsSubmitted && PartConfigViewModels.All(x => x.IsValid);

        public bool IsSubmitted { get; private set; }

        public bool WasSubmissionSuccessful { get; private set; }

        public string? SubmissionMessage { get; private set; } = "hello world test 1234";

        public EditPartMetaViewModel(IEnumerable<BitmapSource> trainImages,
            IEventAggregator eventAggregator,
            Func<PartConfigViewModel> partConfigViewModelFactory,
            IDialogCoordinator dialogCoordinator,
            HttpClient httpClient, 
            IUserSession userSession)
        {
            _eventAggregator = eventAggregator;
            _partConfigViewModelFactory = partConfigViewModelFactory;
            _dialogCoordinator = dialogCoordinator;
            _httpClient = httpClient;
            _userSession = userSession;
            TrainImages = new BindableCollection<BitmapSource>(trainImages);

            PartConfigViewModels = new BindableCollection<PartConfigViewModel>
            {
                CreateNewPartConfigViewModel()
            };
        }

        private PartConfigViewModel CreateNewPartConfigViewModel()
        {
            var partConfigViewModel = _partConfigViewModelFactory.Invoke();
            var binding = partConfigViewModel.Bind(x => x.IsValid, (sender, args) => NotifyOfPropertyChange(nameof(CanSubmitAsync)));
            _bindings.Add(partConfigViewModel, binding);
            return partConfigViewModel;
        }

        private IEnumerable<BitmapSource> GetDisplayImages()
        {
            var displayImages = new List<BitmapSource>();

            if (UseFirstTrainImageAsDisplayImage)
            {
                displayImages.Add(TrainImages.First());
            }

            if (ExtraDisplayImage != null)
            {
                displayImages.Add(ExtraDisplayImage);
            }

            return displayImages;
        }

        //TODO: external class
        private async Task<PostImagesResult> PostImagesAsync(IEnumerable<BitmapSource> images, string filenameTemplate = "image[{0}].png")
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

                var response = await _httpClient.PostAsync("images", formData);
                var responseString = await response.Content.ReadAsStringAsync();
                var jsonObj = JObject.Parse(responseString);
                var jsonData = jsonObj["data"];

                if (jsonData == null)
                {
                    throw new InvalidOperationException(
                        "Received JSON response (for request POST /images) did not contain a 'data' object.");
                }

                var data = jsonData.ToObject<List<DatasetImageDto>>();
                return new PostImagesResult(true, data);
            }
            catch (Exception e)
            {
                //TODO: log exception!
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

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (var bindings in _bindings.Values)
                {
                    bindings.Unbind();
                }
            }

            _disposed = true;
        }

        public void RemoveExtraDisplayImage()
        {
            ExtraDisplayImage = null;
        }

        public void SelectImage()
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Filter = $"{Properties.Resources.ImageFiles} (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png"
            };

            var wasSelected = dialog.ShowDialog();

            if (wasSelected.HasValue && wasSelected.Value)
            {
                var imagePath = dialog.FileName;
                ExtraDisplayImage = new BitmapImage(new Uri(imagePath));
            }
        }

        public void AddComplementaryPart()
        {
            PartConfigViewModels.Add(CreateNewPartConfigViewModel());
            NotifyOfPropertyChange(nameof(CanRemoveComplementaryPart));
            NotifyOfPropertyChange(nameof(CanSubmitAsync));
        }

        public void RemoveComplementaryPart()
        {
            var last = PartConfigViewModels.Last();
            PartConfigViewModels.Remove(last);
            _bindings[last].Unbind();
            NotifyOfPropertyChange(nameof(CanRemoveComplementaryPart));
            NotifyOfPropertyChange(nameof(CanSubmitAsync));
        }

        public void NavigateBack()
        {
            //Clear existing if dataset class was successfully submitted.
            var clearExistingImages = IsSubmitted;
            _eventAggregator.PublishOnUIThread(new OnEditPartMetaCloseRequested(clearExistingImages));
        }

        public async Task SubmitAsync()
        {
            //TODO: Logging

            var controller =
                await _dialogCoordinator.ShowProgressAsync(this,
                    Properties.Resources.Upload,
                    Properties.Resources.SendingData);

            controller.SetIndeterminate();

            //TODO: extract to class / interface
            var datasetClass = new DatasetClassDto();
            var trainImagesResult = await PostImagesAsync(TrainImages);

            datasetClass.TrainingImageIds.AddRange(trainImagesResult.DatasetImages.Select(img => img.Id));

            if (UseFirstTrainImageAsDisplayImage)
            {
                datasetClass.DisplayImageIds.Add(datasetClass.TrainingImageIds.First());
            }

            if (ExtraDisplayImage != null)
            {
                var extraDisplayImageResult = await PostImagesAsync(new List<BitmapSource> { ExtraDisplayImage }, "extra_disp_image[{0}].png");
                datasetClass.DisplayImageIds.AddRange(extraDisplayImageResult.DatasetImages.Select(img => img.Id));
            }

            for (var i = 0; i < PartConfigViewModels.Count; i++)
            {
                var viewModel = PartConfigViewModels[i];
                var item = new DatasetItemDto
                {
                    Number = viewModel.PartNo ?? throw new ArgumentNullException(nameof(viewModel.PartNo)),
                    AdditionalIdentifier = viewModel.AdditionalIdentifier,
                    DatasetColorId = viewModel.SelectedColor?.Id ??
                                     throw new ArgumentNullException(nameof(viewModel.SelectedColor))
                };

                if (viewModel.SpecificDisplayImage != null)
                {
                    var itemDisplayImageResult =
                        await PostImagesAsync(new List<BitmapSource> { viewModel.SpecificDisplayImage },
                            $"item[{i}].disp_image[{{0}}].png");
                    item.DisplayImageIds.AddRange(itemDisplayImageResult.DatasetImages.Select(img => img.Id));
                }

                datasetClass.Items.Add(item);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("dataset/classes/submit", UriKind.Relative));

            var accessToken = await _userSession.GetAccessTokenAsync();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var submitBody = JsonConvert.SerializeObject(datasetClass);
            request.Content = new StringContent(submitBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            //var response = await _httpClient.PostAsync("dataset/classes/submit",new StringContent(submitBody, Encoding.UTF8, "application/json"));

            await controller.CloseAsync();

            WasSubmissionSuccessful = true;
            SubmissionMessage = Properties.Resources.SubmissionSuccessfulMessage;
            IsSubmitted = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
