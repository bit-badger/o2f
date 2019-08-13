namespace Tres.Domain

[<RequireQualifiedAccess>]
module AuthorizationLevel =
  [<Literal>]
  let Administrator = "Administrator"
  [<Literal>]
  let User = "User"

[<RequireQualifiedAccess>]
module ContentType =
  [<Literal>]
  let Html = "Html"
  [<Literal>]        
  let Markdown = "Markdown"

[<RequireQualifiedAccess>]
module PostStatus =
  [<Literal>]
  let Draft = "Draft"
  [<Literal>]
  let Published = "Published"

[<RequireQualifiedAccess>]
module CommentStatus =
  [<Literal>]
  let Approved = "Approved"
  [<Literal>]
  let Pending = "Pending"
  [<Literal>]
  let Spam = "Spam"


type IArticleContent =
  abstract member ContentType : string with get
  abstract member Text : string with get, set
  abstract member Generate : unit -> string

type HtmlArticleContent () =
  let mutable text = ""
  override __.ToString () = sprintf "HTML -> %s" text
  interface IArticleContent with
    member __.ContentType = ContentType.Html
    member __.Text with get () = text and set v = text <- v
    member __.Generate () = text

type MarkdownArticleContent () =
  let mutable text = ""
  override __.ToString () = sprintf "Markdown -> %s" text
  interface IArticleContent with
    member __.ContentType = ContentType.Markdown
    member __.Text with get () = text and set v = text <- v
    member __.Generate () = MarkdownSharp.Markdown().Transform text


open Newtonsoft.Json

type IArticleContentConverter () =
  inherit JsonConverter<IArticleContent> ()

  override __.WriteJson (w : JsonWriter, v : IArticleContent, _ : JsonSerializer) =
    let writePair k (v : string) =
      w.WritePropertyName k
      w.WriteValue        v
    w.WriteStartObject ()
    writePair "ContentType" v.ContentType
    writePair "Text"        v.Text
    w.WriteEndObject ()

  override __.ReadJson (r : JsonReader, _, _, _, _) =
    let readIgnore = r.Read >> ignore
    let typ  = (readIgnore >> r.ReadAsString) () // PropertyName -> String
    let text = (readIgnore >> r.ReadAsString) () // PropertyName -> String
    readIgnore () // EndObject
    let content : IArticleContent =
      match typ with
      | ContentType.Html -> upcast HtmlArticleContent ()
      | ContentType.Markdown -> upcast MarkdownArticleContent ()
      | x -> invalidOp (sprintf "Cannot deserialize %s into IArticleContent" x)
    content.Text <- text
    content


[<CLIMutable; NoComparison; NoEquality>]
type Revision =
  { AsOf : int64
    Text : IArticleContent
    }
with
  static member Empty =
    { AsOf = 0L
      Text = HtmlArticleContent ()
      }

[<CLIMutable; NoComparison; NoEquality>]
type Page =
  { Id             : string
    WebLogId       : string
    AuthorId       : string
    Title          : string
    Permalink      : string
    PublishedOn    : int64
    UpdatedOn      : int64
    ShowInPageList : bool
    Text           : IArticleContent
    Revisions      : Revision list
    }
with
  static member Empty = 
    { Id             = ""
      WebLogId       = ""
      AuthorId       = ""
      Title          = ""
      Permalink      = ""
      PublishedOn    = 0L
      UpdatedOn      = 0L
      ShowInPageList = false
      Text           = HtmlArticleContent ()
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
    TimeZone    : string
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
      TimeZone    = "America/New_York"
      }

[<CLIMutable; NoComparison; NoEquality>]
type Authorization =
  { WebLogId : string
    Level    : string
    }

[<CLIMutable; NoComparison; NoEquality>]
type User =
  { Id             : string
    EmailAddress   : string
    PasswordHash   : string
    FirstName      : string
    LastName       : string
    PreferredName  : string
    Url            : string option
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
    WebLogId    : string
    Name        : string
    Slug        : string
    Description : string option
    ParentId    : string option
    Children    : string list
    }
with
  static member Empty =
    { Id          = "new"
      WebLogId    = ""
      Name        = ""
      Slug        = ""
      Description = None
      ParentId    = None
      Children    = []
      }

[<CLIMutable; NoComparison; NoEquality>]
type Comment =
  { Id          : string
    PostId      : string
    InReplyToId : string option
    Name        : string
    Email       : string
    Url         : string option
    Status      : string
    PostedOn    : int64
    Text        : string
    }
with
  static member Empty =
    { Id          = ""
      PostId      = ""
      InReplyToId = None
      Name        = ""
      Email       = ""
      Url         = None
      Status      = CommentStatus.Pending
      PostedOn    = 0L
      Text        = ""
      }

[<CLIMutable; NoComparison; NoEquality>]
type Post =
  { Id          : string
    WebLogId    : string
    AuthorId    : string
    Status      : string
    Title       : string
    Permalink   : string
    PublishedOn : int64
    UpdatedOn   : int64
    Text        : IArticleContent
    CategoryIds : string list
    Tags        : string list
    Revisions   : Revision list
    }
with
  static member Empty =
    { Id          = "new"
      WebLogId    = ""
      AuthorId    = ""
      Status      = PostStatus.Draft
      Title       = ""
      Permalink   = ""
      PublishedOn = 0L
      UpdatedOn   = 0L
      Text        = HtmlArticleContent ()
      CategoryIds = []
      Tags        = []
      Revisions   = []
      }
