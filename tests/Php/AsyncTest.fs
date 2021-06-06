module Fable.Tests.Async

open System
open PHPUnit.Framework
open Fable.Core

type DisposableAction(f) =
    interface IDisposable with
        member __.Dispose() = f()

let sleepAndAssign token res =
    Async.StartImmediate(async {
        do! Async.Sleep 200
        res := true
    }, token)

let successWork: Async<string> = Async.FromContinuations(fun (onSuccess,_,_) -> onSuccess "success")
let errorWork: Async<string> = Async.FromContinuations(fun (_,onError,_) -> onError (exn "error"))
let cancelWork: Async<string> = Async.FromContinuations(fun (_,_,onCancel) ->
        System.OperationCanceledException("cancelled") |> onCancel)

[<AttachMembers>]
type AsyncTest =
    inherit TestCase

    [<Fact>]
    member this.``test Simple async translates without exception`` () =
        async { return () }
        |> Async.StartImmediate


    [<Fact>]
    member this.``test Async while binding works correctly`` () =
        let mutable result = 0
        async {
            while result < 10 do
                result <- result + 1
        } |> Async.StartImmediate
        this.equal result 10

    [<Fact>]
    member this.``test Async for binding works correctly`` () =
        let inputs = [|1; 2; 3|]
        let result = ref 0
        async {
            for inp in inputs do
                result := !result + inp
        } |> Async.StartImmediate
        this.equal !result 6

    [<Fact>]
    member this.``test Async exceptions are handled correctly`` () =
        let result = ref 0
        let f shouldThrow =
            async {
                try
                    if shouldThrow then failwith "boom!"
                    else result := 12
                with _ -> result := 10
            } |> Async.StartImmediate
            !result
        f true + f false |> this.equal 22

    [<Fact>]
    member this.``test Simple async is executed correctly`` () =
        let result = ref false
        let x = async { return 99 }
        async {
            let! x = x
            let y = 99
            result := x = y
        }
        |> Async.StartImmediate
        this.equal !result true

    [<Fact>]
    member this.``test async use statements should dispose of resources when they go out of scope`` () =
        let isDisposed = ref false
        let step1ok = ref false
        let step2ok = ref false
        let resource = async {
            return new DisposableAction(fun () -> isDisposed := true)
        }
        async {
            use! r = resource
            step1ok := not !isDisposed
        }
        //TODO: RunSynchronously would make more sense here but in JS I think this will be ok.
        |> Async.StartImmediate
        step2ok := !isDisposed
        (!step1ok && !step2ok) |> this.equal true

    [<Fact>]
    member this.``test Try ... with ... expressions inside async expressions work the same`` () =
        let result = ref ""
        let throw() : unit =
            raise(exn "Boo!")
        let append(x) =
            result := !result + x
        let innerAsync() =
            async {
                append "b"
                try append "c"
                    throw()
                    append "1"
                with _ -> append "d"
                append "e"
            }
        async {
            append "a"
            try do! innerAsync()
            with _ -> append "2"
            append "f"
        } |> Async.StartImmediate
        this.equal !result "abcdef"

    // Disable this test for dotnet as it's failing too many times in Appveyor
    #if FABLE_COMPILER

    [<Fact>]
    member this.``test async cancellation works`` () =
        async {
            let res1, res2, res3 = ref false, ref false, ref false
            let tcs1 = new System.Threading.CancellationTokenSource(50)
            let tcs2 = new System.Threading.CancellationTokenSource()
            let tcs3 = new System.Threading.CancellationTokenSource()
            sleepAndAssign tcs1.Token res1
            sleepAndAssign tcs2.Token res2
            sleepAndAssign tcs3.Token res3
            tcs2.Cancel()
            tcs3.CancelAfter(1000)
            do! Async.Sleep 500
            this.equal false !res1
            this.equal false !res2
            this.equal true !res3
        } |> Async.StartImmediate

    [<Fact>]
    member this.``test CancellationTokenSourceRegister works`` () =
        async {
            let mutable x = 0
            let res1 = ref false
            let tcs1 = new System.Threading.CancellationTokenSource(50)
            let foo = tcs1.Token.Register(fun () ->
                x <- x + 1)
            sleepAndAssign tcs1.Token res1
            do! Async.Sleep 500
            this.equal false !res1
            this.equal 1 x
        } |> Async.StartImmediate
    #endif

    [<Fact>]
    member this.``test Async StartWithContinuations works`` () =
        let res1, res2, res3 = ref "", ref "", ref ""
        Async.StartWithContinuations(successWork, (fun x -> res1 := x), ignore, ignore)
        Async.StartWithContinuations(errorWork, ignore, (fun x -> res2 := x.Message), ignore)
        Async.StartWithContinuations(cancelWork, ignore, ignore, (fun x -> res3 := x.Message))
        this.equal "success" !res1
        this.equal "error" !res2
        this.equal "cancelled" !res3

    [<Fact>]
    member this.``test Async.Catch works`` () =
        let assign res = function
            | Choice1Of2 msg -> res := msg
            | Choice2Of2 (ex: Exception) -> res := "ERROR: " + ex.Message
        let res1 = ref ""
        let res2 = ref ""
        async {
            let! x1 = successWork |> Async.Catch
            assign res1 x1
            let! x2 = errorWork |> Async.Catch
            assign res2 x2
        } |> Async.StartImmediate
        this.equal "success" !res1
        this.equal "ERROR: error" !res2

    [<Fact>]
    member this.``test Async.Ignore works`` () =
        let res = ref false
        async {
            do! successWork |> Async.Ignore
            res := true
        } |> Async.StartImmediate
        this.equal true !res
