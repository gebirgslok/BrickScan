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
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using BrickScan.WpfClient.Events;
using BrickScan.WpfClient.Model;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using PropertyChanged;
using Stylet;

namespace BrickScan.WpfClient.ViewModels
{
    public sealed class EditPartMetaViewModel : PropertyChangedBase, IDisposable
    {
        private readonly Dictionary<PartConfigViewModel, IEventBinding> _bindings = new Dictionary<PartConfigViewModel, IEventBinding>();
        private readonly Func<PartConfigViewModel> _partConfigViewModelFactory;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly IBrickScanApiClient _apiClient;
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
            IBrickScanApiClient apiClient)
        {
            _eventAggregator = eventAggregator;
            _partConfigViewModelFactory = partConfigViewModelFactory;
            _dialogCoordinator = dialogCoordinator;
            _apiClient = apiClient;
            TrainImages = new BindableCollection<BitmapSource>(trainImages);

            PartConfigViewModels = new BindableCollection<PartConfigViewModel>
            {
                CreateNewPartConfigViewModel()
            };
        }

        ~EditPartMetaViewModel()
        {
            Dispose(false);
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
            var controller =
                await _dialogCoordinator.ShowProgressAsync(this,
                    Properties.Resources.Upload,
                    Properties.Resources.SendingData);

            controller.SetIndeterminate();

            var items = PartConfigViewModels
                .Select(x => x.ConvertToSubmitDatasetItemDto())
                .ToList();

            var success = await _apiClient.SubmitClassAsync(ExtraDisplayImage, TrainImages,
                UseFirstTrainImageAsDisplayImage, items);

            await controller.CloseAsync();

            WasSubmissionSuccessful = success;

            SubmissionMessage = success ?
                    Properties.Resources.SubmissionSuccessfulMessage : 
                    Properties.Resources.SubmissionFailedMessage;

            IsSubmitted = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
