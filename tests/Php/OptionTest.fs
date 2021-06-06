module Fable.Tests.Option

open PHPUnit.Framework
open Fable.Core

[<AttachMembers>]
type OptionTest = 
    inherit TestCase

    [<Fact>]
    member this.``test defaultArg works`` () =
        let f o = defaultArg o 5
        f (Some 2) |> this.equal 2
        f None |> this.equal 5

    [<Fact>]
    member this.``test Option.defaultValue works`` () =
        let a = Some "MyValue"
        let b = None

        a |> Option.defaultValue "" |> this.equal "MyValue"
        b |> Option.defaultValue "default" |> this.equal "default"

    [<Fact>]
    member this.``test Option.defaultValue works II`` () =
        Some 5 |> Option.defaultValue 4 |> this.equal 5
        None |> Option.defaultValue "foo" |> this.equal "foo"

    [<Fact>]
    member this.``test Option.orElse works`` () =
        Some 5 |> Option.orElse (Some 4) |> this.equal (Some 5)
        None |> Option.orElse (Some "foo") |> this.equal (Some "foo")

    [<Fact>]
    member this.``test Option.defaultWith works`` () =
        Some 5 |> Option.defaultWith (fun () -> 4) |> this.equal 5
        None |> Option.defaultWith (fun () -> "foo") |> this.equal "foo"

    [<Fact>]
    member this.``test Option.orElseWith works`` () =
        Some 5 |> Option.orElseWith (fun () -> Some 4) |> this.equal (Some 5)
        None |> Option.orElseWith (fun () -> Some "foo") |> this.equal (Some "foo")

    [<Fact>]
    member this.``test Option.isSome/isNone works`` () =
        let o1 = None
        let o2 = Some 5
        Option.isNone o1 |> this.equal true
        Option.isSome o1 |> this.equal false
        Option.isNone o2 |> this.equal false
        Option.isSome o2 |> this.equal true

    [<Fact>]
    member this.``test Option.IsSome/IsNone works II`` () =
        let o1 = None
        let o2 = Some 5
        o1.IsNone |> this.equal true
        o1.IsSome |> this.equal false
        o2.IsNone |> this.equal false
        o2.IsSome |> this.equal true

    // [<Fact>]
    // member this.``test Option.iter works`` () =
    //     let mutable res = false
    //     let getOnlyOnce =
    //         let mutable value = Some "Hello"
    //         fun () -> match value with Some x -> value <- None; Some x | None -> None
    //     getOnlyOnce() |> Option.iter (fun s -> if s = "Hello" then res <- true)
    //     this.equal true res

    [<Fact>]
    member this.``test Option.map works`` () =
        let getOnlyOnce =
            let mutable value = Some "Alfonso"
            fun () -> match value with Some x -> value <- None; Some x | None -> None
        getOnlyOnce() |> Option.map ((+) "Hello ") |> this.equal (Some "Hello Alfonso")
        getOnlyOnce() |> Option.map ((+) "Hello ") |> this.equal None
