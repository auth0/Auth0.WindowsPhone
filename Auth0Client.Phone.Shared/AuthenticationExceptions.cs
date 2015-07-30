using System;

namespace Auth0.SDK
{
    public abstract class AuthenticationException : Exception
    {
        protected AuthenticationException()
        {
        }

        protected AuthenticationException(string message) : base(message)
        {
        }
    }

    public class AuthenticationErrorException : AuthenticationException
    {
        public AuthenticationErrorException(string message) : base(message)
        {
        }
    }

    public class AuthenticationCancelException : AuthenticationException
    {
        public AuthenticationCancelException()
        {
        }
    }
}
