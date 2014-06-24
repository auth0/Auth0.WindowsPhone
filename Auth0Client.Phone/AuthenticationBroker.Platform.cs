using System.Threading.Tasks;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using System;
using Windows.UI.Xaml;

namespace Auth0.SDK
{
    internal partial class AuthenticationBroker
    {
        public async Task Logout()
        {
            var cookieManager = new HttpBaseProtocolFilter().CookieManager;

            foreach (HttpCookie cookie in visitedHosts
                .SelectMany(host => cookieManager.GetCookies(
                    new Uri(string.Format("http://{0}/", host)))))
            {
                cookieManager.DeleteCookie(cookie);
            }
        }

        private static Frame GetRootFrame()
        {
            return Window.Current.Content as Frame;
        }
    }
}
