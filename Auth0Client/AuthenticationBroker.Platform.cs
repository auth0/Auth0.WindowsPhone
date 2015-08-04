using Microsoft.Phone.Controls;
using System.Threading.Tasks;
using System.Windows;

namespace Auth0.SDK
{
    internal partial class AuthenticationBroker
    {
        public async Task LogoutAsync()
        {
            if (this.page != null)
            {
                await this.page.BrowserControl.ClearCookiesAsync();
            }
        }

        private static PhoneApplicationFrame GetRootFrame()
        {
            return Application.Current.RootVisual as PhoneApplicationFrame;
        }
    }
}
