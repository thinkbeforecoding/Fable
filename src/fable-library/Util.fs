module Util

open Fable.Core.PhpInterop
open Fable.Core.JsInterop
open System.Collections
open System.Collections.Generic

[<Interface>]
type Iterator<'T> =
    abstract current : unit -> 'T
    abstract key : unit -> obj
    abstract next : unit -> unit
    abstract rewind  : unit -> unit
    abstract valid : unit ->bool

type Enumerator<'T>(iter: Iterator<'T>) =
    let mutable started = false

    interface IEnumerator with
        member _.Reset() =
            started <- false
            iter.rewind()
        member _.MoveNext() = 
            if not started then
              started <- true
              iter.valid()
            else
              iter.next()
              iter.valid()

        member _.Current = box (iter.current())
        

    interface IEnumerator<'T> with
        member _.Current = iter.current()
        member _.Dispose() = ()

let getEnumerator (o: obj) : IEnumerator<'T> =
    if methodExists o "GetEnumerator"  then
        emitJsStatement o "$0->GetEnumerator()"
    else
        new Enumerator<'T>(o :?> Iterator<'T>) :> IEnumerator<'T>

let int32ToString(i: int) =
    emitJsExpr i "strval($0)" 


