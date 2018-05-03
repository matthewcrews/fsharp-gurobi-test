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

let addVarsForMap (model:Gurobi.GRBModel) lb ub t m =
    m
    |> Map.map (fun k v -> addVar model lb ub v t (k.ToString()))


type LinExprComparison =
| Equal
| LessEqual
| GreaterEqual

type ConstraintTuple = {
    Comparison: LinExprComparison
    LHS: Gurobi.GRBLinExpr
    RHS: Gurobi.GRBLinExpr
}

let (:=) (lhs:GRBLinExpr) (rhs:GRBLinExpr) =
    {Comparison = LinExprComparison.Equal; LHS = lhs; RHS = rhs}

let (<==) (lhs:GRBLinExpr) (rhs:GRBLinExpr) =
    {Comparison = LinExprComparison.LessEqual; LHS = lhs; RHS = rhs}

let (>==) (lhs:GRBLinExpr) (rhs:GRBLinExpr) =
    {Comparison = LinExprComparison.GreaterEqual; LHS = lhs; RHS = rhs}

let addConstr (model:Gurobi.GRBModel) (setName:string)  (constraintFunc:string list -> ConstraintTuple) =
    let name = 
    model.AddConstr(linExpr, o, r, n)

let sum (m:Map<string list, Gurobi.GRBVar>) (f:string list) =
    let filter = Filter.create f

    let vars = m |> Map.filter (fun k v -> keyFilter filter k)
        
    if vars.IsEmpty then
        LinExpr 0.0
    else
        vars
        |> Map.toArray
        |> Array.map (fun (k, v) -> 1.0 * v)
        |> Array.reduce (+)


type VarMap = {Mapping: Map<string list, GRBVar>} with
    static member create m =
        {Mapping = m}

    member this.sum(filter) =
        sum this.Mapping filter

    member this.Item
        with get(x) = this.Mapping.[x]

let combinations (a: 'a Set) (b: 'b Set) =
    a
    |> Seq.collect (fun x -> b |> Set.map (fun y -> (x, y)))
    |> Set.ofSeq

let Inf = GRB.INFINITY
let Eq = GRB.EQUAL
let LessEq = GRB.LESS_EQUAL
let GreaterEq = GRB.GREATER_EQUAL
let Cont = GRB.CONTINUOUS