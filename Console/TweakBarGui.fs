module TweakBarGui
open AntTweakBar
open System.Reflection
open System.Reactive
open System.Reactive.Linq

type Color = OpenTK.Vector3

type BlinnMaterial = { 
    AmbientColor : Color
    DiffuseColor : Color 
    SpecularColor : Color
    SpecularExp : float }

let readColor (c:ColorVariable) =
    new Color(c.R, c.G, c.B)

let makeColorVariable b (c:Color) l =
    let colorVariable = new ColorVariable(b)
    colorVariable.R <- c.X
    colorVariable.G <- c.Y
    colorVariable.B <- c.Z
    colorVariable.Label <- l
    let obs = colorVariable.Changed |> Observable.map (fun _ -> readColor colorVariable)
    obs.StartWith(readColor colorVariable)

let makeFloatVariable b l =
    let doubleVariable = new DoubleVariable(b)
    doubleVariable.Label <- l
    let obs = doubleVariable.Changed |> Observable.map (fun _ -> doubleVariable.Value)
    obs.StartWith(doubleVariable.Value)

let makeBlinnMaterialView b d =
    let ambientObservable = makeColorVariable b d.AmbientColor "Ambient"
    let diffuseObservable = makeColorVariable b d.DiffuseColor "Diffuse"
    let specularObservable = makeColorVariable b d.SpecularColor "Specular"
    let specularExpObservable = makeFloatVariable b "Specular Exponent"
    Observable.CombineLatest(
        ambientObservable, 
        diffuseObservable, 
        specularObservable,
        specularExpObservable,
        (fun a d s se-> { AmbientColor = a; DiffuseColor = d; SpecularColor = s; SpecularExp = se}))

type BlinnMaterialViewModel() =
    member val AmbientColor : Color = new Color() with get, set

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
        