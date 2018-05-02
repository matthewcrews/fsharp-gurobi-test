// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open Gurobi
open GurobiSharp
open System


// Define index sets
let commodityIdxs = Set.ofList ["Pencils"; "Pens"]
let sourceIdxs = Set.ofList ["Detroit"; "Denver"]
let destinationIdxs = Set.ofList ["Boston"; "New York"; "Seattle"]
let nodeIdxs = sourceIdxs + destinationIdxs
let arcIdxs = combinations sourceIdxs destinationIdxs
let costIdxs = combinations commodityIdxs arcIdxs

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

    let env = new GRBEnv()
    let m = new GRBModel(env)
    let flow = addVarsForMap m 0.0 Inf Cont costs

    let balanceConstraints =
        [for (h, n) in (combinations commodityIdxs nodeIdxs) ->
            addConstr m ((sum flow [h; "*"; n]) + (inflow.[[h; n]])) Eq (sum flow [h; n; "*"]) (sprintf "Node_%A_%A" h n)]

    let capacityConstraints =
        [for (s, d) in arcIdxs ->
            addConstr m (sum flow ["*"; s; d]) LessEq (capacity.[[s; d]]) (sprintf "Capacity_%A_%A" s d)]
    
    m.Optimize()

    if m.Status =  GRB.Status.OPTIMAL then
        printfn "Model solved to optimality\n"

        for commodityIdx in commodityIdxs do
            printfn "\nOptimal flows for %A:" commodityIdx

            for (sourceIdx, destIdx) in arcIdxs do
                if flow.[[commodityIdx; sourceIdx; destIdx]].X > 0.0 then
                    printfn "\t%A -> %A\tValue: %A" sourceIdx destIdx flow.[[commodityIdx; sourceIdx; destIdx]].X
                ()

    Console.ReadKey() |> ignore
    0 // return an integer exit code
