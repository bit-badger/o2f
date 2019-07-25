### Uno/Dos - Step 2

As the overview page mentioned, this model is pretty straightforward. There are four static classes (`AuthorizationLevel`, `CommentStatus`, `ContentType`, and `PostStatus`) to keep us from using magic strings in the code. The objects that will actually be stored in the database are `Category`, `Comment`, `Page`, `Post`, `User`, and `WebLog`. `Revision` and `Authorization` give structure to the collections stored within other objects (`Page`/`Post` and `User`, respectively). Additionally, `IArticleContent` and its two implementations `HtmlArticleContent` and `MarkdownArticleContent` are used to distinguish the source text format of pages, posts, and revisions.

If you're reading through this for learning, you'll want to familiarize yourself with the [files as they are in C# at this step](https://github.com/bit-badger/o2f/tree/step-2/src/1-Uno/Domain) before continuing to [Tres](tres.html) and [Quatro / Cinco](quatro-cinco.html).

---
[Back to Step 2](../step2)
