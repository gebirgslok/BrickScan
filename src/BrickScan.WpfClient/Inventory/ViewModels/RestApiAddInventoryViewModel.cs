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
using BrickScan.WpfClient.Events;
using BrickScan.WpfClient.Model;
using Stylet;

namespace BrickScan.WpfClient.Inventory.ViewModels
{
    public class RestApiAddInventoryViewModel : PropertyChangedBase
    {
        private readonly OnInventoryServiceRequested _request;
        private readonly IEventAggregator _eventAggregator;

        public DatasetItemContainer Item => _request.Item;

        public RestApiSendInventoryRequestViewModel RestApiSendInventoryRequestViewModel { get; }

        public RestApiAddInventoryViewModel(OnInventoryServiceRequested request, 
            Func<OnInventoryServiceRequested, RestApiSendInventoryRequestViewModel> restApiSendInventoryRequestVmFactory, 
            IEventAggregator eventAggregator)
        {
            _request = request;
            RestApiSendInventoryRequestViewModel = restApiSendInventoryRequestVmFactory.Invoke(_request);
            _eventAggregator = eventAggregator;
        }

        public void NavigateBack()
        {
            _eventAggregator.PublishOnUIThread(new OnInventoryServiceCloseRequested(_request.PredictionResultViewModel));
        }
    }
}
