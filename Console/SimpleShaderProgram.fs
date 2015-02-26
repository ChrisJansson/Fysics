module SimpleShaderProgram
open OpenTK.Graphics.OpenGL
open shader

let vertexShaderSource =
    "#version 400 

layout(location = 0) in vec3 position;

void main()
{
    gl_Position = vec4(position, 1.0);
}"

let fragmentShaderSource = 
    "#version 400 

out vec4 outColor;

void main()
{
    outColor = vec4(1.0, 1.0, 1.0, 1.0);
}"

let rawShaders = [ 
    (ShaderType.VertexShader, vertexShaderSource); 
    (ShaderType.FragmentShader, fragmentShaderSource) ]

let makeSimpleShaderProgram =
    match makeProgram rawShaders with
    | Some programId -> programId
    | _ -> failwith "Program compilation failed"

