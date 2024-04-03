## NOTE _(April 2024)_

This repository is complete in its current state. Nancy and Freya have reached their end of life, Giraffe is rock-solid but looking for maintainers, and even ASP.NET Core itself is more functional with its minimal APIs. If browsing this archive (or its site) helps, I am grateful. For examples using my preferred get-things-done stack (Giraffe, Giraffe View Engine, and htmx), check out [the Bit Badger open source repository](https://git.bitbadger.solutions/bit-badger/); most of those projects follow that pattern.

# objects |> functions

## What

This repository will track the development of a rudimentary multi-site blog platform, in parallel, in 5 different environments:

1. ASP.NET Core MVC / C# ("**Uno**")

2. Nancy / C# ("**Dos**")

3. Nancy / F# ("**Tres**")

4. Giraffe / F# ("**Quatro**")

5. Freya / F# ("**Cinco**")

The goal is to be able to start any of the five solutions, and be able to use the same data store and have the behavior of each site work the same. All five will use [RavenDB](https://ravendb.net) to persist the data (and, where required, for session storage as well).

## Why

The idea for this came out of a F# community Slack chat in which [Daniel](https://github.com/danieljsummers) participated. Lighter weight frameworks can provide real benefits, and a more composable system can be easier to reason about and maintain. However, when one goes "full functional", there are concepts that do not even directly translate. Compound this with the language of "monads" and "applicative functors" and the like, and an OO person's eyes can start to glaze over.

## Who (and continuing with Why)

Daniel is that developer, who had admired F# for years, and almost had **Tres** coded in another repository when he decided to go a different route with his personal sites. He is learning as he writes (though, in this "v2" instance, he has several F# projects that have been online for several years). He is quite grateful for the support of the F# community, and hopes that this guide will demonstrate how to get not just from C# to F#, but from object thinking to functional thinking.

_This is not his primary occupation, so the pace may be slow; ideally, the result will be worth the wait._

## The Steps

The plan is laid out, and will be documented as we go along, on the [project site](https://objects-to-functions.bitbadger.solutions).
