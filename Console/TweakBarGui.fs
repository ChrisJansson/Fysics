module TweakBarGui
open AntTweakBar
open System.Reflection

type ViewModel() =
    member val IntegrationSpeed : float = 1.0 with get, set

let mapFloat (owner:obj) (p:PropertyInfo) (b:Bar) =   
    let propertyValue = unbox<float>(p.GetValue(owner))
    let variable = new DoubleVariable(b, propertyValue)
    variable.Label <- p.Name
    variable.Step <- 0.01
    variable.Min <- 0.0
    variable.Max <- 2.0
    variable.Changed.Add(fun _ -> p.SetValue(owner, variable.Value))

let mapProperty (owner:obj) (p:PropertyInfo) (b:Bar) =
    match p.PropertyType with
    | _ when p.PropertyType = typeof<float> -> mapFloat owner p b
    | _ -> failwith "Unsupported TweakBar type"

let mapViewModel (viewModel:obj) (b:Bar) =
    let properties = viewModel
                        .GetType()
                        .GetProperties()
    for p in properties do
        mapProperty viewModel p b
        