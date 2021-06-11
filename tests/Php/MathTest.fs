module Fable.Tests.Math

open PHPUnit.Framework
open Fable.Core

[<AttachMembers>]
type MathTest =
    inherit TestCase

    [<Fact>]
    member this.``test power works`` () =
        let x = 10.0 ** 2.
        x
        |> this.equal 100.0
