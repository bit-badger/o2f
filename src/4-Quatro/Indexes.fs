module Quatro.Indexes

open Raven.Client.Documents.Indexes
open System.Collections.Generic

 type Categories_ByWebLogIdAndSlug () as this =
  inherit AbstractJavaScriptIndexCreationTask ()
  do
    this.Maps <-
      HashSet<string> [
        "docs.Categories.Select(category => new {
            WebLogId = category.WebLogId,
            Slug = category.Slug
        })"
        ]

type Comments_ByPostId () as this =
  inherit AbstractJavaScriptIndexCreationTask ()
  do
    this.Maps <-
      HashSet<string> [
        "docs.Comments.Select(comment => new {
            PostId = comment.PostId
        })"
        ]

type Pages_ByWebLogIdAndPermalink () as this =
  inherit AbstractJavaScriptIndexCreationTask ()
  do
    this.Maps <-
      HashSet<string> [
        "docs.Pages.Select(page => new {
            WebLogId = page.WebLogId,
            Permalink = page.Permalink
        })"
        ]

type Posts_ByWebLogIdAndCategoryId () as this =
  inherit AbstractJavaScriptIndexCreationTask ()
  do
    this.Maps <-
      HashSet<string> [
        "docs.Posts.SelectMany(post => post.CategoryIds, (post, category) => new {
            WebLogId = post.WebLogId,
            CategoryId = category
        })"
        ]
    
type Posts_ByWebLogIdAndPermalink () as this =
  inherit AbstractJavaScriptIndexCreationTask ()
  do
    this.Maps <-
      HashSet<string> [
        "docs.Posts.Select(post => new {
            WebLogId = post.WebLogId,
            Permalink = post.Permalink
        })"
        ]

type Posts_ByWebLogIdAndTag () as this =
  inherit AbstractJavaScriptIndexCreationTask ()
  do
    this.Maps <-
      HashSet<string> [
        "docs.Posts.SelectMany(post => post.Tags, (post, tag) => new {
            WebLogId = post.WebLogId,
            Tag = tag
        })"
        ]

type Users_ByEmailAddressAndPasswordHash () as this =
  inherit AbstractJavaScriptIndexCreationTask ()
  do
    this.Maps <-
      HashSet<string> [
        "docs.Users.Select(user => new {
            EmailAddress = user.EmailAddress,
            PasswordHash = user.PasswordHash
        })"
        ]
