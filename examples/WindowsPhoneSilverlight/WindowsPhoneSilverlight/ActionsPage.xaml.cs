using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Net.Http;
using System.Threading;
using System.Net.Http.Headers;

namespace WindowsPhoneSilverlight
{
    public partial class ActionsPage : PhoneApplicationPage
    {
        private string idToken;

        public ActionsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        
            string idToken;

            if (!NavigationContext.QueryString.TryGetValue("id_token", out idToken))
            {
                throw new InvalidOperationException("Navigated without id_token parameter");
            }

            this.idToken = idToken;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.idToken);                

            // using online sample to workaround emulator limitations accesing host
            var responseMessage = await client.GetAsync(
                new Uri("http://auth0-aspnet-webapi-owin.azurewebsites.net/secured/ping", UriKind.Absolute),
                CancellationToken.None);

            var content = await responseMessage.Content.ReadAsStringAsync();
        
            MessageBox.Show(content);
        }
    }
}