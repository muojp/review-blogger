using ReVIEWBlogger;

namespace ReVIEWBloggerTool
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new BloggerGlue().Execute(args).Wait();
        }
    }
}
