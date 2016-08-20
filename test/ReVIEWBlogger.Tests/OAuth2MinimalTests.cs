using NUnit.Framework;
using ReVIEWBlogger;

namespace ConsoleApplication
{
    [TestFixture]
    public class OAuth2MinimalTests
    {
        [Test]
        public void TestGeneratingOobAuthUri()
        {
            const string TEST_BLOG_ID = "1234567890987654321";
            const string TEST_CLIENT_ID = "asdfghjkljhgfdsa";
            const string EXPECTED_AUTH_URI = "https://accounts.google.com/o/oauth2/auth?client_id=asdfghjkljhgfdsa&response_type=code&redirect_uri=urn:ietf:wg:oauth:2.0:oob&scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fblogger";

            var b = new BloggerGlue();
            var c = new BloggerGlue.ConfigFile{ BlogId = TEST_BLOG_ID, ClientId = TEST_CLIENT_ID, };
            Assert.AreEqual(EXPECTED_AUTH_URI, b.GetAuthUri(c));
        }
    }
}
