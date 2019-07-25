module Quatro.Domain

type CategoryId = CategoryId of string
type CommentId  = CommentId  of string 
type PageId     = PageId     of string
type PostId     = PostId     of string
type UserId     = UserId     of string
type WebLogId   = WebLogId   of string

type Permalink = Permalink of string
type Tag       = Tag       of string
type Ticks     = Ticks     of int64
type TimeZone  = TimeZone  of string
type Url       = Url       of string

type ArticleContent =
  | Html     of string
  | Markdown of string
with
  member this.Generate () =
    match this with Html x -> x | Markdown y -> MarkdownSharp.Markdown().Transform y

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
    { AsOf       = Ticks 0L
      Text       = Html ""
      }

[<CLIMutable; NoComparison; NoEquality>]
type Page =
  { Id             : PageId
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
    { Id             = PageId ""
      WebLogId       = WebLogId ""
      AuthorId       = UserId ""
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
  { Id          : WebLogId
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
    { Id          = WebLogId ""
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
  { Id : UserId
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
    { Id             = UserId ""
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
  { Id          : CategoryId
    WebLogId    : WebLogId
    Name        : string
    Slug        : string
    Description : string option
    ParentId    : CategoryId option
    Children    : CategoryId list
    }
with
  static member Empty =
    { Id          = CategoryId "new"
      WebLogId    = WebLogId ""
      Name        = ""
      Slug        = ""
      Description = None
      ParentId    = None
      Children    = []
      }

[<CLIMutable; NoComparison; NoEquality>]
type Comment =
  { Id           : CommentId
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
    { Id           = CommentId ""
      PostId       = PostId ""
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
  { Id          : PostId
    WebLogId    : WebLogId
    AuthorId    : UserId
    Status      : PostStatus
    Title       : string
    Permalink   : string
    PublishedOn : Ticks
    UpdatedOn   : Ticks
    Text        : ArticleContent
    CategoryIds : CategoryId list
    Tags        : Tag list
    Revisions   : Revision list
    }
with
  static member Empty =
    { Id          = PostId "new"
      WebLogId    = WebLogId ""
      AuthorId    = UserId ""
      Status      = Draft
      Title       = ""
      Permalink   = ""
      PublishedOn = Ticks 0L
      UpdatedOn   = Ticks 0L
      Text        = Html ""
      CategoryIds = []
      Tags        = []
      Revisions   = []
      }
