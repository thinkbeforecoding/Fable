module Fable.Tests.Loops

open PHPUnit.Framework
open Fable.Core

[<AttachMembers>]
type LoopsTest =
    inherit TestCase

    [<Fact>]
    member this.``test For-loop upto works`` () =
        let mutable result = 0

        for i = 0 to 10 do
            result <- result + i
        done

        result
        |> this.equal 55

    [<Fact>]
    member this.``test For-loop upto minus one works`` () =
        let mutable result = 0

        for i = 0 to 10 - 1 do
            result <- result + i
        done

        result
        |> this.equal 45

    [<Fact>]
    member this.``test For-loop downto works`` () =
        let mutable result = 0
        for i = 10 downto 0 do
            result <- result + i

        result
        |> this.equal 55
