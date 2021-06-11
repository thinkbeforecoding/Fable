module Util

open Fable.Core.PhpInterop
open Fable.Core.JsInterop
open System.Collections
open System.Collections.Generic
open Fable.Core


[<Interface>]
type Iterator<'T> =
    abstract current : unit -> 'T
    abstract key : unit -> obj
    abstract next : unit -> unit
    abstract rewind  : unit -> unit
    abstract valid : unit ->bool

[<Emit "is_array($0)">]
let isArray (o: obj) : bool = jsNative
    

[<Emit("new \ArrayIterator($0)")>]
let newArrayIterator (o: obj) : Iterator<'T> = jsNative

[<Interface>]
type IteratorAggregate<'T> =
    abstract getIterator: unit -> Iterator<'T>

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
    elif isArray o then
        new Enumerator<_>( newArrayIterator o) :> IEnumerator<_>

    else
        new Enumerator<'T>((o :?> IteratorAggregate<'T>).getIterator()) :> IEnumerator<'T>

let int32ToString(i: int) =
    emitJsExpr i "strval($0)" 


let toIterator (e: IEnumerator<'T>) =
    try
        while e.MoveNext() do
            JsInterop.emitJsStatement (e.Current) "yield $0"
            
    finally
        e.Dispose()
