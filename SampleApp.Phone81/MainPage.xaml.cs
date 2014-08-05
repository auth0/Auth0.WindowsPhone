using Auth0.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace SampleApp.Phone81
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        private Auth0Client auth0 = new Auth0Client(
                "contoso.auth0.com",
                "cEPMBtnqXQdFBuUyJeAugh6W7kLIoepB");

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await auth0.LoginAsync();
                //var user = await auth0.LoginAsync("google-oauth2");
                //var user = await auth0.LoginAsync("sql-azure-database", "foo@dasd.com", "bar");
            }
            catch (AuthenticationCancelException)
            {
                return;
            }
            catch (AuthenticationErrorException ex)
            {
                // can't await in catch (until C# 6) avoid complicated error handling
                System.Diagnostics.Debug.WriteLine("Error:" + ex.Message);
                return;
            }

            await MessageHelpers.ShowDialogAsync(
                string.Format(
                    "Your email: {0}\r\nYour id_token: {1}",
                        auth0.CurrentUser.Profile["email"], auth0.CurrentUser.IdToken));

            var command = await MessageHelpers.ShowOKCancelAsync(
                "Do you want a delegation token to call another API?",
                "Delegation Token");

            if (command.Label == "OK")
            {
                var targetClientId = "HmqDkk9qtDgxsiSKpLKzc51xD75hgiRW";
                var options = new Dictionary<string, string>
                {
                    { "scope", "openid profile" }
                };

                var delegationResult = await auth0.GetDelegationToken(targetClientId: targetClientId, options: options);

                await MessageHelpers.ShowDialogAsync(string.Format("Your delegation token: {0}", delegationResult["id_token"]));
            }
        }

        private async void Logout_Click(object sender, RoutedEventArgs e)
        {
            await auth0.LogoutAsync();
            await MessageHelpers.ShowDialogAsync("Logout successful!");
        }
    }
}
