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
using BrickScan.WpfClient.Model;
using Serilog;
using Stylet;

namespace BrickScan.WpfClient.Inventory.ViewModels
{
    public class BlApiAddInventoryViewModel : PropertyChangedBase
    {
        private readonly OnInventoryServiceRequested _request;
        private readonly BlInventoryQueryResult _blQueryResult;
        private readonly ILogger _logger;
        private readonly IBricklinkClient _bricklinkClient;
        private int? _inventoryId;

        private bool WillCreate => !_inventoryId.HasValue;

        public DatasetItemContainer DatasetItem => _request.Item;

        public CatalogItem CatalogItem => _blQueryResult.CatalogItem;

        public InventoryParameterViewModel InventoryParameterViewModel { get; }

        public bool IsProcessing { get; private set; }

        public string CreateOrUpdateText => _inventoryId.HasValue ? 
            Properties.Resources.Update : 
            Properties.Resources.Create;

        public BlApiAddInventoryViewModel(OnInventoryServiceRequested request, 
            BlInventoryQueryResult blQueryResult,
            ILogger logger, 
            IBricklinkClient bricklinkClient)
        {
            _request = request;
            _blQueryResult = blQueryResult;
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

            var inventory = await _bricklinkClient.CreateInventoryAsync(newInventory);
            return inventory.InventoryId;
        }

        private async Task UpdateInventoryAsync()
        {
            await Task.Delay(1000);
        }

        public async Task CreateOrUpdateAsync()
        {
            IsProcessing = true;

            try
            {
                if (WillCreate)
                {
                    var inventoryId = await CreateInventoryAsync();
                    _inventoryId = inventoryId;
                    NotifyOfPropertyChange(nameof(CreateOrUpdateText));
                }
                else
                {
                    await UpdateInventoryAsync();
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, 
                    $"Failed to {(WillCreate ? "create" : "update")} inventory lot. Received {{Message}}", 
                    exception.Message);
            }

            IsProcessing = false;
        }
    }
}