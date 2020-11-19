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
using BrickScan.WpfClient.ViewModels;
using Stylet;

namespace BrickScan.WpfClient.Inventory.ViewModels
{
    public class BricklinkInventoryQueryResult
    {

    }

    public class BricklinkApiViewModelFactory : IBricklinkApiViewModelFactory
    {
        private readonly Func<BricklinkApiAddInventoryViewModel> _addInventoryViewModel;

        public BricklinkApiViewModelFactory(Func<BricklinkApiAddInventoryViewModel> addInventoryViewModel)
        {
            _addInventoryViewModel = addInventoryViewModel;
        }

        public PropertyChangedBase Create(BricklinkInventoryQueryResult queryResult)
        {
            return _addInventoryViewModel.Invoke();
        }
    }

    public interface IBricklinkApiViewModelFactory
    {
        PropertyChangedBase Create(BricklinkInventoryQueryResult queryResult);
    }

    public class BricklinkApiInventoryOverviewViewModel : Conductor<PropertyChangedBase>
    {
        private readonly OnInventoryServiceRequested _request;
        private readonly IBricklinkApiViewModelFactory _bricklinkApiViewModelFactory;

        public NotifyTask<PropertyChangedBase?> InitializationNotifier { get; }

        public BricklinkApiInventoryOverviewViewModel(OnInventoryServiceRequested request,
            Func<Task<PropertyChangedBase?>, PropertyChangedBase?, NotifyTask<PropertyChangedBase?>> notifyTaskFactory, 
            IBricklinkApiViewModelFactory bricklinkApiViewModelFactory)
        {
            _request = request;
            _bricklinkApiViewModelFactory = bricklinkApiViewModelFactory;
            InitializationNotifier = notifyTaskFactory.Invoke(StartQuery(), null);
        }

        private async Task<PropertyChangedBase?> StartQuery()
        {
            //Query Bricklink if requested.

            //Create view

            await Task.Delay(10000);
            return _bricklinkApiViewModelFactory.Create(new BricklinkInventoryQueryResult());
        }

        private void Test(BricklinkInventoryQueryResult result)
        {

        }
    }
}
