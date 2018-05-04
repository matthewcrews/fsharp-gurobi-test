// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open System
open ListHelpers
open Gurobi
open GurobiSharp


// Define index sets
let commodities = Set.ofList ["Pencils"; "Pens"]
let sources = Set.ofList ["Detroit"; "Denver"]
let destinations = Set.ofList ["Boston"; "New York"; "Seattle"]
let nodes = sources + destinations
let arcIdxs = combine2 sources destinations
let costIdxs = combine3 commodities sources destinations
let commodityNodeIdxs = combine2 commodities nodes

let capacity = 
    Map.ofList
        [
            (["Detroit"; "Boston"], 100.)
            (["Detroit"; "New York"], 80.)
            (["Detroit"; "Seattle"], 120.)
            (["Denver"; "Boston"], 120.)
            (["Denver"; "New York"], 120.)
            (["Denver"; "Seattle"], 120.)
        ]
    |> Map.map (fun k v -> LinExpr v)

let costs =
    Map.ofList
        [
            (["Pencils"; "Detroit"; "Boston"], 10.)
            (["Pencils"; "Detroit"; "New York"], 20.)
            (["Pencils"; "Detroit"; "Seattle"],  60.)
            (["Pencils"; "Denver";  "Boston"],   40.)
            (["Pencils"; "Denver";  "New York"], 40.)
            (["Pencils"; "Denver";  "Seattle"],  30.)
            (["Pens";    "Detroit"; "Boston"],   20.)
            (["Pens";    "Detroit"; "New York"], 20.)
            (["Pens";    "Detroit"; "Seattle"],  80.)
            (["Pens";    "Denver";  "Boston"],   60.)
            (["Pens";    "Denver";  "New York"], 70.)
            (["Pens";    "Denver";  "Seattle"],  30.)
        ]

let inflow = 
    Map.ofList
        [
            (["Pencils"; "Detroit"],     50.)
            (["Pencils"; "Denver"],      60.)
            (["Pencils"; "Boston"],     -50.)
            (["Pencils"; "New York"],   -50.)
            (["Pencils"; "Seattle"],    -10.)
            (["Pens";    "Detroit"],     60.)
            (["Pens";    "Denver"],      40.)
            (["Pens";    "Boston"],     -40.)
            (["Pens";    "New York"],   -30.)
            (["Pens";    "Seattle"],    -30.)
        ]
    |> Map.map (fun k v -> LinExpr v)



[<EntryPoint>]
let main argv = 

    let env = Environment.create
    let model = Model.create env
    let flow = Model.addVarsForMap model 0.0 INF CONTINUOUS costs 

    let balanceConstraints =
        Model.addConstrs model "balance" commodityNodeIdxs
            (fun [h; j] -> (sum flow [h; "*"; j]) + inflow.[[h; j]] == (sum flow [h; j; "*"]))

    let capacityConstraints =
        Model.addConstrs model "capacity" arcIdxs
            (fun [i; j] -> (sum flow ["*"; i; j] <== capacity.[[i; j]]))
    
    model.Optimize()

    if model.Status = OPTIMAL then
        printfn "Model solved to optimality\n"

        for commodity in commodities do
            printfn "\nOptimal flows for %A:" commodity

            for [source; destination] in arcIdxs do
                if flow.[[commodity; source; destination]].X > 0.0 then
                    printfn "\t%A -> %A\tValue: %A" source destination flow.[[commodity; source; destination]].X

    Console.ReadKey() |> ignore
    0 // return an integer exit code
