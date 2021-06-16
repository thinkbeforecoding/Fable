module Fable.Transforms.PhpPrinter

open System
open System.IO
open Fable.AST.Php

module Output =
    type Writer =
        { Writer: TextWriter
          Indent: int
          Precedence: int
          UsedTypes: PhpType Set
          //CurrentNamespace: string option
          }

    let indent ctx =
        { ctx with Indent = ctx.Indent + 1}

    module Writer =
        let create w =
            { Writer = w; Indent = 0; Precedence = Int32.MaxValue; UsedTypes = Set.empty  }

    let writeIndent  ctx =
        for _ in 1 .. ctx.Indent do
            ctx.Writer.Write("    ")

    let write ctx txt =
        ctx.Writer.Write(txt: string)


    let writeln ctx txt =
         ctx.Writer.WriteLine(txt: string)

    let writei ctx txt =
        writeIndent ctx
        write ctx txt

    let writeiln ctx txt =
        writeIndent ctx
        writeln ctx txt

    let writeVarList ctx vars =
        let mutable first = true
        for var in vars do
            if first then
                first <- false
            else
                write ctx ", "
            write ctx "$"
            write ctx var
    let writeUseList ctx vars =
        let mutable first = true
        for var in vars do
            if first then
                first <- false
            else
                write ctx ", "
            match var with
            | ByValue v ->
                write ctx "$"
                write ctx v
            | ByRef v ->
                write ctx "&$"
                write ctx v

    module Precedence =
        let binary =
            function
            | "instanceof" -> 2
            | "*" | "/" | "%"         -> 3
            | "+" | "-" | "."         -> 4
            | "<<" | ">>" |  ">>>"    -> 5
            | "<" | "<=" | ">=" | ">" -> 7
            | "==" | "!=" | "==="
            | "!==" | "<>" | "<=>"    -> 7
            | "&" -> 8
            | "^" -> 9
            | "|" -> 10
            | "&&" -> 11
            | "||" -> 12
            | "??" -> 13
            | "="  -> 14
            | op -> failwithf "Unknown binary operator %s" op


        let unary =
            function
            | "new" | "clone" -> 0
            | "!" -> 2
            | "-" -> 4
            | "~~~"
            | "&" -> 8
            | "(void)" -> 10
            | op -> failwithf "Unknown unary operator %s" op

        let _new = 0
        let instanceOf = 1
        let ternary = 14
        let assign = 15
        let arrow = 1


        let clear ctx = { ctx with Precedence = Int32.MaxValue}

    let writeIdent ctx (id: PhpIdentity) =
        match id.Namespace with
        | Some ns ->
            write ctx @"\"
            write ctx ns
            if ns <> "" then
                write ctx @"\"
        | None -> ()
        write ctx id.Name

    let withPrecedence ctx prec f =
        let useParens = prec > ctx.Precedence || (prec = 14 && ctx.Precedence = 14)
        let subCtx = { ctx with Precedence = prec }
        if useParens then
            write subCtx "("

        f subCtx

        if useParens then
            write subCtx ")"

    let writeStr ctx (str: string) =
        if str.Contains("\n") then
            write ctx "\""
            write ctx (str.Replace(@"\",@"\\").Replace("\"","\\\"").Replace("\n","\\n").Replace("\r","\\r"))
            write ctx "\""
        else
            write ctx "'"
            write ctx (str.Replace(@"\",@"\\").Replace("'",@"\'"))
            write ctx "'"

    let writeConst ctx cst =
        match cst with
        | PhpConstNumber n -> write ctx (string n)
        | PhpConstString s -> writeStr ctx s
        | PhpConstBool true -> write ctx "true"
        | PhpConstBool false -> write ctx "false"
        | PhpConstNull -> write ctx "NULL"



    let rec writeExpr ctx expr =
        match expr with
        | PhpBinaryOp(op, left, right) ->
            withPrecedence ctx (Precedence.binary op)
                (fun subCtx ->
                    writeExpr subCtx left
                    write subCtx " "
                    write subCtx op
                    write subCtx " "
                    writeExpr subCtx right)

        | PhpUnaryOp(op, expr) ->
            withPrecedence ctx (Precedence.unary op)
                (fun subCtx ->
                    write subCtx op
                    // extra space for ops ending with letter (like clone)
                    if Char.IsLetter op.[op.Length-1] then
                        write subCtx " "
                    writeExpr subCtx expr )
        | PhpConst cst ->
            writeConst ctx cst
        | PhpVar v ->
            write ctx "$"
            write ctx v
        | PhpMember(PhpIdentMember ident, m, _) ->
            writeIdent ctx ident
            write ctx "::"
            write ctx m
        | PhpMember(PhpParentMember, m, _) ->
            write ctx "parent::"
            write ctx m
        | PhpMember(PhpSelfMember, m, _) ->
            write ctx "self::"
            write ctx m

        | PhpMember(PhpExprMember l,m, _) ->
            withPrecedence ctx (-1)
                (fun subCtx ->
                    writeExpr subCtx l
                    write subCtx "->"
                    write subCtx m
             )
        | PhpIdent id ->
            writeIdent ctx id
        | PhpNew(t,args) ->
            withPrecedence ctx (Precedence._new)
                (fun subCtx ->
                    write subCtx "new "

                    writeIdent subCtx t
                    write subCtx "("
                    writeArgs subCtx args
                    write subCtx ")")
        | PhpNewArray(args) ->
            write ctx "[ "
            let mutable first = true
            for key,value in args do
                if first then
                    first <- false
                else
                    write ctx ", "
                writeArrayIndex ctx key
                writeExpr ctx value
            write ctx " ]"
        | PhpArrayAccess(array, index) ->
            writeExpr ctx array
            write ctx "["
            writeExpr ctx index
            write ctx "]"

        | PhpCall(f,args) ->
            let addParent =
                match f with
                | PhpAnonymousFunc _
                | PhpMember(_, _, PhpField) -> true
                | _ -> false
            if addParent then
                write ctx "("
            writeExpr ctx f
            if addParent then
                write ctx ")"
            write ctx "("
            writeArgs ctx args
            write ctx ")"
        | PhpTernary (guard, thenExpr, elseExpr) ->
            withPrecedence ctx (Precedence.ternary)
                (fun ctx ->
                    writeExpr ctx guard
                    write ctx " ? "
                    writeExpr ctx thenExpr
                    write ctx " : "
                    writeExpr ctx elseExpr)
        | PhpAnonymousFunc(args, uses, body) ->
            write ctx "function ("
            writeVarList ctx args
            write ctx ")"
            match uses with
            | [] -> ()
            | _ ->
                write ctx " use ("
                writeUseList ctx uses
                write ctx ")"

            write ctx " { "
            let multiline = body.Length > 1
            let bodyCtx =
                if multiline then
                    writeln ctx ""
                    indent ctx
                else
                    ctx
            for st in  body do
                writeStatement bodyCtx st
            if multiline then
                writei ctx "}"
            else
                write ctx " }"
        | PhpAnonymousClass cls ->
            write ctx "new class "
            writeClass ctx cls
        | PhpMacro(macro, args) ->
            let regex = System.Text.RegularExpressions.Regex("\$(?<n>\d)(?<s>\.\.\.)?")
            let matches = regex.Matches(macro)
            let mutable pos = 0
            for m in matches do
                let n = int m.Groups.["n"].Value
                write ctx (macro.Substring(pos,m.Index-pos))
                if m.Groups.["s"].Success then
                    if n < args.Length then
                        match args.[n] with
                        | PhpNewArray items ->
                           let mutable first = true
                           for _,value in items do
                               if first then
                                   first <- false
                               else
                                   write ctx ", "
                               writeExpr ctx value


                        | _ ->
                            writeExpr ctx args.[n]

                elif n < args.Length then
                    writeExpr ctx args.[n]

                pos <- m.Index + m.Length
            write ctx (macro.Substring(pos))


    and writeArgs ctx args =
        let mutable first = true
        for arg in args do
            if first then
                first <- false
            else
                write ctx ", "
            writeExpr ctx arg
    and writeArrayIndex ctx index =
        match index with
        | PhpArrayString s  ->
            write ctx "'"
            write ctx s
            write ctx "' => "
        | PhpArrayInt n  ->
            write ctx (string n)
            write ctx " => "
        | PhpArrayNoIndex ->
            ()

    and writeIf ctx guard thenCase elseCase isElseIf =
        if isElseIf then
            writei ctx "elseif ("
        else
            writei ctx "if ("

        writeExpr (Precedence.clear ctx) guard
        write ctx ")"
        let body = indent ctx
        match thenCase with
        | [ st ] ->
            writeln ctx ""
            writeStatement body st
            writei ctx ""
        | _ ->
            writeln ctx " {"
            for st in thenCase do
                writeStatement body st
            writei ctx "} "
        match elseCase with
        | [] ->
            writeiln ctx ""

        | [ PhpIf(guard,thenCase,elseCase) ] ->
            writeln ctx ""
            writeIf ctx guard thenCase elseCase true
        | [ st ] ->
            writeln ctx "else "
            writeStatement body st

        | _ ->
            writeln ctx "else {"
            for st in elseCase do
                writeStatement body st
            writeiln ctx "}"


    and writeStatement ctx st =
        match st with
        | PhpStatement.PhpReturn expr ->
            writei ctx "return "
            writeExpr (Precedence.clear ctx) expr
            writeln ctx ";"
        | PhpStatement.PhpDo (PhpConst PhpConstNull)-> ()
        | PhpStatement.PhpDo (expr) ->
            writei ctx ""
            writeExpr (Precedence.clear ctx) expr
            writeln ctx ";"
        | PhpStaticVar(name, value )->
            writei ctx "static $"
            write ctx name;
            match value with
            | Some v ->
                write ctx " = "
                writeConst ctx v
            | None -> ()
            writeln ctx ";"

        | PhpSwitch(expr, cases) ->
            writei ctx "switch ("
            writeExpr (Precedence.clear ctx)  expr
            writeln ctx ")"
            writeiln ctx "{"
            let casesCtx = indent ctx
            let caseCtx = indent casesCtx
            for case,sts in cases do
                match case with
                | Some case ->
                    writei casesCtx "case "
                    writeExpr casesCtx case
                | None ->
                    writei casesCtx "default"
                writeln casesCtx ":"
                for st in sts do
                    writeStatement caseCtx st

            writeiln ctx "}"
        | PhpBreak level ->
            writei ctx "break"
            match level with
            | Some l ->
                write ctx " "
                write ctx (string level)
            | None -> ()
            writeln ctx ";"

        | PhpIf(guard, thenCase, elseCase) ->
            writeIf ctx guard thenCase elseCase false
        | PhpThrow(expr) ->
            writei ctx "throw "
            writeExpr ctx expr
            writeln ctx ";"
        | PhpStatement.PhpTryCatch(body, catch, finallizer) ->
            writeiln ctx "try {"
            let bodyind = indent ctx
            for st in body do
                writeStatement bodyind st
            writeiln ctx "}"

            match catch with
            | Some(var, sts) ->
                writei ctx "catch (exception $"
                write ctx var
                writeln ctx ") {"
                for st in sts do
                    writeStatement bodyind st
                writeiln ctx "}"
            | None -> ()

            match finallizer with
            | [] -> ()
            | _ ->
                writeiln ctx "finally {"
                for st in finallizer do
                    writeStatement bodyind st
                writeiln ctx "}"
        | PhpStatement.PhpWhileLoop(guard, body) ->
            writei ctx "while ("
            writeExpr ctx guard
            writeln ctx ") {"
            let bodyctx = indent ctx
            for st in body do
                writeStatement bodyctx st
            writeiln ctx "}"
        | PhpStatement.PhpFor(ident, start, limit, isUp, body) ->
            writei ctx "for ($"
            write ctx ident
            write ctx " = "
            writeExpr ctx start
            write ctx "; $"
            write ctx ident
            write ctx " <= "
            writeExpr ctx limit
            write ctx "; $"
            write ctx ident
            if isUp then
                write ctx "++"
            else
                write ctx "--"
            write ctx ")"

            let bodyctx = indent ctx
            match body with
            | [ st ] ->
                writeln ctx "";
                writeStatement bodyctx st
                writeln ctx ""
            | _ ->
                writeln ctx " {"
                for st in body do
                    writeStatement bodyctx st
                writeiln ctx "}"



    and writeFunc ctx (f: PhpFun) =
        write ctx "function "
        write ctx f.Name
        write ctx "("
        let mutable first = true
        for arg in f.Args do
            if first then
                first <- false
            else
                write ctx ", "
            write ctx "$"
            write ctx arg.Name
            match arg.Kind with
            | PhpOptionalArg ->
                write ctx " = NULL"
            | PhpMandatoryArg -> ()
        writeln ctx ") {"
        let bodyCtx = indent ctx

        for s in f.Body do
            writeStatement bodyCtx s
        writeiln ctx "}"

    and writeField ctx (m: string) =
        writei ctx "public $"
        write ctx m
        writeln ctx ";"


    and writeClass ctx (t: PhpClass) =
        match t.BaseType with
        | Some t ->
            write ctx " extends "
            writeIdent ctx t
        | None -> ()

        if t.Interfaces <> [] then
            write ctx " implements "
            let mutable first = true
            for itf in t.Interfaces do
                if first then
                    first <- false
                else
                    write ctx ", "
                writeIdent ctx itf.Identity

        writeln ctx " {"
        let mbctx = indent ctx
        for m in t.Fields do
            writeField mbctx m

        for m in t.Methods do
            writei mbctx ""
            if m.Static then
                write mbctx "static ";
            writeFunc mbctx m.Fun

        writeiln ctx "}"

    let writeType ctx (t: PhpType) =
        writei ctx ""
        if t.Abstract then
            write ctx "abstract "
        write ctx "class "
        write ctx t.Identity.Name
        writeClass ctx t.Class



    let writeAssign ctx n expr =
        writei ctx "$GLOBALS['"
        write ctx n
        write ctx "'] = "
        writeExpr ctx expr
        writeln ctx ";"


    let writeDecl ctx d =
        match d with
        | PhpType t -> writeType ctx t
        | PhpFun t ->
            writei ctx ""
            writeFunc ctx t
        | PhpDeclValue(n,expr) -> writeAssign ctx n expr
        | PhpAction statements ->
            for s in statements do
                writeStatement ctx s

    let writeNamespace ctx (ns: PhpNamespace) =
        match ns.Namespace with
        | Some name ->
            write ctx "namespace "
            write ctx name
            writeln ctx " {"
            let bodyCtx = indent ctx
            for d in ns.Decls do
                writeDecl bodyCtx d
                writeln bodyCtx ""
            writeln ctx "}"
        | None ->
            for d in ns.Decls do
                writeDecl ctx d
                writeln ctx ""



    let writeFile ctx (file: PhpFile) =
        writeln ctx "<?php"

        match file.Namespaces with
        | [ ns ] ->
            match ns.Namespace with
            | Some name ->
                write ctx "namespace "
                write ctx name
                writeln ctx ";"
                writeln ctx ""
            | _ -> ()
        | _ -> ()




        if not (List.isEmpty file.Uses) then
            for u in file.Uses do
                write ctx "use "
                write ctx @"\"
                match u.Identity.Namespace with
                | Some ns ->
                    write ctx ns
                    write ctx @"\"
                | None -> ()
                write ctx u.Identity.Name
                writeln ctx ";"
            writeln ctx ""

        let ctx =
            { ctx with
                UsedTypes = set file.Uses }


        match file.Namespaces with
        | [ ns ] ->
           for d in ns.Decls do
               writeDecl ctx d
               writeln ctx ""

        | _ ->
            for ns in file.Namespaces do
                writeNamespace ctx ns
