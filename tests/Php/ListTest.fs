module Fable.Tests.ListTests

open PHPUnit.Framework
open Fable.Core

[<AttachMembers>]
type ListTest =
    inherit TestCase

    [<Fact>]
    member this.``test List.empty works`` () =
        let xs = List.empty<int>
        List.length xs
        |> this.equal 0

    [<Fact>]
    member this.``test List.length works`` () =
        let xs = [1.; 2.; 3.; 4.]
        List.length xs
        |> this.equal 4

    [<Fact>]
    member this.``test List.map works`` () =
        let xs = [1; 2; 3; 4]
        xs
        |> List.map string
        |> this.equal ["1"; "2"; "3"; "4"]


    [<Fact>]
    member this.``test List.singleton works`` () =
        let xs = List.singleton 42
        xs
        |> this.equal [42]


    [<Fact>]
    member this.``test List.collect works`` () =
        let xs = ["a"; "fable"; "bar" ]
        xs
        |> List.collect (fun a -> [a.Length])
        |> this.equal [1; 5; 3]
