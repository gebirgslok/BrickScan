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
using System.ComponentModel;
using System.Threading.Tasks;
using BricklinkSharp.Client;
using BrickScan.WpfClient.Events;
using BrickScan.WpfClient.ViewModels;
using Stylet;

namespace BrickScan.WpfClient.Inventory.ViewModels
{
    public sealed class BlApiInventoryOverviewViewModel : Conductor<PropertyChangedBase?>, IDisposable
    {
        private readonly OnInventoryServiceRequested _request;
        private readonly IBlApiViewModelFactory _bricklinkApiViewModelFactory;
        private readonly IEventAggregator _eventAggregator;
        private bool _isDisposed;

        public NotifyTask<PropertyChangedBase?> InitializationNotifier { get; }

        public BlApiInventoryOverviewViewModel(OnInventoryServiceRequested request,
            Func<Task<PropertyChangedBase?>, PropertyChangedBase?, NotifyTask<PropertyChangedBase?>> notifyTaskFactory,
            IBlApiViewModelFactory bricklinkApiViewModelFactory,
            IEventAggregator eventAggregator,
            IBricklinkClient bricklinkClient)
        {
            _request = request;
            _bricklinkApiViewModelFactory = bricklinkApiViewModelFactory;
            _eventAggregator = eventAggregator;
            InitializationNotifier = notifyTaskFactory.Invoke(QueryBricklinkData(bricklinkClient), null);
            InitializationNotifier.PropertyChanged += HandleInitializationNotifierPropertyChanged;
        }

        ~BlApiInventoryOverviewViewModel()
        {
            Dispose(false);
        }

        private void HandleInitializationNotifierPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is NotifyTask<PropertyChangedBase?> notifer)
            {
                ActiveItem = notifer.Result;
            }
        }

        private async Task<PropertyChangedBase?> QueryBricklinkData(IBricklinkClient bricklinkClient)
        {
            var item = await bricklinkClient.GetItemAsync(ItemType.Part, _request.Item.Number);
            var colorId = _request.Item.BricklinkColor;

            var inventoryList = await bricklinkClient.GetInventoryListAsync(new[] { ItemType.Part },
                includedStatusFlags: new[] { InventoryStatusType.Available },
                includedCategoryIds: new[] { item.CategoryId },
                includedColorIds: new[] { _request.Item.BricklinkColor });

            var priceGuide = await bricklinkClient.GetPriceGuideAsync(ItemType.Part, item.Number, colorId,
                condition: BricklinkSharp.Client.Condition.Used);

            var queryResult = new BlInventoryQueryResult(item, priceGuide, inventoryList);
            return _bricklinkApiViewModelFactory.Create(_request, queryResult);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                InitializationNotifier.PropertyChanged -= HandleInitializationNotifierPropertyChanged;
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void NavigateBack()
        {
            _eventAggregator.PublishOnUIThread(new OnInventoryServiceCloseRequested(_request.PredictionResultViewModel));
        }
    }
}
