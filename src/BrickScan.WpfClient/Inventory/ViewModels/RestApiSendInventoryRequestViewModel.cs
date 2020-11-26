using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Stylet;

namespace BrickScan.WpfClient.Inventory.ViewModels
{
    public class RestApiSendInventoryRequestViewModel : PropertyChangedBase
    {
        public InventoryParameterViewModel InventoryParameterViewModel { get; }

        public bool IsProcessing { get; private set; }

        public RestApiSendInventoryRequestViewModel(IUserConfiguration userConfiguration)
        {
            InventoryParameterViewModel = new InventoryParameterViewModel
            {
                Condition = userConfiguration.SelectedInventoryCondition
            };
        }

        public async Task SendRequestAsync()
        {
            IsProcessing = true;
            await Task.Delay(1000);

            IsProcessing = false;
        }
    }
}