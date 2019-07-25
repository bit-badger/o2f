namespace Uno.Domain
{
    public class Revision
    {
        public long AsOf { get; set; }

        public IArticleContent Text { get; set; }
    }
}