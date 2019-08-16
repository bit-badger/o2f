[<RequireQualifiedAccess>]
module Quatro.Collection

open MiniGuids
open System

let Category = "Categories"
let Comment  = "Comments"
let Page     = "Pages"
let Post     = "Posts"
let User     = "Users"
let WebLog   = "WebLogs"

let idFor coll (docId : MiniGuid) = sprintf "%s/%s" coll (string docId)

let fromId docId =
  try
    let parts = (match isNull docId with true -> "" | false -> docId).Split '/'
    match parts.Length with
    | 2 -> parts.[0], MiniGuid.Parse parts.[1]
    | _ -> "", MiniGuid Guid.Empty
  with :?FormatException -> "", MiniGuid Guid.Empty