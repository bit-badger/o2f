(*** hide ***)
namespace Tres.Domain

type IArticleContent =
  abstract member ContentType : string with get
  abstract member Text : string with get, set
  abstract member Generate : unit -> string
type HtmlArticleContent () =
  let mutable text = ""
  interface IArticleContent with
    member __.ContentType = "Html"
    member __.Text with get () = text and set v = text <- v
    member __.Generate () = text

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

(**
### Tres - Step 2

As we make the leap to F#, we're changing things around significantly. Remember our
[discussion about the flat structure of an F# project](../step1/tres.html)? Instead of an `Domain` directory with a lot
of little files, we'll define a single `Domain.fs` file in the root of the project. Don't forget to add it to the list
of compiled files in `Tres.fsproj`; it should go above `HomeModule.fs`.

Next up, we will change the static classes that we created to eliminate magic strings into modules.  The
`AuthorizationLevel` type in C# looked like:

    [lang=csharp]
    public static class AuthorizationLevel
    {
        const string Administrator = "Administrator";

        const string User = "User";
    }

The F# version (within the namespace `Tres.Entities`):
*)
[<RequireQualifiedAccess>]
module AuthorizationLevel =
  [<Literal>]
  let Administrator = "Administrator"
  [<Literal>]
  let User = "User"
(**
The `RequireQualifiedAccess` attribute means that this module cannot be `open`ed, which means that `Administrator`
cannot ever be construed to be that value; it must be referenced as `AuthorizationLevel.Administrator`. The
`Literal` attribute means that these values can be used in places where a literal string is required. (There is a
specific place this will help us when we start writing code around these types.) Also of note here is the different
way F# defines attributes from the way C# does; instead of `[` `]` pairs, we use `[<` `>]` pairs.

We are also going to change from class types to record types.  Record types can be thought of as `struct`s, though the
comparison is not exact; record types are reference types, not value types, but they cannot be set to null **in code**
_(huge caveat which we'll see in the next step)_ unless explicitly identified. We're also going to embrace F#'s
immutability-by-default qualities that will save us a heap of null checks (as well as those pesky situations where we
forget to implement them).

As a representative example, consider the `Page` type.  In C#, it looks like this:

    [lang=csharp]
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

It contains strings, for the most part, and a `Revisions` collection. Now, here's how we'll implement this same thing
in F#:
*)
namespace Tres.Domain

//...
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
(**
The field declarations immediately under the `type` declaration mirror those in our C# version; since they are fields,
though, we don't have to define getters and setters.

F# requires record types to always have all fields defined. F# also provides a `with` statement (separate from the one
in the code above) that allows us to create a new instance of a record type that has all the fields of our original
ones, only replacing the ones we specify. So, in C#, while we can do something like

    [lang=csharp]
    var pg = new Page { Title = "Untitled" };

, leaving all the other fields in their otherwise-initialized state, F# will not allow us to do that.  This is where
the `Empty` static property comes in; we can use this to create new pages, while ensuring that we have sensible
defaults for all the other fields.  The equivalent to the above C# statement in F# would be
*)
(*** hide ***)
module PageExample =
(*** unhide ***)
  let pg = { Page.Empty with Title = "Untitled" }
(**
.  Note the default values for `Permalink`: in C#, it's null, but in F#, it's an empty string. Now, certainly, you can
use `String.IsNullOrEmpty()` to check for both of those, but we'll see some advantages to this lack of nullability as
we continue to develop this project.

A few syntax notes:
- The `CLIMutable` attribute instructs the compiler to generate a no-argument constructor for the underlying class. It
is not something we will reference in our code, but when RavenDB tries to create instances of these types when we load
them from the database, this will help it.
- The `NoComparison` and `NoEquality` attributes make these classes more lightweight. By default, F# will generate a
custom equality operation for each record type that compares every field within the record; with these attributes, it
will leave that code out. This is fine for our purposes; we aren't going to be comparing pages with `=` or `>`.
- `[]` represents an empty list in F#. An F# list (as distinguished from `System.Collections.List` or
`System.Collections.Generic.List<T>`) is also an immutable data structure; it consists of a head element, and a tail
list. It can be constructed by creating a new list with an element as its head and the existing list as its tail, and
deconstructed by processing the head, then processing the head of the tail, etc. (There are operators and functions to
support that; we'll definitely use those as we go along.) Items in a list are separated by semicolons;
`[ "one"; "two"; "three" ]` represents a `string list` with three items. It supports most all the collection
operations you would expect, but there are some differences.
- While not demonstrated here, arrays are defined between `[|` `|]` pairs, also with elements separated by semicolons.

Before continuing on to [Quatro / Cinco](quatro-cinco.html), you should familiarize yourself with the
[types in this step](https://github.com/bit-badger/o2f/tree/step-2/src/3-Tres/Domain.fs).

---
[Back to Step 2](../step2)
*)