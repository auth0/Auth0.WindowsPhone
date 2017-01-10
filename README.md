## Usage

1. Install NuGet

  ~~~ps
  Install-Package Auth0.WindowsPhone
  ~~~

2. Instantiate Auth0Client and save into static property in App class

  ~~~cs
  Auth0 = new Auth0Client(
     "{YOUR_AUTH0_DOMAIN}",  // e.g. contoso.auth0.com
     "{YOUR_CLIENT_ID}");    // it's in Auth0 app settings
  ~~~

3. Trigger login (with Widget) 

  ~~~cs
  try
  {
      var user = await App.Auth0.LoginAsync();
      /* Use user.Profile to do wonderful things, e.g.: 
      - get user email => user.Profile["email"].ToString()
      - get facebook/google/twitter/etc access token => user.Profile["identities"][0]["access_token"]
      - get Windows Azure AD groups => user.Profile["groups"]
      - etc.
      */
  }
  catch (AuthenticationCancelException)
  {
      // Handle case when user canceled authentication
  }
  catch (AuthenticationErrorException)
  {
      // Handle case when some error happen while authentication
  }
  catch (AuthenticationException)
  {
      // Handle all Auth0 Authentication error cases
  }
  ~~~

  ![](http://puu.sh/4nZ1J.png)

Or you can use the connection as a parameter (e.g. here we login with a Windows Azure AD account):

~~~cs
var user = await App.Auth0.LoginAsync("auth0waadtests.onmicrosoft.com");
~~~

Or a database connection:

~~~cs
var user = await App.Auth0.LoginAsync("my-db-connection", "username", "password");
~~~

> Note: if the user pressed the back button `LoginAsync` throws a `AuthenticationCancelException`. If consent was not given (on social providers) or some other error happened it will throw a `AuthenticationErrorException`.

### Delegation Token Request

You can obtain a delegation token specifying the ID of the target client (`targetClientId`) and, optionally, an `IDictionary<string, string>` object (`options`) in order to include custom parameters like scope or id_token:

~~~cs
var targetClientId = "{TARGET_CLIENT_ID}";
var options = new Dictionary<string, string>
{
    { "scope", "openid name email" },		// default: openid // Details: https://auth0.com/docs/scopes
    { "id_token", "USER_ID_TOKEN" }		// default: id_token of the authenticated user (auth0.CurrentUser.IdToken)
};

auth0.GetDelegationToken(targetClientId, options)
     .ContinueWith(t =>
        {
            // Call your API using t.Result["id_token"]
        });
~~~

### Refresh id_token using refresh_token

You can obtain a `refresh_token` which **never expires** (unless explicitly revoked) and use it to renew the `id_token`. 

To do that you need to first explicitly request it when logging in:

~~~cs
var user = await auth0.LoginAsync(withRefreshToken: true);

// you can access the refresh token this way
var refreshToken = user.RefreshToken;
~~~

You should store that token in a safe place. The next time, instead of asking the user to log in you will be 
able to use the following code to get a valid `id_token`:
~~~cs

var idToken = auth0.CurrentUser.IdToken;
if (TokenValidator.HasExpired(idToken))
{
    // refresh it
    var result = await auth0.RefreshToken();
    idToken = (string)result["id_token"];
    auth0.CurrentUser.IdToken = idToken;
}

~~~

---

---

## What is Auth0?

Auth0 helps you to:

* Add authentication with [multiple authentication sources](https://docs.auth0.com/identityproviders), either social like **Google, Facebook, Microsoft Account, LinkedIn, GitHub, Twitter, Box, Salesforce, amont others**, or enterprise identity systems like **Windows Azure AD, Google Apps, Active Directory, ADFS or any SAML Identity Provider**.
* Add authentication through more traditional **[username/password databases](https://docs.auth0.com/mysql-connection-tutorial)**.
* Add support for **[linking different user accounts](https://docs.auth0.com/link-accounts)** with the same user.
* Support for generating signed [Json Web Tokens](https://docs.auth0.com/jwt) to call your APIs and **flow the user identity** securely.
* Analytics of how, when and where users are logging in.
* Pull data from other sources and add it to the user profile, through [JavaScript rules](https://docs.auth0.com/rules).

## Create a free Auth0 Account

1. Go to [Auth0](https://auth0.com) and click Sign Up.
2. Use Google, GitHub or Microsoft Account to login.

## Issue Reporting

If you have found a bug or if you have a feature request, please report them at this repository issues section. Please do not report security vulnerabilities on the public GitHub issue tracker. The [Responsible Disclosure Program](https://auth0.com/whitehat) details the procedure for disclosing security issues.

## Author

[Auth0](auth0.com)

## License

This project is licensed under the MIT license. See the [LICENSE](LICENSE) file for more info.
