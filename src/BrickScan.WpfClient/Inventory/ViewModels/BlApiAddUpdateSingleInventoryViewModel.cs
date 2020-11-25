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
using BrickScan.WpfClient.Events;
using Stylet;

namespace BrickScan.WpfClient.Inventory.ViewModels
{
    public class BlApiAddUpdateSingleInventoryViewModel : PropertyChangedBase
    {
        public BlApiAddInventoryViewModel BlApiAddInventoryViewModel { get; }

        public BlApiAddInventoryViewModel BlApiUpdateInventoryViewModel { get; }

        public BlApiAddUpdateSingleInventoryViewModel(OnInventoryServiceRequested request,
            BlInventoryQueryResult blQueryResult,
            Func<OnInventoryServiceRequested, BlInventoryQueryResult, BlApiAddInventoryViewModel> addInventoryVmFactory,
            Func<OnInventoryServiceRequested, BlInventoryQueryResult, BlApiAddInventoryViewModel> updateInventoryVmFactory)
        {
            BlApiAddInventoryViewModel = addInventoryVmFactory.Invoke(request, blQueryResult);

            var updateViewModel = updateInventoryVmFactory.Invoke(request, blQueryResult);
            PopulateUpdateViewModel(updateViewModel, blQueryResult.InventoryList[0]);
            BlApiUpdateInventoryViewModel = updateViewModel;
        }

        private void PopulateUpdateViewModel(BlApiAddInventoryViewModel updateViewModel, BricklinkSharp.Client.Inventory inventory)
        {
            updateViewModel.BlApiCreateUpdateInventoryViewModel.SetInventory(inventory);
            updateViewModel.BlApiCreateUpdateInventoryViewModel.InventoryParameterViewModel.PricePerPart =
                inventory.UnitPrice;
            updateViewModel.BlApiCreateUpdateInventoryViewModel.InventoryParameterViewModel.StorageOrBin =
                inventory.Remarks;
        }
    }
}
