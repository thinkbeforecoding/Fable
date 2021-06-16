namespace System

open Fable.Core

#if FABLE_COMPILER_PHP

[<Erase>]
module Definition = 
    do JsInterop.emitJsExpr () """
interface IComparable {
    public  function  CompareTo($other);
}"""
    
[<CompiledName("Attribute")>]
type FableAttribute = class end


#endif





