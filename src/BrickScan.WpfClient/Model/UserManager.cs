using Stylet;

namespace BrickScan.WpfClient.Model
{
    internal class UserManager : PropertyChangedBase, IUserManager
    {
        public bool IsUserLoggedOn { get; private set; }

        public string? Username { get; private set; }

        public void LogOn()
        {
            IsUserLoggedOn = true;
            Username = "John Wayne";
        }

        public void LogOff()
        {
            IsUserLoggedOn = false;
            Username = null;
        }
    }
}