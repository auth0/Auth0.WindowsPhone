using System;

namespace Auth0.SDK
{
    public class AuthenticationErrorException : Exception
    {
        public AuthenticationErrorException(string message) : base(message)
        {
        }
    }
}
