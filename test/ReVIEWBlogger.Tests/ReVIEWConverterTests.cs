using NUnit.Framework;
using ReVIEWBlogger;
using System.IO;

namespace ConsoleApplication
{
    [TestFixture]
    public class ReVIEWConverterTests
    {
        [Test]
        public void TestCompilingDocument()
        {
            var c = new ReVIEWConverter();
            var actualOutput = c.CompileDocument("1606_foo.re");
            // File.WriteAllText("foo.re.actual.xml", actualOutput);
            Assert.AreEqual(File.ReadAllText("foo.expected.xml"), actualOutput);
        }

        [Test]
        public void TestDecoratingDocument()
        {
            var src = File.ReadAllText("foo.expected.xml");
            var c = new ReVIEWConverter();
            c.LoadPrecompiledDocument(src);
            var actual = c.DecorateForBlogger("http://www.muo.jp/", "http://www.muo.jp/foo/bar", "idprefix").ToString();
            // File.WriteAllText("foo.blogger.actual.txt", actual);
            Assert.AreEqual(File.ReadAllText("foo.blogger.expected.txt"), actual);
        }

        [Test]
        public void TestExtractingTitleAndContent()
        {
            var src = File.ReadAllText("foo.expected.xml");
            var c = new ReVIEWConverter();
            c.LoadPrecompiledDocument(src);
            var doc = c.DecorateForBlogger("http://www.muo.jp/", "http://www.muo.jp/foo/bar", "idprefix");
            var r = c.ExtractTitleAndContent();
            // File.WriteAllText("foo.blogger.entry.title.actual.txt", r.Item1);
            // File.WriteAllText("foo.blogger.entry.content.actual.txt", r.Item2);
            Assert.AreEqual(File.ReadAllText("foo.blogger.entry.title.expected.txt"), r.Item1);
            Assert.AreEqual(File.ReadAllText("foo.blogger.entry.content.expected.txt"), r.Item2);
        }

        [Test]
        public void TestCompilingDocumentWithImages()
        {
            var c = new ReVIEWConverter();
            var actualOutput = c.CompileDocument("1608_bar.re");
            // File.WriteAllText("bar.re.actual.xml", actualOutput);
            Assert.AreEqual(File.ReadAllText("bar.expected.xml"), actualOutput);
        }
    }
}
