module Fable.Tests.String

open System
open PHPUnit.Framework
open Fable.Core

let containsInOrder (substrings: string list) (str: string) =
    let mutable lastIndex = -1
    substrings |> List.forall (fun s ->
      let i = str.IndexOf(s)
      let success = i >= 0 && i > lastIndex
      lastIndex <- i
      success)

[<AttachMembers>]
type StringTest =
    inherit TestCase

    [<Fact>]
    member this.``test sprintf works`` () =
        // Immediately applied
        sprintf "%.2f %g" 0.5468989 5.
        |> this.equal "0.55 5"
        // Curried
        let printer = sprintf "Hi %s, good %s!"
        let printer = printer "Alfonso"
        printer "morning" |> this.equal "Hi Alfonso, good morning!"
        printer "evening" |> this.equal "Hi Alfonso, good evening!"

    [<Fact>]
    member this.``test sprintf works II`` () =
          let printer2 = sprintf "Hi %s, good %s%s" "Maxime"
          let printer2 = printer2 "afternoon"
          printer2 "?" |> this.equal "Hi Maxime, good afternoon?"

    [<Fact>]
    member this.``test sprintf with different decimal digits works`` () =
          sprintf "Percent: %.0f%%" 5.0 |> this.equal "Percent: 5%"
          sprintf "Percent: %.2f%%" 5. |> this.equal "Percent: 5.00%"
          sprintf "Percent: %.1f%%" 5.24 |> this.equal "Percent: 5.2%"
          sprintf "Percent: %.2f%%" 5.268 |> this.equal "Percent: 5.27%"
          sprintf "Percent: %f%%" 5.67 |> this.equal "Percent: 5.670000%"

    [<Fact>]
    member this.``sprintf displays sign correctly`` () =
          sprintf "%i" 1 |> this.equal "1"
          sprintf "%d" 1 |> this.equal "1"
          sprintf "%d" 1L |> this.equal "1"
          sprintf "%.2f" 1. |> this.equal "1.00"
          sprintf "%i" -1 |> this.equal "-1"
          sprintf "%d" -1 |> this.equal "-1"
          sprintf "%d" -1L |> this.equal "-1"
          sprintf "%.2f" -1. |> this.equal "-1.00"

    [<Fact>]
    member this.``test Print.sprintf works`` () =
        let res = Printf.sprintf "%s" "abc"
        this.equal "res: abc" ("res: " + res)

    [<Fact>]
    member this.``test sprintf without arguments works`` () =
        sprintf "hello" |> this.equal "hello"

    [<Fact>]
    member this.``test input of print format can be retrieved`` () =
        let pathScan (pf:PrintfFormat<_,_,_,_,'t>) =
            let formatStr = pf.Value
            formatStr

        this.equal "/hello/%s" (pathScan "/hello/%s")

    [<Fact>]
    member this.``test interpolate works`` () =
        let name = "Phillip"
        let age = 29
        $"Name: {name}, Age: %i{age}"
        |> this.equal "Name: Phillip, Age: 29"

    #if FABLE_COMPILER
    [<Fact>]
    member this.``test string interpolation works with inline expressions`` () =
        $"I think {3.0 + 0.14} is close to %.8f{3.14159265}!"
        |> this.equal "I think 3.14 is close to 3.14159265!"
    #endif

    [<Fact>]
    member this.``test string interpolation works with anonymous records`` () =
        let person =
            {| Name = "John"
               Surname = "Doe"
               Age = 32
               Country = "The United Kingdom" |}
        $"Hi! My name is %s{person.Name} %s{person.Surname.ToUpper()}. I'm %i{person.Age} years old and I'm from %s{person.Country}!"
        |> this.equal "Hi! My name is John DOE. I'm 32 years old and I'm from The United Kingdom!"

    [<Fact>]
    member this.``test interpolated string with double % should be unescaped`` () =
        $"{100}%%" |> this.equal "100%"

    [<Fact>]
    member this.``test sprintf \"%A\" with lists works`` () =
        let xs = ["Hi"; "Hello"; "Hola"]
        (sprintf "%A" xs).Replace("\"", "") |> this.equal "[Hi; Hello; Hola]"

    [<Fact>]
    member this.``test sprintf \"%A\" with nested lists works`` () =
        let xs = [["Hi"]; ["Hello"]; ["Hola"]]
        (sprintf "%A" xs).Replace("\"", "") |> this.equal "[[Hi]; [Hello]; [Hola]]"

    [<Fact>]
    member this.``test sprintf \"%A\" with sequences works`` () =
        let xs = seq { "Hi"; "Hello"; "Hola" }
        sprintf "%A" xs |> containsInOrder ["Hi"; "Hello"; "Hola"] |> this.equal true

    [<Fact>]
    member this.``test Storing result of Seq.tail and printing the result several times works. Related to #1996`` () =
        let tweets = seq { "Hi"; "Hello"; "Hola" }
        let tweetsTailR: seq<string> = tweets |> Seq.tail

        let a = sprintf "%A" (tweetsTailR)
        let b = sprintf "%A" (tweetsTailR)

        containsInOrder ["Hello"; "Hola"] a |> this.equal true
        containsInOrder ["Hello"; "Hola"] b |> this.equal true

    // [<Fact>] FIXME: we should get this working as well.
    // member this.``test sprintf \"%X\" works`` () =
    //     //These should all be the Native JS Versions (except int64 / uint64)
    //     //See #1530 for more information.

    //     sprintf "255: %X" 255 |> this.equal "255: FF"
    //     sprintf "255: %x" 255 |> this.equal "255: ff"
    //     sprintf "-255: %X" -255 |> this.equal "-255: FFFFFF01"
    //     sprintf "4095L: %X" 4095L |> this.equal "4095L: FFF"
    //     sprintf "-4095L: %X" -4095L |> this.equal "-4095L: FFFFFFFFFFFFF001"
    //     sprintf "1 <<< 31: %x" (1 <<< 31) |> this.equal "1 <<< 31: 80000000"
    //     sprintf "1u <<< 31: %x" (1u <<< 31) |> this.equal "1u <<< 31: 80000000"
    //     sprintf "2147483649L: %x" 2147483649L |> this.equal "2147483649L: 80000001"
    //     sprintf "2147483650uL: %x" 2147483650uL |> this.equal "2147483650uL: 80000002"
    //     sprintf "1L <<< 63: %x" (1L <<< 63) |> this.equal "1L <<< 63: 8000000000000000"
    //     sprintf "1uL <<< 63: %x" (1uL <<< 63) |> this.equal "1uL <<< 63: 8000000000000000"

    [<Fact>]
    member this.``test sprintf integers with sign and padding works`` () =
        sprintf "%+04i" 1 |> this.equal "+001"
        sprintf "%+04i" -1 |> this.equal "-001"
        sprintf "%5d" -5 |> this.equal "   -5"
        sprintf "%5d" -5L |> this.equal "   -5"
        sprintf "%- 4i" 5 |> this.equal " 5  "

    // [<Fact>]
    // member this.``test parameterized padding works`` () =
    //     sprintf "[%*s][%*s]" 6 "Hello" 5 "Foo"
    //     |> this.equal "[ Hello][  Foo]"

    [<Fact>]
    member this.``test String.Format combining padding and zeroes pattern works`` () =
        String.Format("{0:++0.00++}", -5000.5657) |> this.equal "-++5000.57++"
        String.Format("{0:000.00}foo", 5) |> this.equal "005.00foo"
        String.Format("{0,-8:000.00}foo", 12.456) |> this.equal "012.46  foo"

    [<Fact>]
    member this.``test StringBuilder works`` () =
        let sb = System.Text.StringBuilder()
        sb.Append "Hello" |> ignore
        sb.AppendLine () |> ignore
        sb.AppendLine "World!" |> ignore
        let expected = System.Text.StringBuilder()
                          .AppendFormat("Hello{0}World!{0}", Environment.NewLine)
                          .ToString()
        sb.ToString() |> this.equal expected

    [<Fact>]
    member this.``test StringBuilder.Lengh works`` () =
        let sb = System.Text.StringBuilder()
        sb.Append("Hello") |> ignore
        // We don't test the AppendLine for Length because depending on the OS
        // the result is different. Unix \n VS Windows \r\n
        // sb.AppendLine() |> ignore
        this.equal 5 sb.Length

    [<Fact>]
    member this.``test StringBuilder.ToString works with index and length`` () =
        let sb = System.Text.StringBuilder()
        sb.Append("Hello") |> ignore
        sb.AppendLine() |> ignore
        this.equal "ll" (sb.ToString(2, 2))

    [<Fact>]
    member this.``test StringBuilder.Clear works`` () =
        let builder = new System.Text.StringBuilder()
        builder.Append("1111") |> ignore
        builder.Clear() |> ignore
        this.equal "" (builder.ToString())

    [<Fact>]
    member this.``test StringBuilder.Append works with various overloads`` () =
        let builder = Text.StringBuilder()
                          .Append(Text.StringBuilder "aaa")
                          .Append("bcd".ToCharArray())
                          .Append('/')
                          .Append(true)
                          .Append(5.2)
                          .Append(34)
        this.equal "aaabcd/true5.234" (builder.ToString().ToLower())

    [<Fact>]
    member this.``test Conversion char to int works`` () =
        this.equal 97 (int 'a')
        this.equal 'a' (char 97)

    [<Fact>]
    member this.``test Conversion string to char works`` () =
        this.equal 'a' (char "a")
        this.equal "a" (string 'a')

    [<Fact>]
    member this.``test Conversion string to negative int8 works`` () =
        this.equal -5y (int8 "-5")
        this.equal "-5" (string -5y)

    [<Fact>]
    member this.``test Conversion string to negative int16 works`` () =
        this.equal -5s (int16 "-5")
        this.equal "-5" (string -5s)

    [<Fact>]
    member this.``test Conversion string to negative int32 works`` () =
        this.equal -5 (int32 "-5")
        this.equal "-5" (string -5)

    [<Fact>]
    member this.``test Conversion string to negative int64 works`` () =
        this.equal -5L (int64 "-5")
        this.equal "-5" (string -5L)

    [<Fact>]
    member this.``test Conversion string to int8 works`` () =
        this.equal 5y (int8 "5")
        this.equal "5" (string 5y)

    [<Fact>]
    member this.``test Conversion string to int16 works`` () =
        this.equal 5s (int16 "5")
        this.equal "5" (string 5s)

    [<Fact>]
    member this.``test Conversion string to int32 works`` () =
        this.equal 5 (int32 "5")
        this.equal "5" (string 5)

    [<Fact>]
    member this.``test Conversion string to int64 works`` () =
        this.equal 5L (int64 "5")
        this.equal "5" (string 5L)

    [<Fact>]
    member this.``test Conversion string to uint8 works`` () =
        this.equal 5uy (uint8 "5")
        this.equal "5" (string 5uy)

    [<Fact>]
    member this.``test Conversion string to uint16 works`` () =
        this.equal 5us (uint16 "5")
        this.equal "5" (string 5us)

    [<Fact>]
    member this.``test Conversion string to uint32 works`` () =
        this.equal 5u (uint32 "5")
        this.equal "5" (string 5u)

    [<Fact>]
    member this.``test Conversion string to uint64 works`` () =
        this.equal 5uL (uint64 "5")
        this.equal "5" (string 5uL)
