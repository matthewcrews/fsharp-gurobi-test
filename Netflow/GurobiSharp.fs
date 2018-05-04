module GurobiSharp
open Gurobi


type internal Filter =
| Any
| String of string

module internal Filter =
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

let internal keyFilter (f:Filter list) (k:string list) =
    k
    |> List.zip f
    |> List.forall (fun (f, k) -> Filter.equals f k)


type LinExprComparison =
| Equal
| LessEqual
| GreaterEqual

type ConstraintTuple = {
    Comparison: LinExprComparison
    LHS: Gurobi.GRBLinExpr
    RHS: Gurobi.GRBLinExpr
}

let LinExpr (x:float) =
    Gurobi.GRBLinExpr(x)

module Environment =
    let create =
        new GRBEnv()

module Model =
    let create (env:Gurobi.GRBEnv) =
        new GRBModel(env)

    let addVar (model:Gurobi.GRBModel) lb ub obj t name =
        model.AddVar(lb, ub, obj, t, name)

    let addVarsForMap (model:Gurobi.GRBModel) lb ub t m =
        m
        |> Map.map (fun k v -> addVar model lb ub v t (k.ToString()))

    let addConstr (model:Gurobi.GRBModel) (name:string) (constraintExpr: ConstraintTuple) =
        let sense = 
            match constraintExpr.Comparison with
            | Equal -> GRB.EQUAL
            | LessEqual -> GRB.LESS_EQUAL
            | GreaterEqual -> GRB.GREATER_EQUAL

        model.AddConstr(constraintExpr.LHS, sense, constraintExpr.RHS, name)

    let addConstrs (model:GRBModel) (setName:string) (setIndexes:string list list) (constraintExpr: string list -> ConstraintTuple) =
        let constraintName (sn:string) (idx:string list) = ([sn] @ idx) |> List.fold (+) "_"

        setIndexes
        |> List.map (fun x -> x, addConstr model (constraintName setName x) (constraintExpr x))
        |> Map.ofList


// Operators

let (==) (lhs:GRBLinExpr) (rhs:GRBLinExpr) =
    {Comparison = LinExprComparison.Equal; LHS = lhs; RHS = rhs}

let (<==) (lhs:GRBLinExpr) (rhs:GRBLinExpr) =
    {Comparison = LinExprComparison.LessEqual; LHS = lhs; RHS = rhs}

let (>==) (lhs:GRBLinExpr) (rhs:GRBLinExpr) =
    {Comparison = LinExprComparison.GreaterEqual; LHS = lhs; RHS = rhs}



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

// Gurobi Constants

let INF = GRB.INFINITY
let CONTINUOUS = GRB.CONTINUOUS
let OPTIMAL = GRB.Status.OPTIMAL