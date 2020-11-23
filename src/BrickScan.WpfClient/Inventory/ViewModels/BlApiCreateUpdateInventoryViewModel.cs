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
using System.Threading.Tasks;
using BricklinkSharp.Client;
using BrickScan.WpfClient.Events;
using BrickScan.WpfClient.Extensions;
using Serilog;
using Stylet;

namespace BrickScan.WpfClient.Inventory.ViewModels
{
    public class BlApiCreateUpdateInventoryViewModel : PropertyChangedBase
    {
        private readonly OnInventoryServiceRequested _request;
        private readonly ILogger _logger;
        private readonly IBricklinkClient _bricklinkClient;
        private int? _inventoryId;
        private int _totalSubmittedQuantity;

        private bool MustCreate => !_inventoryId.HasValue;

        public InventoryParameterViewModel InventoryParameterViewModel { get; }

        public bool IsProcessing { get; private set; }

        public bool IsSubmitted { get; private set; }

        public bool WasSubmissionSuccessful { get; private set; }

        public string? SubmissionMessage { get; private set; }

        public string CreateOrUpdateText => _inventoryId.HasValue ? 
            Properties.Resources.Update : 
            Properties.Resources.Create;

        public BlApiCreateUpdateInventoryViewModel(OnInventoryServiceRequested request, 
            BlInventoryQueryResult blQueryResult,
            ILogger logger, 
            IBricklinkClient bricklinkClient)
        {
            _request = request;
            _logger = logger;
            _bricklinkClient = bricklinkClient;
            InventoryParameterViewModel = new InventoryParameterViewModel
            {
                PricePerPart = blQueryResult.PriceGuide.QuantityAveragePrice
            };
        }

        private async Task<int> CreateInventoryAsync()
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

            await Task.Delay(1000);
            return 15;

            //var inventory = await _bricklinkClient.CreateInventoryAsync(newInventory);
            //return inventory.InventoryId;
        }

        private async Task UpdateInventoryAsync()
        {
            await Task.Delay(1000);
        }

        private string BuildSubmissionMessage()
        {
            return string.Format(Properties.Resources.TotalQuantitySubmitted, _totalSubmittedQuantity);
        }

        public async Task CreateOrUpdateAsync()
        {
            IsProcessing = true;

            try
            {
                if (MustCreate)
                {
                    var inventoryId = await CreateInventoryAsync();
                    _inventoryId = inventoryId;
                    NotifyOfPropertyChange(nameof(CreateOrUpdateText));
                }
                else
                {
                    await UpdateInventoryAsync();
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
