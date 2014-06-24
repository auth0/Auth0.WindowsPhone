// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Apache License (https://github.com/WindowsAzure/azure-mobile-services/blob/master/LICENSE.txt)
// ----------------------------------------------------------------------------

#if WINDOWS_PHONE
using Microsoft.Phone.Controls;
#else
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
#endif
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
#if WINDOWS_PHONE
using System.Windows.Navigation;
#else
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http.Filters;
using Windows.Web.Http;
#endif

namespace Auth0.SDK
{
    /// <summary>
    /// This class mimics the functionality provided by WebAuthenticationStatus available in Win8.
    /// </summary>
    internal enum PhoneAuthenticationStatus
    {
        Success = 0,

        UserCancel = 1,

        ErrorHttp = 2,

        ErrorServer = 3
    }

    /// <summary>
    /// An AuthenticationBroker for the Windows Phone Platform 
    /// that is like the Windows Store WebAuthenticationBroker 
    /// APIs.
    /// </summary>
    internal partial class AuthenticationBroker : IDisposable
    {
        static char[] AmpersandChars = new char[] { '&' };
        static char[] EqualsChars = new char[] { '=' };

        public Uri LoginPageUri { get; set; }

        /// <summary>
        /// Indicates if authentication is currently in progress or not.
        /// </summary>
        public bool AuthenticationInProgress { get; private set; }

        /// <summary>
        /// The URL that the <see cref="AuthenticationBroker"/> started at
        /// to begin the authentication flow. 
        /// </summary>
        public Uri StartUri { get; private set; }

        /// <summary>
        /// The URL that the <see cref="AuthenticationBroker"/> will use to
        /// determine if the authentication flow has completed or not.
        /// </summary>
        public Uri EndUri { get; private set; }

        private string responseData = string.Empty;
        private string responseErrorDetail = string.Empty;
        private PhoneAuthenticationStatus responseStatus = PhoneAuthenticationStatus.UserCancel;
        private AutoResetEvent authenticateFinishedEvent = new AutoResetEvent(false);
        private LoginPage page;
        private static HashSet<string> visitedHosts = new HashSet<string>();

        /// <summary>
        /// Instantiates a new <see cref="AuthenticationBroker"/>.
        /// </summary>
        public AuthenticationBroker()
        {
            this.LoginPageUri = new Uri("/Auth0Client.Phone.Silverlight;component/loginpage.xaml", UriKind.Relative);
        }

        /// <summary>
        /// Begins a server-side authentication flow by navigating the WebAuthenticationBroker
        /// to the <paramref name="startUrl"/>.
        /// </summary>
        /// <param name="startUrl">The URL that the browser-based control should 
        /// first navigate to in order to start the authenication flow.
        /// </param>
        /// <param name="endUrl">The URL that indicates the authentication flow has 
        /// completed. Upon being redirected to any URL that starts with the 
        /// <paramref name="endUrl"/>, the browser-based control must stop navigating and
        /// return the response data to the <see cref="AuthenticationBroker"/>.
        /// </param>
        /// <returns>
        /// The object containing the user profile, JSON Web Token signed and the access_token.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the user cancels the authentication flow or an error occurs during
        /// the authentication flow.
        /// </exception>
        public Task<Auth0User> AuthenticateAsync(Uri startUrl, Uri endUrl)
        {
            this.StartUri = startUrl;
            this.EndUri = endUrl;
            var rootFrame = GetRootFrame();

            if (rootFrame == null)
            {
                throw new InvalidOperationException();
            }

            this.AuthenticationInProgress = true;

            //hook up the broker to the page on the event.
            rootFrame.Navigated += rootFrame_Navigated;

            // Navigate to the login page.
#if WINDOWS_PHONE
            rootFrame.Navigate(this.LoginPageUri);
#else
            rootFrame.Navigate(typeof(LoginPage));
#endif
            Task<Auth0User> task = Task<Auth0User>.Factory.StartNew(() =>
            {
                authenticateFinishedEvent.WaitOne();
                if (this.responseStatus != PhoneAuthenticationStatus.Success)
                {
                    string message;
                    if (this.responseStatus == PhoneAuthenticationStatus.UserCancel)
                    {
                        throw new AuthenticationCancelException();
                    }
                    else
                    {
                        message = string.Format(CultureInfo.InvariantCulture,
                                                "Authentication has failed. {0}",
                                                this.responseErrorDetail);
                    }

                    throw new AuthenticationErrorException(message);
                }

                return GetTokenStringFromResponseData(this.responseData);
            });

            return task;
        }


        /// <summary>
        /// Invoked to track a Uri that was visited during the login.
        /// </summary>
        /// <param name="uri">Uri visited during the login</param>
        internal void TrackUri(Uri uri)
        {
            visitedHosts.Add(uri.Host);
        }

        /// <summary>
        /// Hooks up the broker to the page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = GetRootFrame();
            rootFrame.Navigated -= rootFrame_Navigated;

            LoginPage page = e.Content as LoginPage;
            page.Broker = this;
            this.page = page;
        }

        internal void OnAuthenticationFinished(string data, PhoneAuthenticationStatus status, string error)
        {
            this.responseData = data;
            this.responseStatus = status;
            this.responseErrorDetail = error;

            this.AuthenticationInProgress = false;

            // Signal the waiting task that the authentication operation has finished.
            authenticateFinishedEvent.Set();
        }


        internal Auth0User GetTokenStringFromResponseData(string responseData)
        {
            var url = new Uri(responseData);
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
                throw new InvalidOperationException(description);
            }

            return new Auth0User(fragment);
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

        /// <summary>
        /// Implemenation of <see cref="IDisposable"/>
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implemenation of <see cref="IDisposable"/> for
        /// derived classes to use.
        /// </summary>
        /// <param name="disposing">
        /// Indicates if being called from the Dispose() method
        /// or the finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (this.authenticateFinishedEvent != null)
                {
                    this.authenticateFinishedEvent.Dispose();
                    this.authenticateFinishedEvent = null;
                }
            }
        }
    }
}
