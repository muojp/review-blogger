using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;
using CenterCLR.Sgml;
using System.Linq;

namespace ReVIEWBlogger
{
    public class ReVIEWConverter
    {
        const int DEFAULT_TIMEOUT_MS = 30000;
        XDocument innerDocument;

        public string CompileDocument(string filename, int timeoutMilliSec = DEFAULT_TIMEOUT_MS)
        {
            var absDirectory = Path.GetDirectoryName(Path.GetFullPath(filename));
            var psi = new ProcessStartInfo
            {
                WorkingDirectory = absDirectory, // care for cases currdir != targetdir
                FileName = "review-compile",
                Arguments = $"--target=html {filename}",
                RedirectStandardOutput = true
            };
            var p = Process.Start(psi);
            var content = p.StandardOutput.ReadToEnd();
            // TODO: also handle StandardError
            p.WaitForExit(timeoutMilliSec);

            using (var st = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                this.innerDocument = SgmlReader.Parse(st);
            }

            return content;
        }

        public void LoadPrecompiledDocument(string src)
        {
            using (var st = new MemoryStream(Encoding.UTF8.GetBytes(src)))
            {
                this.innerDocument = SgmlReader.Parse(st);
            }

        }

        public XDocument DecorateForBlogger(string siteUri, string articleUri, string idPrefix)
        {
            var doc = this.innerDocument;
            var ns = doc.Root.Name.Namespace;

            foreach (var el in doc.Descendants(ns + "a"))
            {
                // unite all links as absolute links
                if (el.Attribute("href") != null)
                {
                    var href = el.Attribute("href").Value;
                    if (href.StartsWith("#"))
                    {
                        el.Attribute("href").Value = articleUri + "#" + href.Substring(1);
                    }
                    else
                    {
                        if (href.StartsWith("/"))
                        {
                            el.Attribute("href").Value = siteUri + href.Substring(1);
                        }
                    }
                }

                // make footnote ids unique
                if (el.Attribute("class") != null)
                {
                    var className = el.Attribute("class").Value;
                    if (className == "noteref" || className == "footnote")
                    {
                        if (el.Attribute("id") == null)
                        {
                            throw new InvalidOperationException("footnotes/note references without `id` attribute should never appear.");
                        }
                        el.Attribute("id").Value = idPrefix + el.Attribute("id").Value;
                    }
                }
            }

            foreach (var el in doc.Descendants(ns + "div").Where(
                o => o.Attribute("class") != null ? o.Attribute("class").Value == "footnote" : false))
            {
                el.Attribute("id").Value = idPrefix + el.Attribute("id").Value;
            }
            return doc;
        }

        public Tuple<string, string> ExtractTitleAndContent()
        {
            // create a copy of XDocument to prevent original one from disruptive changes.
            var doc = new XDocument(this.innerDocument);

            var ns = doc.Root.Name.Namespace;
            var h1 = doc.Descendants(ns + "h1").First();
            var title = h1.Value;
            h1.Remove();
            // TODO: find out somehow better way to perform `InnerXml` thingy w/o placing tons of `xmlns="..."` attributes.
            var bodyContent = doc.Descendants(ns + "body").First().ToString();
            // <body xmlns="http://www.w3.org/1999/xhtml">\n\n <- 45 chars
            // .... <- content
            // </body> <- 7 chars
            var content = bodyContent.Substring(45, bodyContent.Length - 45 - 7);
            return new Tuple<string, string>(title, content);
        }
    }
}