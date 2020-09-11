using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using BrickScan.WpfClient.Events;
using Stylet;

namespace BrickScan.WpfClient.ViewModels
{
    public sealed class AddPartsConductorViewModel : Conductor<PropertyChangedBase>, IHandle<OnEditClassMetaRequested>
    {
        private readonly AddPartsViewModel _addPartsViewModel;
        private readonly Func<IEnumerable<BitmapSource>, EditPartMetaViewModel> _editPartMetaViewModelFactory;

        public AddPartsConductorViewModel(AddPartsViewModel addPartsViewModel, 
            Func<IEnumerable<BitmapSource>, EditPartMetaViewModel> editPartMetaViewModelFactory)
        {
            DisposeChildren = false;
            _addPartsViewModel = addPartsViewModel;
            _editPartMetaViewModelFactory = editPartMetaViewModelFactory;
            ActiveItem = _addPartsViewModel;
        }

        public void Handle(OnEditClassMetaRequested message)
        {
            var viewModel = _editPartMetaViewModelFactory.Invoke(_addPartsViewModel.Images);
            ActiveItem = viewModel;
        }
    }
}
