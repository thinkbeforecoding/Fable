module Fable.Tests.Fn

open PHPUnit.Framework
open Fable.Core

let add(a, b, cont) =
    cont(a + b)

let square(x, cont) =
    cont(x * x)

let sqrt(x, cont) =
    cont(sqrt(x))

let pythagoras(a, b, cont) =
    square(a, (fun aa ->
        square(b, (fun bb ->
            add(aa, bb, (fun aabb ->
                sqrt(aabb, (fun result ->
                    cont(result)
                ))
            ))
        ))
    ))

[<AttachMembers>]
type FnTest =
    inherit TestCase

    [<Fact>]
    member this.``test add works`` () =
        let result = add(10., 2., id)
        result
        |> this.equal 12.

    [<Fact>]
    member this.``test sqrt works`` () =
        let result = sqrt(100., id)
        result
        |> this.equal 10.

    [<Fact>]
    member this.``test pythagoras works`` () =
        let result = pythagoras(10.0, 2.1, id)
        result
        |> this.equal 10.218121158021175

    [<Fact>]
    member this.``test nonlocal works`` () =
        let mutable value = 0

        let fn () =
            value <- 42

        fn ()

        value |> this.equal 42
