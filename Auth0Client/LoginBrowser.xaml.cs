using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Auth0.SDK
{
    public partial class LoginBrowser : UserControl
    {
        public Uri StartUrl { get; set; }
        public Uri EndUrl { get; set; }

        static char[] AmpersandChars = new char[] { '&' };
        static char[] EqualsChars = new char[] { '=' };

        public event EventHandler<AuthenticatorCompletedEventArgs> Completed;
        public event EventHandler<AuthenticatorErrorEventArgs> Error;

        private WebBrowser loadingBrowser = new WebBrowser();

        public LoginBrowser(Uri startUrl, Uri endUrl)
        {
            InitializeComponent();
            this.StartUrl = startUrl;
            this.EndUrl = endUrl;

            this.Browser.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void Browser_Navigating(object sender, NavigatingEventArgs e)
        {
            this.Browser.Visibility = System.Windows.Visibility.Visible;

            var url = e.Uri;
            var fragment = FormDecode(url.Fragment);
            var all = new Dictionary<string, string>(FormDecode(url.Query));
            foreach (var kv in fragment)
                all[kv.Key] = kv.Value;

            //
            // Check for errors
            //
            if (all.ContainsKey("error"))
            {
                var description = all["error"];
                if (all.ContainsKey("error_description"))
                {
                    description = all["error_description"];
                }
                OnError(description);
                e.Cancel = true;
                return;
            }

            //
            // Watch for the redirect
            //
            if (UrlMatchesRedirect(url))
            {
                if (fragment == null || fragment.Keys.Count == 1)
                {
                    OnError("The response is too large and Internet Explorer does not support it. Try using scope=openid instead or remove attributes with rules");
                }
                else
                {
                    OnCompleted(fragment);
                }

                e.Cancel = true;
            }
        }

        protected virtual void OnCompleted(IDictionary<string, string> fragment)
        {
            if (Completed != null)
                Completed(this, new AuthenticatorCompletedEventArgs(new Auth0User(fragment)));
        }

        protected virtual void OnError(string error)
        {
            if (Error != null)
                Error(this, new AuthenticatorErrorEventArgs(error));
        }

        protected virtual void OnError(Exception ex)
        {
            if (Error != null)
                Error(this, new AuthenticatorErrorEventArgs(ex));
        }

        private bool UrlMatchesRedirect(Uri url)
        {
            return url.Host == this.EndUrl.Host && url.LocalPath == this.EndUrl.LocalPath;
        }

        private static IDictionary<string, string> FormDecode(string encodedString)
        {
            var inputs = new Dictionary<string, string>();

            if (encodedString.StartsWith("?") || encodedString.StartsWith("#"))
            {
                encodedString = encodedString.Substring(1);
            }

            var parts = encodedString.Split(AmpersandChars);
            foreach (var p in parts)
            {
                var kv = p.Split(EqualsChars);
                var k = Uri.UnescapeDataString(kv[0]);
                var v = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
                inputs[k] = v;
            }

            return inputs;
        }

        public void ShowUI(PhoneApplicationPage owner)
        {
            this.Browser.Navigate(this.StartUrl);
            this.Content = owner;
        }

        public class AuthenticatorCompletedEventArgs : EventArgs
        {
            /// <summary>
            /// Whether the authentication succeeded and there is a valid <see cref="Account"/>.
            /// </summary>
            /// <value>
            /// <see langword="true"/> if the user is authenticated; otherwise, <see langword="false"/>.
            /// </value>
            public bool IsAuthenticated { get { return Account != null; } }

            /// <summary>
            /// Gets the account created that represents this authentication.
            /// </summary>
            /// <value>
            /// The account.
            /// </value>
            public Auth0User Account { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Xamarin.Auth.AuthenticatorCompletedEventArgs"/> class.
            /// </summary>
            /// <param name='account'>
            /// The account created or <see langword="null"/> if authentication failed or was canceled.
            /// </param>
            public AuthenticatorCompletedEventArgs(Auth0User account)
            {
                Account = account;
            }
        }

        public class AuthenticatorErrorEventArgs : EventArgs
        {
            /// <summary>
            /// Gets a message describing the error.
            /// </summary>
            /// <value>
            /// The message.
            /// </value>
            public string Message { get; private set; }

            /// <summary>
            /// Gets the exception that signaled the error if there was one.
            /// </summary>
            /// <value>
            /// The exception or <see langword="null"/>.
            /// </value>
            public Exception Exception { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Xamarin.Auth.AuthenticatorErrorEventArgs"/> class
            /// with a message but no exception.
            /// </summary>
            /// <param name='message'>
            /// A message describing the error.
            /// </param>
            public AuthenticatorErrorEventArgs(string message)
            {
                Message = message;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Xamarin.Auth.AuthenticatorErrorEventArgs"/> class with an exception.
            /// </summary>
            /// <param name='exception'>
            /// The exception signaling the error. The message of this object is retrieved from this exception or
            /// its inner exceptions.
            /// </param>
            public AuthenticatorErrorEventArgs(Exception exception)
            {
                Message = exception.ToString();
                Exception = exception;
            }
        }
    }
}
