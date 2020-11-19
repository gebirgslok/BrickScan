using System;
using Stylet;

namespace BrickScan.WpfClient.Inventory.ViewModels
{
    public interface IInventoryServiceViewModelFactory
    {
        PropertyChangedBase CreateSettingsViewModel(InventoryServiceType inventoryServiceType);
    }

    public class InventoryServiceViewModelFactory : IInventoryServiceViewModelFactory
    {
        private readonly BricklinkApiSettingsViewModel _bricklinkApiSettingsViewModel;

        public InventoryServiceViewModelFactory(BricklinkApiSettingsViewModel bricklinkApiSettingsViewModel)
        {
            _bricklinkApiSettingsViewModel = bricklinkApiSettingsViewModel;
        }

        public PropertyChangedBase CreateSettingsViewModel(InventoryServiceType inventoryServiceType)
        {
            switch (inventoryServiceType)
            {
                case InventoryServiceType.BricklinkApi:
                    return _bricklinkApiSettingsViewModel;

                //TODO: change this
                case InventoryServiceType.BricklinkXml:
                    return _bricklinkApiSettingsViewModel;

                //TODO: change this
                case InventoryServiceType.CustomRestApi:
                    return _bricklinkApiSettingsViewModel;

                default:
                    throw new ArgumentOutOfRangeException(nameof(inventoryServiceType), inventoryServiceType, null);
            }
        }
    }
}
