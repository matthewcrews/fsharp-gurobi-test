// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open System
open Examples

[<EntryPoint>]
let main argv = 

    Netflow.runExample

    Console.ReadKey() |> ignore
    0 // return an integer exit code
