module PHPUnit.Framework

open System

#if FABLE_COMPILER
open Fable.Core
open Fable.Core.PhpInterop

[<Emit("$this->assertEquals($0, $1)")>]
let equal expected actual: unit = phpNative()
[<Emit("$this->assertNotEquals($0, $1)")>]
let notEqual expected actual: unit = phpNative()

type Fact() = inherit System.Attribute()

[<Erase>]
type TestCase() =
    [<Emit("$0->assertEquals($1,$2)")>]
    member _.equal expected actual: unit = phpNative()
#else
open Xunit
type FactAttribute = Xunit.FactAttribute

let equal<'T> (expected: 'T) (actual: 'T): unit = Assert.Equal(expected, actual)
let notEqual<'T> (expected: 'T) (actual: 'T) : unit = Assert.NotEqual(expected, actual)

type TestCase() =
    member _.equal expected actual: unit = equal expected actual

#endif

let rec sumFirstSeq (zs: seq<float>) (n: int): float =
   match n with
   | 0 -> 0.
   | 1 -> Seq.head zs
   | _ -> (Seq.head zs) + sumFirstSeq (Seq.skip 1 zs) (n-1)
