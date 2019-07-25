namespace Uno.Domain
{
    public interface IArticleContent
    {
        string ContentType { get; }
        
        string Text { get; set; }
        
        string Generate();
    }
}