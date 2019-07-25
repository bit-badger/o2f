(*** hide ***)
#r @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\netstandard\v4.0_2.0.0.0__cc7b13ffcd2ddd51\netstandard.dll"
#r "../../../packages/MarkdownSharp/lib/netstandard2.0/MarkdownSharp.dll"
(*** define: module-definition ***)
namespace Quatro.Domain
(*** define: article-content-definition ***)
type ArticleContent =
  | Html     of string
  | Markdown of string
with
  member this.Generate () =
    match this with Html x -> x | Markdown y -> MarkdownSharp.Markdown().Transform y
(*** hide ***)
type Revision =
  { AsOf : int64
    Text : ArticleContent
    }
with
  static member Empty =
    { AsOf = 0L
      Text = Html ""
      }
(**
### Quatro / Cinco - Step 2

As we [converted our model to F#](tres.html), we brought in some immutability. What we created can form the basis of a
fully operational F# application; but we can do better. For example, given "faad05f6-c539-44b7-9e94-1b68da4bba57" -
quick! Is it a post Id? A page Id? The text of a really lame blog post? Also, what is to prevent us from using a
`CommentStatus` value in a spot where a `PostStatus` should go? (Do you really want your own post to be able to be
flagged as spam?)

To be sure, these same problems exist in most OO realms, and developers manage to keep all the strings separate. What
the good ones do is write unit tests that construct these invalid states, and ensure that their application handles them
gracefully. However, just as immutability gets rid of `null` checks, F# has features that go even further, and can help
us create a model where invalid states cannot be represented. _F# for Fun and Profit_ has a great series on
[Designing with Types](http://fsharpforfunandprofit.com/series/designing-with-types.html), and I highly recommend
reading it; it goes into way more depth that we're going to at this point.

The language feature we're going to use is called **discriminated unions** (or "DUs" for short). You've probably dealt
with `enum`s in C#; that is the closest parallel to DUs, but there are significant differences. Like `enum`s, DUs are
an exhaustive list of all expected/valid values. Unlike `enum`s, though, they are not wrappers over another type; they
are their own type. Also, each condition does not have to have the same type; it's perfectly valid to have a DU with
one condition that has one type (or no type at all), and other condition with a completely different type. (We don't
use that with these types.)

To start, bring the `Domain.fs` file over from Tres. The first type of DU we'll use is called a single-case
discriminated union; it can be used to wrap primitives to make them more meaningful. We'll create the following
single-case DUs at the top of the file, before our other types:
*)
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
(**
It may be confusing that we're using the same name twice; the name after the `type` keyword defines the type, while the
one after the equals sign defines the constructor for this type (`CategoryId "abc"` defines a category Id whose value
is the string "abc"). We'll look at these implemented in a bit; next, though, we'll convert our
static-classes-turned-modules into multi-case DUs.
*)
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
(**
This is similar in concept to the single-case DUs, but there are no parameters required for the constructor.

If you are following along in the finished code, at this point, you're thinking "Why did he skip `ArticleContent`?" If
you take a look at
[the definitions of `IArticleContent` and its implementations](https://github.com/bit-badger/o2f/tree/step-2/src/3-Tres/Domain.fs#L34),
it's not too bad; the interface and both implementations fit well within a viewing window. But, seeing all three there
together, we can spot a lot of repeated code and ceremony. When it comes down to it, we really just need to know if a
string has HTML or Markdown, so we can a) either display it as-is or process it before displaying it; and b) populate
the right editor when we're editing posts and pages.

We haven't encountered it yet (though we will when start implementing **Tres**\), but working with interfaces in F# can
be a bit of a pain. If you mean to call code that is specified by the interface, you have to either cast the
implementation to the interface type, or code these casts into the type's properties (which means you end up writing two
implementations). However, this is a _great_ case for a multi-case DU _with types_.
*)
(*** include: article-content-definition ***)
(**
This code does what the other did, but even more succinctly, and will require no special casts. This also shows how we
can add instance methods to DUs. We may end up removing this from here, and making a separate `render` function that
takes an `ArticleContent` and produces HTML; but, for now, these 6 lines replace 4 files in C# and 4 types from
**Tres**.

Now that we have defined our specific types, we can apply them to our record types to more precisely specify the shape
of the information. Let's revisit the `Page` type we dissected for **Tres**.
*)
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
(**
The only primitives\* we now have are the `Title` field (which is free-form text) and the `ShowInPageList` field (for
which yes/no is sufficient, although we could create a `PageListVisibility` DU to further constrain the yes/no values
and distinguish them from others). The compiler will prevent us from crossing boundaries on every other field in this
type!

Let's take a look at the `Empty` property on the `Post` type to see a multi-case DU in use.
*)
(*** hide ***)
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
(** *)
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
(**
`Status` is defined as type `PostStatus`; to set its value, we simply have to write `Draft`. No quotes, no dotted
access\*\*, just `Status = Draft`. (`Status = Spam` does not compile.)

One final thing; notice the top of the file...
*)
(*** include: module-definition ***)
(**
While this file would have no functional difference whether the top-level definition were `namespace` or `module`,
defining our files as modules can have some advantages. Back in step 1, we had to create modules where we wouldn't
necessarily have needed them, simply because you can have `let` statements directly in a namespace. Were we to change
the first line in `App.fs` to `module Quatro.App` instead of `namespace Quatro` (or Cinco), we could end the file with
the `main` function. We'll make this change to those files as part of the next step.

You can [review the entire set of types](https://github.com/bit-badger/o2f/tree/step-2/src/4-Quatro/Domain.fs) to see
where these various DUs were used. While we could certainly take this much further, these simple changes have made our
types more meaningful, while eliminating a lot of the invalid states we could have assigned in our code.

---

[Back to Step 2](../step2)

\* - `string` is a primitive for our purposes here.

\*\* - If our DU condition is not unique, it may need to be qualified. For example, if we were to add a "Draft"
`CommentStatus` so we could auto-save comment text while the visitor was typing\*\*\*, we would need to change the
`Empty` property to assign `PostStatus.Draft` instead. Again, though, the compiler would help us spot that right away.

\*\*\* - This is a really bad idea; don't do this.
*)