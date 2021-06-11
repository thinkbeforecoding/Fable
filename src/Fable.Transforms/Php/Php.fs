namespace rec Fable.AST.Php


type PhpConst =
    | PhpConstNumber of float
    | PhpConstString of string
    | PhpConstBool of bool
    | PhpConstNull

type PhpArrayIndex =
    | PhpArrayNoIndex
    | PhpArrayInt of int
    | PhpArrayString of string


type Capture =
    | ByValue of string
    | ByRef of string

type PhpIdentity =
    { Namespace: string option 
      Name: string }

type PhpMemberKind =
    | PhpSelfMember
    | PhpParentMember
    | PhpIdentMember of PhpIdentity
    | PhpExprMember of PhpExpr

type PhpMemberType =
    | PhpMethod 
    | PhpField 

type PhpArgKind =
    | PhpMandatoryArg
    | PhpOptionalArg

type PhpArg =
    { Name: string 
      Kind: PhpArgKind }


and PhpExpr =
      // Php Variable name (without the $)
    | PhpVar of string
      // Php Identifier for functions and class names
    | PhpIdent of PhpIdentity
    | PhpConst of PhpConst
    | PhpUnaryOp of string * PhpExpr
    | PhpBinaryOp of string *PhpExpr * PhpExpr
    | PhpMember of PhpMemberKind * name: string * PhpMemberType
    | PhpArrayAccess of PhpExpr * PhpExpr
    | PhpNew of PhpIdentity * args:PhpExpr list
    | PhpNewArray of args: (PhpArrayIndex * PhpExpr) list
    | PhpCall of f: PhpExpr * args: PhpExpr list
    | PhpTernary of gard: PhpExpr * thenExpr: PhpExpr * elseExpr: PhpExpr
    | PhpAnonymousFunc of args: string list * uses: Capture list * body: PhpStatement list
    | PhpAnonymousClass of PhpClass
    | PhpMacro of macro: string * args: PhpExpr list
   
and PhpStatement =
    | PhpReturn of PhpExpr
    | PhpDo of PhpExpr
    | PhpSwitch of PhpExpr * (PhpExpr option * PhpStatement list) list
    | PhpBreak
    | PhpStaticVar of string * PhpConst option
    | PhpIf of guard: PhpExpr * thenCase: PhpStatement list * elseCase: PhpStatement list
    | PhpThrow of PhpIdentity * PhpExpr list
    | PhpTryCatch of body: PhpStatement list * catch: (string * PhpStatement list) option * finallizer: PhpStatement list 
    | PhpWhileLoop of guard: PhpExpr * body: PhpStatement list
    | PhpFor of ident: string * start: PhpExpr * limit: PhpExpr * isUp: bool * body: PhpStatement list

and PhpFun = 
    { Name: string
      Args: PhpArg list
      Body: PhpStatement list
    }
and PhpMethod =
    { Fun: PhpFun
      Static: bool}

and PhpClass = 
    { Fields: string list;
      Methods: PhpMethod list
      BaseType: PhpIdentity option
      Interfaces: PhpType list
    }

and PhpType =
    { Identity: PhpIdentity
      Class: PhpClass 
      Abstract: bool
    }

type Comment = string

type PhpDecl =
    | PhpFun of PhpFun
    | PhpDeclValue of name:string * PhpExpr
    | PhpAction of PhpStatement list
    | PhpType of PhpType


type PhpNamespace =
    { Namespace: string option
      Decls:  PhpDecl list
    }

type PhpFile =
    { Filename: string
      //Namespace: string
      Uses: PhpType list
      Namespaces: PhpNamespace list
      }


