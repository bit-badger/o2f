module Cinco.Data

open Chiron
open Raven.Client.Documents

type ConfigParameter =
  | Url      of string
  | Database of string

type DataConfig = { Parameters : ConfigParameter list }

module DataConfig =
  let fromJson json =
    match Json.parse json with
    | Object config ->
        { Parameters =
            config
            |> Map.toList
            |> List.map (fun item ->
                match item with
                | "Url",      String x -> Url x
                | "Database", String x -> Database x
                | key, value -> invalidOp <| sprintf "Unexpected RavenDB config parameter %s (value %A)" key value)
          }
    | _ -> { Parameters = [] }
  let configureStore config store  =
    config.Parameters
    |> List.fold
        (fun (stor : DocumentStore) par -> 
            match par with
            | Url url -> stor.Urls <- [| url |]; stor
            | Database db -> stor.Database <- db; stor)
        store

open Indexes

let ensureIndexes store =
  IndexCreation.CreateIndexes (typeof<Categories_ByWebLogIdAndSlug>.Assembly, store)

/// JSON converters for custom types
module Converters =
  
  open Domain
  open Newtonsoft.Json
  open Microsoft.FSharpLu.Json
  
  /// JSON converter for CategoryId
  type CategoryIdJsonConverter () =
    inherit JsonConverter<CategoryId> ()
    override __.WriteJson (w : JsonWriter, v : CategoryId, _ : JsonSerializer) =
      (CategoryId.toString >> w.WriteValue) v
    override __.ReadJson (r: JsonReader, _, _, _, _) =
      (string >> CategoryId) r.Value

  /// JSON converter for CommentId
  type CommentIdJsonConverter () =
    inherit JsonConverter<CommentId> ()
    override __.WriteJson (w : JsonWriter, v : CommentId, _ : JsonSerializer) =
      (CommentId.toString >> w.WriteValue) v
    override __.ReadJson (r: JsonReader, _, _, _, _) =
      (string >> CommentId) r.Value

  /// JSON converter for PageId
  type PageIdJsonConverter () =
    inherit JsonConverter<PageId> ()
    override __.WriteJson (w : JsonWriter, v : PageId, _ : JsonSerializer) =
      (PageId.toString >> w.WriteValue) v
    override __.ReadJson (r: JsonReader, _, _, _, _) =
      (string >> PageId) r.Value

  /// JSON converter for Permalink
  type PermalinkJsonConverter () =
    inherit JsonConverter<Permalink> ()
    override __.WriteJson (w : JsonWriter, v : Permalink, _ : JsonSerializer) =
      let (Permalink x) = v
      w.WriteValue x
    override __.ReadJson (r: JsonReader, _, _, _, _) =
      (string >> Permalink) r.Value

  /// JSON converter for PostId
  type PostIdJsonConverter () =
    inherit JsonConverter<PostId> ()
    override __.WriteJson (w : JsonWriter, v : PostId, _ : JsonSerializer) =
      (PostId.toString >> w.WriteValue) v
    override __.ReadJson (r: JsonReader, _, _, _, _) =
      (string >> PostId) r.Value

  /// JSON converter for Tag
  type TagJsonConverter () =
    inherit JsonConverter<Tag> ()
    override __.WriteJson (w : JsonWriter, v : Tag, _ : JsonSerializer) =
      let (Tag x) = v
      w.WriteValue x
    override __.ReadJson (r: JsonReader, _, _, _, _) =
      (string >> Tag) r.Value

  /// JSON converter for Ticks
  type TicksJsonConverter () =
    inherit JsonConverter<Ticks> ()
    override __.WriteJson (w : JsonWriter, v : Ticks, _ : JsonSerializer) =
      let (Ticks x) = v
      w.WriteValue x
    override __.ReadJson (r: JsonReader, _, _, _, _) =
      (string >> int64 >> Ticks) r.Value

  /// JSON converter for TimeZone
  type TimeZoneJsonConverter () =
    inherit JsonConverter<TimeZone> ()
    override __.WriteJson (w : JsonWriter, v : TimeZone, _ : JsonSerializer) =
      let (TimeZone x) = v
      w.WriteValue x
    override __.ReadJson (r: JsonReader, _, _, _, _) =
      (string >> TimeZone) r.Value

  /// JSON converter for Url
  type UrlJsonConverter () =
    inherit JsonConverter<Url> ()
    override __.WriteJson (w : JsonWriter, v : Url, _ : JsonSerializer) =
      let (Url x) = v
      w.WriteValue x
    override __.ReadJson (r: JsonReader, _, _, _, _) =
      (string >> Url) r.Value

  /// JSON converter for UserId
  type UserIdJsonConverter () =
    inherit JsonConverter<UserId> ()
    override __.WriteJson (w : JsonWriter, v : UserId, _ : JsonSerializer) =
      (UserId.toString >> w.WriteValue) v
    override __.ReadJson (r: JsonReader, _, _, _, _) =
      (string >> UserId) r.Value

  /// JSON converter for WebLogId
  type WebLogIdJsonConverter () =
    inherit JsonConverter<WebLogId> ()
    override __.WriteJson (w : JsonWriter, v : WebLogId, _ : JsonSerializer) =
      (WebLogId.toString >> w.WriteValue) v
    override __.ReadJson (r: JsonReader, _, _, _, _) =
      (string >> WebLogId) r.Value

  let all : JsonConverter seq =
    seq {
      yield CategoryIdJsonConverter ()
      yield CommentIdJsonConverter ()
      yield PageIdJsonConverter ()
      yield PermalinkJsonConverter ()
      yield PostIdJsonConverter ()
      yield TagJsonConverter ()
      yield TicksJsonConverter ()
      yield TimeZoneJsonConverter ()
      yield UrlJsonConverter ()
      yield UserIdJsonConverter ()
      yield WebLogIdJsonConverter ()
      yield CompactUnionJsonConverter true
      }
