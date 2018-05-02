module GurobiSharp
open Gurobi

type Filter =
| Any
| String of string

module Filter =
    let equals (f:Filter) (s:string) =
        match f with
        | Any -> true
        | String st -> st = s

    let create (f:string list) =
        let transform (s:string) =
            match s with
            | "*" -> Filter.Any
            | text -> Filter.String text

        f |> List.map transform

let private keyFilter (f:Filter list) (k:string list) =
    k
    |> List.zip f
    |> List.forall (fun (f, k) -> Filter.equals f k)


let LinExpr (x:float) =
    Gurobi.GRBLinExpr(x)

let addVar (model:Gurobi.GRBModel) lb ub obj t name =
    model.AddVar(lb, ub, obj, t, name)

let addVarForMap (model:Gurobi.GRBModel) lb ub t m =
    m
    |> Map.map (fun k v -> addVar model lb ub v t (k.ToString()))


let addConstr (m:Gurobi.GRBModel) linExpr o r n =
    m.AddConstr(linExpr, o, r, n)

let sum (m:Map<string list, Gurobi.GRBVar>) (f:string list) =
    let filter = Filter.create f
    m
    |> Map.filter (fun k v -> keyFilter filter k)
    |> Map.toArray
    |> Array.map (fun (k, v) -> 1.0 * v)
    |> Array.reduce (+)

let combinations (a: 'a Set) (b: 'b Set) =
    a
    |> Seq.collect (fun x -> b |> Set.map (fun y -> (x, y)))
    |> Set.ofSeq