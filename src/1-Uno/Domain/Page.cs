using System.Collections.Generic;

namespace Uno.Domain
{
    public class Page
    {
        public string Id { get; set; }

        public string WebLogId { get; set; }

        public string AuthorId { get; set; }

        public string Title { get; set; }

        public string Permalink { get; set; }

        public long PublishedOn { get; set; }

        public long UpdatedOn { get; set; }

        public bool ShowInPageList { get; set; }

        public IArticleContent Text { get; set; }

        public ICollection<Revision> Revisions { get; set; } = new List<Revision>(); 
    }
}