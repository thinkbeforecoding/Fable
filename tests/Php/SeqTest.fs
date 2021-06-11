module Fable.Tests.Seqs

open Fable.Core
open PHPUnit.Framework

do JsInterop.emitJsStatement() "require_once 'vendor/autoload.php'"

[<Emit "echo $0">]
let echo (s) : unit = jsNative

[<Emit "var_dump($0)">]
let vardump (s: 't) : unit = jsNative


let sumFirstTwo (zs: seq<float>) =
    let second = Seq.skip 1 zs |> Seq.head
    let first = Seq.head zs
    first + second

[<AttachMembers>]
type SeqTest =
    inherit TestCase

    [<Fact>]
    member this.``test Seq.empty works`` () =
        let xs = Seq.empty<int>
        Seq.length xs
        |> this.equal 0

    [<Fact>]
    member this.``test Seq.length works`` () =
        let xs = [1.; 2.; 3.; 4.]
        Seq.length xs
        |> this.equal 4

    [<Fact>]
    member this.``test Seq.map works`` () =
        let xs = [1; 2; 3; 4]
        xs
        |> Seq.map string
        |> List.ofSeq
        |> this.equal ["1"; "2"; "3"; "4"]


    [<Fact>]
    member this.``test Seq.singleton works`` () =
        let xs = Seq.singleton 42
        xs
        |> List.ofSeq
        |> this.equal [42]

    [<Fact>]
    member this.``test Seq.collect works`` () =
        let xs = ["a"; "fable"; "bar" ]
        xs
        |> Seq.ofList
        |> Seq.collect (fun a -> [a.Length])
        |> List.ofSeq
        |> this.equal [1; 5; 3]

    [<Fact>]
    member this.``test Seq.collect works II"`` () =
        let xs = [[1.]; [2.]; [3.]; [4.]]
        let ys = xs |> Seq.collect id

        sumFirstTwo ys
        |> this.equal 3.

        let xs1 = [[1.; 2.]; [3.]; [4.; 5.; 6.;]; [7.]]
        let ys1 = xs1 |> Seq.collect id
        sumFirstSeq ys1 5
        |> this.equal 15.

    [<Fact>]
    member this.``test Seq.collect works with Options`` () =
        let xss = [[Some 1; Some 2]; [None; Some 3]]
        Seq.collect id xss
        |> Seq.sumBy (function
            | Some n -> n
            | None -> 0
        )
        |> this.equal 6

    [<Fact>]
    member this.``test Seq.length works with seq expression`` () =

        let xss = [[ Some 1;  Some 2]; [ None;  Some 4]]

        let s = 
            seq {
                for xs in xss do
                    for x in xs do
                        x
            }

        s
        |> Seq.length
        |> this.equal 4
