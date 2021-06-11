module Option

open System

type Some =
    { value: obj }
    

let some (x: obj) =
    if isNull x || x :? Some then
        { value = x} |> box
    else
        x


    

let value (opt: obj) =
    if isNull opt then
        raise (Exception "Option has no value")

    match opt with
    | :? Some as s -> s.value
    | _ -> opt
        
