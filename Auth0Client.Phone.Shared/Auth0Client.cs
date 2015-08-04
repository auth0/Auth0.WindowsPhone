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
        private const string DefaultScope = "openid";

        private readonly string domain;
        private readonly string clientId;
        
        private readonly AuthenticationBroker broker;

        public Auth0Client(string domain, string clientId)
        {
            this.domain = domain;
            this.clientId = clientId;
            this.broker = new AuthenticationBroker();
        }

        public Auth0User CurrentUser { get; private set; }

        public string CallbackUrl
        {
            get
            {
                return string.Format(DefaultCallback, this.domain);
            }
        }

        internal string State { get; set; }

        /// <summary>
        /// Login a user into an Auth0 application. Attempts to do a background login, but if unsuccessful shows an embedded browser window either showing the widget or skipping it by passing a connection name
        /// </summary>
        /// <param name="connection">Optional connection name to bypass the login widget</param>
        /// <param name="scope">Optional scope, either 'openid' or 'openid profile'</param>
        /// <returns>Returns a Task of Auth0User</returns>
        public async Task<Auth0User> LoginAsync(string connection = "", string scope = DefaultScope, IDictionary<string, string> options = null)
        {
            options = options ?? new Dictionary<string, string>();
            var user = await this.broker.AuthenticateAsync(GetStartUri(connection, scope, options), new Uri(this.CallbackUrl));


            if (!string.IsNullOrEmpty(scope) && scope != DefaultScope)
            {
                user.Profile = this.GetUserProfileFromIdToken(user.IdToken);
            }
            else
            {
                user.Profile = await this.GetUserProfileFromUserInfo(user.Auth0AccessToken);
            }

            this.CurrentUser = user;

            return this.CurrentUser;
        }

        /// <summary>
        ///  Log a user into an Auth0 application given an user name and password.
        /// </summary>
        /// <returns>Task that will complete when the user has finished authentication.</returns>
        /// <param name="connection" type="string">The name of the connection to use in Auth0. Connection defines an Identity Provider.</param>
        /// <param name="userName" type="string">User name.</param>
        /// <param name="password type="string"">User password.</param>
        /// <param name="scope">Scope.</param>
        public async Task<Auth0User> LoginAsync(string connection, string userName, string password, string scope = DefaultScope)
        {
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

                            if (!string.IsNullOrEmpty(scope) && scope != DefaultScope)
                            {
                                this.CurrentUser.Profile = this.GetUserProfileFromIdToken(this.CurrentUser.IdToken);
                            }
                            else
                            {
                                this.CurrentUser.Profile = await this.GetUserProfileFromUserInfo(this.CurrentUser.Auth0AccessToken);
                            }
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

        private JObject GetUserProfileFromIdToken(string idToken)
        {
            var temp = Utils.Base64UrlDecode(idToken.Split('.')[1]);
            return JObject.Parse(Encoding.UTF8.GetString(temp, 0, temp.Length));
        }

        /// <summary>
        /// Get a delegation token
        /// </summary>
        /// <returns>Delegation token result.</returns>
        /// <param name="targetClientId">Target client ID.</param>
        /// <param name="options">Custom parameters.</param>
        public async Task<JObject> GetDelegationToken(string targetClientId, IDictionary<string, string> options = null)
        {
            var id_token = string.Empty;
            options = options ?? new Dictionary<string, string>();

            // ensure id_token
            if (options.ContainsKey("id_token"))
            {
                id_token = options["id_token"];
                options.Remove("id_token");
            }
            else
            {
                id_token = this.CurrentUser.IdToken;
            }

            if (string.IsNullOrEmpty(id_token))
            {
                throw new InvalidOperationException(
                        "You need to login first or specify a value for id_token parameter.");
            }

            var taskFactory = new TaskFactory();

            var endpoint = string.Format(DelegationEndpoint, this.domain);
            var parameters = String.Format(
                "grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer&id_token={0}&target={1}&client_id={2}",
                id_token,
                targetClientId,
                this.clientId);

            foreach (var option in options)
            {
                if (!string.IsNullOrEmpty(option.Value))
                {
                    parameters += string.Format("&{0}={1}", option.Key, option.Value);
                }
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
            await this.broker.LogoutAsync();
        }

        private async Task<JObject> GetUserProfileFromUserInfo(string accessToken)
        {
            var endpoint = string.Format(UserInfoEndpoint, this.domain, accessToken);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = "GET";

            TaskFactory taskFactory = new TaskFactory();
            var response = await taskFactory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
            using (Stream responseStream = response.GetResponseStream())
            {
                using (StreamReader streamReader = new StreamReader(responseStream))
                {
                    var text = streamReader.ReadToEnd();
                    return JObject.Parse(text);
                }
            }
        }

        private Uri GetStartUri(string connection, string scope, IDictionary<string, string> options)
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

            authorizeUri += string.Format("&state={0}", this.State);

            authorizeUri += string.Join("", options.Select(kvp => string.Format("&{0}={1}", kvp.Key, kvp.Value)));

            this.State = new string(chars);
            var startUri = new Uri(authorizeUri);

            return startUri;
        }
    }
}
