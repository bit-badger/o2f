namespace Uno.Domain
{
    public class HtmlArticleContent : IArticleContent
    {
        public string ContentType => Uno.Domain.ContentType.Html;

        public string Text { get; set; } = "";

        public string Generate() => Text;
    }
}