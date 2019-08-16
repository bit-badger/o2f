module Quatro.Data

open Domain

open Newtonsoft.Json

module Converters =
  
  open Microsoft.FSharpLu.Json
  
  /// JSON converter for Ticks
  type TicksJsonConverter () =
    inherit JsonConverter<Ticks> ()
    override __.WriteJson(w : JsonWriter, v : Ticks, _ : JsonSerializer) =
      let (Ticks x) = v
      w.WriteValue x
    override __.ReadJson(r: JsonReader, _, _, _, _) =
      (string >> int64 >> Ticks) r.Value

  /// JSON converter for WebLogId
  type WebLogIdJsonConverter () =
    inherit JsonConverter<WebLogId> ()
    override __.WriteJson(w : JsonWriter, v : WebLogId, _ : JsonSerializer) =
      (WebLogId.toString >> w.WriteValue) v
    override __.ReadJson(r: JsonReader, _, _, _, _) =
      (string >> WebLogId) r.Value

  let all : JsonConverter seq =
    seq {
      // other converters
      yield TicksJsonConverter ()
      yield WebLogIdJsonConverter ()
      yield CompactUnionJsonConverter true
      }
