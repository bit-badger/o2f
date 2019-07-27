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
