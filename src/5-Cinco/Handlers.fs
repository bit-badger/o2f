module Cinco.Handlers

open Domain
open Freya.Machines.Http
open Freya.Routers.Uri.Template.Builder
open Freya.Types.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Raven.Client.Documents
open Reader
open System

let private doSeed (store : IDocumentStore) =
  let now () = Ticks DateTime.Now.Ticks
  let subDays nbr ts =
    let (Ticks x) = ts
    x - (TimeSpan (nbr, 0, 0, 0)).Ticks |> Ticks
  task {
    use sess = store.OpenAsyncSession ()
    // Web log
    let webLogId = WebLogId.create ()
    
    do!
      sess.StoreAsync 
        { Id          = WebLogId.toString webLogId
          Name        = "Cinco Blog"
          Subtitle    = Some "Freya F# solution"
          DefaultPage = ""
          ThemePath   = ""
          UrlBase     = "https://localhost:5001"
          TimeZone    = Domain.TimeZone "America/Chicago"
          }

    // Categories
    let catNewsId   = CategoryId.create ()
    let catSportsId = CategoryId.create ()
    let catCatsId   = CategoryId.create ()

    do!
      sess.StoreAsync
        { Id          = CategoryId.toString catNewsId
          WebLogId    = webLogId
          Name        = "News"
          Slug        = "news"
          Description = Some "Commentary on the news of the day"
          ParentId    = None
          Children    = []
          }
    do!
      sess.StoreAsync
        { Id          = CategoryId.toString catSportsId
          WebLogId    = webLogId
          Name        = "Sports"
          Slug        = "sports"
          Description = Some "Athletic punditry"
          ParentId    = None
          Children    = []
          }
    do!
      sess.StoreAsync
        { Id          = CategoryId.toString catCatsId
          WebLogId    = webLogId
          Name        = "Cute Kitties"
          Slug        = "cute-kitties"
          Description = Some "Pictures of adorable felines"
          ParentId    = None
          Children    = []
          }

    // Users / Authors
    let userId = UserId.create ()

    do!
      sess.StoreAsync
        { Id             = UserId.toString userId
          EmailAddress   = "me@example.com"
          PasswordHash   = "####"
          FirstName      = "Cinco"
          LastName       = "Admin"
          PreferredName  = "Eve"
          Url            = (Url >> Some) "http://localhost:5000"
          Authorizations = [ { Level = Administrator; WebLogId = webLogId } ]
          }

    // Pages
    let aboutId  = PageId.create ()
    let aboutNow = now ()

    do!
      sess.StoreAsync
        { Id             = PageId.toString aboutId
          WebLogId       = webLogId
          AuthorId       = userId
          Title          = "About This Blog"
          Permalink      = Permalink "/about.html"
          PublishedOn    = aboutNow
          UpdatedOn      = aboutNow
          ShowInPageList = true
          Text           = Html "This blog is written in <strong>F#</strong> using Freya"
          Revisions      = [
            { AsOf = aboutNow
              Text = Html "This blog is written in <strong>F#</strong> using Freya"
              }
            ]
          }

    let contactId  = PageId.create ()
    let contactNow = now ()

    do!
      sess.StoreAsync
        { Id             = PageId.toString contactId
          WebLogId       = webLogId
          AuthorId       = userId
          Title          = "Contact Me"
          Permalink      = Permalink "/contact-me.html"
          PublishedOn    = contactNow
          UpdatedOn      = contactNow
          ShowInPageList = true
          Text           = Markdown "Just **call** _(123)_ 555-1234"
          Revisions      = [ { AsOf = contactNow; Text = Markdown "Just **call** _(123)_ 555-1234" } ]
          }

    // Posts
    let postNews1Id  = PostId.create ()
    let postNews1Now = now ()

    do!
      sess.StoreAsync
        { Id          = PostId.toString postNews1Id
          WebLogId    = webLogId
          AuthorId    = userId
          Status      = Published
          Title       = "Nice People on the Street"
          Permalink   = Permalink "/2019/nice-people-on-the-street.html"
          PublishedOn = subDays 3 postNews1Now
          UpdatedOn   = subDays 2 postNews1Now
          Text        = Markdown "I couldn't _believe_ it!"
          CategoryIds = [ catNewsId ]
          Tags        = [ Tag "nice"; Tag "street"; Tag "unbelievable" ]
          Revisions   = [
            { AsOf = subDays 3 postNews1Now; Text = Markdown "I coudln't _believe_ it!" }
            { AsOf = subDays 2 postNews1Now; Text = Markdown "I couldn't _believe_ it!" }
            ]
          }

    let postNews2Id   = PostId.create ()
    let postNews2Now  = (now >> subDays 7) ()
    let postNews2Text = "In a historic, never-before-seen circumstance, the presidential election ended in a tie. The Constitution does not provide for any sort of tie-breaker, so the Supreme Court has ruled that neither party has won. Surprisingly, the candidates believe this may be the best thing for the country; one of them was quoted as saying, \"Last month, one of my competitors called Congress a 'do-nothing Congress', and the next day, their approval rating had actually gone **up** 5 points. It's like the American people just want us to leave them alone, and they couldn't have sent a clearer message with today's results.\" Americans are generally optimistic about the next 4 years with no leader in the White House, though they remain apprehensive that, without the responsibility of actually governing, the candidates will just continue campaigning for another 4 years."

    do!
      sess.StoreAsync
        { Id          = PostId.toString postNews2Id
          WebLogId    = webLogId
          AuthorId    = userId
          Status      = Published
          Title       = "Presidential Election Ends in Tie; Nation Named Winner"
          Permalink   = Permalink "/2019/presidential-election-ends-in-tie-nation-named-winner.html"
          PublishedOn = postNews2Now
          UpdatedOn   = postNews2Now
          Text        = Markdown postNews2Text
          CategoryIds = [ catNewsId ]
          Tags        = [ Tag "candidate"; Tag "congress"; Tag "election"; Tag "president"; Tag "result" ]
          Revisions   = [ { AsOf = postNews2Now; Text = Markdown postNews2Text } ]
          }

    let postSportsId  = PostId.create ()
    let postSportsNow = (now >> subDays 10) ()

    do!
      sess.StoreAsync
        { Id          = PostId.toString postSportsId
          WebLogId    = webLogId
          AuthorId    = userId
          Status      = Published
          Title       = "My Team Rules"
          Permalink   = Permalink "/2019/my-team-rules.html"
          PublishedOn = postSportsNow
          UpdatedOn   = postSportsNow
          Text        = Html "...and your team drools!"
          CategoryIds = [ catSportsId ]
          Tags        = [ Tag "teams" ]
          Revisions   = [ { AsOf = postSportsNow; Text = Html "...and your team drools!" } ]
          }

    do! sess.SaveChangesAsync ()
    }
  |> Async.AwaitTask

let hello =
  freyaMachine {
    handleOk (Represent.text "Hello World from Freya")
    }
let seed =
  freyaMachine {
    liftDep getStore doSeed
    |> run deps
    |> Async.RunSynchronously
    handleOk (Represent.text "All done!")
    }

let webApp =
  freyaRouter {
    route GET "/"     hello
    route GET "/seed" seed
  }
