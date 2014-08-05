using Newtonsoft.Json.Linq;
using System.Collections.Generic;

using Auth0.SDK.Utils;

namespace Auth0.SDK
{
    public class Auth0User
    {
        public Auth0User()
        {
        }

        public Auth0User(IDictionary<string, string> accountProperties)
        {
            this.Auth0AccessToken = accountProperties.ValueOrDefault("access_token", string.Empty);
            this.IdToken = accountProperties.ValueOrDefault("id_token", string.Empty);
            this.RefreshToken = accountProperties.ValueOrDefault("refresh_token", string.Empty);
            var profile = accountProperties.ValueOrDefault("profile", null);
            this.Profile = profile != null ? profile.ToJson() : null;

            this.State = accountProperties.ValueOrDefault("state", null);
        }

        public string Auth0AccessToken { get; set; }

        public string IdToken { get; set; }

        public string RefreshToken { get; set; }

        public JObject Profile { get; set; }

        public string State { get; set; }
    }

    internal static class Extensions
    {
        internal static JObject ToJson(this string jsonString)
        {
            return JObject.Parse(jsonString);
        }
    }
}
