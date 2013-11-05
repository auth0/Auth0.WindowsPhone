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
        /// Login a user into an Auth0 application by showing an embedded browser window either showing the widget or skipping it by passing a connection name
        /// </summary>
        /// <param name="owner">The owner window</param>
        /// <param name="connection">Optional connection name to bypass the login widget</param>
        /// <remarks>When using openid profile if the user has many attributes the token might get big and the embedded browser (Internet Explorer) won't be able to parse a large URL</remarks>
        /// </param>
        /// <returns>Returns a Task of Auth0User</returns>
        public async Task<Auth0User> LoginAsync(string connection = "", string scope = "openid")
        {
            var user = await this.broker.AuthenticateAsync(GetStartUri(connection, scope), new Uri(this.CallbackUrl));

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
<<<<<<< HEAD
        public async Task<Auth0User> LoginAsync(string connection, string userName, string password, string scope = "openid profile")
=======
        public async Task<Auth0User> LoginAsync(string connection, string userName, string password)
>>>>>>> find login with resource owner
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

<<<<<<< HEAD
            TaskFactory taskFactory = new TaskFactory();
            var response = await taskFactory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
            try
            {
=======
            using (var requestStream = await Task<Stream>.Factory.FromAsync(request.BeginGetRequestStream, request.EndGetRequestStream, request))
            {
                await requestStream.WriteAsync(postData, 0, postData.Length);
            }
            
            var response = await Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, request);
                
            try
            {
>>>>>>> find login with resource owner
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader streamReader = new StreamReader(responseStream))
                    {
<<<<<<< HEAD
                        var text = streamReader.ReadToEnd();
=======
                        var text = await streamReader.ReadToEndAsync();
>>>>>>> find login with resource owner
                        var data = JObject.Parse(text).ToObject<Dictionary<string, string>>();

                        if (data.ContainsKey("error"))
                        {
                            throw new UnauthorizedAccessException("Error authenticating: " + data["error"]);
                        }
                        else if (data.ContainsKey("access_token"))
<<<<<<< HEAD
                        {
                            this.SetupCurrentUser(data);
                        }
                        else
                        {
                            throw new UnauthorizedAccessException("Expected access_token in access token response, but did not receive one.");
                        }
=======
                        {
                            this.SetupCurrentUser(data);
                        }
                        else
                        {
                            throw new UnauthorizedAccessException("Expected access_token in access token response, but did not receive one.");
                        }
>>>>>>> find login with resource owner

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
    }
}
