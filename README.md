# Gurobi Fsharp Library

The purpose of this library was to create an idiomatic F# wrapper around the Gurobi .NET library. The reason for this is that F# is a functional-first language and the composition model favors functions over methods. F# is perfectly capable of working with objects and their methods but I enjoy the functional composition model so I decided to create my own wrapper around the Gurobi object model to enable that.

## Design Note

Gurobi provides a Python library for their solver which allows for simple and elegant building of models. I am purposefully emulating the style of that library. I wanted to bring the beauty of that library for F#. Hats off to the Gurobi team for providing an excellent blueprint to emulate.

> ***Note*** This is a work in progress. Support will expand as I use the tool more and need to integrate additional capabilities.

## Netflow Example

The following shows an example of a network flow problem provided by Gurobi and modeled in Python. The full formulation can be [found here](http://www.gurobi.com/documentation/8.0/examples/netflow_py.html). In this example I am just comparing and contrasting the Python and F# formulation methods.

> ***Note***: All Python code is copyrighted by Gurobi Optimization, LLC

### Creating a model

#### Python

In Python the creation of the model and decision variables is quite straightforward.

```python
# Create optimization model
m = Model('netflow')

# Create variables
flow = m.addVars(commodities, arcs, obj=cost, name="flow")
```

#### F#

In F# we have a similar syntax but instead of `flow` being a `Dictionary` of decision variables, we produce a `Map<string list, GRBDecVar>` which is essentially the same for our purposes.

```fsharp
// Create a new instance of the Gurobi Environment object
// to host models
let env = Environment.create

// Create a new model with the environment variable
let m = Model.create env "netflow"

// Create a Map of decision variables for the model
// addVarsForMap <model> <lower bound> <upper bound> <type> <input Map>
let flow = Model.addVarsForMap m 0.0 INF CONTINUOUS costs
```

Instead of using the methods on the object, functions have been provided which operate on the values that are passed in. This is more idiomatic for F#. The `Model` module in the library hosts all of the functions for working with objects of type `Model`.

The `Model.adddVarsForMap` takes a `Map<string list, float>` and produces a `Map<string list, GRBDecVar>` for the modeler to work work. This is similar to how the Python tuples are working in the `gurobipy` library. Instead of indexing into a Python dictionary with `tuples`, F# uses a `string list` as the index.

### Adding Constraints

#### Python
The `gurobipy` library offers a succinct way expressing a whole set of constraints by using generators. There is additional magic going on under the hood though that may not be obvious at first. The following method generates a set of constraints for each element in `arcs` but also creates a meaningful constraint name. The prefix for the constraint name is the last argument of the method (`"cap"` in this instance).

```python
# Arc capacity constraints
m.addConstrs(
    (flow.sum('*',i,j) <= capacity[i,j] for i,j in arcs), "cap")
```


There is also something special going on with the `flow.sum('*',i,j)` syntax. `flow` is a dictionary which is indexed by a 3 element tuple. What this `sum()` method is doing is summing across all elements in the dictionary which fit the pattern. The `*` symbol is a wildcard and will match against any element. This is a powerful way to sum across dimensions of the optimization model.

#### F#

In F# we can do something similar but instead of having a generator we pass in a lambda to create the constraints.

```fsharp
let capacityConstraints =
    Model.addConstrs m "capacity" arcs
        (fun [i; j] -> (sum flow ["*"; i; j] <== capacity.[[i; j]]))
```

The function `Model.addConstrs` takes a `model` object as its first argument (`m` in this case), the prefix for what the constraints are going to be named (`"capacity"` in this case), and the set of indices the constraints will be created over, `arcs` in this case. The key point is that the types of the indices must match the input type of the lambda.

The `addConstrs` function will iterate through each of the indices in the set, create a constraint from the lambda that was passed, and name the constraint approriatly. If the first elemens of the `arcs` set was `["Detroit"; "Boston"]` then the name of the first constraint would be `capacity_Detroit_Boston`.