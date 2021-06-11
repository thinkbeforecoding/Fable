module System

#if FABLE_COMPILER_PHP
do Fable.Core.JsInterop.emitJsExpr () """
interface IComparable {
    public  function  CompareTo($other);
}
"""

do Fable.Core.JsInterop.emitJsExpr () """
class Attribute {
}
"""

#endif



