using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Uno.Domain;

namespace Uno.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() {
            HttpContext.Session.SetInt32("Count", (HttpContext.Session.GetInt32("Count") ?? 0) + 1);
            return Content($"You have viewed this index {HttpContext.Session.GetInt32("Count")} times this session");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            Content(string.Format("Error {0}, Request ID {1}", HttpContext.Response.StatusCode,
                Activity.Current?.Id ?? HttpContext.TraceIdentifier));

        public async Task<IActionResult> Seed()
        {
            string newId() => Guid.NewGuid().ToString("N");
            long now() => DateTime.Now.Ticks;
            long days(int nbr) => new TimeSpan(nbr, 0, 0, 0).Ticks;

            using (var sess =
                (HttpContext.RequestServices.GetService(typeof(IDocumentStore)) as IDocumentStore)
                    .OpenAsyncSession())
            {
                // Web log
                var webLogId = newId();
                
                await sess.StoreAsync(new WebLog
                {
                    Id = webLogId,
                    Name = "Uno Blog",
                    Subtitle = "ASP.NET Core MVC solution",
                    DefaultPage = "",
                    ThemePath = "",
                    UrlBase = "http://localhost:5000",
                    TimeZone = "America/Chicago"
                });

                // Categories
                var catNewsId = newId();
                var catSportsId = newId();
                var catCatsId = newId();

                await sess.StoreAsync(new Category
                {
                    Id = catNewsId,
                    WebLogId = webLogId,
                    Name = "News",
                    Slug = "news",
                    Description = "Commentary on the news of the day",
                    ParentId = null
                });
                await sess.StoreAsync(new Category
                {
                    Id = catSportsId,
                    WebLogId = webLogId,
                    Name = "Sports",
                    Slug = "sports",
                    Description = "Athletic punditry",
                    ParentId = null
                });
                await sess.StoreAsync(new Category
                {
                    Id = catCatsId,
                    WebLogId = webLogId,
                    Name = "Cute Kitties",
                    Slug = "cute-kitties",
                    Description = "Pictures of adorable felines",
                    ParentId = null
                });

                // Users / Authors
                var userId = newId();

                await sess.StoreAsync(new User
                {
                    Id = userId,
                    EmailAddress = "me@example.com",
                    PasswordHash = "####",
                    FirstName = "Uno",
                    LastName = "Admin",
                    PreferredName = "Alice",
                    Url = "http://localhost:5000",
                    Authorizations = new[]
                    {
                        new Authorization { Level = AuthorizationLevel.Administrator, WebLogId = webLogId }
                    }
                });

                // Pages
                var aboutId = newId();
                var aboutNow = now();

                await sess.StoreAsync(new Page
                {
                    Id = aboutId,
                    WebLogId = webLogId,
                    AuthorId = userId,
                    Title = "About This Blog",
                    Permalink = "/about.html",
                    PublishedOn = aboutNow,
                    UpdatedOn = aboutNow,
                    ShowInPageList = true,
                    Text = new HtmlArticleContent
                    {
                        Text = "This blog is written in <strong>C#</strong> using ASP.NET Core MVC"
                    },
                    Revisions = new[]
                    {
                        new Revision
                        {
                            AsOf = aboutNow,
                            Text = new HtmlArticleContent
                            {
                                Text = "This blog is written in <strong>C#</strong> using ASP.NET Core MVC"
                            }
                        }
                    }
                });

                var contactId = newId();
                var contactNow = now();

                await sess.StoreAsync(new Page
                {
                    Id = contactId,
                    WebLogId = webLogId,
                    AuthorId = userId,
                    Title = "Contact Me",
                    Permalink = "/contact-me.html",
                    PublishedOn = contactNow,
                    UpdatedOn = contactNow,
                    ShowInPageList = true,
                    Text = new MarkdownArticleContent { Text = "Just **call** _(123)_ 555-1234" },
                    Revisions = new[]
                    {
                        new Revision
                        {
                            AsOf = contactNow,
                            Text = new MarkdownArticleContent { Text = "Just **call** _(123)_ 555-1234" }
                        }
                    }
                });

                // Posts
                var postNews1Id = newId();
                var postNews1Now = now();

                await sess.StoreAsync(new Post
                {
                    Id = postNews1Id,
                    WebLogId = webLogId,
                    AuthorId = userId,
                    Status = PostStatus.Published,
                    Title = "Nice People on the Street",
                    Permalink = "/2019/nice-people-on-the-street.html",
                    PostedOn = postNews1Now - days(3),
                    UpdatedOn = postNews1Now - days(2),
                    Text = new MarkdownArticleContent { Text = "I couldn't _believe_ it!" },
                    CategoryIds = new[] { catNewsId },
                    Tags = new[] { "nice", "street", "unbelievable" },
                    Revisions = new[]
                    {
                        new Revision
                        {
                            AsOf = postNews1Now - days(3),
                            Text = new MarkdownArticleContent { Text = "I coudln't _believe_ it!" }
                        },
                        new Revision
                        {
                            AsOf = postNews1Now - days(2),
                            Text = new MarkdownArticleContent { Text = "I couldn't _believe_ it!" }
                        }
                    }
                });

                var postNews2Id = newId();
                var postNews2Now = now() - days(7);
                var postNews2Text = "In a historic, never-before-seen circumstance, the presidential election ended in a tie. The Constitution does not provide for any sort of tie-breaker, so the Supreme Court has ruled that neither party has won. Surprisingly, the candidates believe this may be the best thing for the country; one of them was quoted as saying, \"Last month, one of my competitors called Congress a 'do-nothing Congress', and the next day, their approval rating had actually gone **up** 5 points. It's like the American people just want us to leave them alone, and they couldn't have sent a clearer message with today's results.\" Americans are generally optimistic about the next 4 years with no leader in the White House, though they remain apprehensive that, without the responsibility of actually governing, the candidates will just continue campaigning for another 4 years.";

                await sess.StoreAsync(new Post
                {
                    Id = postNews2Id,
                    WebLogId = webLogId,
                    AuthorId = userId,
                    Status = PostStatus.Published,
                    Title = "Presidential Election Ends in Tie; Nation Named Winner",
                    Permalink = "/2019/presidential-election-ends-in-tie-nation-named-winner.html",
                    PostedOn = postNews2Now,
                    UpdatedOn = postNews2Now,
                    Text = new MarkdownArticleContent { Text = postNews2Text },
                    CategoryIds = new[] { catNewsId },
                    Tags = new[] { "candidate", "congress", "election", "president", "result" },
                    Revisions = new[]
                    {
                        new Revision
                        {
                            AsOf = postNews2Now,
                            Text = new MarkdownArticleContent { Text = postNews2Text }
                        }
                    }
                });

                var postSportsId = newId();
                var postSportsNow = now() - days(10);

                await sess.StoreAsync(new Post
                {
                    Id = postSportsId,
                    WebLogId = webLogId,
                    AuthorId = userId,
                    Status = PostStatus.Published,
                    Title = "My Team Rules",
                    Permalink = "/2019/my-team-rules.html",
                    PostedOn = postSportsNow,
                    UpdatedOn = postSportsNow,
                    Text = new HtmlArticleContent { Text = "...and your team drools!" },
                    CategoryIds = new[] { catSportsId },
                    Tags = new[] { "teams" },
                    Revisions = new[] {
                        new Revision
                        {
                            AsOf = postSportsNow,
                            Text = new HtmlArticleContent { Text = "...and your team drools!" }
                        }
                    }
                });

                await sess.SaveChangesAsync();
            }

            return Content("All done!");
        }
    }
}