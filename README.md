## Usage

1. Install NuGet

  ~~~ps
  Install-Package Auth0.WindowsPhone
  ~~~

2. Instantiate Auth0Client

  ~~~cs
  var auth0 = new Auth0Client(
     "{YOUR_AUTH0_DOMAIN}",  // e.g. contoso.auth0.com
     "{YOUR_CLIENT_ID}");    // it's in Auth0 app settings
  ~~~

3. Trigger login (with Widget) 

  ~~~cs
  var user = await auth0.LoginAsync();
  /* Use user to do wonderful things, e.g.: 
    - get user email => user.Profile["email"].ToString()
    - get facebook/google/twitter/etc access token => user.Profile["identities"][0]["access_token"]
    - get Windows Azure AD groups => user.Profile["groups"]
    - etc.
  */
  ~~~

  ![](http://puu.sh/4nZ1J.png)

Or you can use the connection as a parameter (e.g. here we login with a Windows Azure AD account):

~~~cs
var user = await auth0.LoginAsync("auth0waadtests.onmicrosoft.com");
~~~

Or a database connection:

~~~cs
var user = await auth0.LoginAsync("my-db-connection", "username", "password");
~~~

> Note: if the user pressed the back button `LoginAsync` throws a `AuthenticationCancelException`. If consent was not given (on social providers) or some other error happened it will throw a `AuthenticationErrorException`.

### Delegation Token Request

You can obtain a delegation token specifying the ID of the target client (`targetClientId`) and, optionally, an `IDictionary<string, string>` object (`options`) in order to include custom parameters like scope or id_token:

~~~cs
var options = new Dictionary<string, string>
{
    { "scope", "openid profile" },		// default: openid
};

var result = await auth0.GetDelegationToken(
  targetClientId: "{TARGET_CLIENT_ID}", // defaults to: ""
  idToken: "{USER_ID_TOKEN}", // defaults to: id_token of the authenticated user (auth0 CurrentUser.IdToken)
  options: options);

// id_token available throug result["id_token"]
~~~

### Renew id_token if not expired

If the id_token of the logged in user has not expired (["exp" claim](http://self-issued.info/docs/draft-ietf-oauth-json-web-token.html#expDef)) you can renew it by calling:

~~~cs
var options = new Dictionary<string, string>
{
    { "scope", "openid profile" }, // default: passthrough i.e. same as previous time token was asked for
};

auth0.RenewIdToken(options: options);
~~~

### Checking if the id_token has expired

You can check if the `id_token` for the current user has expired using the following code:

~~~cs
bool expired = auth0.HasTokenExpired();
~~~

If you want to check if a different `id_token` has expired you can use this snippet:
~~~cs
string idToken = // get if from somewhere...
bool expired = TokenValidator.HasTokenExpired(idToken);
~~~

### Refresh id_token using refresh_token

You can obtain a `refresh_token` which **never expires** (unless explicitly revoked) and use it to renew the `id_token`. 

To do that you need to first explicitly request it when logging in:
~~~cs
var user = await auth0.LoginAsync(withRefreshToken: true);
var refreshToken = user.RefreshToken;
~~~

You should store that token in a safe place. The next time, instead of asking the user to log in you will be able to use the following code to get the `id_token`:
~~~cs
var refreshToken = // retrieve from safe place
var result = await auth0.RefreshToken(refreshToken);
// access to result["id_token"];
~~~

---

## What is Auth0?

Auth0 helps you to:

* Add authentication with [multiple authentication sources](https://docs.auth0.com/identityproviders), either social like **Google, Facebook, Microsoft Account, LinkedIn, GitHub, Twitter**, or enterprise identity systems like **Windows Azure AD, Google Apps, AD, ADFS or any SAML Identity Provider**. 
* Add authentication through more traditional **[username/password databases](https://docs.auth0.com/mysql-connection-tutorial)**.
* Add support for **[linking different user accounts](https://docs.auth0.com/link-accounts)** with the same user.
* Support for generating signed [Json Web Tokens](https://docs.auth0.com/jwt) to call your APIs and **flow the user identity** securely.
* Analytics of how, when and where users are logging in.
* Pull data from other sources and add it to the user profile, through [JavaScript rules](https://docs.auth0.com/rules).

## Create a free account in Auth0

1. Go to [Auth0](http://developers.auth0.com) and click Sign Up.
2. Use Google, GitHub or Microsoft Account to login.
