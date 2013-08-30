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
        private const string AuthorizeUrl = "https://{0}.auth0.com/authorize?client_id={1}&scope=openid%20profile&redirect_uri={2}&response_type=token&connection={3}";
        private const string LoginWidgetUrl = "https://{0}.auth0.com/login/?client={1}&scope=openid%20profile&redirect_uri={2}&response_type=token";
        private const string ResourceOwnerEndpoint = "https://{0}.auth0.com/oauth/ro";
        private const string DefaultCallback = "https://{0}.auth0.com/mobile";

        private readonly string subDomain;
        private readonly string clientId;
        private readonly string clientSecret;

        public Auth0Client(string subDomain, string clientId, string clientSecret)
        {
            this.subDomain = subDomain;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
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
        ///  Log a user into an Auth0 application given an user name and password.
        /// </summary>
        /// <returns>Task that will complete when the user has finished authentication.</returns>
        /// <param name="connection" type="string">The name of the connection to use in Auth0. Connection defines an Identity Provider.</param>
        /// <param name="userName" type="string">User name.</param>
        /// <param name="password type="string"">User password.</param>
        public Task<Auth0User> LoginAsync(string connection, string userName, string password)
        {
            var endpoint = string.Format(ResourceOwnerEndpoint, this.subDomain);
            var parameters = String.Format(
                "client_id={0}&client_secret={1}&connection={2}&username={3}&password={4}&grant_type=password&scope=openid%20profile",
                this.clientId,
                this.clientSecret,
                connection,
                userName,
                password);

            byte[] postData = Encoding.UTF8.GetBytes(parameters);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postData.Length;

            TaskFactory taskFactory = new TaskFactory();
            return taskFactory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null).ContinueWith(t =>
            {
                try
                {
                    using (Stream responseStream = t.Result.GetResponseStream())
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
            });
        }

        /// <summary>
        /// Log a user out of a Auth0 application.
        /// </summary>
        public void Logout()
        {
            this.CurrentUser = null;
        }

        private void SetupCurrentUser(IDictionary<string, string> accountProperties)
        {
            this.CurrentUser = new Auth0User(accountProperties);
        }

        private LoginBrowser GetAuthenticator(string connection)
        {
            // Generate state to include in startUri
            var chars = new char[16];
            var rand = new Random();
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)rand.Next((int)'a', (int)'z' + 1);
            }

            var redirectUri = this.CallbackUrl;
            var authorizeUri = !string.IsNullOrWhiteSpace(connection) ?
                string.Format(AuthorizeUrl, subDomain, clientId, Uri.EscapeDataString(redirectUri), connection) :
                string.Format(LoginWidgetUrl, subDomain, clientId, Uri.EscapeDataString(redirectUri));

            var state = new string(chars);
            var startUri = new Uri(authorizeUri + "&state=" + state);
            var endUri = new Uri(redirectUri);

            return new LoginBrowser(startUri, endUri);
        }

        private static Dictionary<string, string> parseResult(string result)
        {
            Dictionary<string, string> tokens = new Dictionary<string, string>();

            //result will be: https://callback#id_token=1234&access_token=12345&...
            var strTokens = result.Split('#')[1].Split('&');

            foreach (var t in strTokens)
            {
                var tok = t.Split('=');
                tokens.Add(tok[0], tok[1]);
            }

            return tokens;
        }
    }
}
