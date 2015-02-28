module SimpleShaderProgram
open OpenTK.Graphics.OpenGL
open shader

let vertexShaderSource =
    "#version 400 

uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;
uniform mat4 modelMatrix;

layout(location = 0) in vec3 position;

void main()
{
    gl_Position = projectionMatrix * viewMatrix * modelMatrix * vec4(position, 1.0);
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

type SimpleProgram = {
        ProgramId : int
        ProjectionMatrixUniform : MatrixUniform
        ViewMatrix : MatrixUniform
        ModelMatrix : MatrixUniform
    }

let makeSimpleShaderProgram =
    match makeProgram rawShaders with
    | Some programId -> 
        { 
            ProgramId = programId; 
            ProjectionMatrixUniform = makeMatrixUniform programId "projectionMatrix"
            ViewMatrix = makeMatrixUniform programId "viewMatrix"
            ModelMatrix = makeMatrixUniform programId "modelMatrix"
        }
    | _ -> failwith "Program compilation failed"

