// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open Gurobi
open GurobiSharp


let commodityIdx = Set.ofList ["Penciles"; "Pens"]
let sourceIdx = Set.ofList ["Detroit"; "Denver"]
let destinationIdx = Set.ofList ["Boston"; "New York"; "Seattle"]
let nodeIdx = sourceIdx + destinationIdx
let arcIdx = combinations sourceIdx destinationIdx
let costIdx = combinations commodityIdx arcIdx

let capacityMap = 
    Map.ofList
        [
            (["Detroit"; "Boston"], 100.)
            (["Detroit"; "New York"], 80.)
            (["Detroit"; "Seattle"], 120.)
            (["Denver"; "Boston"], 120.)
            (["Denver"; "New York"], 120.)
            (["Denver"; "Seattle"], 120.)
        ]


let costMap =
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


let inflowMap = 
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

module Gurobi =


[<EntryPoint>]
let main argv = 
    printfn "%A" argv

    let capacity = 
        capacityMap
        |> Map.map (fun k v -> LinExpr v)

    let inflow =
        inflowMap
        |> Map.map (fun k v -> LinExpr v)

    let env = new GRBEnv()
    let m = new GRBModel(env)
    let flow = 
        costMap
        |> addVarForMap m 0.0 GRB.INFINITY GRB.CONTINUOUS

    for (s, d) in arcIdx do
        addConstr m (sum flow ["*"; s; d]) GRB.LESS_EQUAL (capacity.[[s; d]]) (sprintf "Capacity_%A_%A" s d) 
        |> ignore

    for (h, n) in (combinations commodityIdx nodeIdx) do
        addConstr m ((sum flow [h; "*"; n]) + (inflow.[[h; n]])) GRB.EQUAL (sum flow [h; n; "*"]) (sprintf "Node_%A_%A" h n) 
        |> ignore
    
    m.Optimize()

    if m.Status =  GRB.Status.OPTIMAL then
        printfn "Hello world"

    0 // return an integer exit code
