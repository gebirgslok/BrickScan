using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using BrickScan.WpfClient.Events;
using Stylet;

namespace BrickScan.WpfClient.ViewModels
{
    public sealed class AddPartsConductorViewModel : Conductor<PropertyChangedBase>, 
        IHandle<OnEditClassMetaRequested>, 
        IHandle<OnEditPartMetaCloseRequested>
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

        public void Handle(OnEditPartMetaCloseRequested message)
        {
            if (ActiveItem is EditPartMetaViewModel editPartMetaViewModel)
            {
                editPartMetaViewModel.Dispose();
            }

            if (message.ClearExistingImages)
            {
                _addPartsViewModel.Clear();
            }

            ActiveItem = _addPartsViewModel;
        }
    }
}
