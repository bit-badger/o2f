using MarkdownSharp;

namespace Uno.Domain
{
    public class MarkdownArticleContent : IArticleContent
    {
        public string ContentType => Uno.Domain.ContentType.Markdown;

        public string Text { get; set; } = "";

        public string Generate() => new Markdown().Transform(Text);
    }
}