using Auth0.SDK;
using Microsoft.Phone.Controls;
using System.Collections.Generic;
using System.Windows;

namespace SampleApp
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var auth0 = new Auth0Client(
                "contoso.auth0.com",
                "cEPMBtnqXQdFBuUyJeAugh6W7kLIoepB");

            var user = await auth0.LoginAsync();
            //var user = await auth0.LoginAsync("google-oauth2");
            //var user = await auth0.LoginAsync("sql-azure-database", "foo@dasd.com", "bar");
            
            MessageBox.Show(
                string.Format(
                    "Your email: {0}\r\nYour id_token: {1}",
                        user.Profile["email"], user.IdToken));

            if (MessageBox.Show(
                "Do you want a delegation token?", 
                "Delegation Token", 
                MessageBoxButton.OK) == MessageBoxResult.OK)
            {
                var targetClientId = "HmqDkk9qtDgxsiSKpLKzc51xD75hgiRW";
                var options = new Dictionary<string, string>
                {
                    { "scope", "openid profile" }
                };

                //var delegationResult = await auth0.GetDelegationToken(targetClientId);
                var delegationResult = await auth0.GetDelegationToken(targetClientId, options);

                MessageBox.Show(
                    string.Format("Your delegation token: {0}", delegationResult["id_token"]));
            }
        }

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}