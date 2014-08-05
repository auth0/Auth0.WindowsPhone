using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Auth0.SDK;
using Newtonsoft.Json;

namespace Auth0Client.Phone.Tests
{
    [TestClass]
    public class TokenValidatorTests
    {
        [TestMethod]
        public void ShouldReturnFalseWhenHasExpiredIsInvokedWithNotExpiredToken()
        {
            // expires some time in 2674
            var hasExpired = TokenValidator.HasExpired("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOjEyMzQ1Njc4OTAsIm5hbWUiOiJKb2huIERvZSIsImFkbWluIjp0cnVlLCJleHAiOjIyMjIyMjIyMjIyfQ.eGV-Lr-2Vv5oef6udcQCUpuWg4edXWuAmYiLRAadHJY");
            Assert.IsFalse(hasExpired);
        }

        [TestMethod]
        public void ShouldReturnTrueWhenHasExpiredIsInvokedWithExpiredToken()
        {
            var hasExpired = TokenValidator.HasExpired("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOjEyMzQ1Njc4OTAsIm5hbWUiOiJKb2huIERvZSIsImFkbWluIjp0cnVlLCJleHAiOjIyMjIyMjIyMn0.6imnNPCySe87wuD2Xj300N7guYT4qRKrBeP2suIbfE0");
            Assert.IsTrue(hasExpired);
        }

        [TestMethod]
        public void ShouldReturnFalseIfTokenHasNoExpClaim()
        {
            var hasExpired = TokenValidator.HasExpired("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOjEyMzQ1Njc4OTAsIm5hbWUiOiJKb2huIERvZSIsImFkbWluIjp0cnVlfQ.eoaDVGTClRdfxUZXiPs3f8FmJDkDE_VCQFXqKxpLsts");
            Assert.IsFalse(hasExpired);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ShouldThrowIfTokenIsEmpty()
        {
            TokenValidator.HasExpired(string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ShouldThrowIfTokenDoesNotHaveHeaderBodyAndSignature()
        {
            TokenValidator.HasExpired("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOjEyMzQ1Njc4OTAsIm5hbWUiOiJKb2huIERvZSIsImFkbWluIjp0cnVlfQ.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowIfTokenIsNull()
        {
            TokenValidator.HasExpired(null);
        }

        [TestMethod]
        [ExpectedException(typeof(JsonReaderException))]
        public void ShouldThrowIfJsonIsMalformed()
        {
            // missing } to close json
            TokenValidator.HasExpired("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOjEyMzQ1Njc4OTAsIm5hbWUiOiJKb2huIERvZSIsImFkbWluIjp0cnVl.eoaDVGTClRdfxUZXiPs3f8FmJDkDE_VCQFXqKxpLsts");
        }
    }
}
