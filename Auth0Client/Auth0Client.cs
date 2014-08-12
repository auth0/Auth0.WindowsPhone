#if WINDOWS_PHONE
using Microsoft.Phone.Controls;
#endif
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Auth0.SDK
{
    /// <summary>
    /// A simple client to Authenticate Users with Auth0.
    /// </summary>
    public partial class Auth0Client
    {
        private const string AuthorizeUrl = "https://{0}/authorize?client_id={1}&scope={2}&redirect_uri={3}&response_type=token&connection={4}";
        private const string LoginWidgetUrl = "https://{0}/login/?client={1}&scope={2}&redirect_uri={3}&response_type=token";
        private const string ResourceOwnerEndpoint = "https://{0}/oauth/ro";
        private const string DelegationEndpoint = "https://{0}/delegation";
        private const string UserInfoEndpoint = "https://{0}/userinfo?access_token={1}";
        private const string DefaultCallback = "https://{0}/mobile";

        private readonly string domain;
        private readonly string clientId;
        
        private readonly AuthenticationBroker broker;

        public Auth0Client(string domain, string clientId)
        {
            this.domain = domain;
            this.clientId = clientId;
            this.broker = new AuthenticationBroker();
            this.DeviceIdProvider = new Device();
        }

        public Auth0User CurrentUser { get; private set; }

        public string CallbackUrl
        {
            get
            {
                return string.Format(DefaultCallback, this.domain);
            }
        }

        /// <summary>
        /// The component used to generate the device's unique id
        /// </summary>
        public IDeviceIdProvider DeviceIdProvider { get; set; }

        internal string State { get; set; }

        /// <summary>
        /// Login a user into an Auth0 application. Attempts to do a background login, but if unsuccessful shows an embedded browser window either showing the widget or skipping it by passing a connection name
        /// </summary>
        /// <param name="connection">Optional connection name to bypass the login widget</param>
        /// <param name="withRefreshToken">true to include the refresh_token in the response, false (default) otherwise.
        /// The refresh_token allows you to renew the id_token indefinitely (does not expire) unless specifically revoked.</param>
        /// <param name="scope">Optional scope, either 'openid' or 'openid profile'</param>
        /// <returns>Returns a Task of Auth0User</returns>
        public async Task<Auth0User> LoginAsync(string connection = "", bool withRefreshToken = false, string scope = "openid")
        {
            scope = IncreaseScopeWithOfflineAccess(withRefreshToken, scope);

            var user = await this.broker.AuthenticateAsync(await GetStartUri(connection, scope), new Uri(this.CallbackUrl));
            var endpoint = string.Format(UserInfoEndpoint, this.domain, user.Auth0AccessToken);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = "GET";

            TaskFactory taskFactory = new TaskFactory();
            var response = await taskFactory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
            using (Stream responseStream = response.GetResponseStream())
            {
                using (StreamReader streamReader = new StreamReader(responseStream))
                {
                    var text = streamReader.ReadToEnd();
                    user.Profile = JObject.Parse(text);

                    this.CurrentUser = user;
                }
            }

            return this.CurrentUser;
        }

        private static string IncreaseScopeWithOfflineAccess(bool withRefreshToken, string scope)
        {
            if (withRefreshToken && !scope.Contains("offline_access"))
            {
                scope += " offline_access";
            }
            return scope;
        }

        /// <summary>
        ///  Log a user into an Auth0 application given an user name and password.
        /// </summary>
        /// <returns>Task that will complete when the user has finished authentication.</returns>
        /// <param name="connection" type="string">The name of the connection to use in Auth0. Connection defines an Identity Provider.</param>
        /// <param name="userName" type="string">User name.</param>
        /// <param name="password" type="string">User password.</param>
        /// <param name="withRefreshToken">true to include the refresh_token in the response, false otherwise.
        /// The refresh_token allows you to renew the id_token indefinitely (does not expire) unless specifically revoked.</param>
        /// <param name="scope">Scope.</param>
        public async Task<Auth0User> LoginAsync(string connection, string userName, string password, bool withRefreshToken = false, string scope = "openid")
        {
            scope = IncreaseScopeWithOfflineAccess(withRefreshToken, scope);

            var taskFactory = new TaskFactory();

            var endpoint = string.Format(ResourceOwnerEndpoint, this.domain);
            var parameters = String.Format(
                "client_id={0}&connection={1}&username={2}&password={3}&grant_type=password&scope={4}",
                this.clientId,
                connection,
                userName,
                password,
                Uri.EscapeDataString(scope));

            byte[] postData = Encoding.UTF8.GetBytes(parameters);

            var request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
#if WINDOWS_PHONE
            request.ContentLength = postData.Length;
#endif

            using (var stream = await taskFactory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null))
            {
                await stream.WriteAsync(postData, 0, postData.Length);
                await stream.FlushAsync();
            };

            var response = await taskFactory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);

            try
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader streamReader = new StreamReader(responseStream))
                    {
                        var text = await streamReader.ReadToEndAsync();
                        var data = JObject.Parse(text).ToObject<Dictionary<string, string>>();

                        if (data.ContainsKey("error"))
                        {
                            throw new UnauthorizedAccessException("Error authenticating: " + data["error"]);
                        }
                        else if (data.ContainsKey("access_token"))
                        {
                            this.CurrentUser = new Auth0User(data);
                        }
                        else
                        {
                            throw new UnauthorizedAccessException("Expected access_token in access token response, but did not receive one.");
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return this.CurrentUser;
        }

        /// <summary>
        /// Renews the idToken (JWT)
        /// </summary>
        /// <returns>The refreshed token.</returns>
        /// <param name="refreshToken">The refresh token</param>
        /// <param name="options">Additional parameters.</param>
        public async Task<JObject> RefreshToken(
            string refreshToken = "", 
            Dictionary<string, string> options = null)
        {
            var emptyToken = string.IsNullOrEmpty(refreshToken);
            if (emptyToken && this.CurrentUser != null && string.IsNullOrEmpty(this.CurrentUser.RefreshToken))
            {
                throw new InvalidOperationException(
                    "The current user's refresh_token could not be retrieved and no refresh_token was provided as parameter");
            }

            return await this.GetDelegationToken(
                api: "app",
                refreshToken: emptyToken ? this.CurrentUser.RefreshToken : refreshToken,
                options: options);
        }

        /// <summary>
        /// Verifies if the jwt for the current user has expired.
        /// </summary>
        /// <returns>true if the token has expired, false otherwise.</returns>
        /// <remarks>Must be logged in before invoking.</remarks>
        public bool HasTokenExpired()
        {
            if (string.IsNullOrEmpty(this.CurrentUser.IdToken))
            {
                throw new InvalidOperationException("You need to either login first.");
            }

            return TokenValidator.HasExpired(this.CurrentUser.IdToken);
        }

        /// <summary>
        /// Renews the idToken (JWT)
        /// </summary>
        /// <returns>The refreshed token.</returns>
        /// <remarks>The JWT must not have expired.</remarks>
        /// <param name="options">Additional parameters.</param>
        public Task<JObject> RenewIdToken(Dictionary<string, string> options = null)
        {
            if (string.IsNullOrEmpty(this.CurrentUser.IdToken))
            {
                throw new InvalidOperationException("You need to login first.");
            }

            options = options ?? new Dictionary<string, string>();

            if (!options.ContainsKey("scope"))
            {
                options["scope"] = "passthrough";
            }

            return this.GetDelegationToken(
                api: "app", 
                idToken: this.CurrentUser.IdToken, 
                options: options);
        }

        /// <summary>
        /// Get a delegation token
        /// </summary>
        /// <returns>Delegation token result.</returns>
        /// <param name="api">The type of the API to be used.</param>
        /// <param name="idToken">The string representing the JWT. Useful only if not expired.</param>
        /// <param name="refreshToken">The refresh token.</param>
        /// <param name="targetClientId">The clientId of the target application for which to obtain a delegation token.</param>
        /// <param name="options">Additional parameters.</param>
        public async Task<JObject> GetDelegationToken(string api = "",
            string idToken = "",
            string refreshToken = "",
            string targetClientId = "",
            Dictionary<string, string> options = null)
        {
            if (!(string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(refreshToken)))
            {
                throw new InvalidOperationException(
                    "You must provide either the idToken parameter or the refreshToken parameter, not both.");
            }

            if (string.IsNullOrEmpty(idToken) && string.IsNullOrEmpty(refreshToken))
            {
                if (this.CurrentUser == null || string.IsNullOrEmpty(this.CurrentUser.IdToken)){
                    throw new InvalidOperationException(
                    "You need to login first or specify a value for idToken or refreshToken parameter.");
                }
                
                idToken = this.CurrentUser.IdToken;
            }

            options = options ?? new Dictionary<string, string>();
            options["id_token"] = idToken;
            options["api_type"] = api;
            options["refresh_token"] = refreshToken;
            options["target"] = targetClientId;

            var taskFactory = new TaskFactory();

            var endpoint = string.Format(DelegationEndpoint, this.domain);
            var parameters = String.Format(
                "grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer&client_id={0}",
                this.clientId);

            foreach (var option in options.Where(o => !string.IsNullOrEmpty(o.Value)))
            {
                parameters += string.Format("&{0}={1}", option.Key, option.Value);
            }

            byte[] postData = Encoding.UTF8.GetBytes(parameters);

            var request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
#if WINDOWS_PHONE
            request.ContentLength = postData.Length;
#endif

            using (var stream = await taskFactory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null))
            {
                await stream.WriteAsync(postData, 0, postData.Length);
                await stream.FlushAsync();
            };

            JObject delegationResult;
            var response = await taskFactory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);

            try
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (var streamReader = new StreamReader(responseStream))
                    {
                        var text = await streamReader.ReadToEndAsync();
                        delegationResult = JObject.Parse(text);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return delegationResult;
        }

        /// <summary>
        /// Log a user out of a Auth0 application.
        /// </summary>
        public virtual async Task LogoutAsync()
        {
            this.CurrentUser = null;
            await this.broker.Logout();
        }

        private async Task<Uri> GetStartUri(string connection, string scope)
        {
            // Generate state to include in startUri
            var chars = new char[16];
            var rand = new Random();
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)rand.Next((int)'a', (int)'z' + 1);
            }

            var authorizeUri = !string.IsNullOrWhiteSpace(connection) ?
                string.Format(AuthorizeUrl, domain, clientId, Uri.EscapeDataString(scope), Uri.EscapeDataString(this.CallbackUrl), connection) :
                string.Format(LoginWidgetUrl, domain, clientId, Uri.EscapeDataString(scope), Uri.EscapeDataString(this.CallbackUrl));

            if (scope.Contains("offline_access"))
            {
                var deviceId = Uri.EscapeDataString(await this.DeviceIdProvider.GetDeviceId());
                authorizeUri += string.Format("&device={0}", deviceId);
            }
            
            this.State = new string(chars);
            var startUri = new Uri(authorizeUri + "&state=" + this.State);

            return startUri;
        }
    }
}
