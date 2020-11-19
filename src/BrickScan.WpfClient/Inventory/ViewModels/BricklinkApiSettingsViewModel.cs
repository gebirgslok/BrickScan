using PropertyChanged;
using Stylet;

namespace BrickScan.WpfClient.Inventory.ViewModels
{
    public class BricklinkApiSettingsViewModel : PropertyChangedBase
    {
        private readonly IUserConfiguration _userConfiguration;
        private string? _tokenValueBeforeSave;
        private string? _tokenSecretBeforeSave;
        private string? _consumerKeyBeforeSave;
        private string? _consumerSecretBeforeSave;

        public string? BricklinkTokenValue
        {
            get => _userConfiguration.BricklinkTokenValue;
            set => _userConfiguration.BricklinkTokenValue = value;
        }

        public string? BricklinkTokenSecret
        {
            get => _userConfiguration.BricklinkTokenSecret;
            set => _userConfiguration.BricklinkTokenSecret = value;
        }

        public string? BricklinkConsumerKey
        {
            get => _userConfiguration.BricklinkConsumerKey;
            set => _userConfiguration.BricklinkConsumerKey = value;
        }

        public string? BricklinkConsumerSecret
        {
            get => _userConfiguration.BricklinkConsumerSecret;
            set => _userConfiguration.BricklinkConsumerSecret = value;
        }

        [DependsOn(nameof(BricklinkTokenValue), nameof(BricklinkTokenSecret), nameof(BricklinkConsumerKey), nameof(BricklinkConsumerSecret))]
        public bool HasUnsavedBlTokenProperties => !string.Equals(_tokenValueBeforeSave, BricklinkTokenValue) ||
                                                   !string.Equals(_tokenSecretBeforeSave, BricklinkTokenSecret) ||
                                                   !string.Equals(_consumerKeyBeforeSave, BricklinkConsumerKey) ||
                                                   !string.Equals(_consumerSecretBeforeSave, BricklinkConsumerSecret);

        public BricklinkApiSettingsViewModel(IUserConfiguration userConfiguration)
        {
            _userConfiguration = userConfiguration;           
            UpdateTokenPropertiesBeforeSave();
        }

        private void UpdateTokenPropertiesBeforeSave()
        {
            _tokenValueBeforeSave = _userConfiguration.BricklinkTokenValue;
            _tokenSecretBeforeSave = _userConfiguration.BricklinkTokenSecret;
            _consumerKeyBeforeSave = _userConfiguration.BricklinkConsumerKey;
            _consumerSecretBeforeSave = _userConfiguration.BricklinkConsumerSecret;
        }

        public void SaveBricklinkCredentials()
        {
            _userConfiguration.SaveBricklinkCredentials();
            UpdateTokenPropertiesBeforeSave();
            NotifyOfPropertyChange(nameof(HasUnsavedBlTokenProperties));
        }
    }
}