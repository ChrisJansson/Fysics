module shader

open OpenTK
open OpenTK.Graphics.OpenGL

type shaderCompilationStatus = 
    | Success of shaderId : int
    | Error of message : string

let getShaderCompilationError (shaderId:int) =
    GL.GetShaderInfoLog(shaderId)

let getShaderCompilationStatus (shaderId:int) =
    let mutable status = -1
    GL.GetShader(shaderId, ShaderParameter.CompileStatus, &status)
    let convertedStatus = enum status
    match convertedStatus with
        | Boolean.True -> Success shaderId
        | _ -> Error (getShaderCompilationError shaderId)

let compileShader shaderType source =
    let shaderId = GL.CreateShader shaderType
    GL.ShaderSource(shaderId, source)
    GL.CompileShader(shaderId)
    getShaderCompilationStatus shaderId

type shadersCompilationResult =
    | Success of shaderIds : list<int>
    | Error of errors : list<string>

let compileShaders shaders =
    let seed = Success(List.empty)
    shaders 
        |> List.map (fun (t, s) -> compileShader t s)
        |> List.fold (fun state compiledShader ->  
            match (state, compiledShader) with
            | (Success shaderIds, shaderCompilationStatus.Success shaderId) -> Success(shaderId::shaderIds)
            | (Success _, shaderCompilationStatus.Error message) -> Error([message])
            | (Error messages, shaderCompilationStatus.Success _) -> Error(messages)
            | (Error messages, shaderCompilationStatus.Error message) -> Error(message::messages))
            seed

let linkProgram shaders =
    let programId = GL.CreateProgram()
    for shaderId in shaders do
        GL.AttachShader(programId, shaderId)
    GL.LinkProgram programId
    programId

let makeProgram shaders =
    match compileShaders shaders with
    | Success shaderIds -> Some (linkProgram shaderIds)
    | Error messages -> None

let glSetUniform uniformId (matrix:Matrix4d byref) =
    GL.UniformMatrix4(uniformId, false, &matrix)

let uniformSetterForMatrix4d uniformId =
    fun (matrix:Matrix4d) -> 
        let mutable m = matrix
        GL.UniformMatrix4(uniformId, false, &m)
    
type MatrixUniform  = {
        set : OpenTK.Matrix4d -> unit
    }