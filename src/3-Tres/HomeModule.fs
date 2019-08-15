namespace Tres

open FSharp.Control.Tasks.V2.ContextInsensitive
open Nancy
open Nancy.Session.Persistable
open Raven.Client.Documents
open System
open Tres.Domain

type HomeModule (store : IDocumentStore) as this =
  inherit NancyModule ()

  let seed () =
    let newId collection = sprintf "%s/%s" collection (Guid.NewGuid().ToString "N")
    let now () = DateTime.Now.Ticks
    let days nbr = (TimeSpan (nbr, 0, 0, 0)).Ticks
    let markdown text =
      let md = MarkdownArticleContent ()
      (md :> IArticleContent).Text <- text
      md
    let html text =
      let h = HtmlArticleContent ()
      (h :> IArticleContent).Text <- text
      h

    use sess = store.OpenAsyncSession ()
    
    task {
      // Web log
      let webLogId = newId Collection.WebLog
      
      do!
        sess.StoreAsync 
          { Id          = webLogId
            Name        = "Tres Blog"
            Subtitle    = Some "Nancy in F# solution"
            DefaultPage = ""
            ThemePath   = ""
            UrlBase     = "http://localhost:5000"
            TimeZone    = "America/Chicago"
            }

      // Categories
      let catNewsId   = newId Collection.Category
      let catSportsId = newId Collection.Category
      let catCatsId   = newId Collection.Category

      do!
        sess.StoreAsync
          { Id          = catNewsId
            WebLogId    = webLogId
            Name        = "News"
            Slug        = "news"
            Description = Some "Commentary on the news of the day"
            ParentId    = None
            Children    = []
            }
      do!
        sess.StoreAsync
          { Id          = catSportsId
            WebLogId    = webLogId
            Name        = "Sports"
            Slug        = "sports"
            Description = Some "Athletic punditry"
            ParentId    = None
            Children    = []
            }
      do!
        sess.StoreAsync
          { Id          = catCatsId
            WebLogId    = webLogId
            Name        = "Cute Kitties"
            Slug        = "cute-kitties"
            Description = Some "Pictures of adorable felines"
            ParentId    = None
            Children    = []
            }

      // Users / Authors
      let userId = newId Collection.User

      do!
        sess.StoreAsync
          { Id             = userId
            EmailAddress   = "me@example.com"
            PasswordHash   = "####"
            FirstName      = "Dos"
            LastName       = "Admin"
            PreferredName  = "Bob"
            Url            = Some "http://localhost:5000"
            Authorizations = [ { Level = AuthorizationLevel.Administrator; WebLogId = webLogId } ]
            }

      // Pages
      let aboutId  = newId Collection.Page
      let aboutNow = now ()

      do!
        sess.StoreAsync
          { Id             = aboutId
            WebLogId       = webLogId
            AuthorId       = userId
            Title          = "About This Blog"
            Permalink      = "/about.html"
            PublishedOn    = aboutNow
            UpdatedOn      = aboutNow
            ShowInPageList = true
            Text           = html "This blog is written in <strong>F#</strong> using Nancy"
            Revisions      = [
              { AsOf = aboutNow
                Text = html "This blog is written in <strong>F#</strong> using Nancy"
                }
              ]
            }

      let contactId  = newId Collection.Page
      let contactNow = now ()

      do!
        sess.StoreAsync
          { Id             = contactId
            WebLogId       = webLogId
            AuthorId       = userId
            Title          = "Contact Me"
            Permalink      = "/contact-me.html"
            PublishedOn    = contactNow
            UpdatedOn      = contactNow
            ShowInPageList = true
            Text           = markdown "Just **call** _(123)_ 555-1234"
            Revisions      = [ { AsOf = contactNow; Text = markdown "Just **call** _(123)_ 555-1234" } ]
            }

      // Posts
      let postNews1Id  = newId Collection.Post
      let postNews1Now = now ()

      do!
        sess.StoreAsync
          { Id          = postNews1Id
            WebLogId    = webLogId
            AuthorId    = userId
            Status      = PostStatus.Published
            Title       = "Nice People on the Street"
            Permalink   = "/2019/nice-people-on-the-street.html"
            PublishedOn = postNews1Now - days 3
            UpdatedOn   = postNews1Now - days 2
            Text        = markdown "I couldn't _believe_ it!"
            CategoryIds = [ catNewsId ]
            Tags        = [ "nice"; "street"; "unbelievable" ]
            Revisions   = [
              { AsOf = postNews1Now - days 3; Text = markdown "I coudln't _believe_ it!" }
              { AsOf = postNews1Now - days 2; Text = markdown "I couldn't _believe_ it!" }
              ]
            }

      let postNews2Id   = newId Collection.Post
      let postNews2Now  = now () - days 7
      let postNews2Text = "In a historic, never-before-seen circumstance, the presidential election ended in a tie. The Constitution does not provide for any sort of tie-breaker, so the Supreme Court has ruled that neither party has won. Surprisingly, the candidates believe this may be the best thing for the country; one of them was quoted as saying, \"Last month, one of my competitors called Congress a 'do-nothing Congress', and the next day, their approval rating had actually gone **up** 5 points. It's like the American people just want us to leave them alone, and they couldn't have sent a clearer message with today's results.\" Americans are generally optimistic about the next 4 years with no leader in the White House, though they remain apprehensive that, without the responsibility of actually governing, the candidates will just continue campaigning for another 4 years.";

      do!
        sess.StoreAsync
          { Id          = postNews2Id
            WebLogId    = webLogId
            AuthorId    = userId
            Status      = PostStatus.Published
            Title       = "Presidential Election Ends in Tie; Nation Named Winner"
            Permalink   = "/2019/presidential-election-ends-in-tie-nation-named-winner.html"
            PublishedOn = postNews2Now
            UpdatedOn   = postNews2Now
            Text        = markdown postNews2Text
            CategoryIds = [ catNewsId ]
            Tags        = [ "candidate"; "congress"; "election"; "president"; "result" ]
            Revisions   = [ { AsOf = postNews2Now; Text = markdown postNews2Text } ]
            }

      let postSportsId  = newId Collection.Post
      let postSportsNow = now () - days 10

      do!
        sess.StoreAsync
          { Id          = postSportsId
            WebLogId    = webLogId
            AuthorId    = userId
            Status      = PostStatus.Published
            Title       = "My Team Rules"
            Permalink   = "/2019/my-team-rules.html"
            PublishedOn = postSportsNow
            UpdatedOn   = postSportsNow
            Text        = html "...and your team drools!"
            CategoryIds = [ catSportsId ]
            Tags        = [ "teams" ]
            Revisions   = [ { AsOf = postSportsNow; Text = html "...and your team drools!" } ]
            }

      do! sess.SaveChangesAsync ()
      
      return "All done!" :> obj
      }
    |> Async.AwaitTask
    |> Async.RunSynchronously
    |> Threading.Tasks.Task.FromResult   
  
  do
    this.Get("/", fun _ ->
        let count =
          (this.Request.PersistableSession.Get<Nullable<int>> "Count"
           |> Option.ofNullable
           |> function Some x -> x | None -> 0) + 1
        this.Request.PersistableSession.["Count"] <- count
        (sprintf "You have visited this page %i times this session" count) :> obj)
    this.Get("/seed", fun _ -> seed ())

