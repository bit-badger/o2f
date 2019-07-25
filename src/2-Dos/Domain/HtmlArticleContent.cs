namespace Dos.Domain
{
    public class HtmlArticleContent : IArticleContent
    {
        public string ContentType => Dos.Domain.ContentType.Html;

        public string Text { get; set; } = "";

        public string Generate() => Text;
    }
}