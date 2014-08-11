using Auth0.SDK;
using Microsoft.Phone.Controls;
using System;
using System.Windows;
using System.Windows.Input;

namespace WindowsPhoneSilverlight
{
    public partial class MainPage : PhoneApplicationPage
    {
        private Auth0Client auth0Client = new Auth0Client("samples.auth0.com", "BUIJSW9x60sIHBw8Kd9EmCbj8eDIFxDC"); 

        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private async void LoginClick(object sender, RoutedEventArgs args)
        {
            try
            {
                var user = await auth0Client.LoginAsync();
                NavigationService.Navigate(new Uri("/ActionsPage.xaml?id_token=" + user.IdToken, UriKind.Relative));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }
       }
    }
}