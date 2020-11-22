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
using BrickScan.Library.Core.Dto;
using BrickScan.WpfClient.Controls;
using BrickScan.WpfClient.Events;
using BrickScan.WpfClient.Inventory.ViewModels;
using BrickScan.WpfClient.Model;
using OpenCvSharp;
using Stylet;

namespace BrickScan.WpfClient.ViewModels
{
    public sealed class PredictionConductorViewModel : Conductor<PropertyChangedBase>, 
        IHandle<OnPredictionRequested>, 
        IHandle<OnPredictionResultCloseRequested>,
        IHandle<OnInventoryServiceRequested>,
        IHandle<OnInventoryServiceCloseRequested>
    {
        private readonly PredictViewModel _predictViewModel;
        private readonly Func<Mat, PredictionResultViewModel> _predictionResultViewModelFunc;
        private readonly IInventoryServiceViewModelFactory _inventoryServiceViewModelFactory;
        private readonly IUserConfiguration _configuration;

        public PredictionConductorViewModel(PredictViewModel predictViewModel, 
            Func<Mat, PredictionResultViewModel> predictionResultViewModelFunc, 
            IInventoryServiceViewModelFactory inventoryServiceViewModelFactory, 
            IUserConfiguration configuration)
        {
            DisposeChildren = false;
            _predictViewModel = predictViewModel;
            _predictionResultViewModelFunc = predictionResultViewModelFunc;
            _inventoryServiceViewModelFactory = inventoryServiceViewModelFactory;
            _configuration = configuration;
            ActiveItem = _predictViewModel;
        }

        public void Handle(OnPredictionRequested message)
        {
            ActiveItem = _predictionResultViewModelFunc.Invoke(message.ImageSection);
        }

        public void Handle(OnPredictionResultCloseRequested message)
        {
            ActiveItem = _predictViewModel;
        }

        public void Handle(OnInventoryServiceRequested message)
        {
            ActiveItem = _inventoryServiceViewModelFactory.CreateViewModel(message, _configuration.SelectedInventoryServiceType);
        }

        public void Temp()
        {
            var c = new ColorDto
            {
                BricklinkColorHtmlCode = "#FF5577FF",
                BricklinkColorId = 1,
                BricklinkColorName = "Super cool",
                BricklinkColorType = "Solid"
            };
            var r = new OnInventoryServiceRequested(new DatasetItemContainer 
            {
                AdditionalIdentifier = "Foo", 
                HtmlColor = c.BricklinkColorHtmlCode,
                ColorName = c.BricklinkColorName,
                Number = "1234abc",
                DisplayImageUrls = new List<string>
                {
                    @"E:\dataset_images\img-20200929_205041457-26c8.png",
                    @"E:\dataset_images\img-20200929_205102971-0808.png"
                }
            }, null);
            ActiveItem = _inventoryServiceViewModelFactory.CreateViewModel(r, _configuration.SelectedInventoryServiceType);
        }

        public void Handle(OnInventoryServiceCloseRequested message)
        {
            if (message.PredictionResultViewModel != null)
            {
                ActiveItem = message.PredictionResultViewModel;
            }
            else
            {
                ActiveItem = _predictViewModel;
            }
        }
    }
}