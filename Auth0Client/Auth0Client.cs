using System.Threading;
using Microsoft.Phone.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Auth0.SDK
{
    /// <summary>
    /// A simple client to Authenticate Users with Auth0.
    /// </summary>
    public partial class Auth0Client
    {
        private const string AuthorizeUrl = "https://{0}.auth0.com/authorize?client_id={1}&scope={2}&redirect_uri={3}&response_type=token&connection={4}";
        private const string LoginWidgetUrl = "https://{0}.auth0.com/login/?client={1}&scope={2}&redirect_uri={3}&response_type=token";
        private const string ResourceOwnerEndpoint = "https://{0}.auth0.com/oauth/ro";
        private const string UserInfoEndpoint = "https://{0}.auth0.com/userinfo?access_token={1}";
        private const string DefaultCallback = "https://{0}.auth0.com/mobile";

        private readonly string subDomain;
        private readonly string clientId;
        private readonly string clientSecret;

        private readonly AuthenticationBroker broker;

        internal string State { get; set; }

        public Auth0Client(string subDomain, string clientId, string clientSecret)
        {
            this.subDomain = subDomain;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.broker = new AuthenticationBroker();
        }

        public Auth0User CurrentUser { get; private set; }

        public string CallbackUrl
        {
            get
            {
                return string.Format(DefaultCallback, this.subDomain);
            }
        }

        /// <summary>
        /// Login a user into an Auth0 application. Attempts to do a background login, but if unsuccessful shows an embedded browser window either showing the widget or skipping it by passing a connection name
        /// </summary>
        /// <param name="connection">Optional connection name to bypass the login widget</param>
        /// <remarks>When using openid profile if the user has many attributes the token might get big and the embedded browser (Internet Explorer) won't be able to parse a large URL</remarks>
        /// <returns>Returns a Task of Auth0User</returns>
        public async Task<Auth0User> LoginAsync(string connection = "", string scope = "openid")
        {
            var startUri = GetStartUri(connection, scope);
            var expectedEndUri = new Uri(this.CallbackUrl);

            var backgroundLoginResult = await DoBackgroundLoginAsync(startUri, expectedEndUri);
            
            var user = backgroundLoginResult.Success
                ? backgroundLoginResult.User
                : await DoInteractiveLoginAsync(backgroundLoginResult.LoginProcessUri, expectedEndUri);
            
            return await RetrieveAuthenticatedUserProfileAsync(user);
        }

        /// <summary>
        /// Uses a hidden browser object to perform a background authentication, for re-authentication attemps after initial registration
        /// </summary>
        /// <param name="startUri">Uri pointing to the start of the authentication process</param>
        /// <param name="expectedEndUri">Expected callback Uri at successful completion of authentication process</param>
        /// <returns>Authenticated Auth0User</returns>
        private async Task<BackgroundLoginResult> DoBackgroundLoginAsync(Uri startUri, Uri expectedEndUri)
        {
            Uri endUri = null;
            var resetEvent = new AutoResetEvent(false);
            var backgroundBrowser = new WebBrowser();
            backgroundBrowser.Navigated += (o, e) =>
            {
                endUri = e.Uri;
                resetEvent.Set();
            };
            
            backgroundBrowser.Navigate(startUri);
            await Task.Factory.StartNew(() => resetEvent.WaitOne());
            
            if (endUri == expectedEndUri)
            {
                return new BackgroundLoginResult(broker.GetTokenStringFromResponseData(endUri.ToString()));
            }

            return new BackgroundLoginResult(endUri);
        }

        /// <summary>
        /// Takes over the root frame to display a browser to the user
        /// </summary>
        /// <param name="startUri">Uri pointing to the start of the authentication process</param>
        /// <param name="expectedEndUri">Expected callback Uri at successful completion of authentication process</param>
        /// <returns>Authenticated Auth0User</returns>
        private async Task<Auth0User> DoInteractiveLoginAsync(Uri startUri, Uri expectedEndUri)
        {
            return await this.broker.AuthenticateAsync(startUri, expectedEndUri);
        }

        /// <summary>
        /// Augments an authenticated Auth0User with profile information
        /// </summary>
        /// <param name="user">Authenticated Auth0User</param>
        /// <returns>Authenticated Auth0User populated with profile information</returns>
        private async Task<Auth0User> RetrieveAuthenticatedUserProfileAsync(Auth0User user)
        {
            var endpoint = string.Format(UserInfoEndpoint, this.subDomain, user.Auth0AccessToken);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = "GET";

            TaskFactory taskFactory = new TaskFactory();
            var response = await taskFactory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
            try
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader streamReader = new StreamReader(responseStream))
                    {
                        var text = streamReader.ReadToEnd();
                        user.Profile = JObject.Parse(text);
                        streamReader.Close();
                    }
                    responseStream.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return user;
        }

        /// <summary>
        ///  Log a user into an Auth0 application given an user name and password.
        /// </summary>
        /// <returns>Task that will complete when the user has finished authentication.</returns>
        /// <param name="connection" type="string">The name of the connection to use in Auth0. Connection defines an Identity Provider.</param>
        /// <param name="userName" type="string">User name.</param>
        /// <param name="password type="string"">User password.</param>
        public async Task<Auth0User> LoginAsync(string connection, string userName, string password, string scope = "openid profile")
        {
            var endpoint = string.Format(ResourceOwnerEndpoint, this.subDomain);
            var parameters = String.Format(
                "client_id={0}&client_secret={1}&connection={2}&username={3}&password={4}&grant_type=password&scope={5}",
                this.clientId,
                this.clientSecret,
                connection,
                userName,
                password,
                scope);

            byte[] postData = Encoding.UTF8.GetBytes(parameters);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postData.Length;

            TaskFactory taskFactory = new TaskFactory();
            var response = await taskFactory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
            try
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader streamReader = new StreamReader(responseStream))
                    {
                        var text = streamReader.ReadToEnd();
                        var data = JObject.Parse(text).ToObject<Dictionary<string, string>>();

                        if (data.ContainsKey("error"))
                        {
                            throw new UnauthorizedAccessException("Error authenticating: " + data["error"]);
                        }
                        else if (data.ContainsKey("access_token"))
                        {
                            this.SetupCurrentUser(data);
                        }
                        else
                        {
                            throw new UnauthorizedAccessException("Expected access_token in access token response, but did not receive one.");
                        }

                        streamReader.Close();
                    }
                    responseStream.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return this.CurrentUser;
        }

        /// <summary>
        /// Log a user out of a Auth0 application.
        /// </summary>
        public async Task LogoutAsync()
        {
            this.CurrentUser = null;
            await this.broker.Logout();
        }

        private void SetupCurrentUser(IDictionary<string, string> accountProperties)
        {
            this.CurrentUser = new Auth0User(accountProperties);
        }

        private Uri GetStartUri(string connection, string scope)
        {
            // Generate state to include in startUri
            var chars = new char[16];
            var rand = new Random();
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)rand.Next((int)'a', (int)'z' + 1);
            }

            var authorizeUri = !string.IsNullOrWhiteSpace(connection) ?
                string.Format(AuthorizeUrl, subDomain, clientId, scope, Uri.EscapeDataString(this.CallbackUrl), connection) :
                string.Format(LoginWidgetUrl, subDomain, clientId, scope, Uri.EscapeDataString(this.CallbackUrl));

            this.State = new string(chars);
            var startUri = new Uri(authorizeUri + "&state=" + this.State);

            return startUri;
        }

        private class BackgroundLoginResult
        {
            internal BackgroundLoginResult(Auth0User user)
            {
                User = user;
                Success = true;
            }

            internal BackgroundLoginResult(Uri loginUri)
            {
                LoginProcessUri = loginUri;
                Success = false;
            }

            public Auth0User User { get; private set; }
            public Uri LoginProcessUri { get; private set; }
            public bool Success { get; private set; }
        }
    }
}
