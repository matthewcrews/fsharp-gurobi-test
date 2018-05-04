# Gurobi Fsharp Library
The purpose of this library was to create an idiomatic F# wrapper around the Gurobi .NET library. The reason for this is that F# is a functional-first language and the composition model favors functions over methods. F# is perfectly capable of working with objects and their methods but I enjoy the functional composition model so I decided to create my own wrapper around the Gurobi object model to enable that.

## Design Note
Gurobi provides a Python library for their solver which allows for simple and elegant building of models. I am purposefully emulating the style of that library. I wanted to bring the beauty of that library for F#. Hats off to the Gurobi team for providing an excellent blueprint to emulate.

