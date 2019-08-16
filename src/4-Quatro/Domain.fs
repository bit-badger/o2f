module Quatro.Domain

open MiniGuids
open System

type CategoryId = CategoryId of string
module CategoryId =
  let ofGuid = Collection.idFor Collection.Category >> CategoryId
  let create = MiniGuid.NewGuid >> ofGuid
  let empty = (MiniGuid >> ofGuid) Guid.Empty
  let ofString (stringGuid : string) = ofGuid <| MiniGuid.Parse stringGuid
  let toString x = match x with CategoryId y -> y
  let asGuidString x = (toString >> Collection.fromId >> snd) x |> string

type CommentId = CommentId of string 
module CommentId =
  let ofGuid = Collection.idFor Collection.Comment >> CommentId
  let create = MiniGuid.NewGuid >> ofGuid
  let empty = (MiniGuid >> ofGuid) Guid.Empty
  let ofString (stringGuid : string) = ofGuid <| MiniGuid.Parse stringGuid
  let toString x = match x with CommentId y -> y
  let asGuidString x = (toString >> Collection.fromId >> snd) x |> string

type PageId = PageId of string
module PageId =
  let ofGuid = Collection.idFor Collection.Page >> PageId
  let create = MiniGuid.NewGuid >> ofGuid
  let empty = (MiniGuid >> ofGuid) Guid.Empty
  let ofString (stringGuid : string) = ofGuid <| MiniGuid.Parse stringGuid
  let toString x = match x with PageId y -> y
  let asGuidString x = (toString >> Collection.fromId >> snd) x |> string

type PostId = PostId of string
module PostId =
  let ofGuid = Collection.idFor Collection.Post >> PostId
  let create = MiniGuid.NewGuid >> ofGuid
  let empty = (MiniGuid >> ofGuid) Guid.Empty
  let ofString (stringGuid : string) = ofGuid <| MiniGuid.Parse stringGuid
  let toString x = match x with PostId y -> y
  let asGuidString x = (toString >> Collection.fromId >> snd) x |> string

type UserId = UserId of string
module UserId =
  let ofGuid = Collection.idFor Collection.User >> UserId
  let create = MiniGuid.NewGuid >> ofGuid
  let empty = (MiniGuid >> ofGuid) Guid.Empty
  let ofString (stringGuid : string) = ofGuid <| MiniGuid.Parse stringGuid
  let toString x = match x with UserId y -> y
  let asGuidString x = (toString >> Collection.fromId >> snd) x |> string

type WebLogId = WebLogId of string
module WebLogId =
  let ofGuid = Collection.idFor Collection.WebLog >> WebLogId
  let create = MiniGuid.NewGuid >> ofGuid
  let empty = (MiniGuid >> ofGuid) Guid.Empty
  let ofString (stringGuid : string) = ofGuid <| MiniGuid.Parse stringGuid
  let toString x = match x with WebLogId y -> y
  let asGuidString x = (toString >> Collection.fromId >> snd) x |> string

type Permalink = Permalink of string
type Tag       = Tag       of string
type Ticks     = Ticks     of int64
type TimeZone  = TimeZone  of string
type Url       = Url       of string

type ArticleContent =
  | Html     of string
  | Markdown of string
module ArticleContent =
  let generate content =
    match content with Html x -> x | Markdown y -> MarkdownSharp.Markdown().Transform y

type AuthorizationLevel =
  | Administrator
  | User

type PostStatus =
  | Draft
  | Published

type CommentStatus =
  | Approved
  | Pending
  | Spam

[<CLIMutable; NoComparison; NoEquality>]
type Revision =
  { AsOf : Ticks
    Text : ArticleContent
    }
with
  static member Empty =
    { AsOf = Ticks 0L
      Text = Html ""
      }


[<CLIMutable; NoComparison; NoEquality>]
type Page =
  { Id             : string
    WebLogId       : WebLogId
    AuthorId       : UserId
    Title          : string
    Permalink      : Permalink
    PublishedOn    : Ticks
    UpdatedOn      : Ticks
    ShowInPageList : bool
    Text           : ArticleContent
    Revisions      : Revision list
    }
with
  static member Empty = 
    { Id             = ""
      WebLogId       = WebLogId.empty
      AuthorId       = UserId.empty
      Title          = ""
      Permalink      = Permalink ""
      PublishedOn    = Ticks 0L
      UpdatedOn      = Ticks 0L
      ShowInPageList = false
      Text           = Html ""
      Revisions      = []
      }

[<CLIMutable; NoComparison; NoEquality>]
type WebLog =
  { Id          : string
    Name        : string
    Subtitle    : string option
    DefaultPage : string
    ThemePath   : string
    UrlBase     : string
    TimeZone    : TimeZone
    }
with
  /// An empty web log
  static member Empty =
    { Id          = ""
      Name        = ""
      Subtitle    = None
      DefaultPage = ""
      ThemePath   = "default"
      UrlBase     = ""
      TimeZone    = TimeZone "America/New_York"
      }

[<CLIMutable; NoComparison; NoEquality>]
type Authorization =
  { WebLogId : WebLogId
    Level    : AuthorizationLevel
    }

[<CLIMutable; NoComparison; NoEquality>]
type User =
  { Id             : string
    EmailAddress   : string
    PasswordHash   : string
    FirstName      : string
    LastName       : string
    PreferredName  : string
    Url            : Url option
    Authorizations : Authorization list
    }
with
  static member Empty =
    { Id             = ""
      EmailAddress   = ""
      FirstName      = ""
      LastName       = ""
      PreferredName  = ""
      PasswordHash   = ""
      Url            = None
      Authorizations = []
      }

[<CLIMutable; NoComparison; NoEquality>]
type Category =
  { Id          : string
    WebLogId    : WebLogId
    Name        : string
    Slug        : string
    Description : string option
    ParentId    : CategoryId option
    Children    : CategoryId list
    }
with
  static member Empty =
    { Id          = "new"
      WebLogId    = WebLogId.empty
      Name        = ""
      Slug        = ""
      Description = None
      ParentId    = None
      Children    = []
      }

[<CLIMutable; NoComparison; NoEquality>]
type Comment =
  { Id           : string
    PostId       : PostId
    InReplyToId  : CommentId option
    Name         : string
    EmailAddress : string
    Url          : Url option
    Status       : CommentStatus
    PostedOn     : Ticks
    Text         : string
    }
with
  static member Empty =
    { Id           = ""
      PostId       = PostId.empty
      InReplyToId  = None
      Name         = ""
      EmailAddress = ""
      Url          = None
      Status       = Pending
      PostedOn     = Ticks 0L
      Text         = ""
      }

[<CLIMutable; NoComparison; NoEquality>]
type Post =
  { Id          : string
    WebLogId    : WebLogId
    AuthorId    : UserId
    Status      : PostStatus
    Title       : string
    Permalink   : Permalink
    PublishedOn : Ticks
    UpdatedOn   : Ticks
    Text        : ArticleContent
    CategoryIds : CategoryId list
    Tags        : Tag list
    Revisions   : Revision list
    }
with
  static member Empty =
    { Id          = "new"
      WebLogId    = WebLogId.empty
      AuthorId    = UserId.empty
      Status      = Draft
      Title       = ""
      Permalink   = Permalink ""
      PublishedOn = Ticks 0L
      UpdatedOn   = Ticks 0L
      Text        = Html ""
      CategoryIds = []
      Tags        = []
      Revisions   = []
      }
