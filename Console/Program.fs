open vector

[<EntryPoint>]
let main argv =
    let a = { x = 1.0; y = 2.0; z = 3.0 } 
    let b = { x = 2.0; y = 3.0; z = 4.0 }
    crossProduct a b
    |> printfn "%A"
    0 // return an integer exit code
