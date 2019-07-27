[<RequireQualifiedAccess>]
module Quatro.Collection

open System

let Category = "Categories"
let Comment  = "Comments"
let Page     = "Pages"
let Post     = "Posts"
let User     = "Users"
let WebLog   = "WebLogs"

let idFor coll (docId : Guid) = sprintf "%s/%s" coll (docId.ToString "N")

let fromId docId =
  try
    let parts = (match isNull docId with true -> "" | false -> docId).Split '/'
    match parts.Length with
    | 2 -> parts.[0], Guid.Parse parts.[1]
    | _ -> "", Guid.Empty
  with :?FormatException -> "", Guid.Empty