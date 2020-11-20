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
using System.Threading.Tasks;
using BrickScan.Library.Core.Dto;
using BrickScan.WpfClient.Events;
using BrickScan.WpfClient.Inventory;
using BrickScan.WpfClient.Model;
using MahApps.Metro.Controls.Dialogs;
using Serilog;
using Stylet;

namespace BrickScan.WpfClient.ViewModels
{
    public sealed class SingleItemPredictedClassViewModel : PredictedClassViewModelBase, IHandle<OnImageConfirmed>
    {
        private const string BRICKLINK_PART_URL_TEMPLATE =
            "https://www.bricklink.com/v2/catalog/catalogitem.page?P={0}#T=S&C={1}";

        private readonly ILogger _logger;
        private readonly IBrickScanApiClient _apiClient;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly IEventAggregator _eventAggregator;
        private readonly PredictedDatasetClassDto _predictedDatasetClassDto;

        public override int PredictedClassId => _predictedDatasetClassDto.Id;

        public IUserSession UserSession { get; }

        public bool CanConfirmImageBelongsToClassAsync { get; private set; } = true;

        public PredictedDatasetItemDto Item => _predictedDatasetClassDto.Items.First();

        public float Score => _predictedDatasetClassDto.Score;

        public string? ConfirmImageBelongsToClassToolTip => Properties.Resources.ConfirmImageBelongsToClassToolTip;

        public SingleItemPredictedClassViewModel(PredictedDatasetClassDto predictedDatasetClassDto,
            ILogger logger,
            IBrickScanApiClient apiClient,
            IDialogCoordinator dialogCoordinator,
            IEventAggregator eventAggregator,
            IUserSession userSession)
        {
            _predictedDatasetClassDto = predictedDatasetClassDto;
            _logger = logger;
            _apiClient = apiClient;
            _dialogCoordinator = dialogCoordinator;
            _eventAggregator = eventAggregator;
            UserSession = userSession;
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
            var request = new OnInventoryServiceRequested(_predictedDatasetClassDto.Items.First(), ParentViewModel);
            _eventAggregator.PublishOnUIThread(request);
        }

        public async Task ConfirmImageBelongsToClassAsync()
        {
            _eventAggregator.PublishOnUIThread(new OnImageConfirmed(_predictedDatasetClassDto.Id));

            var controller =
                await _dialogCoordinator.ShowProgressAsync(this,
                    Properties.Resources.Confirmation,
                    Properties.Resources.WaitingForConfirmationResponse);
            
            controller.SetIndeterminate();

            try
            {
                var imageId = _predictedDatasetClassDto.UnconfirmedImageId!.Value;
                var classId = _predictedDatasetClassDto.Id;
                await _apiClient.AssignTrainImageToClassAsync(imageId, classId);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
            finally
            {
                await controller.CloseAsync();
            }
        }

        public void Handle(OnImageConfirmed message)
        {
            CanConfirmImageBelongsToClassAsync = false;
        }
    }
}