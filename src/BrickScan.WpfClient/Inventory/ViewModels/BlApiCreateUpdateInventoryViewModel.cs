﻿#region License
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
using System.Threading.Tasks;
using BricklinkSharp.Client;
using BrickScan.WpfClient.Events;
using BrickScan.WpfClient.Extensions;
using PropertyChanged;
using Stylet;
using ILogger = Serilog.ILogger;

namespace BrickScan.WpfClient.Inventory.ViewModels
{
    public class BlApiCreateUpdateInventoryViewModel : PropertyChangedBase
    {
        private readonly OnInventoryServiceRequested _request;
        private readonly ILogger _logger;
        private readonly IBricklinkClient _bricklinkClient;
        private int _totalSubmittedQuantity;

        private bool MustCreate => InventoryDto == null;

        [AlsoNotifyFor(nameof(SubmissionMessage), nameof(HeaderText))]
        public InventoryDto? InventoryDto { get; private set; }

        public InventoryParameterViewModel InventoryParameterViewModel { get; }

        public bool IsProcessing { get; private set; }

        public bool IsSubmitted { get; private set; }

        public bool WasSubmissionSuccessful { get; private set; }

        public string? SubmissionMessage { get; private set; }

        public string HeaderText => InventoryDto == null
            ? Properties.Resources.CreateNewBricklinkInventory
            : string.Format(Properties.Resources.UpdateBricklinkInventory, InventoryDto.Id, InventoryDto.Quantity);

        public string CreateOrUpdateText => InventoryDto != null ?
            Properties.Resources.Update :
            Properties.Resources.Create;

        public BlApiCreateUpdateInventoryViewModel(OnInventoryServiceRequested request,
            BlInventoryQueryResult blQueryResult,
            ILogger logger,
            IBricklinkClient bricklinkClient,
            IUserConfiguration userConfiguration)
        {
            _request = request;
            _logger = logger;
            _bricklinkClient = bricklinkClient;
            var c = userConfiguration.BlPriceFixingC;
            var f = userConfiguration.BlPriceFixingF;
            var finalPrice =
                userConfiguration.SelectedPriceFixingBaseMethod.GetFinalPrice(blQueryResult.PriceGuide, c, f);

            _logger.Information("Final price = {FinalPrice} (C = {C}, F = {F}).", finalPrice, c, f);

            InventoryParameterViewModel = new InventoryParameterViewModel
            {
                PricePerPart = finalPrice,
                Condition = userConfiguration.SelectedInventoryCondition
            };
        }

        private async Task<BricklinkSharp.Client.Inventory> CreateInventoryAsync()
        {
            var newInventory = new NewInventory
            {
                Bulk = 1,
                ColorId = _request.Item.BricklinkColor,
                Condition = InventoryParameterViewModel.Condition.ToBricklinkSharpCondition(),
                Item = new ItemBase
                {
                    Number = _request.Item.Number,
                    Type = ItemType.Part
                },
                Quantity = InventoryParameterViewModel.Quantity,
                UnitPrice = InventoryParameterViewModel.PricePerPart,
                Remarks = InventoryParameterViewModel.StorageOrBin
            };

            var inventory = await _bricklinkClient.CreateInventoryAsync(newInventory);
            return inventory;
        }

        private async Task<BricklinkSharp.Client.Inventory> UpdateInventoryAsync()
        {
            var updateInventory = new UpdateInventory
            {
                ChangedQuantity = InventoryParameterViewModel.Quantity,
                UnitPrice = InventoryParameterViewModel.PricePerPart,
                Remarks = InventoryParameterViewModel.StorageOrBin
            };

            var inventory = await _bricklinkClient.UpdateInventoryAsync(InventoryDto!.Id, updateInventory);

            _logger.Information("Received updated inventory: {q = {{Quantity}}, ID = {{Id}}.", inventory.Quantity, inventory.InventoryId);

            return inventory;
        }

        private string BuildSubmissionMessage()
        {
            return string.Format(Properties.Resources.TotalQuantitySubmitted, _totalSubmittedQuantity);
        }

        internal void SetInventory(InventoryDto inventory)
        {
            InventoryDto = inventory;
        }

        public async Task CreateOrUpdateAsync()
        {
            IsProcessing = true;

            try
            {
                if (MustCreate)
                {
                    var inventory = await CreateInventoryAsync();
                    InventoryDto = new InventoryDto(inventory);
                    InventoryParameterViewModel.CanChangeCondition = false;
                }
                else
                {
                    InventoryDto!.Inventory = await UpdateInventoryAsync();
                    NotifyOfPropertyChange(nameof(CreateOrUpdateText));
                    NotifyOfPropertyChange(nameof(HeaderText));
                }

                WasSubmissionSuccessful = true;
                _totalSubmittedQuantity += InventoryParameterViewModel.Quantity;
                InventoryParameterViewModel.Quantity = 1;
                SubmissionMessage = BuildSubmissionMessage();
            }
            catch (Exception exception)
            {
                _logger.Error(exception,
                    $"Failed to {(MustCreate ? "create" : "update")} inventory lot. Received {{Message}}",
                    exception.Message);
                WasSubmissionSuccessful = false;
                SubmissionMessage = Properties.Resources.RequestFailedMessage;
            }

            IsProcessing = false;
            IsSubmitted = true;
        }
    }
}
