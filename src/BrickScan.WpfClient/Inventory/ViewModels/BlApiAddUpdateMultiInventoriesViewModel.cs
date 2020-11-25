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
using System.Linq.Expressions;
using BrickScan.WpfClient.Events;
using PropertyChanged;
using Stylet;

namespace BrickScan.WpfClient.Inventory.ViewModels
{
    public class InventoryDto : PropertyChangedBase
    {
        [AlsoNotifyFor(nameof(UnitPrice), nameof(Id), nameof(Quantity), nameof(Remarks))]
        public BricklinkSharp.Client.Inventory Inventory { get; set; }

        public decimal UnitPrice => Inventory.UnitPrice;

        public int Id => Inventory.InventoryId;

        public int Quantity => Inventory.Quantity;

        public string? Remarks => Inventory.Remarks;

        public InventoryDto(BricklinkSharp.Client.Inventory inventory)
        {
            Inventory = inventory;
        }
    }

    public class BlApiAddUpdateMultiInventoriesViewModel : Conductor<BlApiAddInventoryViewModel?>
    {
        private readonly BlInventoryQueryResult _blQueryResult;
        private readonly OnInventoryServiceRequested _request;
        private readonly Func<OnInventoryServiceRequested, BlInventoryQueryResult, BlApiAddInventoryViewModel> _addInventoryVmFactory;

        public BlApiAddInventoryViewModel BlApiAddInventoryViewModel { get; }

        public IEnumerable<InventoryDto> Inventories { get;  }

        public InventoryDto? SelectedInventory { get; set; }

        public BlApiAddUpdateMultiInventoriesViewModel(OnInventoryServiceRequested request,
            BlInventoryQueryResult blQueryResult,
            Func<OnInventoryServiceRequested, BlInventoryQueryResult, BlApiAddInventoryViewModel> addInventoryVmFactory)
        {
            _request = request;
            _blQueryResult = blQueryResult;

            Inventories = _blQueryResult
                .InventoryList
                .Select(x => new InventoryDto(x))
                .ToArray();

            _addInventoryVmFactory = addInventoryVmFactory;
            BlApiAddInventoryViewModel = _addInventoryVmFactory.Invoke(request, _blQueryResult);
            SelectedInventory = Inventories.First();
        }

        private void ResolveActiveItem()
        {
            if (SelectedInventory == null)
            {
                ActiveItem = null;
                return;
            }

            var viewModel = _addInventoryVmFactory.Invoke(_request, _blQueryResult);
            BlApiHelpers.PopulateUpdateViewModel(viewModel, SelectedInventory);
            ActiveItem = viewModel;
        }

        protected override void NotifyOfPropertyChange(string propertyName = "")
        {
            base.NotifyOfPropertyChange(propertyName);

            if (string.Equals(propertyName, nameof(SelectedInventory)))
            {
                ResolveActiveItem();
            }
        }
    }
}