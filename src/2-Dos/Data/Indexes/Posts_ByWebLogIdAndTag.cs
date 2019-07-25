using Raven.Client.Documents.Indexes;
using System.Linq;
using Dos.Domain;

namespace Dos.Data.Indexes
{
    public class Posts_ByWebLogIdAndTag : AbstractIndexCreationTask<Post>
    {
        public Posts_ByWebLogIdAndTag()
        {
            Map = posts => from post in posts
                           from tag in post.Tags
                           select new
                           {
                               post.Id,
                               Tag = tag
                           };
        }
    }
}