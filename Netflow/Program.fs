// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open Gurobi

type F =
| Any
| Str of string

let LinExpr (x:float) =
    Gurobi.GRBLinExpr(x)

let AddConstr (m:Gurobi.GRBModel) l o r n =
    m.AddConstr(l, o, r, n)
    ()

module Filter =
    let equals (f:F) (s:string) =
        match f with
        | Any -> true
        | Str st -> st = s

module GRBMap =
    let private keyFilter (f:F list) (k:string list) =
        k
        |> List.zip f
        |> List.forall (fun (f, k) -> Filter.equals f k)

    let sum (f:F list) (m:Map<string list, Gurobi.GRBVar>) =
        m
        |> Map.filter (fun k v -> keyFilter f k)
        |> Map.toArray
        |> Array.map (fun (k, v) -> 1.0 * v)
        |> Array.reduce (+)

let combinations (a: 'a Set) (b: 'b Set) =
    a
    |> Seq.collect (fun x -> b |> Set.map (fun y -> (x, y)))
    |> Set.ofSeq


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
    let addVar (m:Gurobi.GRBModel) lb ub o t name =
        m.AddVar(lb, ub, o, t, name)

[<EntryPoint>]
let main argv = 
    printfn "%A" argv

    let env = new GRBEnv()
    let m = new GRBModel(env)
    let flow = 
        costMap
        |> Map.map (fun k v -> Gurobi.addVar m 0.0 GRB.INFINITY v GRB.CONTINUOUS (k.ToString()))

    for (s, d) in arcIdx do
        AddConstr m (GRBMap.sum [F.Any; F.Str s; F.Str d] flow) GRB.LESS_EQUAL (LinExpr capacityMap.[[s; d]]) (sprintf "Capacity_%A_%A" s d)
        ()

    for (h, n) in (combinations commodityIdx nodeIdx) do
        AddConstr m ((GRBMap.sum [F.Str h; F.Any; F.Str n] flow) + (LinExpr inflowMap.[[h; n]])) GRB.EQUAL (GRBMap.sum [F.Str h; F.Str n; F.Any] flow) (sprintf "Node_%A_%A" h n)
        ()
    
    m.Optimize()

    if m.Status =  GRB.Status.OPTIMAL then
        printfn "Hello world"

    0 // return an integer exit code
