using MarkdownSharp;

namespace Dos.Domain
{
    public class MarkdownArticleContent : IArticleContent
    {
        public string ContentType => Dos.Domain.ContentType.Markdown;

        public string Text { get; set; } = "";

        public string Generate() => new Markdown().Transform(Text);
    }
}