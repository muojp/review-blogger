using NUnit.Framework;
using ReVIEWBlogger;

namespace ConsoleApplication
{
    [TestFixture]
    public class ReVIEWBloggerControlFlow
    {
        [Test]
        public void TestOverallControlFlow()
        {
            // Assert.DoesNotThrowAsync(async () => await new BloggerGlue().Execute(new[] { "1606_foo.re" }));
        }

        [Test]
        public void TestFilenameHandling()
        {
            var g = new BloggerGlue();
            Assert.AreEqual("2016/06/foobar", g.FilenameToDocId("1606_foobar.re"));
            Assert.AreEqual("foobar", g.ExtractIdFromFileName("1606_foobar.re"));
        }
    }
}
