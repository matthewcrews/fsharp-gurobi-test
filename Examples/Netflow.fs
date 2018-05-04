module Examples.Netflow
open Gurobi.Fsharp
open ListHelpers


let runExample =

    // Define index sets
    let commodities = Set.ofList ["Pencils"; "Pens"]
    let sources = Set.ofList ["Detroit"; "Denver"]
    let destinations = Set.ofList ["Boston"; "New York"; "Seattle"]
    let nodes = sources + destinations
    let commodityNodeIdxs = combine2 commodities nodes

    let capacity = 
        Map.linExprMap
            [
                (["Detroit"; "Boston"], 100.)
                (["Detroit"; "New York"], 80.)
                (["Detroit"; "Seattle"], 120.)
                (["Denver"; "Boston"], 120.)
                (["Denver"; "New York"], 120.)
                (["Denver"; "Seattle"], 120.)
            ]

    let arcs = capacity |> Map.toList |> List.map fst

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
        Map.linExprMap
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

    let env = Environment.create
    let m = Model.create env "netflow"
    let flow = Model.addVarsForMap m 0.0 INF CONTINUOUS costs 

    let capacityConstraints =
        Model.addConstrs m "capacity" arcs
            (fun [i; j] -> (sum flow ["*"; i; j] <== capacity.[[i; j]]))

    let balanceConstraints =
        Model.addConstrs m "balance" commodityNodeIdxs
            (fun [h; j] -> (sum flow [h; "*"; j]) + inflow.[[h; j]] == (sum flow [h; j; "*"]))
    
    m.Optimize()

    if m.Status = OPTIMAL then
        printfn "Model solved to optimality\n"

        for commodity in commodities do
            printfn "\nOptimal flows for %A:" commodity

            for [source; destination] in arcs do
                if flow.[[commodity; source; destination]].X > 0.0 then
                    printfn "\t%A -> %A\tValue: %A" source destination flow.[[commodity; source; destination]].X