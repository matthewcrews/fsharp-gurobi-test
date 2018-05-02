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
let commodityNodeIdxs = combinations commodityIdxs nodeIdxs

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

type Comparison =
| Equal
| LessEqual
| GreaterEqual

type ConstraintTuple = {
    Comparison: Comparison
    LHS: Gurobi.GRBLinExpr
    RHS: Gurobi.GRBLinExpr
}


let (:=) (lhs:GRBLinExpr) (rhs:GRBLinExpr) =
    {Comparison = Comparison.Equal; LHS = lhs; RHS = rhs}

let (<==) (lhs:GRBLinExpr) (rhs:GRBLinExpr) =
    {Comparison = Comparison.LessEqual; LHS = lhs; RHS = rhs}

let (>==) (lhs:GRBLinExpr) (rhs:GRBLinExpr) =
    {Comparison = Comparison.GreaterEqual; LHS = lhs; RHS = rhs}

[<EntryPoint>]
let main argv = 

    let env = new GRBEnv()
    let model = new GRBModel(env)
    let flow = addVarsForMap model 0.0 Inf Cont costs |> VarMap.create

    //let balanceConstraints = Constraint.ofSeq
    //    // Auto gen indexes
    //    {for (h, n) in commodityNodeIdxs ->
    //        // Add name from iterator
    //        addConstr model (flow.sum([h; "*"; n]) + (inflow.[[h; n]])) Eq (flow.sum([h; n; "*"])) (sprintf "Node_%A_%A" h n)}


    let expressions = [for (c, n) in commodityNodeIdxs -> flow.sum([c; "*"; n]) + inflow.[[c; n]] := flow.sum([c; n; "*"])]
        //[for (c, n) in commodityNodeIdxs -> (flow.sum([c; "*"; n] + inflow.[[c; n]]) := (flow.sum([c; n; "*"]))]

    let capacityConstraints =
        [for (s, d) in arcIdxs ->
            addConstr model (flow.sum(["*"; s; d])) LessEq (capacity.[[s; d]]) (sprintf "Capacity_%A_%A" s d)]
    
    model.Optimize()

    if model.Status =  GRB.Status.OPTIMAL then
        printfn "Model solved to optimality\n"

        for commodityIdx in commodityIdxs do
            printfn "\nOptimal flows for %A:" commodityIdx

            for (sourceIdx, destIdx) in arcIdxs do
                if flow.[[commodityIdx; sourceIdx; destIdx]].X > 0.0 then
                    printfn "\t%A -> %A\tValue: %A" sourceIdx destIdx flow.[[commodityIdx; sourceIdx; destIdx]].X

    Console.ReadKey() |> ignore
    0 // return an integer exit code
