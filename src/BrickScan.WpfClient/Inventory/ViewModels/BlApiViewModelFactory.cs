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
    public class BlApiViewModelFactory : IBlApiViewModelFactory
    {
        private readonly Func<OnInventoryServiceRequested,BlInventoryQueryResult, BlApiAddInventoryViewModel> _addInventoryVmFactory;
        private readonly Func<OnInventoryServiceRequested,BlInventoryQueryResult, BlApiAddUpdateSingleInventoryViewModel> _addUpdateSingleInventoryVmFactory;
        private readonly Func<OnInventoryServiceRequested,BlInventoryQueryResult, BlApiAddUpdateMultiInventoriesViewModel> _updateMultiInventoriesVmFactory;

        public BlApiViewModelFactory(Func<OnInventoryServiceRequested, BlInventoryQueryResult, BlApiAddInventoryViewModel> addInventoryVmFactory, Func<OnInventoryServiceRequested, BlInventoryQueryResult, BlApiAddUpdateSingleInventoryViewModel> addUpdateSingleInventoryVmFactory, Func<OnInventoryServiceRequested, BlInventoryQueryResult, BlApiAddUpdateMultiInventoriesViewModel> updateMultiInventoriesVmFactory)
        {
            _addInventoryVmFactory = addInventoryVmFactory;
            _addUpdateSingleInventoryVmFactory = addUpdateSingleInventoryVmFactory;
            _updateMultiInventoriesVmFactory = updateMultiInventoriesVmFactory;
        }

        public PropertyChangedBase Create(OnInventoryServiceRequested request, BlInventoryQueryResult queryResult)
        {
            if (queryResult.InventoryList.Length == 0)
            {
                return _addInventoryVmFactory.Invoke(request, queryResult);
            }

            if (queryResult.InventoryList.Length == 1)
            {
                return _addUpdateSingleInventoryVmFactory.Invoke(request, queryResult);
            }

            return _updateMultiInventoriesVmFactory.Invoke(request, queryResult);
        }
    }
}